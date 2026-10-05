# Architecture & Code Structure Audit Run — 2067e25c-bb6f-491c-8dd1-b489ce80babe

- Audit ID: d0f9281c-e845-4531-a28a-ce66d6a48f6c
- Audit run ID: 2067e25c-bb6f-491c-8dd1-b489ce80babe
- Status: PROVISIONAL
- Baseline readiness: FINAL_ELIGIBLE for this axis; parent remains BLOCKED
- Started at: 2026-10-04T00:00:00+02:00
- Ended at: 2026-10-04T00:00:00+02:00
- Auditor/reviewer: Codex / pending human review
- Scope: Consent feature types, startup composition, settings persistence boundary, and focused tests.
- Audit profile: STANDARD_TARGETED
- Standard ID/version/status: FC-ARCH / 2.0.0 / Active
- Source extract definition version/status: 2.0 / Active
- Jev classifier/configuration: NOT_APPLICABLE; small, manually enumerated changed-scope type set with mandatory lifecycle and boundary checks retained.
- Repository revision and working-tree state: 70c1e1c3da564a51bd8ae6544e5a699e7b6842d7 / dirty-after-consent-commit
- Append-only extract path and identity: `docs/software-factory/audits/d0f9281c-e845-4531-a28a-ce66d6a48f6c/2067e25c-bb6f-491c-8dd1-b489ce80babe/source-extract.yaml` / ARCH-EXTRACT-001
- Latest extract extension revision: 1
- Progress phase: FINALIZE
- Progress updated at: 2026-10-04T00:00:00+02:00
- Source items: 6 / 6 / 6 / 0 / 6
- Checks: 5 planned / 5 completed / 0 remaining
- Current item and position: ARCH-ITEM-tests / 6 of 6
- Current item relevant checks: TEST-001, TEST-005
- Progress estimate and basis: 100% of the targeted six-item packet; process coverage only.

## Extract history

| Extension revision | Source revision | Working-tree state | Standard/definition versions | Generated at | Change summary | Affected extract-entry IDs |
|---|---|---|---|---|---|---|
| 1 | 70c1e1c3da564a51bd8ae6544e5a699e7b6842d7 | dirty-after-consent-commit | FC-ARCH 2.0.0 / extract 2.0 | 2026-10-04T00:00:00+02:00 | Initial consent-scope inventory; unrelated current-tree edits excluded. | ARCH-ENTRY-001..006 |
| 2 | worktree-consent-audit-fixes | dirty | FC-ARCH 2.0.0 / extract 2.0 | 2026-10-04T12:05:00+02:00 | Re-extracted changed ViewModel and added Application service after architecture finding. | ARCH-ENTRY-007..008 |

## Item/criterion assessments

| Assessment ID | Source item ID | Extract-entry ID/revision | Source revision | Standard version | Criterion ID/heading | Result | Evidence references | Implementation references/dependencies | Finding IDs | Challenge IDs | Supersedes assessment ID |
|---|---|---|---|---|---|---|---|---|---|---|---|
| A-ARCH-001 | ARCH-ITEM-app-terms-consent-vm | ARCH-ENTRY-001/1 | 70c1e1c | 2.0.0 | ARCH-003 / MVVM-005 application boundary | FAIL | ViewModel performs `SaveAsync` and constructs the persisted record. | `TermsConsentViewModel.AcceptAsync` lines 144-159; `IApplicationSettingsStore` | F-ARCH-001 | NONE | NONE |
| A-ARCH-002 | ARCH-ITEM-app-startup | ARCH-ENTRY-003/1 | 70c1e1c | 2.0.0 | ARCH-005, LIFE-006 composition and startup ownership | PASS | App owns startup sequencing and does not let the main window open before the gate completes. | `App.InitializeStartupAsync` lines 91-120; `AppServicesFactory.Create` | NONE | NONE | NONE |
| A-ARCH-003 | ARCH-ITEM-integration-settings | ARCH-ENTRY-005/1 | 70c1e1c | 2.0.0 | ARCH-008 persistence boundary | PASS | JSON serialization remains in Integration behind `IApplicationSettingsStore`; schema version is advanced compatibly. | `JsonApplicationSettingsStore` lines 12, 136-162, 321-344 | NONE | NONE | NONE |
| A-ARCH-004 | ARCH-ITEM-app-terms-consent-window | ARCH-ENTRY-002/1 | 70c1e1c | 2.0.0 | MVVM-001, MVVM-008 view/code-behind separation | PASS | XAML owns presentation; code-behind only closes the dialog on completion/cancel. | `TermsConsentWindow.axaml`, `.axaml.cs` | NONE | NONE | NONE |
| A-ARCH-005 | ARCH-ITEM-tests | ARCH-ENTRY-006/1 | 70c1e1c | 2.0.0 | TEST-001, TEST-005 visible dependencies and owning-layer tests | PASS | Focused App tests passed: 10 consent tests; deterministic fake settings store and headless view coverage are present. | `tests/FusionCanvas.App.Tests/TermsConsent/*` | NONE | NONE | NONE |

## Findings

| Finding ID | Assessment ID | Source item ID | Extract-entry ID/revision | Source revision | Criterion ID | Type/severity | Title | GitHub issue URL/status |
|---|---|---|---|---|---|---|---|---|
| F-ARCH-001 | A-ARCH-001 | ARCH-ITEM-app-terms-consent-vm | ARCH-ENTRY-001/1 | 70c1e1c | ARCH-003 / MVVM-005 | Architecture boundary / Major | TermsConsentViewModel directly owns application-settings persistence | Not created; issue creation not requested |

### F-ARCH-001 detail

`TermsConsentViewModel` depends directly on `IApplicationSettingsStore`, creates the consent record, replaces settings, and calls `SaveAsync`. This makes the presentation layer own persistence orchestration and policy transition. The recommended correction is a narrow Application use case/service that validates the acknowledgement set, creates the versioned record, persists it, and returns a result; the ViewModel should translate that result into presentation state. This is a design finding, not an instruction to add a speculative abstraction layer.

### Reassessment after extract extension 2

| Assessment ID | Source item ID | Extract-entry ID/revision | Source revision | Criterion ID/heading | Result | Evidence | Supersedes |
|---|---|---|---|---|---|---|---|
| A-ARCH-006 | ARCH-ITEM-app-terms-consent-vm | ARCH-ENTRY-007/2 | worktree-consent-audit-fixes | ARCH-003 / MVVM-005 application boundary | PASS | `TermsConsentViewModel` delegates acceptance to `TermsConsentService`; the Application service validates all selections, creates the record, and calls `IApplicationSettingsStore`. | `src/FusionCanvas.Application/TermsConsent/TermsConsentService.cs`; `src/FusionCanvas.App/TermsConsent/TermsConsentViewModel.cs` | A-ARCH-001 |
| A-ARCH-007 | ARCH-ITEM-application-terms-consent-service | ARCH-ENTRY-008/2 | worktree-consent-audit-fixes | ARCH-003 / ARCH-006 use-case ownership | PASS | The new service is in Application and is injected from the composition root in production startup and AppServicesFactory. | `src/FusionCanvas.App/App.axaml.cs`; `src/FusionCanvas.App/AppServicesFactory.cs` | NONE |

F-ARCH-001 is **resolved in the current worktree**, pending clean App compilation and commit-level reassessment.

## Challenges

| Challenge ID | Assessment ID | Source item ID | Extract-entry ID/revision | Source revision | Criterion ID | Uncertainty and competing interpretations | Review disposition | Owner/next action |
|---|---|---|---|---|---|---|---|---|
| CH-ARCH-001 | A-ARCH-001 | ARCH-ITEM-app-terms-consent-vm | ARCH-ENTRY-001/1 | 70c1e1c | ARCH-003 / MVVM-005 | Existing App ViewModels may directly use application-facing stores in this early codebase; the standard expects a use-case boundary for behavior-changing persistence. | Pending | Human architecture review; decide whether the Application service is required before merge. |

## Coverage, limitations, and certification status

Targeted architecture coverage is complete for the consent change. The prior finding is superseded and resolved in the current worktree. The axis is PROVISIONAL in this parent audit because the overall request includes Draft/Working baselines. The current-tree consent launcher refactor was not treated as part of this correction; reassess it if it changes dependency ownership.
