import contextlib
import importlib.util
import io
import json
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch


spec = importlib.util.spec_from_file_location("jev_audit", Path(__file__).parents[1] / "jev_audit.py")
audit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit)


class JevAuditTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.request = self.root / "request.json"
        self.script = self.root / "fake_jev.py"
        self.request.write_text(json.dumps({
            "state": "PRIVATE-CONTEXT-SENTINEL",
            "questions": {"FC-SEC-001": {
                "type": "choice", "instructions": "Check the SQL boundary.",
                "criteria": {category: category for category in audit.RELEVANCE},
            }},
        }), encoding="utf-8")
        self.script.write_text(
            "import json, sys\n"
            "from pathlib import Path\n"
            "assert Path(sys.argv[sys.argv.index('-q') - 1][1:]).read_text() == 'PRIVATE-CONTEXT-SENTINEL'\n"
            "assert '-j' in sys.argv and '-e' in sys.argv\n"
            "assert int(sys.argv[sys.argv.index('-t') + 1]) > 0\n"
            "assert '--key' not in sys.argv\n"
            "print(json.dumps({'model': 'typesafe/jev-1.13', 'answers': {"
            "'FC-SEC-001': {'type': 'choice', 'choice': 'RELEVANT'}}, "
            "'secret': 'RESPONSE-SECRET-SENTINEL'}))\n"
            "print('STDERR-SECRET-SENTINEL', file=sys.stderr)\n",
            encoding="utf-8")
        self.common = ["--audit-id", "11111111-1111-4111-8111-111111111111",
                       "--run-id", "22222222-2222-4222-8222-222222222222",
                       "--axis", "security-privacy", "--profile", "STANDARD_TARGETED",
                       "--source-item-id", "SEC-SURFACE-001", "--extract-entry-id", "SEC-SURFACE-001@1",
                       "--source-revision", "1234567", "--log-root", str(self.root / "logs")]
        self.invoke = ["invoke", *self.common, "--request", str(self.request),
                       "--jev-script", str(self.script), "--gateway", "openrouter",
                       "--model", "typesafe/jev-1.13", "--threshold", "0.5"]

    def run_tool(self, arguments=None):
        stdout, stderr = io.StringIO(), io.StringIO()
        with contextlib.redirect_stdout(stdout), contextlib.redirect_stderr(stderr):
            result = audit.main(arguments or self.invoke)
        return result, stdout.getvalue(), stderr.getvalue()

    def records(self):
        return sorted((json.loads(line) for path in (self.root / "logs").rglob("*.jsonl")
                       for line in path.read_text(encoding="utf-8").splitlines()),
                      key=lambda record: record["timestamp"])

    def test_real_child_process_records_execution_and_retains_only_safe_evidence(self):
        result, stdout, stderr = self.run_tool()
        self.assertEqual(0, result, stderr)
        records = self.records()
        self.assertEqual(["STARTED", "SUCCEEDED"], [record["event"] for record in records])
        succeeded = records[-1]
        self.assertEqual({"FC-SEC-001": "RELEVANT"}, succeeded["decisions"])
        self.assertEqual(0, succeeded["exit_code"])
        self.assertEqual(audit.digest(self.script.read_bytes()), succeeded["cli_sha256"])
        self.assertEqual(audit.digest(self.request.read_bytes()), succeeded["request_sha256"])
        self.assertEqual("SEC-SURFACE-001@1", succeeded["extract_entry_id"])
        self.assertEqual("typesafe/jev-1.13", succeeded["resolved_model"])
        self.assertEqual(records[0]["invocation_id"], succeeded["invocation_id"])
        exposed = json.dumps(records) + stdout + stderr
        for secret in ("PRIVATE-CONTEXT-SENTINEL", "RESPONSE-SECRET-SENTINEL", "STDERR-SECRET-SENTINEL"):
            self.assertNotIn(secret, exposed)

    def test_nonzero_exit_is_failed_and_does_not_expose_provider_error(self):
        self.script.write_text("import sys\nprint('sk-or-v1-SECRET', file=sys.stderr)\nsys.exit(7)\n")
        result, stdout, stderr = self.run_tool()
        self.assertEqual(1, result)
        self.assertEqual("FAILED", self.records()[-1]["event"])
        self.assertEqual("CLI_ERROR", self.records()[-1]["failure_code"])
        self.assertEqual("UNKNOWN", self.records()[-1]["routing_state"])
        self.assertNotIn("sk-or-v1-SECRET", stdout + stderr + json.dumps(self.records()))

    def test_missing_cli_logs_an_attempt_rather_than_success(self):
        self.script.unlink()
        result, _, _ = self.run_tool()
        self.assertEqual(1, result)
        self.assertEqual("CLI_UNAVAILABLE", self.records()[-1]["failure_code"])

    def test_missing_request_is_logged_and_never_calls_cli(self):
        self.request.unlink()
        with patch.object(audit.subprocess, "run") as process:
            self.assertEqual(1, self.run_tool()[0])
            process.assert_not_called()
        self.assertEqual("INVALID_REQUEST", self.records()[-1]["failure_code"])

    def test_success_exit_with_malformed_or_partial_answers_is_failed(self):
        for response in ("not JSON", "{}", '{"model":"typesafe/jev-1.13","answers":{}}',
                         '{"model":"typesafe/jev-1.13","answers":{"FC-SEC-001":{"type":"choice","choice":"PASS"}}}'):
            with self.subTest(response=response):
                self.script.write_text(f"print({response!r})\n")
                self.assertEqual(1, self.run_tool()[0])
        self.assertTrue(all(record["event"] != "SUCCEEDED" for record in self.records()))

    def test_timeout_or_interrupt_records_unknown(self):
        for exception, expected in ((subprocess.TimeoutExpired("jev", 1), "TIMEOUT"),
                                    (KeyboardInterrupt(), "INTERRUPTED")):
            with self.subTest(expected=expected), patch.object(audit.subprocess, "run", side_effect=exception):
                self.assertEqual(1, self.run_tool()[0])
                self.assertEqual(expected, self.records()[-1]["failure_code"])

    def test_bypass_is_an_operator_declaration_not_execution(self):
        result, stdout, stderr = self.run_tool(["bypass", *self.common,
                                              "--reason-code", "NOT_APPLICABLE",
                                              "--reason", "PRIVATE-BYPASS-SENTINEL"])
        self.assertEqual(0, result, stderr)
        self.assertEqual(["BYPASSED"], [record["event"] for record in self.records()])
        self.assertEqual("OPERATOR_DECLARATION", self.records()[0]["evidence_kind"])
        self.assertNotIn("PRIVATE-BYPASS-SENTINEL", json.dumps(self.records()) + stdout)

    def test_multiple_calls_keep_independent_append_only_logs(self):
        self.assertEqual(0, self.run_tool()[0])
        first = next((self.root / "logs").rglob("*.jsonl"))
        initial_bytes = first.read_bytes()
        self.assertEqual(0, self.run_tool()[0])
        self.assertEqual(initial_bytes, first.read_bytes())
        self.assertEqual(2, len(list((self.root / "logs").rglob("*.jsonl"))))

    def test_unwritable_log_prevents_invocation(self):
        log_root = self.root / "logs"
        log_root.write_text("A file cannot be used as a log directory.")
        with patch.object(audit.subprocess, "run") as process:
            self.assertEqual(1, self.run_tool()[0])
            process.assert_not_called()

    def test_invalid_metadata_rejected_before_creating_logs(self):
        for option, value in (("--run-id", "../../escape"), ("--threshold", "nan"), ("--timeout", "0.5"),
                              ("--source-revision", "private/path"), ("--model", "sk-or-v1-secret")):
            with self.subTest(option=option):
                arguments = self.invoke.copy()
                if option in arguments:
                    arguments[arguments.index(option) + 1] = value
                else:
                    arguments.extend([option, value])
                with self.assertRaises(SystemExit), contextlib.redirect_stderr(io.StringIO()):
                    audit.main(arguments)
        self.assertFalse((self.root / "logs").exists())

    def test_crash_leaves_started_as_incomplete_not_successful(self):
        arguments = audit.parser().parse_args(self.invoke)
        log = audit.EventLog(arguments)
        log.write("STARTED")
        log.close()
        self.assertEqual(["STARTED"], [record["event"] for record in self.records()])


if __name__ == "__main__":
    unittest.main()
