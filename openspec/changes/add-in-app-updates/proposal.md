## Why

FusionCanvas can now produce a versioned Windows NSIS installer, but users have no dependable way to learn that a newer stable release exists or apply it from the application. This module connects the existing installer contract to a small, explicit in-app update flow while preserving local-first behavior and user control.

## What Changes

- Establish a manual GitHub Release channel for stable Windows releases, with aligned version metadata, NSIS setup asset, SHA-256 checksum, and machine-readable update manifest.
- Check for a newer compatible Windows release in the background after startup without delaying or blocking the primary workspace.
- Show a compact `Update available` action in the main shell only when a newer release is available.
- Let the user explicitly download, verify, launch, and apply the NSIS installer, then close FusionCanvas safely so the installer can replace locked files.
- Add a discoverable manual update check and actionable loading, unavailable, error, cancellation, and success states in the About/settings surface.
- Keep user data outside the installer-owned directory and preserve it through the update flow.
- Do not add silent updates, automatic installation, delta patches, a background updater service, rollback orchestration, or non-Windows package update support in this module.

## Capabilities

### New Capabilities

- `stable-release-channel`: Publish stable Windows release assets and update metadata from an explicitly created version tag.
- `in-app-updates`: Discover newer stable Windows releases, present update availability, verify the downloaded installer, and hand off to the existing NSIS installer after explicit user action.

### Modified Capabilities

- None.

## Impact

- Adds release/update workflow and repository-owned manifest assets under `.github/workflows/` and release tooling.
- Adds application-facing update contracts and version comparison in `FusionCanvas.Application`, external release/download adapters in `FusionCanvas.Integration`, and presentation state/commands in `FusionCanvas.App`.
- Extends the main shell and About settings surface without changing workspace data or database schema.
- Uses the existing canonical application version provider and NSIS installer; no new third-party updater framework is required.
- Requires deterministic tests for manifest parsing, SemVer comparison, checksum verification, failure states, and view-model commands, plus Avalonia headless coverage for the visible update action and About update states. A real-desktop/Appium journey is not required for the first module because the native installer handoff is already covered by the installer smoke harness; this module will record that rationale and cover process-launch behavior at a lower test seam.
- Stable releases remain explicit maintainer actions; merges to `main` continue to produce candidate artifacts but do not become update notifications automatically.
