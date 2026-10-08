"""Run Jev with durable, content-free execution evidence for a factory audit."""

import argparse
from datetime import datetime, timezone
import hashlib
import json
import math
import os
from pathlib import Path
import re
import subprocess
import sys
import tempfile
import time
import uuid


AXES = ("architecture", "functionality", "ux", "ui", "security-privacy", "values")
PROFILES = ("LIGHT_BASELINE", "STANDARD_TARGETED", "DEEP_CHANGE")
RELEVANCE = {"RELEVANT", "POSSIBLE", "IRRELEVANT", "UNKNOWN", "MANDATORY"}
ENDPOINTS = {
    "openrouter": "https://openrouter.ai/api/alpha/decisions",
    "official": "https://api.typesafe.ai/v1/systemone",
}


def digest(value):
    return hashlib.sha256(value).hexdigest()


def identifier(value):
    if not re.fullmatch(r"[A-Za-z0-9_.:@-]{1,128}", value):
        raise argparse.ArgumentTypeError("Use a stable identifier of at most 128 characters.")
    return value


def uuid_argument(value):
    try:
        return str(uuid.UUID(value))
    except ValueError as error:
        raise argparse.ArgumentTypeError("Use a UUID for the audit and run identities.") from error


def positive_seconds(value):
    try:
        result = int(value)
    except ValueError as error:
        raise argparse.ArgumentTypeError("Timeout must be a positive integer.") from error
    if result <= 0:
        raise argparse.ArgumentTypeError("Timeout must be a positive integer.")
    return result


def threshold_argument(value):
    result = float(value)
    if not math.isfinite(result) or not 0 <= result <= 1:
        raise argparse.ArgumentTypeError("Threshold must be between zero and one.")
    return result


def model_argument(value):
    if not re.fullmatch(r"(?:typesafe/)?jev-[A-Za-z0-9.-]{1,64}", value):
        raise argparse.ArgumentTypeError("Use a Jev model identifier.")
    return value


def revision_argument(value):
    if not re.fullmatch(r"[0-9a-fA-F]{7,64}", value):
        raise argparse.ArgumentTypeError("Source revision must be a Git commit hash.")
    return value


class EventLog:
    """Each call owns a new file; concurrent callers never share an append target."""

    def __init__(self, arguments):
        self.identity = str(uuid.uuid4())
        directory = (arguments.log_root / arguments.audit_id / arguments.run_id / "jev").resolve()
        if not directory.is_relative_to(arguments.log_root.resolve()):
            raise ValueError("Log directory escapes the configured root.")
        directory.mkdir(parents=True, exist_ok=True)
        self.path = directory / f"{self.identity}.jsonl"
        self.stream = self.path.open("x", encoding="utf-8", newline="\n")
        self.common = {
            "schema_version": 1,
            "invocation_id": self.identity,
            "audit_id": arguments.audit_id,
            "audit_run_id": arguments.run_id,
            "axis": arguments.axis,
            "profile": arguments.profile,
            "source_item_id": arguments.source_item_id,
            "extract_entry_id": arguments.extract_entry_id,
            "source_revision": arguments.source_revision,
            "logger_sha256": digest(Path(__file__).read_bytes()),
        }

    def write(self, event, **properties):
        record = {**self.common, "timestamp": datetime.now(timezone.utc).isoformat(),
                  "event": event, **properties}
        self.stream.write(json.dumps(record, allow_nan=False, sort_keys=True) + "\n")
        self.stream.flush()
        # Persist STARTED before invoking the provider, including across interruption.
        os.fsync(self.stream.fileno())

    def close(self):
        self.stream.close()


def validate_request(request):
    if not isinstance(request, dict) or not isinstance(request.get("state"), str) or not request["state"].strip():
        raise ValueError("A nonempty state is required.")
    questions = request.get("questions")
    if not isinstance(questions, dict) or not questions:
        raise ValueError("At least one relevance question is required.")
    for check_id, question in questions.items():
        identifier(check_id)
        if not isinstance(question, dict) or question.get("type") != "choice":
            raise ValueError("Relevance questions must be choice questions.")
        if not isinstance(question.get("instructions"), str) or not question["instructions"].strip():
            raise ValueError("Question instructions are required.")
        criteria = question.get("criteria")
        if not isinstance(criteria, dict) or set(criteria) != RELEVANCE:
            raise ValueError("Questions must include all five relevance categories.")
        if any(not isinstance(text, str) or not text.strip() for text in criteria.values()):
            raise ValueError("Each relevance category needs a description.")
    return questions


def validated_decisions(response, questions):
    if not isinstance(response, dict) or not isinstance(response.get("answers"), dict):
        raise ValueError("Response has no answers.")
    answers = response["answers"]
    if set(answers) != set(questions):
        raise ValueError("Response does not cover exactly the requested checks.")
    decisions = {}
    for check_id, answer in answers.items():
        if not isinstance(answer, dict) or answer.get("type") != "choice":
            raise ValueError("Response contains an unexpected answer type.")
        choice = answer.get("choice")
        if not isinstance(choice, str) or choice not in RELEVANCE:
            raise ValueError("Response contains an invalid relevance category.")
        decisions[check_id] = choice
    return decisions


def invoke(arguments, log):
    started = time.monotonic()
    configuration = {
        "gateway": arguments.gateway, "endpoint": ENDPOINTS[arguments.gateway],
        "requested_model": arguments.model, "threshold": arguments.threshold,
        "timeout_seconds": arguments.timeout, "failure_behavior": "UNKNOWN_RETAIN_MANDATORY",
    }
    log.write("STARTED", **configuration)
    failure = "INVALID_REQUEST"
    evidence = {}
    try:
        request_bytes = arguments.request.read_bytes()
        evidence["request_sha256"] = digest(request_bytes)
        request = json.loads(request_bytes)
        questions = validate_request(request)
        evidence["batch_size"] = len(questions)
        failure = "CLI_UNAVAILABLE"
        evidence["cli_sha256"] = digest(arguments.jev_script.read_bytes())
        failure = "PROCESS_START_FAILED"
        with tempfile.TemporaryDirectory(prefix="fc-jev-") as temporary:
            state_path = Path(temporary) / "state.txt"
            state_path.write_text(request["state"], encoding="utf-8")
            command = [sys.executable, "-X", "utf8", str(arguments.jev_script.resolve()),
                       f"@{state_path}", "-q", json.dumps(questions), "-j",
                       "-g", arguments.gateway, "-e", ENDPOINTS[arguments.gateway],
                       "-m", arguments.model, "-t", str(arguments.timeout)]
            # No shell and no credential arguments. Let Jev resolve credentials locally.
            process = subprocess.run(command, capture_output=True, timeout=arguments.timeout + 5)
        evidence.update(exit_code=process.returncode, response_sha256=digest(process.stdout),
                        stderr_sha256=digest(process.stderr))
        if process.returncode != 0:
            failure = "CLI_ERROR"
            raise ValueError("Jev returned a nonzero exit code.")
        failure = "INVALID_RESPONSE"
        response = json.loads(process.stdout)
        decisions = validated_decisions(response, questions)
        # Keep only allowlisted values; upstream errors and extra response fields can echo secrets.
        resolved_model = response.get("model")
        model_argument(resolved_model if isinstance(resolved_model, str) else "")
        log.write("SUCCEEDED", **configuration, **evidence, resolved_model=resolved_model,
                  decisions=decisions, duration_ms=round((time.monotonic() - started) * 1000))
        print(json.dumps({"event": "SUCCEEDED", "invocation_id": log.identity,
                          "decisions": decisions}, sort_keys=True))
        return 0
    except subprocess.TimeoutExpired:
        failure = "TIMEOUT"
    except KeyboardInterrupt:
        failure = "INTERRUPTED"
    except (OSError, ValueError, argparse.ArgumentTypeError):
        pass
    log.write("FAILED", **configuration, **evidence, failure_code=failure,
              routing_state="UNKNOWN", duration_ms=round((time.monotonic() - started) * 1000))
    print(f"Jev failed ({failure}); retain UNKNOWN and mandatory checks. Invocation: {log.identity}",
          file=sys.stderr)
    return 1


def parser():
    result = argparse.ArgumentParser(description=__doc__)
    commands = result.add_subparsers(dest="command", required=True)
    for name in ("invoke", "bypass"):
        command = commands.add_parser(name)
        command.add_argument("--audit-id", required=True, type=uuid_argument)
        command.add_argument("--run-id", required=True, type=uuid_argument)
        command.add_argument("--axis", required=True, choices=AXES)
        command.add_argument("--profile", required=True, choices=PROFILES)
        command.add_argument("--source-item-id", required=True, type=identifier)
        command.add_argument("--extract-entry-id", required=True, type=identifier)
        command.add_argument("--source-revision", required=True, type=revision_argument)
        command.add_argument("--log-root", type=Path,
                             default=Path(__file__).resolve().parents[2] / "docs/software-factory/audits")
        if name == "invoke":
            command.add_argument("--request", required=True, type=Path)
            command.add_argument("--jev-script", required=True, type=Path,
                                 help="Path to the installed Python jev-cli bin/jev script (not jev.cmd).")
            command.add_argument("--gateway", required=True, choices=ENDPOINTS)
            command.add_argument("--model", required=True, type=model_argument)
            command.add_argument("--threshold", required=True, type=threshold_argument)
            command.add_argument("--timeout", type=positive_seconds, default=60)
        else:
            command.add_argument("--reason-code", required=True,
                                 choices=("NOT_APPLICABLE", "UNAVAILABLE", "MANUAL_FALLBACK"))
            command.add_argument("--reason", required=True,
                                 help="Explanation; only its hash is retained. Put safe rationale in Audit.md.")
    return result


def main(argv=None):
    arguments = parser().parse_args(argv)
    if arguments.command == "bypass" and not arguments.reason.strip():
        parser().error("A bypass explanation is required.")
    log = None
    try:
        log = EventLog(arguments)
        if arguments.command == "bypass":
            log.write("BYPASSED", reason_code=arguments.reason_code,
                      reason_sha256=digest(arguments.reason.encode("utf-8")),
                      evidence_kind="OPERATOR_DECLARATION")
            print(json.dumps({"event": "BYPASSED", "invocation_id": log.identity}))
            return 0
        return invoke(arguments, log)
    except (OSError, ValueError):
        print("Cannot persist Jev evidence; no successful invocation may be claimed.", file=sys.stderr)
        return 1
    finally:
        if log is not None:
            log.close()


if __name__ == "__main__":
    sys.exit(main())
