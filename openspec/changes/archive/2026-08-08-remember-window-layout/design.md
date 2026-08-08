## Context

The application already loads application-wide settings before constructing `MainWindow`, writes them as versioned JSON through `JsonApplicationSettingsStore`, serializes writes through `SettingsViewModel`, and flushes the pending save chain during main-window shutdown. `MainWindow.axaml` currently supplies a `1180x760` default, `900x600` minimum, and a `GridSplitter` whose navigation column starts at `300px` and is constrained to `240–560px`. No existing code owns native window geometry or splitter persistence.

This is a user-facing convenience used frequently during normal creative work, but it requires native-window and multi-monitor handling. It belongs in the persistent main shell, not in a settings dialog or workspace database. The existing light/dark settings and AI settings must continue to work unchanged.

## Goals / Non-Goals

**Goals:**

- Remember the main window's latest normal position and size plus the navigation-pane width.
- Restore the layout after the window exists and before normal user work begins.
- Keep restoration safe across removed monitors, changed working areas, invalid files, DPI changes, and settings written by older versions.
- Reuse the existing versioned JSON settings and save/flush path.
- Preserve current defaults when no usable saved layout exists.
- Make persistence and layout decisions deterministic enough for integration and Avalonia headless tests.

**Non-Goals:**

- Persisting secondary dialog/window geometry.
- Persisting maximized, minimized, fullscreen, or platform-specific window-state flags.
- Adding a settings UI for these preferences.
- Changing workspace databases, workspace packages, or the application theme behavior.
- Requiring interactive desktop automation for the deterministic test baseline.

## Decisions

### Store optional layout as part of application settings

Add an optional layout value to the application-layer `ApplicationSettings` model and serialize it in a new settings-document version. The layout contains normal-state position, normal-state size, and navigation width. Missing layout is represented as absent/null and means “use XAML defaults.” This keeps geometry independent of workspace content and lets the existing atomic JSON writer protect the whole preference document.

The writer advances the document version; the reader continues accepting versions 1 and 2, defaults the new optional value for them, and ignores unknown properties. Invalid layout fields should not invalidate a readable appearance or AI section: the reader returns the rest of the settings and omits only the unusable layout. A malformed or unsupported whole document retains the existing all-default fallback.

### Keep Avalonia-specific validation at the main-window boundary

The application settings contract stores neutral numeric values only. `MainWindow` owns interpretation against `Window.Width`, `Window.Height`, `Window.Position`, `MinWidth`, `MinHeight`, the navigation column bounds, and `Screens.All`. This prevents Avalonia types from entering Application or Integration and avoids a speculative cross-platform window service for one shell.

### Restore a usable normal rectangle

After `MainWindow` is constructed and shown, apply a saved layout only when all numeric values are finite and positive, the width/height satisfy the existing minimums, and the navigation width is within its existing `240–560px` range. Select the current screen whose working area contains or is nearest to the saved position; if none matches, use the primary screen. Convert working-area pixel dimensions using the target screen's scaling when comparing against Avalonia's logical window dimensions.

Clamp the size so it fits within the target working area, then clamp position so a usable portion of the title bar/window remains within that working area. If screen information is unavailable or any candidate cannot be made valid, leave the XAML defaults untouched. Never restore a saved maximized/fullscreen state; a saved layout is only a normal rectangle.

### Capture only the latest normal state

Subscribe at the main-window boundary to position/size changes and splitter-column changes. Update an in-memory layout snapshot only while the window is in its normal state and after the platform has supplied meaningful dimensions. On close, merge that snapshot into the current `ApplicationSettings` and queue it through the existing settings save chain before the existing shutdown `FlushAsync` completes. Do not write on every pixel of a drag; debounce or coalesce updates so the existing latest-generation save behavior remains effective.

When the window is maximized or fullscreen, retain the last known normal rectangle rather than replacing it with platform-managed bounds. If no valid normal snapshot has ever been observed, do not create a geometry preference.

### No user-facing settings surface

The main shell itself is the interaction surface: users move/resize the window and drag the existing splitter as they do today. There are no new controls, focus changes, prompts, or destructive actions. Missing, invalid, or rejected values fail silently to the established defaults because the user can still interact with and resize the shell normally.

## Risks / Trade-offs

- [Risk] Native window coordinates and working areas are pixel-based while Avalonia dimensions are logical and can vary by monitor scaling. → Use `Screen.Scaling` for comparisons/clamping, keep position in platform coordinates, and cover conversion helpers with deterministic tests.
- [Risk] A monitor can disappear between save and restore, leaving a window off-screen. → Choose a current screen from `Screens.All`, prefer primary when no saved screen is available, and clamp to its working area.
- [Risk] A settings file may contain partially corrupt geometry. → Validate each field independently and discard only the layout section when appearance/AI data remains readable.
- [Risk] Saving continuously during a resize can create excessive writes or stale ordering. → Coalesce layout changes and reuse the existing serialized latest-generation save chain; shutdown flush remains the final durability boundary.
- [Risk] Headless Avalonia tests do not model real OS monitor topology. → Test pure validation/clamping with injected screen data where practical and add one optional live multi-monitor check as supplemental evidence.
- [Risk] Persisting current maximized bounds would make the next normal launch unusable. → Capture only normal-state values and explicitly ignore maximized/fullscreen state.

## Migration Plan

1. Ship the new settings document version with optional layout fields.
2. On first launch after upgrade, version 1/2 files load normally with no layout and retain existing defaults.
3. On close, a valid normal layout is written in the new version alongside the existing appearance and AI settings.
4. If a newer build must roll back, the documented compatibility policy should be reviewed before release; the implementation should not silently downgrade or discard unsupported settings. No database migration is required.

## Open Questions

None for implementation. The minimum visible portion used by clamping should be a named constant in the UI layer and covered by tests; its exact pixel value is an implementation detail, not a product choice.

## Implementation Plan

1. **Settings model and compatibility**
   - Update `src/FusionCanvas.Application/Settings/ApplicationSettings.cs` with an optional neutral layout record.
   - Update `src/FusionCanvas.Integration/Settings/JsonApplicationSettingsStore.cs` to read versions 1–3, tolerate absent layout, validate finite numeric JSON values, write version 3, and preserve existing atomic/error behavior.
   - Keep secrets and workspace data out of the document.

2. **Main-window restoration and capture**
   - Update `src/FusionCanvas.App/Views/MainWindow.axaml.cs` to apply layout after construction/showing, observe normal-state window bounds and the navigation `Grid` column, and merge the final snapshot into the settings view model/save path during close.
   - Expose the minimum necessary settings update/queue operation without moving Avalonia concerns into Application or Integration.
   - Use the existing XAML defaults and splitter constraints as the fallback source of truth.

3. **Screen-safe layout algorithm**
   - Implement focused UI-layer helpers for finite-value validation, target-screen selection, scaling-aware working-area conversion, size clamping, and visible-position clamping.
   - Treat missing, invalid, off-screen, maximized, fullscreen, or unavailable-screen values as fallback cases without exceptions or user prompts.

4. **Verification**
   - Add Integration tests for version 3 round-trip, versions 1/2 compatibility, missing/invalid layout, partial corruption, and write failure behavior.
   - Add App tests for restoration fallback, valid bounds, screen clamping, splitter limits, normal-state capture, and shutdown flush integration using isolated settings stores and deterministic screen fixtures.
   - Run `dotnet test .\\FusionCanvas.sln` and `openspec validate` strictly.
   - Optionally perform a live Windows check with two monitors at different resolutions/scales: move/resize/split, restart, disconnect or change the display, and confirm the window remains reachable and usable.

## Acceptance-to-Verification Plan

| Acceptance area | Planned verification |
| --- | --- |
| Optional layout persists and round-trips | Integration store tests with isolated temporary JSON files |
| Legacy/missing/invalid values preserve defaults | Integration tests plus App fallback tests |
| Valid layout restores after creation | Avalonia headless main-window test with deterministic settings |
| Off-screen and monitor-change placement is usable | Pure clamping/helper tests with fake working areas; optional live multi-monitor check |
| Min/max dimensions and splitter constraints hold | Headless layout tests and helper tests |
| Maximized/fullscreen values are not persisted as normal bounds | App lifecycle/state tests |
| Latest normal layout survives close/shutdown flush | App test with recording settings store and explicit close/flush |
