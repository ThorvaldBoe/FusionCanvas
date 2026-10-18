## Why

FusionCanvas needs a conventional Windows setup program whose default location matches the standard 64-bit Windows application directory. Installer testing showed that the initial per-user default under `%LOCALAPPDATA%\Programs` does not match that expectation; the installer will use an elevated machine-wide install while continuing to keep each user's data separate.

NSIS is the selected installer technology because it is open source, scriptable, and produces a conventional Windows setup executable without coupling application behavior to a proprietary updater or store. The first module should establish a reliable, testable installer artifact; in-app update discovery and application will follow as a separate dependent module.

## What Changes

- Add an NSIS-based Windows installer pipeline for the existing self-contained `win-x64` publish output.
- Gate installer creation on the deterministic solution test workflow.
- Produce a version-aligned `FusionCanvas-<SemVer>-win-x64-Setup.exe` candidate artifact with a matching checksum and provenance attestation.
- Install fresh copies machine-wide by default under `%ProgramFiles%\FusionCanvas` (the 64-bit Program Files directory), with application files separated from each user's `%LOCALAPPDATA%\FusionCanvas` workspace, settings, and telemetry data.
- Request administrator elevation for installation, upgrade, and uninstall operations that write to the protected application directory.
- Register machine-wide installs in HKLM and offer selected Start Menu and desktop shortcuts to all users.
- Preserve existing machine-wide install locations during upgrade, and migrate the initial per-user installation to Program Files on its first elevated upgrade rather than creating a second copy.
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
- Updates installer script, smoke checks, installer documentation, and Windows verification evidence for elevation, machine-wide registration, shared shortcuts, and compatibility with existing per-user installations.
- Adds no application-domain or persistence schema changes. Existing application versioning remains the source for the installer version, and existing user-data paths remain unchanged.
- The future in-app update module will depend on this module's upgrade contract and will separately define release-channel discovery, update checks, download verification, restart/apply behavior, rollback, and user-facing notification states.

