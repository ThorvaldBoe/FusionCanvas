# Jev audit execution evidence

Use `jev_audit.py` for every Jev relevance-routing call in a Software Factory audit. Python 3.10 or later is required; the wrapper uses only the standard library. The installed Jev CLI continues to own provider authentication and inference. This tool adds execution evidence, not audit verdicts or automatic certification.

## Invoke Jev

Prepare a temporary JSON request containing a concise, non-sensitive `state` and `questions`. Each question uses the Jev `choice` wire format and all five factory relevance categories:

```json
{
  "state": "Synthetic example: a SQLite adapter persists workspace records using parameters.",
  "questions": {
    "FC-SEC-001": {
      "type": "choice",
      "instructions": "Classify the relevance of checking SQL parameterization for this source item.",
      "criteria": {
        "RELEVANT": "The check applies.",
        "POSSIBLE": "The check may apply; inspect more context.",
        "IRRELEVANT": "The check does not apply.",
        "UNKNOWN": "There is not enough context to decide.",
        "MANDATORY": "A critical or deterministic safety floor requires the check."
      }
    }
  }
}
```

Run from the repository, replacing the identities, commit hash and installed script path:

```powershell
python tools/software-factory/jev_audit.py invoke `
  --audit-id 11111111-1111-4111-8111-111111111111 `
  --run-id 22222222-2222-4222-8222-222222222222 `
  --axis security-privacy --profile STANDARD_TARGETED `
  --source-item-id SEC-SURFACE-001 --extract-entry-id SEC-SURFACE-001@1 `
  --source-revision 1234567 --request TestResults/jev-request.json `
  --jev-script '<installed-jev-cli>/bin/jev' `
  --gateway openrouter --model typesafe/jev-1.13 --threshold 0.5
```

Supply the Python `bin/jev` script, not the Windows `jev.cmd` launcher. To locate it, inspect the launcher returned by `Get-Command jev`. The wrapper executes Python directly without a shell, forces the recorded gateway/endpoint/model, and leaves credentials out of arguments. It does not change global Jev configuration. One invocation covers one source/extract entry and any number of candidate checks. Thresholds are recorded for provenance; the auditor still applies profile routing and mandatory overrides separately.

## Log location and interpretation

Each invocation owns a new append-only JSONL file at:

`docs/software-factory/audits/<audit-id>/<run-id>/jev/<invocation-id>.jsonl`

The log records UTC timestamps, audit/run/source/extract identities, source commit, profile, threshold, gateway, requested and returned model, logger and CLI content hashes, request/response hashes, batch size, exit code, elapsed time, and validated relevance decisions. `STARTED` is flushed to disk before launching Jev. A successful exit with missing, malformed, incomplete or unexpected answers is a failure.

| Events | Meaning |
| --- | --- |
| `STARTED` → `SUCCEEDED` | Jev executed, exited successfully and returned valid answers for every requested check. |
| `STARTED` → `FAILED` | Invocation failed; retain `UNKNOWN` and mandatory checks. The safe failure code identifies the failure category. |
| `STARTED` alone | Incomplete/interrupted execution; do not claim successful Jev use. |
| `BYPASSED` | Operator declared a bypass; Jev did not execute. |
| No log | No retained execution evidence. |

Link the invocation ID and relative log path from the axis `Audit.md`. Retain the safe log with the audit artifacts. Record the standard version, criterion catalogue, routing rationale, needed context, confidence (or explicitly unavailable), manual safety-floor overrides, and calibration evidence in the normal routing register. Jev's CLI does not supply confidence/rationale for every response, and this logger must not invent them.

Read a run's records with:

```powershell
Get-ChildItem docs/software-factory/audits/<audit-id>/<run-id>/jev/*.jsonl |
  Get-Content | ForEach-Object { $_ | ConvertFrom-Json } |
  Select-Object timestamp,event,invocation_id,failure_code,reason_code
```

Logs omit prompts, state, raw responses, raw stderr, credentials, local executable paths, and free-text bypass explanations. Only hashes and allowlisted metadata/decisions are retained. Use public, stable source/check identifiers; keep request files under ignored `TestResults/` and send only non-sensitive summaries to the provider. These are local execution records, not signed or tamper-proof attestations. They cannot prove calls made outside this wrapper, and cannot retroactively establish historical usage.

## Record a bypass

Use the same audit/run/source metadata with `bypass` instead of `invoke`, plus:

```powershell
  --reason-code NOT_APPLICABLE --reason 'Small rule set; every candidate was assessed manually.'
```

Allowed reasons are `NOT_APPLICABLE`, `UNAVAILABLE`, and `MANUAL_FALLBACK`. Put the safe, readable justification in `Audit.md`; the log stores its hash and marks the event as an operator declaration. The factory's existing exception and failure rules still govern whether a bypass is justified. An unavailable invocation should first have a `FAILED` record whenever it can be attempted.

## Verification

```powershell
python -m unittest discover -s tools/software-factory/tests -v
dotnet test .\FusionCanvas.sln
```

The tooling tests run without a network or credentials, using isolated temporary resources and a fake CLI process. Appium/headless coverage is not applicable: this maintenance tool does not change the desktop application.
