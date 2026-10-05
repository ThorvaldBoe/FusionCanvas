## 1. Installer inputs and script

- [x] 1.1 Add the repository-owned NSIS script and build-directory conventions described in `design.md`.
- [x] 1.2 Define canonical version propagation, installer filename metadata, self-contained publish-file inclusion, default per-user destination, optional shortcuts, uninstall registration, and explicit exclusion of `%LOCALAPPDATA%\FusionCanvas`.
- [x] 1.3 Implement safe rerun/upgrade behavior, including running-process detection, close-and-retry messaging, and failure handling that cannot report a partial upgrade as successful.

## 2. Hosted installer workflow

- [x] 2.1 Add a Windows installer workflow that reuses the deterministic test workflow and supports merge-to-`main` and manual candidate triggers.
- [x] 2.2 Add RID-aware self-contained publish and canonical `NuGetPackageVersion` extraction with SemVer validation.
- [x] 2.3 Acquire and pin the NSIS compiler/toolchain, verify the expected toolchain integrity, build `FusionCanvas-<SemVer>-win-x64-Setup.exe`, and fail closed when the version or compiler is unavailable.
- [x] 2.4 Generate the installer checksum, create the provenance attestation, and upload the setup program and checksum as a retained workflow artifact without creating a GitHub Release or update manifest.

## 3. Installer verification

- [x] 3.1 Add deterministic Windows smoke checks for fresh per-user installation, selected shortcut creation, installed-file presence, exit codes, and uninstaller registration.
- [x] 3.2 Add an upgrade scenario that installs or stages an earlier candidate, runs the newer installer, verifies the new application files, and confirms a user-data sentinel remains intact.
- [x] 3.3 Add a running-process scenario that verifies the installer refuses or safely defers locked-file replacement and reports an actionable retry path.
- [x] 3.4 Add an uninstall scenario that verifies installer-owned files and shortcuts are removed while `%LOCALAPPDATA%\FusionCanvas` remains intact and reinstall is possible.

## 4. Documentation and acceptance evidence

- [x] 4.1 Document the NSIS candidate artifact, per-user install location, shortcut behavior, upgrade contract, uninstall data-preservation rule, and the distinction between installer candidates and future public releases.
- [x] 4.2 Review and correct proposal, design, specs, or tasks if implementation exposes a requirement or acceptance mismatch; record any approved change in the authoritative artifact.
- [x] 4.3 Run focused installer checks and map every windows-installer scenario to concrete evidence in `verification.md`.
- [x] 4.4 Run `openspec validate build-nsis-windows-installer --strict`.
- [x] 4.5 Run the baseline `dotnet test .\FusionCanvas.sln` and record any environment limitation separately from installer evidence.
- [ ] 4.6 Verify the hosted Windows installer workflow after merge and record artifact, checksum, attestation, and smoke-check results.

