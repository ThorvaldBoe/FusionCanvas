## Why

Window placement persistence is currently implemented through scattered per-window attachments and close handlers. This has already caused repeated regressions where a moved dialog is saved for one surface but not another; an app-wide pattern will make the behavior consistent and make newly added non-transient windows opt in through one clearly testable boundary.

## What Changes

- Introduce one application-owned registration/lifecycle pattern for non-transient window geometry.
- Apply consistent persistence of normal-state position and size while a registered window is open and when it closes.
- Use native window coordinates when the platform exposes them, with Avalonia geometry as the fallback.
- Centralize stable window identities, screen-safe restoration, validation, maximized/fullscreen handling, and save-failure tolerance.
- Refactor existing Settings, Workspace Management, Store Editor, Assets, Ideation, document/tool editors, and other registered windows to use the shared pattern.
- Keep transient confirmation and selection dialogs explicitly excluded.
- Add coverage that verifies registration completeness, lifecycle capture, restoration, close re-entry safety, and per-window key isolation.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `window-layout-persistence`: make the existing secondary-window placement requirement an app-wide registration contract with consistent capture and restoration for every non-transient window.

## Impact

- Affected layers: `FusionCanvas.App` window creation/lifecycle code and focused Avalonia/headless tests; application settings contracts and JSON persistence remain the storage boundary.
- Likely affected types include `WindowGeometryPersistence`, `WindowLayoutKeys`, `MainWindow`, `IdeationWindow`, `StoreEditorWindow`, and secondary-window factories/owners.
- No settings format break is intended; existing per-window geometry keys remain compatible.
- No new user-facing controls are required. The primary workflow is ordinary repeated opening, moving, resizing, and closing of focused management/tool windows; transient confirmation dialogs remain default-placed.
- Verification combines deterministic unit/headless tests, strict OpenSpec validation, and an optional live Windows smoke check for native coordinate capture.
