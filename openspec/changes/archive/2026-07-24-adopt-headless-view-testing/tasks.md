## 1. Canonical Testing Policy

- [x] 1.1 Update `AGENTS.md` and `openspec/project.md` so Avalonia headless view tests are the routine cross-agent UI lane and live desktop testing is optional and ad hoc
- [x] 1.2 Update `docs/architecture.md` and `README.md` with the lowest-reliable-layer strategy and the current absence of a headless harness
- [x] 1.3 Update the repository OpenSpec propose, apply, and archive skill instructions so future changes plan and verify headless view coverage without desktop-environment handoffs

## 2. QA Routine

- [x] 2.1 Update `docs/qa-review.md` so QA-3 reviews applicable Avalonia headless view coverage
- [x] 2.2 Replace mandatory QA-6 real-desktop regression with a deterministic headless UI view review and document live desktop checks as optional supplemental evidence
- [x] 2.3 Reconcile QA cadence, reporting, completion review, and finding routing with the headless-first policy

## 3. Verification and Acceptance

- [x] 3.1 Create `verification.md` mapping every delta-spec scenario to inspection or command evidence
- [x] 3.2 Search canonical non-historical guidance for contradictory mandatory desktop language and reconcile it
- [x] 3.3 Run strict OpenSpec validation for the change and all active changes, then run `git diff --check`
- [x] 3.4 Run `dotnet test .\FusionCanvas.sln` and record the result
