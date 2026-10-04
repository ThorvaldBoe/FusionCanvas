# Design

## Functional design

`CatalogSetupViewModel` remains the owner of the session-only local source-image selection. `SelectedLocalSource` remains the active row used by the existing detail editor. A separate ordered selection set tracks all selected drafts and exposes the selected count and whether the archive action is available. The selection anchor is the row used by Shift range selection; the visible order is the current sorted `LocalSourceDrafts` order.

Plain selection replaces the set. Ctrl selection toggles one row without losing the active editor context. Shift selection selects the inclusive range from the anchor to the clicked row. A modifier gesture updates the active row to the clicked row. Sorting preserves selected draft references and the active row. Removing a selected row reconciles the selection and chooses the last remaining selected row, then a remaining row, as active.

The existing archive service calls remain one request per source image during save. The command gathers the selected drafts and removes them from the visible draft collection; managed rows are queued in the existing archive list, while new rows are simply removed from the draft. The detail editor continues to bind only to the active row, so bulk selection does not invent bulk metadata semantics.

The table communicates multi-selection through selected-row styling, a compact selected-count label, and an archive command enabled when one or more rows are selected. Rows remain keyboard reachable and expose selection state through accessibility metadata.

## Implementation Plan

1. Add a small session-only selection state in `CatalogSetupViewModel` and notification properties for selected count, selection summary, and archive availability. Reconcile it when rows are loaded, sorted, added, or removed.
2. Extend source-row pointer and keyboard handlers in `MockupTemplateEditorWindow` to pass Ctrl/Shift intent while preserving child action behavior and the existing active-row editor.
3. Bind the table action/count presentation in `MockupTemplateEditorWindow.axaml` and preserve the current layout and other commands.
4. Add framework-free selection/action tests in `CatalogSetupViewModelTests` and Avalonia headless tests in `StoreEditorHeadlessTests`.
5. Update this change's task and verification artifacts, run strict validation and the required solution baseline.

## Decisions not to reopen

- Ctrl and Shift are the modifier gestures; plain click remains the compatibility path.
- The active row is always the last clicked/toggled row and is the only row shown in the detail editor.
- Only the existing archive action is bulk-enabled; metadata, mapping, upload, sorting, and mapping reuse remain single-row operations.
- Selection is transient and is not persisted.

## Acceptance-to-verification mapping

| Acceptance scenario | Verification |
| --- | --- |
| Replace/toggle/range gestures select expected rows | `CatalogSetupViewModelTests` selection tests and `StoreEditorHeadlessTests` modifier gesture test |
| Active row continues to drive the detail editor | Existing selected-source editor tests plus the headless modifier test |
| Archive selected targets all selected rows and preserves unselected rows | View-model archive/action test and headless command-state test |
| Keyboard/accessibility state is discoverable | Headless test checks row focus, item status, selected count, and archive action metadata |
