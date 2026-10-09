## Context

The focused Mockup Template editor currently renders `LocalSourceDrafts` as custom repeated `Border` rows. It has external sort buttons, row-level Archive buttons, variable-height preview warnings, and custom pointer/keyboard routing into `CatalogSetupViewModel`'s transient multi-selection state. The lower editor binds to `SelectedLocalSource` and remains the authoritative detail-editing surface.

FusionCanvas already carries the App-layer `AvaloniaVirtualDataGrid` trial dependency. Its existing integration uses `InMemoryDataProvider<T>` snapshots: the grid's panel requires an `IDataProvider`, fixed row heights require a compact warning presentation, and the grid's tunneling pointer handling can conflict with interactive cell content. The current view-model and headless tests already protect sort semantics, active-selection behavior, and the main selection gestures.

## Goals / Non-Goals

**Goals:**

- Replace the repeated source-image rows with a virtual grid in the same focused editor region.
- Preserve existing draft, sorting, transient selection, keyboard, archive, incomplete-state, and master-detail behavior.
- Keep preview-read warnings visible and their full explanation available in a compact row.
- Keep provider adaptation and grid input handling inside the App presentation boundary.
- Verify layout and interaction behavior with deterministic Avalonia headless tests.

**Non-Goals:**

- Add search, paging, new filtering, column resizing, drag-reordering, or data editing in grid cells.
- Change upload, source metadata, archive persistence, template readiness, revisioning, or file storage.
- Claim a measured performance improvement or add Appium coverage for this presentation refactor.
- Replace the existing selection model or make the grid's native selection state authoritative.

## Decisions

### Keep source rows and selection state in `CatalogSetupViewModel`

`LocalSourceDrafts`, `SelectedLocalSources`, `SelectedLocalSource`, and the existing commands remain the source of truth. Bind row templates to `LocalMockupSourceDraftViewModel`; retain its `IsSelected`, `IsAlternateRow`, display, applicability, status, and warning properties. This keeps editing and archive state independent of grid realization and avoids moving business or draft behavior into the control.

**Alternative considered:** Bind the grid's native selected item or selected-items collection directly to the draft. Rejected because the existing UI supports distinct active-row and multi-selection state, and grid virtualization/recycling must not own that contract.

### Adapt the draft collection to a grid data provider in the window

`MockupTemplateEditorWindow` should own an `InMemoryDataProvider<LocalMockupSourceDraftViewModel>` snapshot and bind it to the grid, following `BulkAddVariantsWindow`. Subscribe to the current view model's `LocalSourceDrafts.CollectionChanged`; reset the provider from the complete current ordered collection after insert, remove, and move notifications. Detach the old collection when `DataContext` changes and detach all subscriptions when the window closes. Do not use the provider's add/remove paths or grid-native sorting: the trial documented that they can leave its original-items sorting snapshot stale.

**Alternative considered:** Add the grid provider to `CatalogSetupViewModel`. Rejected because that couples draft workflow state to a particular view control; the window already owns grid lifetime adaptation for the existing trial.

### Preserve custom sort headings and delegate ordering to the existing command

Keep File, Applicability, and Status headings as accessible buttons aligned to the fixed grid columns. They continue to invoke `SortLocalSourcesCommand`; the existing view model orders `LocalSourceDrafts` and controls the sort direction. Disable or make the grid's built-in header interaction inert, and disable native sorting, resizing, and header drag reordering. Rebuilding the provider snapshot after collection moves makes the grid reflect the view-model order.

**Alternative considered:** Enable native grid sorting. Rejected because it would create a second sort authority and rely on the provider's documented sorting limitations.

### Preserve custom row selection and route input to existing commands

Set grid selection mode to `None`. Adapt the existing row pointer and Enter/Space handlers to identify a realized `VirtualDataRow` and its `LocalMockupSourceDraftViewModel`, then call the existing plain-selection or modifier-selection path. Keep row-level Archive as an explicit action, and ensure clicking it does not also activate or toggle the row. Keep focused-row Enter/Space and Ctrl/Shift equivalents. If the grid consumes an input before the window's current tunneling handler can process it, add a narrowly scoped grid-level routed handler in the view code-behind; do not duplicate selection rules.

**Alternative considered:** Use built-in extended selection. Rejected because the current contract distinguishes the most recently activated selected row (the active detail row) from the set of selected rows, and requires modifier behavior in addition to visible highlighting.

### Use fixed-height rows with a visible warning affordance

Render File, Applicability, Status, and Action in four aligned columns. Keep row height fixed and text compact. When `HasPreviewReadError` is true, show a visible warning glyph in the status area; bind the full `PreviewReadError` to its tooltip and automation help/name. Keep selected-row styling, alternating rows, ellipsis, and full applicability text on hover. Do not rely on a tooltip alone to signal that a warning exists.

**Alternative considered:** Keep the second warning line by allowing variable-height rows. Rejected because the grid uses fixed row heights. Moving the entire warning only into the detail editor is also rejected because an unselected warning would become undiscoverable.

## Risks / Trade-offs

- [The grid can intercept row or child-button pointer input] → Headless tests cover plain click, Ctrl/Shift click, row Enter/Space, and Archive-button isolation against the actual grid visual tree.
- [A provider snapshot can become stale after list mutations or sorting] → Observe collection changes, reset from the complete ordered collection, and test add, move/sort, archive/remove, DataContext replacement, and window close.
- [Fixed-width columns can truncate long content] → Retain ellipsis and tooltips for ordinary summaries; warning glyphs carry full accessible text and tooltip, and column widths remain aligned at the dialog's minimum size.
- [Virtualization may mean not all rows exist in the visual tree simultaneously] → Tests assert the provider's complete row set and scroll to rows outside the initial viewport rather than assuming all rows are realized.
- [The component's own theme may not fully match FusionCanvas tokens] → Apply existing resource brushes and selected/alternate row treatments, verify light/dark headless rendering, and treat native visual review as optional supplemental evidence.

## Migration Plan

No data or user-data migration is required. The change is limited to App XAML/code-behind and App tests. Rollback consists of restoring the existing ItemsControl table markup and handlers; no persisted state or external API is affected.

## Open Questions

None. The preview warning is visibly signaled in-row and exposes full text through both tooltip and automation help/name. Existing selection and sorting semantics are the accepted behavior to preserve.

## Implementation Plan

### Affected layers and responsibilities

- **App view:** Replace `MockupSourceTableBorder`'s `ItemsControl`/`ScrollViewer` with `VirtualDataGrid` and four template columns in `src/FusionCanvas.App/Stores/MockupTemplateEditorWindow.axaml`. Keep the external, accessible sort headings aligned with the grid columns. Retain the upload, coverage, empty, selected-image editor, and dialog actions in their current positions.
- **App view code-behind:** In `MockupTemplateEditorWindow.axaml.cs`, own and bind an `InMemoryDataProvider<LocalMockupSourceDraftViewModel>`, attach/detach it to the active `CatalogSetupViewModel.LocalSourceDrafts`, refresh snapshots on collection mutations, and route grid row pointer/keyboard input through the existing commands. Keep Archive button input separate from row selection. Preserve window close and DataContext lifetime behavior.
- **App view model:** Keep `CatalogSetupViewModel` as owner of row ordering, selection, sort direction, and archive command behavior. Reuse `SortLocalSourcesCommand`, `SelectLocalSourceCommand`, and `SelectLocalSourceWithModifiers`; change `LocalMockupSourceDraftViewModel` only if a small presentation property is needed to expose warning accessibility text or status glyph state.
- **Domain/Application/Integration:** No changes. No persisted state, provider contract, file lifecycle, or revision behavior changes.

### Sequencing and edge cases

1. Add or refine focused tests around current view-model selection/sort behavior only if needed to name a gap; preserve the existing tests as behavior-level coverage.
2. Build the four grid columns and fixed row template, including compact warning glyph, tooltip/help text, selected and alternate states, and ellipsis for long labels.
3. Add provider snapshot ownership and collection/DataContext/window lifecycle handling. Treat source collection order as authoritative after sort; reset the provider as a complete snapshot.
4. Bridge realized row pointer and keyboard events into existing selection methods. Avoid applying selection for child archive button events. Preserve active row after sort and multi-selection after archive reconciliation.
5. Update focused headless view tests; validate empty/no-warning/warning states, sorted order, active and multiple selected rows, all keyboard modifier variants, Archive button isolation, scroll/virtualization, DataContext rebinding, and detach-on-close.

### Compatibility and decisions not to reopen

- The existing `LocalSourceDrafts` collection and selection commands stay authoritative.
- File, Applicability, and Status sort through the existing command and ordering implementation; Action is not sortable.
- Grid-native sorting, editing, resizing, drag-reordering, paging, and selection stay disabled.
- Use the existing bundled grid DLL and styles. Do not add a package or replace the trial dependency in this module.
- Keep all mutation and persistence in the existing save/archive paths. No schema migration or compatibility handling is needed.
- Do not create an Appium journey. Headless Avalonia coverage is the deterministic verification for routed input, focus, selection, and visual-tree behavior; a live visual check may supplement it but is not a gate.

### Acceptance-to-verification plan

| Acceptance scenario | Planned verification |
| --- | --- |
| Creator sorts source-image rows | Existing `CatalogSetupViewModelTests` sort/selection coverage plus an Avalonia headless test that activates headings by pointer and keyboard, checks rendered order and accessible direction, and confirms selection/active detail remain unchanged. |
| Source image preview cannot be read | Avalonia headless test using a draft with `PreviewReadError`; assert visible warning affordance, full tooltip and automation help/name, and fixed row height. |
| User selects multiple source images / contiguous range | Existing view-model tests plus headless grid input tests for plain, Ctrl, Shift, Ctrl+Shift pointer and Enter/Space behavior, visible `IsSelected`, active detail row, and selected count. |
| User archives selected source images | Existing view-model archive test plus headless click on Archive; assert the action does not also toggle selection and that the selected/visible replacement row is active afterward. |
| Creator uploads images independently / selects an image row | Existing upload and draft tests plus headless bindings test that grid row activation selects the row while the lower editor remains bound to that draft. |
| Empty and incomplete states remain clear | Existing source-image tests plus a headless empty-state test and incomplete status rendering assertion. |
| Grid stays current through collection changes and lifetime | Headless tests for add, sort/move, remove/archive, DataContext replacement, scroll realization, and provider detachment on close. |

After implementation, run `dotnet test .\\FusionCanvas.sln -m:1` and `openspec validate --specs --strict --no-interactive`; record criterion-level results in the change verification record before completion.
