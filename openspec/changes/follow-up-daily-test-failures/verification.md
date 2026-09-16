# Verification — follow-up-daily-test-failures

| Capability / scenario | Method | Result | Evidence / limitation |
| --- | --- | --- | --- |
| Daily deterministic failures produce actionable follow-up / The baseline fails | Workflow inspection | Pass | The test command remains a normal failing step; TRX upload and job summary use `always()`; failure follow-up uses `failure()` and links the run. Hosted failure behavior requires the first controlled scheduled/manual run. |
| Daily deterministic failures produce actionable follow-up / Later baseline fails | Workflow inspection | Pass | The failure step searches open issues for the exact stable title and comments on the first match instead of creating a duplicate. |
| Daily deterministic failures produce actionable follow-up / Baseline recovers | Workflow inspection | Pass | The success step searches for the same title, comments with the recovery run, and closes the issue when present. |
| Daily deterministic failures produce actionable follow-up / Issue automation cannot complete | Workflow inspection | Pass | Issue commands are not allowed to override the test step; errors remain visible in the workflow log while the failed test keeps the job failed. |
| Restricted repository automation / Workflow executes | Workflow inspection | Pass | Permissions are `contents: read` and `issues: write`; checkout is pinned to `${{ github.event.repository.default_branch }}`; no third-party issue action or external credential is used. |

## Validation commands

- PASS: `openspec validate follow-up-daily-test-failures --strict`
- PASS: `git diff --check`
- PASS: `dotnet test .\\FusionCanvas.sln -m:1 --no-restore --nologo -v minimal` (exit code 0; this runner emitted no test summary)

## Limitations

- GitHub-hosted issue creation, commenting, closing, artifact upload, and job-summary rendering require a scheduled or manually dispatched workflow run after merge.
- No test failure was injected locally because doing so would alter or weaken the repository test baseline; failure semantics were verified by workflow structure.
