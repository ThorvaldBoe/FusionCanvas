## Context

The application already persists the main window's normal-state layout through three cooperating pieces:

- `ApplicationSettings.WindowLayout` (`WindowLayoutSettings`: position, size, navigation width) in the Application layer.
- `JsonApplicationSettingsStore` writes version 3 and reads versions 1–3, tolerating absent layout and validating finite numeric JSON values.
- `MainWindowLayoutNormalizer` (App) captures normal-state bounds and normalizes saved bounds against `Screens.All` working areas with scaling-aware clamping.
- `MainWindow.axaml.cs` restores on `Opened` and captures on `Closing`, merging the final snapshot through `SettingsViewModel.UpdateWindowLayout` into the existing queued save/flush path.

Secondary windows are created in two places: `MainWindow` sync helpers (`SettingsWindow`, `WorkspaceManagementWindow`, `StoreEditorWindow`, `AssetsWindow`, `IdeationWindow`, `DesignPreviewWindow`, `ItemImportWindow`) and `IdeationWindow` code-behind (`RejectIdeaWindow`, `SnowcloneLibraryWindow`, `RejectedPhrasesWindow`). Transient confirmation dialogs (`GroupActionConfirmationWindow`, `GroupDeleteConfirmationWindow`, `GroupSelectionWindow`, `IdeationDiscardConfirmationWindow`, `DesignAreaArchiveConfirmationWindow`) are shown with `ShowDialog` and dismissed without persisted placement today.

This is user-facing convenience behavior used frequently during creative work. It belongs in the persistent window layer, not in a settings dialog or workspace database. Existing appearance, AI, and main-window layout behavior must continue unchanged.

## Goals / Non-Goals

**Goals:**

- Remember the normal-state position and size of every non-transient secondary window.
- Restore on open and capture on close for each persisted window.
- Reuse the existing main-window screen-safe normalization so a window last placed on a disconnected monitor or moved off-screen comes back in a usable position.
- Define the persistence boundary explicitly: non-transient windows persist; transient confirmation dialogs keep default placement.
- Preserve backward compatibility with settings documents written by the current version (single `windowLayout` section) and keep the main window on that section.
- Keep persisted values local and free of personal or workspace content.

**Non-Goals:**

- No per-workspace window layouts.
- No layout export/import, reset UI, or settings surface for these preferences.
- No persistence of maximized, minimized, fullscreen, or platform-specific window-state flags.
- No persistence for transient confirmation dialogs.
- No change to the main window's existing `windowLayout` section semantics.
- No interactive desktop automation as a completion gate.

## Decisions

### Keep the main window on its existing section; add a per-window geometry dictionary for secondary windows

Add a new optional `WindowGeometry` dictionary to `ApplicationSettings`, keyed by a stable window identity string, holding a `WindowGeometrySettings` record (position and size only; secondary windows have no navigation splitter). The main window continues to read and write its existing `windowLayout` section unchanged. This avoids touching the working main-window path (no regression risk), satisfies backward compatibility (existing documents load with empty `WindowGeometry`), and limits the change to the new surface.

Alternatives considered: unify the main window into the same dictionary under a `"main"` key with migration logic on read. Rejected because it changes the working main-window write/restore path and adds migration complexity for no current user benefit; the legacy section is preserved by requirement.

### Stable window identity keys

Persist geometry under stable, lowercase camelCase keys owned by a single internal `WindowLayoutKeys` class so the boundary is explicit and typos cannot drift between writer and reader:

- `settings`, `workspaceManagement`, `storeEditor`, `assets`, `ideation`, `rejectIdea`, `snowcloneLibrary`, `rejectedPhrases`, `designPreview`, `itemImport`.

Transient confirmation dialogs are not listed and never persist.

### Thin Application contract for geometry read/write

Introduce `IWindowGeometryStore` in the Application layer exposing `WindowGeometry` for reads and `UpdateWindowGeometry(string key, WindowGeometrySettings?)` for writes. `SettingsViewModel` implements it (it already owns the queued save path). This lets secondary windows and the shared helper depend on a small consumer-focused contract instead of the full `SettingsViewModel`, and gives tests a deterministic seam. It is justified by real consumers and a testing need, not added speculatively.

### Centralize capture/restore in a shared UI-layer helper

Add `WindowGeometryPersistence.Attach(Window, IWindowGeometryStore, string key, double minWidth, double minHeight)` in the App layer. It wires `Opened` to restore normalized geometry and `Closing` to capture the current normal-state geometry, using `Window.Screens.All` and `MainWindowLayoutNormalizer`. Window code-behind stays thin: each creation site calls `Attach` once. Capture on a cancelled close is harmless because geometry is just the current normal bounds and the next actual close re-captures.

### Generalize the normalizer without breaking the main-window contract

Add `TryCaptureGeometry` and `TryNormalizeGeometry` overloads to `MainWindowLayoutNormalizer` that operate on `WindowGeometrySettings` (no navigation width). Extract the shared screen selection, scaling-aware size clamping, and visible-position clamping into private helpers used by both the main-window and geometry paths. The existing `TryCapture`/`TryNormalize` signatures and behavior are unchanged so existing main-window tests stay green.

### Settings document version 4 with independent per-entry validation

Advance `SupportedVersion` to 4. Write a `windowGeometry` object whose properties are window keys and whose values are `{ positionX, positionY, width, height }`. Read it for version 4 only; versions 1–3 default to empty geometry. Validate each entry independently: a malformed or out-of-range entry is discarded with a warning while the remaining entries, the main `windowLayout`, appearance, and AI settings stay readable. The existing atomic temp-file write and error behavior are unchanged.

## Risks / Trade-offs

- [Risk] A monitor disappears between save and restore, leaving a secondary window off-screen. → Reuse the existing screen selection and working-area clamping for every persisted window.
- [Risk] A settings file contains partially corrupt per-window geometry. → Validate each entry independently and discard only that entry; preserve the rest of the document.
- [Risk] Adding capture/restore to many windows duplicates Avalonia-specific logic. → Centralize in `WindowGeometryPersistence` and the generalized normalizer; keep code-behind thin.
- [Risk] `ShowDialog` windows cancel close and re-close. → Capture in `Closing` is harmless on a cancelled close; the final close re-captures.
- [Risk] Headless tests do not model real monitor topology. → Cover pure validation/clamping with injected `ScreenLayoutInfo` and the restore/capture wiring with a headless window plus a recording store; native multi-monitor behavior stays optional supplemental evidence.
- [Risk] Dictionary keys are affected by the JSON naming policy. → Keys are already lowercase camelCase, so the camelCase write policy is a no-op on them; reads are case-insensitive.

## Migration Plan

1. Ship settings document version 4 with the optional `windowGeometry` section.
2. On first launch after upgrade, version 1–3 files load normally with empty secondary geometry and existing defaults; the main window keeps using `windowLayout`.
3. On close, each non-transient secondary window writes its geometry into `windowGeometry` alongside the existing sections.
4. No database migration is required. Rollback to a build that only reads up to version 3 would ignore the new section; the documented compatibility policy should be reviewed before any such rollback.

## Open Questions

None for implementation. The minimum visible portion used by clamping is the existing named constant in the UI layer; the window identity keys are an implementation detail, not a product choice.

## Implementation Plan

1. **Settings contract and compatibility (Application + Integration)**
   - Add `src/FusionCanvas.Application/Settings/WindowGeometrySettings.cs` (record: `PositionX`, `PositionY`, `Width`, `Height`).
   - Add `src/FusionCanvas.Application/Settings/IWindowGeometryStore.cs` (`WindowGeometry` read + `UpdateWindowGeometry` write).
   - Extend `ApplicationSettings` with `IReadOnlyDictionary<string, WindowGeometrySettings>? WindowGeometry = null` as the last positional parameter, preserving existing constructor calls.
   - Extend `JsonApplicationSettingsStore`: `SupportedVersion = 4`; write `windowGeometry`; read it for version 4 only with independent per-entry validation and a warning; versions 1–3 default to empty; keep `TryReadWindowLayout` and atomic/error behavior unchanged.
   - Keep secrets and workspace data out of the document.

2. **SettingsViewModel geometry orchestration (App)**
   - Implement `IWindowGeometryStore` on `SettingsViewModel`: expose `WindowGeometry` from current settings; `UpdateWindowGeometry` rebuilds an immutable dictionary, updates `_currentSettings`, and queues a save through the existing path.

3. **Shared normalizer and persistence helper (App)**
   - Add `MainWindowLayoutNormalizer.TryCaptureGeometry` and `TryNormalizeGeometry` for `WindowGeometrySettings`; extract shared screen/clamping helpers; keep existing signatures unchanged.
   - Add `src/FusionCanvas.App/Views/WindowGeometryPersistence.cs` with `Attach(Window, IWindowGeometryStore, string key, double minWidth, double minHeight)` wiring `Opened`/`Closing`.
   - Add `src/FusionCanvas.App/Views/WindowLayoutKeys.cs` with the stable key constants.

4. **Wire secondary windows (App)**
   - In `MainWindow` sync helpers, call `WindowGeometryPersistence.Attach` for `SettingsWindow`, `WorkspaceManagementWindow`, `StoreEditorWindow`, `AssetsWindow`, `IdeationWindow`, `DesignPreviewWindow`, and `ItemImportWindow` using `_settings` and the matching key.
   - Add an internal `IWindowGeometryStore?` property on `IdeationWindow` set by `MainWindow` before `ShowDialog`; in `IdeationWindow`, `Attach` `RejectIdeaWindow`, `SnowcloneLibraryWindow`, and `RejectedPhrasesWindow` with the matching keys.
   - Do not attach transient confirmation dialogs.

5. **Verification**
   - Integration tests: version 4 round-trips `windowGeometry`; version 3 loads with empty geometry; an invalid entry is discarded with a warning while other entries, `windowLayout`, and appearance/AI remain; `windowLayout` still round-trips alongside `windowGeometry`.
   - App tests: `TryNormalizeGeometry` clamps off-screen/oversized windows and rejects invalid values; `TryCaptureGeometry` ignores maximized/fullscreen and captures normal; `WindowGeometryPersistence` restore-on-open applies saved size and capture-on-close writes to a recording store using deterministic fixtures.
   - Run `dotnet test .\FusionCanvas.sln` and strict `openspec validate`; confirm no test touches the contributor's real workspace or settings path.
   - Optionally perform a live Windows multi-monitor check as supplemental evidence.

## Acceptance-to-Verification Plan

| Acceptance area | Planned verification |
| --- | --- |
| Per-window geometry round-trips | Integration store tests with isolated temporary JSON |
| Legacy single-layout documents load cleanly | Integration test loading a version 3 document with empty geometry |
| Invalid entry discarded, rest preserved | Integration test with one malformed entry alongside valid entries |
| Each non-transient window restores on open | App headless test for `WindowGeometryPersistence` restore plus normalizer tests |
| Each non-transient window captures on close | App headless test with a recording store |
| Off-screen/disconnected-monitor placement is usable | `TryNormalizeGeometry` tests with injected screen fixtures |
| Maximized/fullscreen not captured as normal | `TryCaptureGeometry` state tests |
| Transient dialogs keep default placement | Spec scenario verified by absence of attach calls and a test asserting transient windows are not keyed |
| Save/load failures do not break the session | Existing save-failure contract; geometry uses the same non-throwing queued save path |
| Main window behavior unchanged | Existing main-window layout tests remain green |
