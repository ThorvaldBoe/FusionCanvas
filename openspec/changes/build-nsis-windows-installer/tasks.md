## 1. Revise installer ownership and elevation

- [ ] 1.1 Change the fresh-install default to the 64-bit Program Files directory and request administrator elevation for setup and uninstallation.
- [ ] 1.2 Write machine-wide install/uninstall metadata and selected shortcuts under all-users locations; ensure cancellation and failures leave no partial state.
- [ ] 1.3 Preserve machine-wide paths and detect the initial per-user HKCU registration; migrate the known default installation to Program Files and remove its old files, registration, and shortcuts only after successful replacement; reject legacy custom paths before mutation.
- [ ] 1.4 Inspect application and integration storage paths for runtime writes under the installation directory; record evidence or escalate if accepted data-path behavior must change.

## 2. Update deterministic installer verification

- [ ] 2.1 Extend the Windows smoke harness for fresh default and alternate paths, HKLM metadata, shortcut selection, and clean elevation cancellation/failure outcomes.
- [ ] 2.2 Cover machine-wide upgrade, legacy per-user upgrade, no duplicate installation, running-process protection, and data preservation.
- [ ] 2.3 Verify elevated uninstall removes only installer-owned application files, registration, and shortcuts; verify cancellation leaves the install intact.

## 3. Align documentation and preserve distribution contract

- [ ] 3.1 Update installation documentation to describe the Program Files default, elevation, all-users shortcuts, upgrade compatibility, and per-user data preservation.
- [ ] 3.2 Confirm candidate workflow still gates on deterministic tests and publishes versioned installer, checksum, and provenance without creating a release or update manifest.

## 4. Verify and record evidence

- [ ] 4.1 Run focused installer checks and map every requirement scenario to evidence in `verification.md`.
- [ ] 4.2 Run `openspec validate build-nsis-windows-installer --strict` and correct all reported issues.
- [ ] 4.3 Run `dotnet test .\FusionCanvas.sln -m:1` and record any environment limitation separately.
- [ ] 4.4 Perform a supplemental interactive Windows check of the elevation prompt and Program Files destination.
- [ ] 4.5 Record the user-testing feedback, approved change, applicability, classification, and promotion target in `retrospective.md`.
