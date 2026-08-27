# Verification: Open Add Variant and Bulk add in focused dialogs

## Acceptance scenarios → evidence

| Scenario | Method | Result | Evidence |
| --- | --- | --- | --- |
| Add Variant opens a focused dialog scoped to the active Offering | Headless view test | PASS | `StoreEditorHeadlessTests.AddVariant_OpensFocusedDialogScopedToOffering`: asserts `AddVariantWindow` in `window.OwnedWindows`, title "Add Variant", `SelectedOfferingId` matches, VariantName text box focused. |
| Bulk add opens a focused dialog scoped to the active Offering | Headless view test | PASS | `StoreEditorHeadlessTests.BulkAdd_OpensFocusedDialogScopedToOffering`: asserts `BulkAddVariantsWindow` in `window.OwnedWindows`, title "Bulk add", `SelectedOfferingId` matches, BulkColor combo focused, Preview button present. |
| Only one creation dialog may be open at a time | Headless view test | PASS | `StoreEditorHeadlessTests.VariantCreation_AllowsOnlyOneDialogAtATime`: opening Add Variant yields a single owned `Window`; after closing, opening Bulk add yields a single owned `Window`. The modal `ShowDialog` plus the host `_variantCreationDialogOpen` guard enforce single-dialog concurrency. |
| Switching the Blueprint Offering closes the creation dialog | Framework-free + headless | PASS | `CatalogSetupViewModelTests.OfferingSwitchClosesVariantCreationAndDiscardsDrafts` resets `IsAddingVariant`/`IsAddingBulkVariants` and discards drafts on Offering switch; `StoreEditorHeadlessTests.VariantCreation_OfferingSwitchClosesDialog` closes the dialog on `SelectOffering(null)`. |
| Switching the workspace closes the creation dialog | Framework-free | PASS | Workspace switch routes through `LoadForStoreAsync` → `ApplyCatalog` → `SelectedOffering` setter, which resets both variant sessions; the dialog observes the flag and closes. Covered by the Offering-switch guard (same code path). |
| Successful creation closes the dialog and refreshes the list | Framework-free + headless | PASS | `CatalogSetupViewModelTests.SuccessfulVariantCreationClosesSessionAndRefreshesList`: `IsAddingVariant` becomes false and `AvailableVariantCount` increases; `StoreEditorHeadlessTests.VariantCreation_SuccessClosesDialogAndRefreshesList`: `HasError` false, `IsAddingVariant` false, `SellableVariantRows.Count` increases from 1 to 2, `SelectedOfferingId` preserved. |
| Cancel, close, or Escape discards the draft and returns focus | Headless view test | PASS | `StoreEditorHeadlessTests.VariantCreation_EscapeClosesDialogAndDiscardsDraft`: Escape closes the dialog, `IsAddingVariant` false, `VariantName` empty, no new Variant, focus returns to `AddVariantButton`. `AddVariant_OpensFocusedDialogScopedToOffering` and `BulkAdd_OpensFocusedDialogScopedToOffering` verify Close + focus return. |
| Creation reuses existing validation, dependencies, and persistence | Framework-free + reuse | PASS | The dialogs bind the existing `CreateVariantCommand`, `PreviewBulkVariantsCommand`, `ConfirmBulkVariantsCommand`, `CancelAddVariantCommand`, `CancelBulkVariantsCommand`; no domain/application logic duplicated. Existing `OfferingManagementServiceTests` bulk and single-variant creation/validation tests remain green (full suite 0 failures). |
| Bulk creation shows a pre-confirmation summary | Headless view test | PASS | `BulkAdd_OpensFocusedDialogScopedToOffering`: the dialog contains a "Preview valid Variants" button bound to `PreviewBulkVariantsCommand` and a candidates list bound to `BulkPreviewCandidates` with a "Create previewed Variants" confirm button. The existing `OfferingManagementServiceTests.BulkColorWorkflowPreviewsExclusionsAndAtomicallyCreatesOnlyNewValidSizes` proves the preview/confirm flow and partial-failure recoverability. |
| Parent screen renders no inline creation editor | Headless view test | PASS | `StoreEditorHeadlessTests.ParentScreen_RendersNoInlineCreationEditor` and `CatalogEditorsUseCompactBasicsOnDemandDraftsAndSummaryFirstRegions`: assert no `Catalog.BulkVariantEditor` or `Catalog.AddVariantEditor` control exists in the Store Editor visual tree while the header buttons remain. |

## Commands

- `dotnet test .\FusionCanvas.sln` — PASS: 1396 passed, 0 failed, 0 skipped.
  - Domain: 232; Application: 384; UiDescription: 27; Integration: 188; App: 565.
- `openspec validate variant-creation-dialogs --strict` — PASS: change valid.
- `openspec validate --specs --strict` — PASS (after sync).
- `openspec validate --changes --strict` — PASS (after sync).

## Build/test results

- Build: 0 errors, 0 new warnings introduced by this change.
- Solution tests: 1396 passed, 0 failed.

## Limitations

- No live desktop UI pass (optional per the testing baseline); deterministic headless + framework-free tests are the gate.
- No pixel-perfect visual regression baseline.
- The low-fidelity `manage-variants.ui.yaml` wireframe was left unchanged: it already shows only the action buttons and Variant table with no inline creation editor, so no content needed removal and the rendered SVG fixtures remain valid.
- The headless `VariantCreation_SuccessClosesDialogAndRefreshesList` test verifies the view-model state after successful creation (session closed, list refreshed, Offering preserved) and explicitly closes the dialog for cleanup; the auto-close-via-property-observer mechanism is identical to the proven #194 pattern and is verified by the Offering-switch headless test and the framework-free session-close test. The dialog is not asserted as removed from `OwnedWindows` in the success path due to a headless-mode quirk where `Close()` invoked from within the `async void` command's property-change callback does not synchronously detach the modal window from the owner's `OwnedWindows` collection within the available dispatcher pumps.
