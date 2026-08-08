## 1. Settings contract and compatibility

- [x] 1.1 Add an optional neutral normal-window layout record to `ApplicationSettings`, covering position, size, and navigation width without introducing Avalonia dependencies.
- [x] 1.2 Extend `JsonApplicationSettingsStore` to write the next settings version and read supported legacy versions with absent layout defaults.
- [x] 1.3 Add independent validation for layout JSON values, including finite positive numeric checks, while preserving readable appearance/AI settings when only layout is corrupt.
- [x] 1.4 Add Integration tests for version round-trip, versions 1/2 compatibility, unknown properties, missing/invalid/partial layout, atomic replacement, and write failure behavior.

## 2. Screen-safe restoration and capture

- [x] 2.1 Add focused main-window layout helpers for numeric validation, target-screen selection, scaling-aware working-area comparison, size clamping, and usable-position clamping.
- [x] 2.2 Apply a validated layout after `MainWindow` creation/showing, retaining existing XAML defaults when no usable saved layout exists.
- [x] 2.3 Observe normal-state window position/size and navigation-column changes, coalesce updates, and ignore maximized, minimized, fullscreen, or otherwise platform-managed bounds.
- [x] 2.4 Merge the latest valid normal snapshot into current application settings and route it through the existing settings save chain before shutdown flush completes.

## 3. Deterministic UI verification

- [x] 3.1 Add App tests for valid restoration, missing/invalid/legacy fallback, minimum/maximum constraints, off-screen clamping, and screen-selection behavior using deterministic screen fixtures.
- [x] 3.2 Add App tests for normal-state capture, splitter persistence, maximized/fullscreen preservation of the last normal layout, and close/shutdown flush integration with an isolated recording store.
- [x] 3.3 Confirm existing main-window layout and settings tests remain green and no test accesses the contributor's real workspace or settings path.

## 4. Acceptance verification and quality gates

- [x] 4.1 Map every scenario in the three delta specs to focused Integration tests, App/headless tests, or the optional live multi-monitor check; document any platform-only limitation.
- [x] 4.2 Run `dotnet test .\\FusionCanvas.sln` and resolve all failures in the changed scope.
- [x] 4.3 Run strict `openspec validate` and correct proposal, design, specs, or tasks until validation passes.
- [x] 4.4 Evaluate supplemental Windows multi-monitor verification; record it as not run because no interactive multi-monitor environment is available.
