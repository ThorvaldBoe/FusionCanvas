# Adopt Headless View Testing Verification

## Environment

- Repository: `C:\Code\FusionCanvas`
- Platform: Windows / PowerShell
- Change type: process, specification, and documentation only
- Runtime/package changes: none

## Acceptance Evidence

| Capability / scenario | Method | Result | Evidence / limitation |
| --- | --- | --- | --- |
| testing-baseline / A user-facing view changes | Specification and canonical-guidance inspection | Pass | Delta spec, `AGENTS.md`, `openspec/project.md`, architecture guidance, and workflow skills require applicable Avalonia headless view tests. |
| testing-baseline / Decision logic can be tested without Avalonia | Guidance inspection | Pass | Canonical guidance consistently uses the lowest reliable layer and retains framework-free view-model/command tests. |
| testing-baseline / Static markup has no meaningful behavior | Guidance inspection | Pass | Delta spec, contributor guide, QA-3, and QA-6 reject superficial static-markup tests. |
| testing-baseline / Contributor completes a user-facing module | Canonical-guidance search | Pass | No canonical non-historical routine makes live desktop testing a completion gate. |
| testing-baseline / A risk is not represented by headless testing | Guidance inspection | Pass | Optional live checks are limited to native-window, OS input, assistive-technology, platform, visual, and diagnostic risks and are supplemental evidence. |
| testing-baseline / Live desktop testing is unavailable | Cross-agent routine inspection | Pass | Mandatory routine uses `dotnet test` without an interactive display; no handoff or N/A desktop gate remains in canonical guidance. |
| testing-baseline / Verification is planned | Delta and workflow-skill inspection | Pass | Planning guidance includes focused tests, integration tests, headless views, inspection, optional live checks, or N/A rationale. |
| testing-baseline / Verification is recorded | Delta and archive-skill inspection | Pass | Criterion-level evidence remains required; aggregate suite results do not replace it. |
| testing-baseline / Contributor runs the baseline suite | Command | Pass | Release baseline passed: 430 tests (96 domain, 150 application, 37 integration, 147 app). Debug was blocked by the already-running app locking output DLLs, so Release used separate outputs. |
| testing-baseline / Contributor inspects test project layout | Project inspection | Pass | Mirrored test projects exist; `FusionCanvas.App.Tests` references the app but no Avalonia headless package. |
| testing-baseline / Navigation logic changes | Policy inspection | Pass | Updated requirement uses decision-logic tests plus headless tests for material framework behavior. |
| testing-baseline / Contributor evaluates test scope | Policy and QA inspection | Pass | Baseline permits focused headless views but excludes live desktop automation, pixels, external services, and performance suites. |
| qa-review-baseline / Module completion is reviewed | QA playbook inspection | Pass | Completion QA retains deterministic tests, strict validation, evidence, drift, and risk review. |
| qa-review-baseline / Module changes user-facing behavior | QA playbook inspection | Pass | Completion QA checks decision logic and applicable headless view tests; live evidence is optional. |
| qa-review-baseline / Module has broad cross-cutting risk | QA playbook inspection | Pass | Broad risk expands deterministic QA tasks or full review. |
| qa-review-baseline / Testing review executes | QA-3 inspection | Pass | QA-3 runs the solution baseline and audits meaningful headless view coverage. |
| qa-review-baseline / Coverage gaps are found | Finding-routing inspection | Pass | Missing meaningful headless coverage routes to test additions or OpenSpec clarification. |
| qa-review-baseline / Contributor requests a full QA review | QA-6 inspection | Pass | All six mandatory QA tasks are executable without an interactive display. |
| qa-review-baseline / Reviewer elects to run the desktop application | QA-6 inspection | Pass | Live checks are optional supplemental evidence and do not affect the verdict. |

## Validation Commands

| Command | Result | Evidence |
| --- | --- | --- |
| Canonical mandatory-desktop wording search | Pass | `rg` found only new headless-first and optional-live wording in canonical non-historical guidance. |
| Avalonia headless implementation search | Pass (inventory) | No `Avalonia.Headless`, `AvaloniaTest`, `UseHeadless`, or equivalent harness/test symbol exists under `src` or `tests`. |
| `git diff --check` | Pass | No whitespace errors; Git emitted line-ending notices only. |
| `openspec validate adopt-headless-view-testing --strict` | Pass | Change is valid. |
| `openspec validate --all --strict` | Pass | 29 changes/specs passed, 0 failed. |
| `dotnet test .\FusionCanvas.sln -c Release -m:1 /p:UseSharedCompilation=false` | Pass | 430 passed, 0 failed, 0 skipped. Existing analyzer warnings remain. Release configuration avoided Debug output files locked by the running Visual Studio/app session. |
| Accepted-spec sync | Pass | Delta requirements were merged into `openspec/specs/testing-baseline/spec.md` and `openspec/specs/qa-review-baseline/spec.md`; strict validation remained green afterwards. |

## Limitations and Follow-up

- This change intentionally does not add the Avalonia headless package, application fixture, or representative view tests.
- The current app test suite contains view-model, command, navigation, and coordination tests, not instantiated Avalonia view tests.
- A separate bounded implementation change should select Avalonia 12-compatible headless packages/APIs, establish fixture and dispatcher isolation, and add representative tests before QA-6 can pass without a test-gap finding.
- Live desktop testing remains available ad hoc but was not needed to verify this process-only change.
