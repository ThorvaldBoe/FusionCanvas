## Why

The Mockup Template editor's source-image list is a hand-built table whose rows can grow to show preview-read warnings. A virtual grid is a better fit for a potentially long, dense list, provided the current sorting, multi-selection, keyboard access, archive actions, warning visibility, and selected-image editing workflow remain intact.

## What Changes

- Replace the hand-built source-image table presentation with the existing `VirtualDataGrid` in the focused Mockup Template editor.
- Preserve the existing file, applicability, and status sort actions; transient Ctrl/Shift multi-selection; keyboard row activation; archive behavior; and active-row detail editing.
- Keep preview-read warnings visibly indicated in the compact row and expose their full text accessibly without relying on variable-height rows.
- Keep the existing upload, incomplete-configuration, and empty states, and leave the lower selected-image editor and persistence workflow authoritative.
- Add focused Avalonia headless coverage for grid rendering, input routing, selection, sorting, warning access, collection refresh, and archive outcomes.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `mockup-template-source-images`: Specify sortable source-image columns and visible, accessible preview-read warning details in the focused editor table.

The existing `mockup-template-management` multi-selection requirement remains the acceptance authority for selection and keyboard semantics; this module preserves it without changing that contract.

## Impact

- Affects `MockupTemplateEditorWindow.axaml` and its code-behind, plus focused App tests; existing `CatalogSetupViewModel` sort, selection, and archive behavior remains authoritative.
- Uses the existing App-layer `AvaloniaVirtualDataGrid` trial dependency and `InMemoryDataProvider<T>`; no Domain, Application, Integration, schema, or file-storage changes are expected.
- The grid has fixed row heights, requires an `IDataProvider`, and handles pointer input at the grid level. These constraints make warning presentation, provider refresh, and interactive row-action routing the main implementation risks.
- Primary workflow: creators upload and configure source images in the focused Mockup Template editor. Upload and archive are occasional setup actions; selecting rows and reviewing applicability/status are repeated during that setup session. The existing modal editor remains the surface, and the grid does not consume space in the primary workspace.
- Empty, incomplete, warning, read-only, selection, and unsaved-draft outcomes remain governed by current behavior. No new user journey or Appium pack is warranted; deterministic headless view tests cover the meaningful Avalonia input and visual-tree risks, with existing view-model tests retaining behavior-level coverage.
- This is one reviewable presentation module: it changes one dense table while relying on existing draft, selection, archive, and persistence owners. Search, paging, new filtering, data-model changes, and performance claims are out of scope.
