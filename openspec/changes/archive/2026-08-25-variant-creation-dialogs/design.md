# Design: Open Add Variant and Bulk add in focused dialogs

## Conceptual design

Variant creation moves from two inline regions below the Sellable Variants list to two focused modal dialogs owned by the Store Editor window. **Add Variant** opens a dialog titled "Add Variant" for creating one concrete sellable combination; **Bulk add** opens a dialog titled "Bulk add" for the color-plus-valid-sizes workflow. Both reuse the shared `CatalogSetupViewModel` creation state and commands, and never duplicate domain or application logic.

### Dialog host pattern

The Store Editor is itself a window (`StoreEditorWindow`). The established pattern (from #198 Design Area archive confirmation and #194 Option value management) is a modal child `Window` shown with `ShowDialog` from the Store Editor code-behind. The view model raises a request event; the Store Editor window opens the dialog; when the dialog closes, the Store Editor resets the creation session and returns focus to the originating header action.

This keeps all view-model logic framework-free and unit-testable (the view model only raises events and manages the `IsAddingVariant`/`IsAddingBulkVariants` session flags), while the code-behind owns windowing and focus — the same separation the codebase already uses for Option value management and Design Area archive confirmation.

### Offering identity and stale-context guard

Both dialogs operate on the shared `CatalogSetupViewModel`, which already keys `AvailableColors`, `BulkSizeChoices`, `VariantValueChoices`, and `AvailableVariants` off `SelectedOffering?.Id`. To guarantee a dialog cannot edit stale data:

- `BeginVariantDraft` sets `IsAddingVariant` or `IsAddingBulkVariants` and raises the corresponding request event.
- When `SelectedOffering` changes to a different Offering while either session flag is true, the view model resets both variant drafts (clearing the flags, the draft fields, and the bulk preview). Because `LoadForStoreAsync` (workspace switch) and `ApplyCatalog` route through the `SelectedOffering` setter, this single guard covers both Offering and workspace switches.
- Each dialog observes its session flag; if it becomes false while the dialog is open (e.g., a programmatic context switch), the dialog closes.

### Draft discard and focus return

Every close path (Save success, Cancel, system close, Escape, context switch) routes through one reset on the host after the dialog closes. The host calls the existing `CancelAddVariantCommand` (individual) or `CancelBulkVariantsCommand` (bulk), which clears the draft fields/preview, resets the session flag, and raises the existing `VariantActionsFocusRequested` or `BulkVariantActionFocusRequested` event. The Store Editor's existing handlers focus the `AddVariantButton` or `BulkAddVariantButton` by name, so focus return is reused without new logic. On Save success, `CreateVariantAsync`/`ConfirmBulkVariantsAsync` already reset the draft, which sets the session flag to false; the dialog observes this and closes; the host's subsequent reset call is a no-op for the flag but still triggers focus return.

### Single-creation-dialog guard

The Store Editor tracks a single `_variantCreationDialogOpen` flag. While true, additional creation-request events are ignored, satisfying "only one creation dialog may be open at a time" across both types. The view model's `BeginVariantDraft` already guarantees the two sessions are mutually exclusive (each resets the other), and the modal nature of `ShowDialog` blocks the parent, but the host flag makes the guarantee explicit and testable.

### Bulk pre-confirmation summary and partial failure

The bulk dialog reuses the existing preview/confirm flow unchanged: the user picks a Color and Sizes, **Preview valid Variants** calls `PreviewBulkVariantsCommand`, the view model shows `BulkPreviewCandidates` (each candidate shows whether it will be created and any exclusion reason such as "already exists" or "does not allow"), then **Create previewed Variants** calls `ConfirmBulkVariantsCommand`. On success the dialog closes and the list refreshes; on failure the dialog stays open showing the recoverable `BulkResultMessage` and confirmed data stays consistent (the existing atomic-create semantics guarantee no partial save).

## Implementation plan

### Affected layers and files

- **App (view model)**: `src/FusionCanvas.App/Stores/CatalogSetupViewModel.cs`
  - Add `AddVariantRequested` and `BulkVariantsRequested` events.
  - In `BeginVariantDraft(bool bulk)`, raise `AddVariantRequested` when `bulk` is false and `BulkVariantsRequested` when `bulk` is true. Stop raising `VariantEditorFocusRequested` and `BulkVariantEditorFocusRequested` (those inline-only focus events are removed; the dialogs handle initial focus on `Opened`).
  - Remove the `VariantEditorFocusRequested` and `BulkVariantEditorFocusRequested` events and their raises (they referenced inline-only controls and are no longer needed).
  - In the `SelectedOffering` setter, when the Offering changes and (`IsAddingVariant` or `IsAddingBulkVariants`) is true, reset both variant drafts (`ResetVariantDraft()` and `ResetBulkDraft()`) without raising focus events (focus is irrelevant during a context switch). Mirror the existing `IsManagingOptionValues` guard.
  - Keep `CancelAddVariantCommand` (resets draft + raises `VariantActionsFocusRequested`) and `CancelBulkVariantsCommand` (resets bulk draft + resets flag + raises `BulkVariantActionFocusRequested`) as the host's single reset path.
  - Keep `CreateVariantAsync` (resets draft on success → closes dialog via the flag observer) and `ConfirmBulkVariantsAsync` (resets bulk draft on success → closes dialog).
- **App (host window)**: `src/FusionCanvas.App/Stores/StoreEditorWindow.axaml.cs`
  - Add `OnAddVariantRequested` and `OnBulkVariantsRequested` handlers mirroring `OnOptionValueManagementRequested`: guard `_variantCreationDialogOpen`; create the dialog `{ DataContext = catalog }`; `await dialog.ShowDialog(this)`; after it returns, invoke `catalog.CancelAddVariantCommand.Execute(null)` or `catalog.CancelBulkVariantsCommand.Execute(null)` (resets draft + session + raises focus event); clear the flag in `finally`.
  - Subscribe/unsubscribe `AddVariantRequested` and `BulkVariantsRequested` in `OnDataContextChanged`.
  - Remove the `OnVariantEditorFocusRequested` and `OnBulkVariantEditorFocusRequested` handlers and their subscriptions (the inline named controls they targeted are removed); keep `OnVariantActionsFocusRequested` (focuses `AddVariantButton`) and `OnBulkVariantActionFocusRequested` (focuses `BulkAddVariantButton`).
- **App (dialog windows)**: new `src/FusionCanvas.App/Stores/AddVariantWindow.axaml` + `.axaml.cs` and `src/FusionCanvas.App/Stores/BulkAddVariantsWindow.axaml` + `.axaml.cs`
  - `AddVariantWindow`: `Window` with `x:DataType="stores:CatalogSetupViewModel"`, `Title="Add Variant"`, `ShowActivated="True"`, `WindowStartupLocation="CenterOwner"`, `Width="420"`, `SizeToContent="Height"`, `CanResize="False"`, `AutomationProperties.AutomationId="Catalog.AddVariantDialog"`. Content: the existing individual-creation markup (Variant name TextBox, Option Values checkboxes bound to `VariantValueChoices`, Save/Cancel buttons bound to `CreateVariantCommand`/`CancelAddVariantCommand`, error message). Reuse the same command bindings and styles as `OptionValueManagementWindow`.
  - `BulkAddVariantsWindow`: `Window` with the same attributes, `Title="Bulk add"`, `AutomationProperties.AutomationId="Catalog.BulkAddVariantsDialog"`. Content: the existing bulk-creation markup (Color ComboBox bound to `AvailableColors`/`BulkColor`, Size checkboxes bound to `BulkSizeChoices`, Preview/Cancel buttons, `BulkResultMessage`, `BulkPreviewCandidates` list, Create button bound to `ConfirmBulkVariantsCommand`).
  - Each code-behind: on `Opened`, focus the primary input (Variant name TextBox for Add Variant; Color ComboBox for Bulk add); subscribe to `DataContext` change to observe the session flag (`IsAddingVariant` / `IsAddingBulkVariants`) via `PropertyChanged` and `Close()` when it becomes false while visible; handle Escape to `Close()`; unsubscribe on `Closed`. Mirror `OptionValueManagementWindow.axaml.cs`.
- **App (parent markup)**: `src/FusionCanvas.App/Stores/StoreEditorWindow.axaml`
  - Remove the inline individual-creation `<StackPanel IsVisible="{Binding CatalogSetup.IsAddingVariant}" ...>` block (Variant name TextBox, Option Values checkboxes, Save/Cancel).
  - Remove the inline bulk-creation `<Border Classes="panel" ... IsVisible="{Binding CatalogSetup.IsAddingBulkVariants}" AutomationProperties.AutomationId="Catalog.BulkVariantEditor">` block.
  - The **Add Variant** and **Bulk add** header buttons (named `AddVariantButton` / `BulkAddVariantButton`) and their command bindings stay.
- **UI description**: `docs/Visuals/ui-descriptions/manage-variants.ui.yaml` — no change. The wireframe shows the `add-variant` and `bulk-add` action buttons and the Variant table but no inline creation editor, so no content needs removal and the golden SVG fixtures remain valid.
- **Tests**:
  - `tests/FusionCanvas.App.Tests/CatalogSetupViewModelTests.cs`: add framework-free tests for `AddVariantRequested`/`BulkVariantsRequested` firing on the add/bulk commands, and Offering-switch closing both variant sessions and discarding drafts.
  - `tests/FusionCanvas.App.Tests/StoreEditorHeadlessTests.cs`: rewrite the inline-editor assertions in `CatalogEditorsUseCompactBasicsOnDemandDraftsAndSummaryFirstRegions` to assert no inline creation editor and dialog open/close for Bulk add; add dedicated dialog headless tests (Add Variant opens titled dialog + focus; Bulk add opens titled dialog + focus; only one creation dialog at a time; Escape discards draft + focus returns; Offering switch closes dialog; successful creation closes dialog and refreshes list; parent renders no inline editor).

### Sequencing

1. View-model changes (request events, remove inline-only focus events, Offering-switch guard).
2. New dialog windows (axaml + code-behind) reusing existing commands and styles.
3. Host wiring (open on request, close on session end/context switch, focus return, single-dialog guard).
4. Remove inline editors from parent axaml.
5. Framework-free tests, then headless tests.
6. Build + `dotnet test .\FusionCanvas.sln` + strict OpenSpec validation.

### Decisions not to reopen

- Modal `ShowDialog` owned by the Store Editor window is the dialog host pattern (not an in-window overlay). It matches the #198/#194 pattern and keeps view-model logic framework-free.
- Both dialogs reuse the shared `CatalogSetupViewModel` as their DataContext (same as the Option-value and Design Area dialogs). No separate dialog view models are introduced.
- Cancel/Escape/Save-success share one host-side reset path so the in-progress draft is always discarded and focus always returns to the opening action.
- A single `_variantCreationDialogOpen` host flag enforces "only one creation dialog at a time" across both types.
- The low-fidelity `manage-variants.ui.yaml` wireframe is not modified; it already omits inline creation editors.

### Edge cases

- Opening a creation action while a creation dialog is already open: ignored by the host flag (and blocked by the modal).
- Duplicate, cross-Offering, or incomplete combination: handled by the existing `CreateVariantAsync`/`PreviewBulkVariantsAsync`/`ConfirmBulkVariantsAsync` validation and error surfacing (unchanged). The dialog stays open showing the error.
- Successful creation: `CreateVariantAsync`/`ConfirmBulkVariantsAsync` reset the draft, the flag becomes false, the dialog closes, and the host focuses the opening action. `ApplyCatalog`/`ApplyOfferingState` already call `RefreshOfferingCollections`, which rebuilds `SellableVariantRows` and the count.
- Offering/workspace switch mid-dialog: the `SelectedOffering` setter guard resets the session; the dialog observes the flag and closes; no stale edits.
- Bulk partial failure: the existing atomic-create semantics leave confirmed data consistent; the dialog stays open with `BulkResultMessage` guidance.
