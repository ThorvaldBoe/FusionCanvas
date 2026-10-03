## Context

The daily deterministic workflow currently runs the serialized solution suite but offers no proactive follow-up beyond a failed Actions run. The workflow is scheduled and manually dispatched, so its source is repository-controlled and its result can safely be connected to a repository issue. The test result must remain authoritative: issue automation is supplemental diagnostics, not a replacement for the failed job status.

## Goals / Non-Goals

**Goals:**

- Preserve `.trx` results for failed and successful runs.
- Put a short actionable summary in the Actions job summary.
- Maintain one open tracking issue for the active daily-baseline failure state.
- Close the issue after a successful run.
- Use the built-in `gh` CLI and `GITHUB_TOKEN` with only `issues: write` permission.

**Non-Goals:**

- Do not auto-assign individuals, page external services, or send email outside GitHub.
- Do not create one issue per failing test or per workflow run.
- Do not make a failed test run pass with `continue-on-error`.
- Do not parse or expose secrets from test output.
- Do not change test code or production application behavior.

## Decisions

1. **Use one stable issue identified by a unique title.** The workflow searches open issues for `CI: deterministic tests failing` and creates one only when none exists. Later failures add a comment with the run link and commit, keeping the issue actionable without issue spam.

   Alternative considered: create an issue for every failed run. Rejected because repeated daily failures would create noise and obscure the underlying incident.

2. **Close the issue on a successful full baseline.** A green run is the clearest recovery signal. The issue history remains available after closure, while the next failure can create a fresh active issue.

3. **Use `if: failure()` for failure follow-up and `if: success()` for recovery.** Artifact upload uses `if: always()` so diagnostics survive both failure and recovery. The test command itself remains a normal failing step.

4. **Force workflow checkout to the repository default branch.** This keeps scheduled and manually dispatched diagnostics tied to the shared daily baseline and avoids granting issue-writing workflow code to arbitrary selected refs.

5. **Use `gh` rather than a third-party issue action.** This avoids adding an external action dependency. The job grants `issues: write` and `contents: read` only.

## Risks / Trade-offs

- **[Risk] Issue creation or commenting fails due to GitHub API availability.** → Keep the test failure authoritative; make follow-up failure visible in the job summary and workflow log.
- **[Risk] Test output is too large or contains sensitive paths.** → Upload standard TRX artifacts with retention and summarize only counts/run metadata in the issue; never paste raw logs into the issue.
- **[Risk] Multiple runs race while updating the issue.** → Reuse the existing workflow concurrency group so obsolete runs are cancelled.
- **[Risk] A successful run closes an issue while a newer failure is active.** → Concurrency serializes runs in the workflow group; the issue update uses the run’s observed state and remains supplemental.
- **[Trade-off] Maintainers must act on the issue manually.** → Include direct links to the failed run and artifact location; automatic remediation is out of scope.

## Migration Plan

1. Add the new requirement and workflow implementation.
2. Run strict OpenSpec validation and local syntax/diff checks.
3. Trigger the workflow manually after merge to verify artifact publication and issue behavior.
4. If follow-up automation causes noise, remove the issue steps while retaining test execution and artifacts; no data migration is required.

## Implementation Plan

- **Workflow:** Modify `.github/workflows/ci.yml` to add `issues: write`, check out the default branch, run the unchanged test command with a TRX logger, upload results with `if: always()`, write a compact `$GITHUB_STEP_SUMMARY`, and create/comment/close the stable tracking issue in conditional shell steps.
- **Issue operations:** Use `gh issue list --state open --search` and `gh issue create/comment/close`; quote run URL, commit SHA, and branch values as data. Do not include raw test logs.
- **Verification:** Validate workflow and OpenSpec text by inspection; run the local deterministic suite; manually dispatch the workflow after merge and verify failure/recovery behavior using a controlled test failure only if maintainers choose to do so.
- **Decisions not to reopen:** One issue, stable title, close-on-green, built-in `gh`, narrow permissions, default-branch checkout, and preserved failed status are fixed by this design.

## Open Questions

None.
