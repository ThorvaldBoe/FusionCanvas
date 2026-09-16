# Verification — schedule-deterministic-tests-daily

| Capability / scenario | Method | Result | Evidence / limitation |
| --- | --- | --- | --- |
| Pull-request baseline / A pull request is opened | Workflow inspection | Pass | `.github/workflows/ci.yml` has no `pull_request` or `push` trigger and therefore does not run the full suite for PR updates. |
| Pull-request baseline / Contributor needs full-suite confidence | Documentation inspection | Pass | `README.md`, `docs/testing-strategy.md`, and the PR template document `dotnet test .\\FusionCanvas.sln -m:1` as the local full-suite command. |
| Pull-request baseline / Real-desktop automation remains separate | Workflow and command inspection | Pass | The workflow invokes the solution baseline only; it does not invoke `FusionCanvas.UITests`, Appium, or Developer Mode. |
| Daily baseline / Scheduled execution | Workflow inspection | Pass | `.github/workflows/ci.yml` uses `schedule` with `30 2 * * *`, which runs daily at 02:30 UTC on the default branch. Hosted execution is not reproduced locally. |
| Daily baseline / Immediate maintainer run | Workflow inspection | Pass | `.github/workflows/ci.yml` includes `workflow_dispatch` and uses the same test job. |
| Daily baseline / Failure remains visible and runs are not duplicated unnecessarily | Workflow inspection and local command | Pass | The test step has no error suppression, and scoped concurrency cancels obsolete runs. `dotnet test .\\FusionCanvas.sln -m:1 --no-restore --nologo -v minimal` exited 0 locally. |

## Validation commands

- PASS: `openspec validate schedule-deterministic-tests-daily --strict`
- PASS: `git diff --check`
- PASS: `dotnet test .\\FusionCanvas.sln -m:1 --no-restore --nologo -v minimal` (exit code 0; this runner emitted no test summary)

## Limitations

- GitHub-hosted scheduled and manual workflow execution cannot be simulated from the local runner; the first scheduled/manual run remains the operational verification of trigger delivery.
- No production code or UI behavior changed, so focused application tests were not applicable.
