## Context

The completed Windows package pipeline produces a self-contained `win-x64` ZIP after the deterministic test gate. Its accepted specification intentionally excludes installers, releases, and automatic updates. FusionCanvas already obtains a canonical application version from Nerdbank.GitVersioning and already stores workspace, settings, and telemetry data below `%LOCALAPPDATA%\FusionCanvas`.

This module adds a conventional Windows setup program without putting installer behavior into Domain or Application code. The installer is an operational distribution boundary: it packages the published application, manages shortcuts and uninstall registration, and leaves user data under application ownership.

## Goals / Non-Goals

**Goals:**

- Build a reproducible NSIS installer from the existing self-contained publish output.
- Gate installer creation on the reusable deterministic test workflow.
- Produce versioned, checksum-protected, provenance-attested installer candidates.
- Make the default installation per user so normal installation and future updates do not require administrator rights.
- Verify fresh install, upgrade, running-application protection, uninstall, shortcut behavior, and user-data preservation.
- Leave a stable installer upgrade contract for the later in-app update module.

**Non-Goals:**

- Creating GitHub Releases or a public release channel.
- Implementing the in-app “Update available” button, release manifest, download logic, restart flow, rollback, or update policy.
- Authenticode signing or certificate procurement; provenance attestation and code signing remain separate concerns.
- Supporting all-users machine installation, enterprise deployment, MSI, MSIX, or another installer technology.
- Changing application runtime code, workspace schema, user-data paths, or database migration behavior.

## Decisions

### Use NSIS as the installer engine

The installer SHALL be expressed as a repository-owned NSIS script with a pinned compiler/toolchain in CI. NSIS provides a conventional setup executable and script-level control while keeping the application independent of the packaging technology.

Alternatives considered:

- Inno Setup was not selected because its commercial policy introduces a licensing consideration for qualifying commercial use and CI compiler use.
- WiX was not selected because its current project policy introduces a maintenance-fee consideration for revenue-generating use.
- MSIX was not selected for this first module because it couples distribution more closely to Windows package identity and signing mechanisms.
- ZIP-only distribution remains useful for portable candidates but does not provide installation, shortcut, or uninstall behavior.

### Keep the installer thin and consume the existing publish output

The workflow SHALL publish the application once as self-contained `win-x64` output, then feed that directory to NSIS. The NSIS script owns only packaging concerns: file inclusion, install destination, shortcuts, uninstall registration, version metadata, and safe upgrade behavior. It SHALL not contain application configuration, database migration, or workspace logic.

### Default to per-user installation

The default destination SHALL be `%LOCALAPPDATA%\Programs\FusionCanvas`. This avoids administrator elevation for the normal individual-creator scenario and makes later installer-driven updates compatible with the same permission boundary. It remains distinct from `%LOCALAPPDATA%\FusionCanvas`, which continues to hold workspace databases, managed files, settings, AI cache, and telemetry data.

The first module will not offer an all-users installation mode. Adding one later would require separate elevation, permissions, multi-user, and update decisions.

### Treat user data as outside installer ownership

The installer SHALL never delete or rewrite the existing `%LOCALAPPDATA%\FusionCanvas` data tree during install, upgrade, or uninstall. Upgrade verification will create a sentinel user-data file or database fixture, run the installer again, and verify the sentinel remains. Uninstall verification will perform the same check after removal.

### Build candidates without creating a release channel

The installer workflow will be triggered in the same candidate-oriented manner as the existing package workflow (merge to `main` and manual dispatch), will reuse the deterministic test gate, and will upload a retained workflow artifact. It will not create a GitHub Release or update manifest. That boundary lets the separate update module choose and secure its release channel after the installer contract is proven.

### Verification strategy

Installer behavior will be verified in the Windows workflow with scripted NSIS install, upgrade, and uninstall operations in an isolated temporary profile/directory. The verification will inspect installed files, shortcuts, uninstaller registration, exit codes, and preservation of a user-data sentinel. A fresh installer build and a second install of the same or newer candidate will exercise upgrade behavior.

No Avalonia headless view test is warranted because this module adds no application view or binding behavior. The installer wizard is native NSIS UI, and deterministic silent-mode Windows checks provide more reliable coverage. A manual interactive wizard check may supplement CI for visual judgment but is not a completion gate.

## Risks / Trade-offs

- **[NSIS compiler acquisition changes or becomes unavailable]** → Pin the compiler version and source, verify its checksum, fail closed when the expected toolchain is not available, and document the acquisition path.
- **[The setup executable is not trusted by Windows SmartScreen]** → Keep provenance attestation and checksum publication; treat Authenticode signing as a separate release-readiness module.
- **[An application process keeps files locked during upgrade]** → Detect or safely defer replacement, show an actionable close-and-retry message, and verify that no partial upgrade is reported as successful.
- **[A future update mechanism assumes machine-wide installation]** → Capture the per-user installation contract now and make the later updater use the installer’s same user context.
- **[Install and data paths are accidentally conflated]** → Keep explicit path constants in the script and assert data preservation in automated upgrade/uninstall verification.
- **[A candidate installer is mistaken for a supported release]** → Use candidate naming and workflow-artifact documentation; defer release-channel and update-manifest creation to the later module.

## Migration Plan

Existing ZIP users can continue running their extracted copy. Running the setup program installs a new copy under the default per-user installation directory and does not move or delete `%LOCALAPPDATA%\FusionCanvas`. Later setup runs upgrade that installation in place. No database migration or workspace conversion is introduced.

Rollback is operational: a user can reinstall an earlier setup candidate, while the preserved user-data tree remains available. The workflow will retain the ZIP candidate during this module so the installer path can be compared or used as a fallback.

## Implementation Plan

1. Add a repository-owned NSIS script under a focused installer/build directory. Define application file inclusion from the publish directory, canonical version propagation, default per-user destination, optional shortcut components, uninstall registration, upgrade behavior, running-process detection, and explicit exclusion of `%LOCALAPPDATA%\FusionCanvas`.
2. Add a Windows installer workflow that calls the reusable deterministic test workflow, performs RID-aware self-contained publish, obtains and validates `NuGetPackageVersion`, acquires a pinned NSIS compiler, builds the setup executable, generates the checksum, creates the provenance attestation, and uploads the installer candidate.
3. Add isolated Windows packaging smoke checks for fresh install, upgrade, running-process handling, shortcut selection, uninstall, exit codes, and user-data preservation. Keep them outside the normal application test solution if they require a Windows installer process, but make them deterministic and runnable by the hosted workflow.
4. Update contributor/release documentation to distinguish ZIP candidates, NSIS installer candidates, and future public releases/update channels. Document the per-user install and user-data preservation contract.
5. Verify workflow structure and action/tool pins, run strict OpenSpec validation, run the full deterministic solution test baseline, and verify the hosted Windows installer workflow after merge.

## Acceptance-to-Verification Mapping

| Acceptance area | Planned evidence |
| --- | --- |
| Deterministic gate blocks installer failures | Workflow inspection plus hosted success/failure control-path evidence where practical |
| Versioned setup, checksum, and provenance | Hosted workflow outputs plus checksum validation and attestation inspection |
| Fresh per-user install | Scripted Windows installer smoke check in an isolated profile |
| Upgrade preserves app operation and user data | Two-version or repeated-install smoke check with sentinel data |
| Running app is handled safely | Scripted process-lock scenario with actionable exit/result assertion |
| Uninstall removes only installer-owned content | Scripted uninstall followed by file/shortcut/data-tree assertions |
| Candidate boundary remains intact | Workflow inspection confirming no GitHub Release or update manifest |

## Decisions Not to Reopen During Implementation

- NSIS is the installer engine for this module.
- The default installation is per user under `%LOCALAPPDATA%\Programs\FusionCanvas`.
- User data remains under `%LOCALAPPDATA%\FusionCanvas` and is preserved by install, upgrade, and uninstall.
- Installer candidates are workflow artifacts; release-channel and in-app update behavior belong to the next module.
- The existing ZIP candidate workflow remains available.

