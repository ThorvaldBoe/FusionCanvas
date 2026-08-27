# Tasks: Open Add Variant and Bulk add in focused dialogs

## 1. View-model: request events and Offering-switch guard
- [x] Add `AddVariantRequested` and `BulkVariantsRequested` events to `CatalogSetupViewModel`.
- [x] In `BeginVariantDraft(bool bulk)`, raise `AddVariantRequested` when `bulk` is false and `BulkVariantsRequested` when true; remove the `VariantEditorFocusRequested`/`BulkVariantEditorFocusRequested` raises.
- [x] Remove the `VariantEditorFocusRequested` and `BulkVariantEditorFocusRequested` events.
- [x] In the `SelectedOffering` setter, reset both variant drafts (`ResetVariantDraft()` + `ResetBulkDraft()` + `IsAddingBulkVariants = false`) when the Offering changes and (`IsAddingVariant` || `IsAddingBulkVariants`) is true, mirroring the `IsManagingOptionValues` guard.

## 2. Dialog windows: Add Variant
- [x] Create `src/FusionCanvas.App/Stores/AddVariantWindow.axaml` titled "Add Variant", `x:DataType="stores:CatalogSetupViewModel"`, `CenterOwner`, `SizeToContent="Height"`, `CanResize="False"`, `AutomationId="Catalog.AddVariantDialog"`, reusing the existing individual-creation markup (Variant name, Option Values checkboxes, Save/Cancel) and `OptionValueManagementWindow` styles.
- [x] Create `AddVariantWindow.axaml.cs`: focus Variant name on `Opened`; observe `IsAddingVariant` via `PropertyChanged` and `Close()` when false while visible; Escape closes; unsubscribe on `Closed`.

## 3. Dialog windows: Bulk add
- [x] Create `src/FusionCanvas.App/Stores/BulkAddVariantsWindow.axaml` titled "Bulk add", same window attributes, `AutomationId="Catalog.BulkAddVariantsDialog"`, reusing the existing bulk-creation markup (Color combo, Size checkboxes, Preview/Cancel, result message, candidates list, Create button).
- [x] Create `BulkAddVariantsWindow.axaml.cs`: focus Color combo on `Opened`; observe `IsAddingBulkVariants` and `Close()` when false while visible; Escape closes; unsubscribe on `Closed`.

## 4. Host wiring
- [x] In `StoreEditorWindow.axaml.cs`, add `OnAddVariantRequested` and `OnBulkVariantsRequested` handlers guarded by a single `_variantCreationDialogOpen` flag: open the dialog with `ShowDialog(this)`, then call `CancelAddVariantCommand`/`CancelBulkVariantsCommand` on return, clear the flag in `finally`.
- [x] Subscribe/unsubscribe `AddVariantRequested` and `BulkVariantsRequested` in `OnDataContextChanged`.
- [x] Remove `OnVariantEditorFocusRequested`/`OnBulkVariantEditorFocusRequested` handlers and subscriptions; keep `OnVariantActionsFocusRequested`/`OnBulkVariantActionFocusRequested`.

## 5. Parent screen: remove inline editors
- [x] In `StoreEditorWindow.axaml`, remove the inline individual-creation `StackPanel` (`IsVisible="{Binding CatalogSetup.IsAddingVariant}"`) and the inline bulk `Border` (`AutomationId="Catalog.BulkVariantEditor"`). Keep the `AddVariantButton`/`BulkAddVariantButton` header buttons.

## 6. Tests: framework-free
- [x] In `CatalogSetupViewModelTests.cs`, add tests: `AddVariantRequested` fires on `StartAddVariantCommand`; `BulkVariantsRequested` fires on `StartBulkVariantsCommand`; successful creation closes session and refreshes list; Offering switch closes both variant sessions and discards drafts.

## 7. Tests: Avalonia headless
- [x] Rewrite the bulk inline-editor assertions in `CatalogEditorsUseCompactBasicsOnDemandDraftsAndSummaryFirstRegions` to assert no inline `Catalog.BulkVariantEditor`/`Catalog.AddVariantEditor` and that Bulk add opens a dialog.
- [x] Add `AddVariant_OpensFocusedDialogScopedToOffering`, `BulkAdd_OpensFocusedDialogScopedToOffering`, `VariantCreation_AllowsOnlyOneDialogAtATime`, `VariantCreation_EscapeClosesAndDiscardsDraft`, `VariantCreation_OfferingSwitchClosesDialog`, `VariantCreation_SuccessClosesDialogAndRefreshesList`, `ParentScreen_RendersNoInlineCreationEditor`.

## 8. Verification gates
- [x] `dotnet build .\FusionCanvas.sln` — 0 errors, 0 new warnings.
- [x] `dotnet test .\FusionCanvas.sln` — 0 failures.
- [x] `openspec validate variant-creation-dialogs --strict` — valid.
- [x] Fill `verification.md` mapping every acceptance scenario to evidence.
