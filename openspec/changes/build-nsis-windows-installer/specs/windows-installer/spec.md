## ADDED Requirements

### Requirement: Installer candidates are gated by deterministic tests

The Windows installer workflow SHALL run the deterministic solution test workflow before building an installer and SHALL not upload an installer candidate when that gate fails or is cancelled.

#### Scenario: Tests pass before installer packaging
- **WHEN** a supported installer workflow trigger starts
- **AND** all deterministic test projects pass
- **THEN** the workflow builds and uploads the NSIS installer candidate

#### Scenario: Tests fail before installer packaging
- **WHEN** the deterministic test workflow fails or is cancelled
- **THEN** the installer job does not build or upload an installer candidate

### Requirement: Installer candidates are versioned and verifiable

The workflow SHALL build a self-contained `win-x64` NSIS installer from the same published application input as the Windows candidate package, SHALL use Nerdbank.GitVersioning's canonical package SemVer for the candidate identity, and SHALL publish a matching SHA-256 checksum and artifact provenance attestation.

#### Scenario: Versioned setup program is produced
- **WHEN** the deterministic gate succeeds and the published application has a usable canonical package version
- **THEN** the workflow uploads `FusionCanvas-<SemVer>-win-x64-Setup.exe`
- **AND** the workflow uploads a matching checksum
- **AND** the workflow creates a provenance attestation for the setup program

#### Scenario: Installer version metadata is unavailable
- **WHEN** the workflow cannot obtain or validate the canonical package version
- **THEN** the installer build fails before upload
- **AND** no misleadingly named installer candidate is published

### Requirement: Installer performs an elevated machine-wide installation

The setup program SHALL install a fresh FusionCanvas installation machine-wide by default under the 64-bit `%ProgramFiles%\FusionCanvas` directory, SHALL request administrator elevation for installation operations, and SHALL register the installation for all users. The user MAY select another supported location. The setup program SHALL create only the Start Menu and desktop shortcuts the user selects, and those shortcuts SHALL be available to all users. Application data SHALL remain in each user's `%LOCALAPPDATA%\FusionCanvas` directory.

#### Scenario: User completes a fresh installation
- **WHEN** the user runs the setup program and accepts the installation
- **THEN** FusionCanvas application files are installed under `%ProgramFiles%\FusionCanvas` by default
- **AND** the installation is registered machine-wide
- **AND** the selected Start Menu or desktop shortcuts are created for all users
- **AND** the user approves administrator elevation before protected files and machine-wide registration are written

#### Scenario: User changes the installation location
- **WHEN** the user selects another supported installation directory
- **THEN** the setup program installs the application files in that directory
- **AND** machine-wide registration and shortcut behavior remain consistent
- **AND** each user's data location remains unchanged

#### Scenario: User cancels administrator elevation
- **WHEN** Windows requests administrator approval for installation
- **AND** the user declines or cancels the elevation prompt
- **THEN** the setup program exits without reporting a successful installation
- **AND** it does not leave a partial machine-wide registration or shortcuts

### Requirement: Installer preserves the registered location during upgrade

The setup program SHALL reuse the registered location of an existing machine-wide FusionCanvas installation during upgrade. When upgrading the initial per-user installation at `%LOCALAPPDATA%\Programs\FusionCanvas`, it SHALL migrate application files to the machine-wide default under `%ProgramFiles%`. If a legacy per-user installation is registered at another location, the setup program SHALL stop before modifying files and explain that the user must uninstall that version before continuing. It SHALL require administrator elevation and SHALL avoid leaving a parallel or orphaned application installation. A fresh installation without a registered location SHALL use the machine-wide default.

#### Scenario: User upgrades the initial per-user installation
- **WHEN** the setup program finds the existing per-user installation registration
- **AND** the user approves administrator elevation
- **THEN** the application files are installed under `%ProgramFiles%\FusionCanvas` by default
- **AND** the previous per-user application files, registration, and shortcuts are removed after the machine-wide install succeeds
- **AND** no second or orphaned FusionCanvas installation remains
- **AND** user data remains available under the user's `%LOCALAPPDATA%\FusionCanvas` directory
#### Scenario: User upgrades an existing machine-wide installation
- **WHEN** the setup program finds an existing machine-wide installation
- **AND** the user approves administrator elevation
- **THEN** the application files are upgraded in the registered installation directory
- **AND** the selected shortcuts continue to target that installation
- **AND** user data remains available

#### Scenario: User cancels elevation during upgrade
- **WHEN** Windows requests administrator approval for an upgrade
- **AND** the user declines or cancels the elevation prompt
- **THEN** the existing installation remains intact
- **AND** the setup program reports that the upgrade did not complete

### Requirement: Installer handles unsupported legacy custom paths safely

The setup program SHALL NOT use an arbitrary path read from a per-user registry key for elevated file replacement or recursive deletion.

#### Scenario: Setup detects a legacy custom per-user install path
- **WHEN** the setup program finds a per-user install registration outside `%LOCALAPPDATA%\Programs\FusionCanvas`
- **THEN** it exits before modifying application files, registrations, or shortcuts
- **AND** it explains that the legacy installation must be uninstalled before installing the machine-wide version
### Requirement: Installer supports safe upgrade and uninstall

The setup program SHALL support rerunning a newer setup program over an existing FusionCanvas installation, SHALL refuse or safely defer replacement while FusionCanvas is running, SHALL remove installed application files and installer-owned shortcuts during an elevated uninstall, and SHALL not delete FusionCanvas user data as part of uninstall.

#### Scenario: Application is running during upgrade
- **WHEN** the user starts an upgrade while FusionCanvas is running
- **THEN** the setup program does not overwrite files that are in use
- **AND** it explains that FusionCanvas must be closed before the upgrade can complete
- **AND** it provides a safe cancellation or retry path

#### Scenario: User uninstalls FusionCanvas
- **WHEN** the user runs the FusionCanvas uninstaller and approves administrator elevation
- **THEN** the machine-wide application files, registration, and installer-owned shortcuts are removed
- **AND** each user's `%LOCALAPPDATA%\FusionCanvas` data is preserved
- **AND** a user can reinstall later without losing the existing workspace and settings

#### Scenario: User cancels elevation during uninstall
- **WHEN** Windows requests administrator approval for uninstall
- **AND** the user declines or cancels the elevation prompt
- **THEN** the installed application and its registration remain intact

### Requirement: Installer candidates remain candidates rather than releases

The installer workflow SHALL publish a retained candidate artifact and SHALL not imply a public release channel or enable in-app automatic updating.

#### Scenario: Installer candidate workflow completes
- **WHEN** the installer workflow succeeds
- **THEN** the setup program is available as a workflow artifact
- **AND** no GitHub Release or update manifest is created by this capability
