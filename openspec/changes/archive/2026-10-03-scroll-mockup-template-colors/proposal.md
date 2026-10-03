## Why

The Mockup Template editor renders every applicable Color as an unconstrained vertical list inside the selected-image editor. When an offering has many colors, that list expands beyond the available window height and pushes the Save and Cancel actions out of reach. The editor needs to keep the color choices usable while preserving access to the rest of the workflow.

## What Changes

- Constrain the Color applicability list to the editor's available height and provide a vertical scrollbar when the list is longer than that area.
- Keep the remaining selected-image metadata controls and the Mockup Template Save/Cancel actions reachable without requiring the window to grow with the number of colors.
- Preserve color selection, editing/read-only behavior, and existing draft/save semantics while scrolling.
- Add deterministic Avalonia headless coverage for a long color list and the continued visibility of the bottom actions.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `mockup-template-source-images`: clarify that the focused selected-image editor keeps large Color applicability collections in a scrollable region so essential metadata and confirmation actions remain reachable.

## Impact

- Affects the Avalonia Mockup Template editor layout in `src/FusionCanvas.App/Stores/MockupTemplateEditorWindow.axaml`.
- Adds/extends focused UI coverage in `tests/FusionCanvas.App.Tests/StoreEditorHeadlessTests.cs`.
- No domain, application, persistence, public API, migration, or dependency changes are required.
