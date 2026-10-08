# Jev logging maintenance verification — 2026-10-08

This is a verification record for repository audit tooling, not a Software Factory axis audit or certification. The two JSONL files in this directory are synthetic execution checks with synthetic source identities. They do not establish Jev usage in any historical audit.

| Criterion | Method | Result / evidence |
| --- | --- | --- |
| Successful execution has durable evidence | Actual installed CLI and OpenRouter call using a public synthetic SQLite description | `412c2632-89ef-4d57-a4fa-3d88c5575bf9.jsonl`: `STARTED` → `SUCCEEDED`; resolved model `typesafe/jev-1.13-20260917`; `FC-SEC-001` → `RELEVANT`. |
| Failed execution cannot count as success | Initial synthetic CLI invocation | `a1a97f8b-f2fd-4845-8264-7d701a5b2d1d.jsonl`: `STARTED` → `FAILED`, exit code 2. The installed CLI requires an integer timeout; the wrapper was corrected and the fake CLI now enforces that contract. |
| Bypasses, interruptions, invalid responses and unavailable tools remain distinct | Deterministic isolated tooling tests | 11 tests pass with no network or credentials; missing/incomplete logs cannot establish successful execution. |
| Logs omit sensitive content | Sentinel input, response and stderr tests plus retained-log inspection | Only hashes and allowlisted identities, configuration and decisions are retained. |
| No invocation occurs when evidence cannot be persisted | Unwritable log-root test | Passed; child process never launched. |
| Repeated calls retain independent records | Independent invocation-file test | Passed; previous file remains byte-for-byte unchanged. |
| Factory agents discover the wrapper | Repository guide and tooling documentation inspection | `AGENTS.md` requires the wrapper or an explicit bypass record; the normal CI lane runs the tooling tests. |
| Accepted product behavior is preserved | Scope review | Only tooling, documentation, CI and synthetic evidence changed. No production application code or accepted specs changed. |

The request and raw response were not committed. The safe synthetic state describes a SQLite adapter using SQL parameters, and its sole question classifies the relevance of reviewing SQL parameterization. The request/response, logger and installed CLI byte hashes in the logs identify the invocation inputs and adapter used at that time.

Mandatory local checks: `python -m unittest discover -s tools/software-factory/tests -v`, `dotnet test .\FusionCanvas.sln -m:1`, `openspec validate --all --strict`, and `git diff --check`. The tooling tests and all 85 OpenSpec items passed. The initial solution baseline passed; the final baseline is rerun after rebasing onto the latest main and its result is recorded in the pull request and CI.

Existing baseline warnings include ImageSharp vulnerability warnings, nullable warnings and xUnit cancellation-analyzer warnings; this tooling change does not alter those dependencies or application code. Appium/headless coverage is not warranted because no desktop behavior changed.

The logs are local execution evidence, not signed attestations. Direct Jev calls outside the wrapper are not intercepted. Threshold application, safety-floor overrides, routing rationale and calibration remain the audit agent's responsibility under the factory procedure.
