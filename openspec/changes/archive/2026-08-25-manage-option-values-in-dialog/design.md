# Design: Manage Option values in a focused dialog

## Conceptual design

Value management moves from an inline region of the Manage Variants page to a focused modal dialog owned by the Store Editor window. The dialog is scoped to one Option by stable identity, reuses all existing `CatalogSetupViewModel` value-management state and commands, and never duplicates domain or application logic.

### Dialog host pattern

The Store Editor is itself a window (`StoreEditorWindow`). The cleanest fit, and the one that matches existing code, is a modal child `Window` shown with `ShowDialog` from the Store Editor code-behind, exactly as #198 already does for `DesignAreaArchiveConfirmationWindow`. The view model raises a request event; the Store Editor window opens the dialog; when the dialog closes, the Store Editor resets the management session and returns focus to the originating **Manage values** control.

This keeps all view-model logic framework-free and unit-testable (the view model only raises events and manages the `IsManagingOptionValues` session flag), while the code-behind owns windowing and focus — the same separation the codebase already uses for Design Area archive confirmation.

### Option identity and stale-context guard

The dialog operates on the shared `CatalogSetupViewModel`, which already keys `AvailableValues` off `SelectedOffering?.Id` and `SelectedOption?.Id`. To guarantee the dialog cannot edit stale data:

- `BeginManageOptionValues` records the stable Option (and its Offering) on the view model.
- When `SelectedOffering` changes to a different Offering while `IsManagingOptionValues` is true, the view model closes the management session (resets `IsManagingOptionValues`, `IsAddingOptionValue`, and `OptionValue`). Because `LoadForStoreAsync` (workspace switch) and `ApplyCatalog` route through `SelectedOffering`, this single guard covers both Offering and workspace switches.
- The Store Editor window observes the session flag; if `IsManagingOptionValues` becomes false while the dialog is open (e.g., a programmatic context switch), it closes the dialog.

### Draft discard and focus return

Every close path (Done, Cancel, system close, Escape, context switch) routes through one reset on the host after the dialog closes, which clears `IsAddingOptionValue`/`OptionValue` (discarding the unfinished draft) and raises the existing `OptionChoiceFocusRequested` event. The Store Editor's existing `OnOptionChoiceFocusRequested` handler already locates the originating **Manage values** button by stable Option identity and focuses it, so focus return is reused without new logic.

### Single-dialog guard

The Store Editor tracks a `_optionValueManagementOpen` flag (mirroring `_designAreaArchiveConfirmationOpen`). While true, additional `OptionValueManagementRequested` events are ignored, satisfying "only one dialog at a time."

### Custom Option kinds

The dialog title is data-bound to a `ManageOptionValuesDialogTitle` view-model property computed from `SelectedOption?.Name`. No kind-specific screens exist; the same dialog serves Color, Size, and any custom Option kind.

## Implementation plan

### Affected layers and files

- **App (view model)**: `src/FusionCanvas.App/Stores/CatalogSetupViewModel.cs`
  - Add `OptionValueManagementRequested` event; raise it from `BeginManageOptionValues`.
  - Add `ManageOptionValuesDialogTitle` computed property (`"Manage {SelectedOption?.Name} values"`; emit change on `SelectedOption` change).
  - Stop raising `OptionValueEditorFocusRequested` from `BeginManageOptionValues` (the dialog code-behind handles initial focus on `Opened`); keep raising it from `BeginAddOptionValue` so the dialog focuses the value text box mid-session.
  - In the `SelectedOffering` setter, when the Offering changes and `IsManagingOptionValues` is true, reset the management session (`IsManagingOptionValues = false`, `IsAddingOptionValue = false`, `OptionValue = string.Empty`) without raising `OptionChoiceFocusRequested` (focus is irrelevant during a context switch).
  - Keep `CloseOptionValueManagement` (used by the host after the dialog closes) and `CancelActiveDrafts` (already resets `IsManagingOptionValues`).
- **App (host window)**: `src/FusionCanvas.App/Stores/StoreEditorWindow.axaml.cs`
  - Add `OnOptionValueManagementRequested` handler: guard `_optionValueManagementOpen`; create `OptionValueManagementWindow { DataContext = catalog }`; `await dialog.ShowDialog(this)`; after it returns, invoke `catalog.CloseOptionValueManagementCommand.Execute(null)` (resets draft + session + raises focus event); clear the flag.
  - Subscribe/unsubscribe `OptionValueManagementRequested` in `OnDataContextChanged` alongside the existing catalog events.
  - Observe `catalog.PropertyChanged` for `IsManagingOptionValues`; if it becomes false while the dialog is open, close the dialog (covers context-switch closure).
  - Remove the inline `OnOptionValueEditorFocusRequested` focusing of `OptionValueDoneButton`/`OptionValueTextBox` (those named controls move to the dialog); keep the handler only to forward to the dialog if still subscribed, or remove the subscription and let the dialog own it (see below).
- **App (dialog window)**: new `src/FusionCanvas.App/Stores/OptionValueManagementWindow.axaml` + `.axaml.cs`
  - `Window` with `x:DataType="stores:CatalogSetupViewModel"`, `Title` bound to `ManageOptionValuesDialogTitle`, `ShowActivated`, `WindowStartupLocation="CenterOwner"`, fixed-ish `Width` and `SizeToContent="Height"`, `CanResize="False"`.
  - Content: the existing value editor markup (Option name, Done button, empty-state text, values list with **Archive Option Value** buttons, **Add Option Value** + draft TextBox + Save/Cancel). Reuse the same command bindings (`StartAddOptionValueCommand`, `CreateOptionValueCommand`, `CancelAddOptionValueCommand`, `ArchiveOptionValueCommand`, `CloseOptionValueManagementCommand`).
  - Code-behind: on `Opened`, focus the Done button; subscribe to `OptionValueEditorFocusRequested` on the DataContext to focus the value text box when an add-value draft starts; handle Escape to close; handle `Closing`/`Closed` to ensure the host resets the session.
  - Done button uses a click handler that closes the window (the host then runs `CloseOptionValueManagementCommand`). This guarantees Done and Cancel/Escape share one reset path and the draft is always discarded.
- **App (parent markup)**: `src/FusionCanvas.App/Stores/StoreEditorWindow.axaml`
  - Remove the inline `<Border IsVisible="{Binding CatalogSetup.IsManagingOptionValues}" ... AutomationProperties.AutomationId="Catalog.OptionValueEditor">` block (lines ~800–832). The **Manage values** button and its command binding (`ManageOptionCommand`) stay on the card.
- **UI description**: `docs/Visuals/ui-descriptions/manage-variants.ui.yaml`
  - The wireframe currently shows **Manage colors**/**Manage sizes** opening nothing; it does not describe an inline value editor. Add a note or state that **Manage values** opens a focused dialog (keep the wireframe low-fidelity; no inline editor to remove here).
- **Tests**:
  - `tests/FusionCanvas.App.Tests/CatalogSetupViewModelTests.cs`: add framework-free tests for `ManageOptionValuesDialogTitle`, the `OptionValueManagementRequested` event on `ManageOptionCommand`, Offering-switch closes the session and discards the draft, and close discards the draft.
  - `tests/FusionCanvas.App.Tests/StoreEditorHeadlessTests.cs`: rewrite the inline-editor assertions in `CatalogEditorsUseCompactBasicsOnDemandDraftsAndSummaryFirstRegions` to assert dialog open/close, title, focus, and draft discard; add dedicated dialog headless tests (open via **Manage values**, title, Done closes + focus returns, Escape discards draft, only one dialog, custom Option kind, Offering switch closes).

### Sequencing

1. View-model changes (event, title property, Offering-switch guard).
2. New dialog window (axaml + code-behind) reusing existing commands.
3. Host wiring (open on request, close on session end/context switch, focus return).
4. Remove inline editor from parent axaml.
5. UI description reconciliation.
6. Framework-free tests, then headless tests.
7. Build + `dotnet test .\FusionCanvas.sln` + strict OpenSpec validation.

### Decisions not to reopen

- Modal `ShowDialog` owned by the Store Editor window is the dialog host pattern (not an in-window overlay). It matches the existing #198 pattern and keeps view-model logic framework-free.
- The dialog reuses the shared `CatalogSetupViewModel` as its DataContext (same as the Design Area confirmation dialog). No separate dialog view model is introduced.
- Done and Cancel/Escape share one host-side reset path so the unfinished draft is always discarded.
- Focus return reuses the existing `OptionChoiceFocusRequested` handler that locates **Manage values** by Option identity.

### Edge cases

- Opening **Manage values** while a dialog is already open: ignored by the host flag.
- Archiving a value referenced by a Variant/Design Area: handled by the existing `RunArchive`/dependency safeguards (unchanged).
- Successful add/archive: `ApplyCatalog`/`ApplyOfferingState` already call `RefreshOfferingCollections`, which rebuilds `AvailableChoiceGroups` (value summaries) and `SellableVariantRows`. The dialog stays open for further edits until Done.
- Offering/workspace switch mid-dialog: the `SelectedOffering` guard resets the session; the host observes the flag and closes the dialog; no stale edits.
