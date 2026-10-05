## Why

FusionCanvas currently produces a self-contained Windows ZIP candidate, but users do not yet have a conventional setup program for installing, upgrading, or uninstalling the application. Issue #120 needs an installable Windows distribution while preserving the project's local-first data ownership and avoiding a packaging tool with unwanted commercial or ecosystem constraints.

NSIS is the selected installer technology because it is open source, scriptable, and produces a conventional Windows setup executable without coupling application behavior to a proprietary updater or store. The first module should establish a reliable, testable installer artifact; in-app update discovery and application will follow as a separate dependent module.

## What Changes

- Add an NSIS-based Windows installer pipeline for the existing self-contained `win-x64` publish output.
- Gate installer creation on the deterministic solution test workflow.
- Produce a version-aligned `FusionCanvas-<SemVer>-win-x64-Setup.exe` candidate artifact with a matching checksum and provenance attestation.
- Install the application per user by default, with application files separated from the existing `%LOCALAPPDATA%\FusionCanvas` workspace, settings, and telemetry data locations.
- Support fresh installation, upgrade over an existing installation, uninstall, Start Menu/desktop shortcut selection, and preservation of user data during installer operations.
- Keep the existing ZIP candidate workflow intact; installer candidates are an additional distribution artifact rather than a replacement.
- Record the installer boundary needed by the future update module: the installer must be safely re-runnable for upgrades and must not own or delete user data.

## Capabilities

### New Capabilities

- `windows-installer`: Build and verify a versioned NSIS setup program from the gated self-contained Windows publish output.

### Modified Capabilities

None. The existing candidate-package capability remains scoped to ZIP artifacts and explicitly excludes installers; this change adds a separate installer capability.

## Impact

- Adds an NSIS script and installer build inputs under the repository's build/installer area.
- Adds or extends a GitHub Actions Windows workflow for installer candidates, including pinned toolchain acquisition, deterministic test gating, version propagation, checksum generation, and provenance attestation.
- Adds installer-focused documentation and Windows verification evidence.
- Adds no application-domain or persistence schema changes. Existing application versioning remains the source for the installer version, and existing user-data paths remain unchanged.
- The future in-app update module will depend on this module's upgrade contract and will separately define release-channel discovery, update checks, download verification, restart/apply behavior, rollback, and user-facing notification states.

