## Why

FusionCanvas currently loses the creator's desktop arrangement on every restart: the main window returns to its XAML defaults and the navigation pane returns to its fixed initial width. Remembering the normal window layout reduces repeated setup while preserving the existing local-first settings model and protects users from restored windows becoming unreachable after monitor or display-scale changes.

## What Changes

- Extend the versioned application-settings document with optional normal main-window bounds and navigation-pane width.
- Restore saved layout after the main window is created, using existing defaults when values are missing, invalid, legacy, off-screen, maximized, or fullscreen.
- Validate finite positive dimensions, respect the existing minimum/maximum window and splitter constraints, and clamp placement to the current screens' working areas.
- Capture the latest normal-state window position/size and navigation splitter width through the existing settings save chain before shutdown flush completes.
- Preserve compatibility with existing settings versions and keep geometry independent of workspace data.
- Add deterministic persistence and headless UI coverage, plus optional live multi-monitor verification for native window behavior.

## Capabilities

### New Capabilities

- `window-layout-persistence`: Persist and safely restore the user's normal main-window bounds and navigation-pane width.

### Modified Capabilities

- `application-settings`: Define compatibility, fallback, and local persistence behavior for the new optional layout preferences.
- `desktop-application-foundation`: Define main-window startup restoration and shutdown capture behavior.

## Impact

- Affects `ApplicationSettings`, the JSON application-settings store, application startup/shutdown wiring, `MainWindow` layout and lifecycle handling, and related App/Integration tests.
- Adds no external dependencies and changes no workspace database or package schema.
- The feature is intentionally limited to the primary main window and its navigation/document splitter; secondary dialog/window geometry is out of scope.
- The module is cohesive because settings serialization, main-window lifecycle, screen-safe restoration, and their tests form one independently verifiable “resume my workspace layout” outcome.
