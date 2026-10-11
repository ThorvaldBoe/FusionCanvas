## Why

The Assets window currently places every asset card in a non-virtualized `ItemsControl` inside a `ScrollViewer`, so a large collection creates UI elements for off-screen assets as well as visible ones. A thumbnail-first one-column virtualized list can reduce that realization cost while retaining the visual browsing and editing workflow creators already use.

## What Changes

- Keep the current horizontal asset row with thumbnail preview, name and managed filename, missing-file and context labels, purpose selector, and confirmed removal action.
- Virtualize asset rows within the existing focused Assets window and preserve the import, empty, loading, error, and removal-confirmation flows.
- Keep long names and file state accessible, preserve the current asset order, and leave search and sorting out of scope.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `asset-management`: Define the thumbnail-first one-column virtualized asset collection and preserve its per-asset information and actions.

## Impact

- Affected UI: `src/FusionCanvas.App/Assets/AssetsWindow.axaml` and its Avalonia view tests.
- No domain, application, persistence, data-model, or dependency changes are expected.
- The focused Assets window remains the surface for occasional asset administration; no persistent main-workspace area is added.
- Verification will include a headless view test for virtualization and existing asset actions, focused asset tests, the full solution baseline, and strict OpenSpec validation.
