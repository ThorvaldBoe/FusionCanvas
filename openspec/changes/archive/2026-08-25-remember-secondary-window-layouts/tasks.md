## 1. Settings contract and compatibility

- [x] 1.1 Add `WindowGeometrySettings` record (position and size) to `FusionCanvas.Application/Settings`.
- [x] 1.2 Add `IWindowGeometryStore` contract (read `WindowGeometry`, write `UpdateWindowGeometry`) to `FusionCanvas.Application/Settings`.
- [x] 1.3 Extend `ApplicationSettings` with an optional `WindowGeometry` dictionary as the last positional parameter, preserving existing constructor calls.
- [x] 1.4 Extend `JsonApplicationSettingsStore` to write version 4 with `windowGeometry`, read it for version 4 only with independent per-entry validation and a warning, and default to empty geometry for versions 1–3; keep `TryReadWindowLayout`, atomic writes, and error behavior unchanged.
- [x] 1.5 Add Integration tests for version 4 geometry round-trip, version 3 legacy empty-geometry load, invalid-entry discard with preserved siblings, and `windowLayout` round-trip alongside `windowGeometry`.

## 2. SettingsViewModel geometry orchestration

- [x] 2.1 Implement `IWindowGeometryStore` on `SettingsViewModel`: expose `WindowGeometry` and `UpdateWindowGeometry` rebuilding an immutable dictionary and queueing a save through the existing path.

## 3. Shared normalizer and persistence helper

- [x] 3.1 Add `MainWindowLayoutNormalizer.TryCaptureGeometry` and `TryNormalizeGeometry` for `WindowGeometrySettings`, extracting shared screen/clamping helpers and leaving existing main-window signatures unchanged.
- [x] 3.2 Add `WindowLayoutKeys` with the stable window identity constants.
- [x] 3.3 Add `WindowGeometryPersistence.Attach(Window, IWindowGeometryStore, string key, double minWidth, double minHeight)` wiring `Opened` to restore and `Closing` to capture.

## 4. Wire secondary windows

- [x] 4.1 In `MainWindow` sync helpers, `Attach` `SettingsWindow`, `WorkspaceManagementWindow`, `StoreEditorWindow`, `AssetsWindow`, `IdeationWindow`, `DesignPreviewWindow`, and `ItemImportWindow` with `_settings` and the matching keys.
- [x] 4.2 Add an internal `IWindowGeometryStore?` property on `IdeationWindow` set by `MainWindow` before `ShowDialog`; in `IdeationWindow`, `Attach` `RejectIdeaWindow`, `SnowcloneLibraryWindow`, and `RejectedPhrasesWindow` with the matching keys.
- [x] 4.3 Ensure no transient confirmation dialog is attached.

## 5. Deterministic UI verification

- [x] 5.1 Add App tests for `TryNormalizeGeometry` (off-screen/oversized clamping, invalid rejection) and `TryCaptureGeometry` (maximized/fullscreen ignored, normal captured).
- [x] 5.2 Add App headless test for `WindowGeometryPersistence` restore-on-open and capture-on-close using a recording `IWindowGeometryStore` and deterministic fixtures.
- [x] 5.3 Confirm existing main-window layout and settings tests remain green and no test accesses the contributor's real workspace or settings path.

## 6. Acceptance verification and quality gates

- [x] 6.1 Map every scenario in the delta specs to focused Integration or App tests; document any platform-only limitation.
- [x] 6.2 Run `dotnet test .\FusionCanvas.sln` and resolve all failures in the changed scope.
- [x] 6.3 Run strict `openspec validate remember-secondary-window-layouts` and correct artifacts until validation passes.
- [x] 6.4 Evaluate supplemental Windows multi-monitor verification; record it as not run if no interactive multi-monitor environment is available.
