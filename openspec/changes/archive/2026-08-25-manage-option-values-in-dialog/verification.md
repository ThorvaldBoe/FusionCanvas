# Verification: Manage Option values in a focused dialog

## Acceptance scenarios → evidence

| Scenario | Method | Result | Evidence |
| --- | --- | --- | --- |
| Manage values opens a focused dialog for the selected Option | Headless view test | PASS | `StoreEditorHeadlessTests.ManageValues_OpensFocusedDialogScopedToOneOption`: asserts `OptionValueManagementWindow` in `window.OwnedWindows`, title `Manage Color values`, `SelectedOptionId` matches the card's Option, Done focused. |
| Only one value-management dialog at a time | Headless view test | PASS | `StoreEditorHeadlessTests.ManageValues_AllowsOnlyOneDialogAtATime`: a second `ManageOptionCommand` while open keeps a single owned dialog. |
| Switching the Blueprint Offering closes the dialog | Framework-free + headless | PASS | `CatalogSetupViewModelTests.OfferingSwitchClosesOptionValueManagementAndDiscardsDraft` resets `IsManagingOptionValues`/draft; `StoreEditorHeadlessTests.ManageValues_OfferingSwitchClosesDialogWithoutStaleEditing` closes the dialog on `SelectOffering(null)`. |
| Switching the workspace closes the dialog | Framework-free | PASS | Workspace switch routes through `LoadForStoreAsync` → `ApplyCatalog` → `SelectedOffering` setter, which resets the session; the dialog observes `IsManagingOptionValues` and closes. Covered by the Offering-switch guard (same code path). |
| Explicit finish closes the dialog | Headless view test | PASS | `ManageValues_OpensFocusedDialogScopedToOneOption` and `CatalogEditorsUseCompactBasicsOnDemandDraftsAndSummaryFirstRegions`: Done/Close closes the dialog and focus returns to the **Manage values** control. |
| Cancel or close discards an unfinished add-value draft | Framework-free + headless | PASS | `CatalogSetupViewModelTests.ManageOptionCommandRequestsDialogAndCloseDiscardsDraft` (OptionValue cleared, IsAddingOptionValue false); `StoreEditorHeadlessTests.ManageValues_EscapeClosesDialogAndDiscardsAddValueDraft` (Escape closes, draft gone, no new value persisted, focus returns). |
| Value management reuses validation, dependencies, persistence | Framework-free + reuse | PASS | The dialog binds the existing `CreateOptionValueCommand`, `ArchiveOptionValueCommand`, `CancelAddOptionValueCommand`; no domain/application logic duplicated. Existing archive-dependency tests remain green (full suite 0 failures). |
| Value management dialog supports custom Option kinds | Headless view test | PASS | `StoreEditorHeadlessTests.ManageValues_OpensSameDialogForCustomOptionKind`: creates an `OptionKind.Other` "Material" Option and opens the same dialog titled `Manage Material values`. |
| Inline value editor removed from parent page | Headless view test | PASS | `CatalogEditorsUseCompactBasicsOnDemandDraftsAndSummaryFirstRegions`: asserts no `Catalog.OptionValueEditor` control exists in the Store Editor visual tree. |
| Each Option's Manage values opens the dialog with correct stable identity | Headless view test | PASS | `ManageValues_OpensFocusedDialogScopedToOneOption`: `SelectedOptionId` equals the card Option's Id after opening. |
| Successful changes update Option and parent summary | Reuse | PASS | Add/archive reuse `RunMutationAsync` → `ApplyCatalog` → `RefreshOfferingCollections`, which rebuilds `AvailableChoiceGroups` (value summaries) and `SellableVariantRows`. No change to that path; existing refresh tests remain green. |

## Commands

- `dotnet test .\FusionCanvas.sln` — PASS: 1385 passed, 0 failed, 0 skipped.
  - Domain: 232; Application: 384; UiDescription: 27; Integration: 188; App: 554.
- `openspec validate manage-option-values-in-dialog --strict` — PASS: change valid.
- `openspec validate --specs --strict` — PASS (after sync).
- `openspec validate --changes --strict` — PASS (after sync).

## Build/test results

- Build: 0 errors, 0 new warnings introduced by this change.
- Solution tests: 1385 passed, 0 failed.

## Limitations

- No live desktop UI pass (optional per the testing baseline); deterministic headless + framework-free tests are the gate.
- No pixel-perfect visual regression baseline.
- The low-fidelity `manage-variants.ui.yaml` wireframe was left unchanged: it already describes only the manage buttons and no inline value editor, so no content needed removal and the rendered SVG fixtures remain valid.
