## Context

The original installer package selected a per-user default at `%LOCALAPPDATA%\Programs\FusionCanvas` so installation and updates would not need elevation. During installer testing, the user requested the standard 64-bit Program Files directory as the default. The revised contract is a machine-wide install under `%ProgramFiles%\FusionCanvas`, with administrator elevation, HKLM registration, and shortcuts in shared shell locations. Workspace, settings, managed files, AI cache, and telemetry remain owned by each user's `%LOCALAPPDATA%\FusionCanvas` directory.

The existing NSIS installer, smoke harness, and docs implement the original per-user contract. This change revises the same active installer delivery package before it is archived. It does not change the application data model or distribution candidate pipeline.

## Goals / Non-Goals

**Goals:**

- Default fresh installs to the 64-bit Program Files directory and require elevation for protected writes.
- Register machine-wide installs in HKLM and expose only user-selected shortcuts to all users.
- Preserve installed data and registration on elevation cancellation or failed installation.
- Upgrade existing machine-wide installations in place.
- Recognize the initial per-user installation during upgrade, migrate it to the machine-wide default, and remove its old application files only after successful replacement.
- Preserve each user's existing application data through install, upgrade, and uninstall.
- Verify that the application runs without attempting to write mutable data under Program Files.

**Non-Goals:**

- A 32-bit installer or installation under `Program Files (x86)`.
- An optional per-user installation mode.
- Changing app data locations, application behavior, release channels, or automatic updates.
- Authenticode signing, MSI/MSIX, or enterprise deployment policy.

## Decisions

### Use the 64-bit Program Files directory

Use NSIS `$PROGRAMFILES64` (or the equivalent verified 64-bit shell constant) for the default path. The published application is `win-x64`; `Program Files (x86)` is not an appropriate default. The directory browser remains available for a user-selected supported location.

### Elevate installer operations

Request administrator execution level for setup and uninstaller operations that write application files, HKLM registration, or shared shortcuts. Cancellation must stop before mutation, and failures must not report success or leave partial registration. The installer UI remains the existing focused NSIS wizard; no application UI change is introduced.

### Use machine-wide ownership for new installations

Write install and uninstall metadata under HKLM and place selected Start Menu and desktop shortcuts in all-users shell locations. Keep shortcut checkboxes and the current setup wizard flow. The application itself continues to store mutable files under the current user's LocalAppData; review the app's path usage to ensure it never needs write access to the installation directory.

### Upgrade registrations safely

When both registrations exist, prefer the HKLM machine-wide install path. If no HKLM installation exists but the legacy HKCU registration does, migrate only when its registered path exactly matches the initial default `%LOCALAPPDATA%\Programs\FusionCanvas`; use the fixed machine-wide destination under Program Files. Remove the legacy directory, HKCU installer registration, and user-scoped shortcuts only after the machine-wide install and registration succeed. Never recursively delete an arbitrary HKCU-supplied path from an elevated process. If a legacy custom location is registered, stop before mutation and ask the user to uninstall that legacy version first. If migration fails, retain the prior usable installation and registration. Future upgrades use the HKLM path.

### Keep data outside installer ownership

Install, upgrade, and uninstall must not delete or rewrite any user's `%LOCALAPPDATA%\FusionCanvas` tree. Smoke tests use disposable profiles and assert sentinels for the invoking user; tests should also verify another user's data fixture remains untouched if the harness can create a second profile without introducing nondeterminism.

### Keep candidate distribution unchanged

Retain the existing deterministic test gate, pinned NSIS compiler, canonical version, checksum, provenance, and workflow artifact behavior. This change alters install mechanics only; it does not create releases or update manifests.

## Risks / Trade-offs

- **[Elevation is declined or unavailable]** → Exit cleanly before mutation and verify no partial files, registrations, or shortcuts.
- **[Program Files permissions reveal runtime writes to the install directory]** → Inspect app storage paths and add a focused regression check; redirect mutable data only if existing application contracts already define the destination, otherwise stop for a product decision.
- **[Legacy HKCU upgrade creates parallel or orphaned installs]** → Detect both registry hives, prefer the machine-wide path, migrate registration only after successful file replacement, and assert that no parallel installation remains.
- **[Uninstaller runs without elevation]** → Request elevation before removing machine-wide files; on cancellation leave files and registration intact.
- **[Shared shortcuts surprise users]** → Keep shortcut selection explicit and test that only selected shared shortcuts are created and removed.
- **[A 32-bit process resolves the wrong Program Files directory]** → Build and run the installer harness on x64 Windows and assert the resolved path is the 64-bit Program Files directory.

## Migration Plan

Fresh installations use `%ProgramFiles%\FusionCanvas`. Existing machine-wide installs retain their registered path. The initial per-user default install is migrated to Program Files on the first elevated setup run; successful migration removes the old app files, registration, and shortcuts. A legacy custom per-user path is not trusted for elevated cleanup; the setup stops with instructions to uninstall it first. No application data is moved or deleted. A failed or cancelled upgrade preserves the usable prior installation. Rollback remains operational: reinstall the prior candidate using the retained registration while keeping user data intact.

## Implementation Plan

1. Update `installer/FusionCanvas.nsi`: request administrator execution level; choose the 64-bit Program Files directory for a fresh install; detect existing HKLM then legacy HKCU install registrations; write machine-wide uninstall and install metadata; use all-users shortcut roots; elevate uninstallation; and make registration/shortcut cleanup conditional on successful install/uninstall. Preserve the running-process guard and user-data exclusion.
2. Review application and integration storage paths used at runtime to confirm writable files remain under user data locations rather than the install directory. Do not relocate data or change application behavior as part of this installer change.
3. Update `tools/installer/Test-FusionCanvasInstaller.ps1` to verify fresh default path, selected alternate path, HKLM registration, selected shared shortcuts, elevation cancellation/failure behavior where automatable, upgrade of both machine-wide and legacy HKCU installs, no duplicate installation, running-process protection, elevated uninstall, and user-data preservation.
4. Update installer documentation and CI workflow permissions only as needed. Preserve the deterministic test gate, versioning, checksum, provenance, candidate artifact retention, and no-release boundary.
5. Run installer-specific checks and `openspec validate build-nsis-windows-installer --strict`; run `dotnet test .\FusionCanvas.sln -m:1` and record any environment limitation. Exercise the interactive setup once on Windows to confirm the elevation prompt and Program Files destination, as supplemental evidence.

## Acceptance-to-Verification Mapping

| Acceptance area | Planned evidence |
| --- | --- |
| Fresh default install and machine-wide registration | Isolated Windows smoke test asserts `$PROGRAMFILES64` path, HKLM keys, installer exit code, and selected shared shortcuts |
| Alternate location | Smoke test supplies a temporary alternate path and verifies HKLM registration and shortcut targets |
| Elevation cancelled | Interactive installer check plus deterministic cancellation/result checks where supported; assert no partial state |
| Legacy per-user upgrade | Seed HKCU registration and files, run elevated candidate, assert migration to Program Files, HKLM registration, old app cleanup, no duplicate, and data sentinel preservation |
| Machine-wide upgrade | Run newer/repeated candidate and assert same registered location, versioned files, shortcut targets, and data preservation |
| Running app guard | Existing process-stub scenario verifies safe refusal/retry path |
| Elevated uninstall and cancellation | Isolated smoke test verifies machine files, HKLM registration, and selected shared shortcuts are removed; cancellation leaves install intact |
| User data ownership | Sentinel checks under LocalAppData before/after install, upgrade, and uninstall |
| Runtime writable paths | Source inspection of path providers plus focused check that launch does not create mutable files under Program Files |
| Candidate boundary | Workflow inspection confirms artifact-only publishing and no release/update manifest |

## Decisions Not to Reopen During Implementation

- New installations are machine-wide and default to 64-bit `%ProgramFiles%\FusionCanvas`.
- The setup and uninstaller require administrator elevation for protected operations.
- Install/uninstall registration and selected shortcuts are machine-wide.
- Existing machine-wide install paths are preserved during upgrade; the initial per-user install is migrated to the machine-wide default with no parallel copy.
- Each user's application data remains under `%LOCALAPPDATA%\FusionCanvas` and is not installer-owned.
- Installer candidates remain workflow artifacts; release-channel behavior is out of scope.
