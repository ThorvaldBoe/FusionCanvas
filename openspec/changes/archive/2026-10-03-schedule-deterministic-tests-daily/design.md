## Context

The current `.github/workflows/ci.yml` runs the full solution test suite on pull requests and pushes to `main`. The suite includes Avalonia headless tests and is intentionally deterministic, but its runtime slows the review loop. The requested change is a verification-cadence change only; production code and test semantics remain unchanged.

## Goals / Non-Goals

**Goals:**

- Run the full deterministic solution baseline once per day on the default branch.
- Make the same baseline available through manual workflow dispatch.
- Remove the full-suite PR requirement from the accepted testing-baseline contract.
- Keep failures visible and preserve a clear local command for contributors.
- Reduce duplicate work from obsolete scheduled/manual runs where practical.

**Non-Goals:**

- Do not delete, skip, quarantine, or weaken failing tests.
- Do not make deterministic tests non-deterministic or dependent on external services.
- Do not add a replacement UI automation framework or alter Avalonia headless coverage.
- Do not change merge permissions, branch protection settings, or production behavior in this change.

## Decisions

1. **Use a scheduled GitHub Actions workflow with manual dispatch.** This keeps the repository-controlled baseline reproducible and gives maintainers an immediate diagnostic path. A developer machine alone cannot provide a shared, neutral signal.

   Alternative considered: keep PR execution but make it advisory. Rejected because it would continue consuming PR-runner time without providing a reliable merge gate.

2. **Keep the full baseline command unchanged.** The command remains `dotnet test .\\FusionCanvas.sln -m:1 --no-restore --nologo`; only when it runs changes. This avoids creating divergent local and CI definitions.

   Alternative considered: replace it with a smaller test subset. Rejected for the daily baseline because it would reduce coverage rather than only change cadence.

3. **Run on the default branch daily rather than on every PR update.** The schedule provides a regular health signal while contributors use focused tests and the full local command during development and review.

4. **Retain failure semantics.** A failing build or test must fail the scheduled/manual job. Known failures remain defects to classify and repair; they are not hidden with `continue-on-error`.

5. **Use workflow concurrency for scheduled/manual runs.** A single current full-suite run should be preferred over duplicate pending or obsolete runs, while an active diagnostic run should not be cancelled unnecessarily without an explicit repository policy.

## Risks / Trade-offs

- **[Risk] A regression can merge before the next daily run.** → Document and retain the local full-suite command, require contributors to run it before merging, and preserve focused checks or other existing PR checks where they are already present.
- **[Risk] The daily run is missed or disabled.** → Keep `workflow_dispatch`, visible workflow history, and repository-owned workflow configuration; consider a later freshness monitor if operational experience shows this is needed.
- **[Risk] Existing failures make the daily signal permanently red.** → Triage failures by product regression, stale test, environmental defect, or obsolete expectation; repair or update them rather than suppressing the job.
- **[Risk] Scheduled and manual runs consume unnecessary runner time.** → Add caching and concurrency controls, and avoid duplicate full-suite triggers.
- **[Trade-off] Review feedback becomes slower for full-suite regressions.** → This is an explicit acceptance trade-off in exchange for a faster merge loop and lower recurring CI cost.

## Migration Plan

1. Update the testing-baseline delta and repository documentation.
2. Change `.github/workflows/ci.yml` from PR/full-suite triggering to daily scheduling plus manual dispatch, retaining the full command and failure behavior.
3. Validate workflow syntax and OpenSpec strict validation.
4. Run the full deterministic suite locally when verifying the implementation; record any pre-existing failures without masking them.
5. Roll back by restoring the prior PR trigger if daily coverage proves insufficient.

## Implementation Plan

- **Workflow:** Modify `.github/workflows/ci.yml`; use `schedule` with a UTC cron expression, `workflow_dispatch`, the default-branch checkout behavior, dependency restore, and the unchanged serialized test command. Add scoped concurrency/cache improvements if compatible with the current workflow.
- **Documentation:** Update the contributor testing instructions and any CI references to distinguish focused/local verification from the daily shared baseline. Do not claim that a PR is fully tested when it has not run the full suite.
- **Specification:** Add the `testing-baseline` delta in this change and validate that the modified requirement includes its complete updated behavior.
- **Verification:** Inspect the workflow trigger and command; run `openspec validate --strict`; run the focused affected checks and the solution baseline locally as environment permits; verify that a failing test exits non-zero.
- **Decisions not to reopen:** Daily cadence, manual dispatch, unchanged full-suite command, visible failure semantics, and no production/UI changes are fixed by this proposal.

## Open Questions

None. The exact UTC schedule time is an implementation detail and may be selected to avoid the repository’s busiest period.
