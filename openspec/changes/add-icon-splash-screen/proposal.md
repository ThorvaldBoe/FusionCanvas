## Why

FusionCanvas currently launches with generic desktop chrome and provides no branded feedback while its local startup work is running. Adding the supplied FusionCanvas icon and banner will make the application identifiable in the operating system and give startup a clear, intentional visual surface.

## What Changes

- Package the supplied square FusionCanvas logo as the desktop application's icon and window icon.
- Package the supplied FusionCanvas banner as an application-owned startup splash surface.
- Show the splash before the main workspace window while the existing startup/composition work completes.
- Close the splash when the main window is ready, without delaying startup with an arbitrary fixed timer.
- Keep startup failures recoverable and ensure the splash cannot leave the application without a usable main-window or error state.
- Add deterministic coverage for the startup presentation contract where Avalonia lifecycle/view construction is involved.

The module does not add progress reporting, animated branding, a first-run wizard, or new workspace-loading behavior.

## Capabilities

### New Capabilities

<!-- No separate capability is introduced; the behavior belongs to the desktop foundation contract. -->

### Modified Capabilities

- `desktop-application-foundation`: define the branded application icon and startup splash behavior as part of desktop launch.

## Impact

- `src/FusionCanvas.App`: application startup/lifetime composition, splash view/window, main-window icon assignment, and packaged resources.
- `src/FusionCanvas.App/FusionCanvas.App.csproj`: resource and executable-icon metadata, including a repository-owned icon format suitable for desktop packaging.
- `tests/FusionCanvas.App.Tests`: startup/lifecycle and resource-contract coverage as appropriate for headless Avalonia tests.
- New repository asset files copied from `C:\temp\FusionCanvas\`; runtime must not depend on that temporary path.
- No Domain, Application, Integration, persistence, settings, or public API changes are expected.
