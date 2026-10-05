## ADDED Requirements

### Requirement: Installer candidates are gated by deterministic tests

The Windows installer workflow SHALL run the deterministic solution test workflow before building an installer and SHALL not upload an installer candidate when that gate fails or is cancelled.

#### Scenario: Tests pass before installer packaging
- **WHEN** a supported installer workflow trigger starts
- **AND** all deterministic test projects pass
- **THEN** the workflow builds and uploads the NSIS installer candidate

#### Scenario: Tests fail before installer packaging
- **WHEN** a supported installer workflow trigger starts
- **AND** the deterministic test workflow fails or is cancelled
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

### Requirement: Installer performs a per-user installation

The setup program SHALL install FusionCanvas for the current user by default without requiring administrator elevation, SHALL install application files below `%LOCALAPPDATA%\Programs\FusionCanvas` unless the user selects another supported location, and SHALL create only the shortcuts the user selects.

#### Scenario: User completes a fresh installation
- **WHEN** the user runs the setup program and accepts the installation
- **THEN** FusionCanvas application files are installed for that user
- **AND** the selected Start Menu or desktop shortcuts are created
- **AND** no administrator approval is required for the default installation

#### Scenario: User changes the installation location
- **WHEN** the user selects another supported installation directory
- **THEN** the setup program installs the application files in that directory
- **AND** the user-data locations remain unchanged

### Requirement: Installer supports safe upgrade and uninstall

The setup program SHALL support rerunning a newer setup program over an existing FusionCanvas installation, SHALL refuse or safely defer replacement while FusionCanvas is running, SHALL remove installed application files and installer-owned shortcuts during uninstall, and SHALL not delete FusionCanvas user data as part of uninstall.

#### Scenario: User upgrades an existing installation
- **WHEN** the user runs a newer setup program for an existing installation
- **AND** FusionCanvas is not running
- **THEN** the installed application files are replaced by the newer version
- **AND** installer-owned shortcuts continue to target the installed application
- **AND** existing user data remains available

#### Scenario: Application is running during upgrade
- **WHEN** the user starts an upgrade while FusionCanvas is running
- **THEN** the setup program does not overwrite files that are in use
- **AND** it explains that FusionCanvas must be closed before the upgrade can complete
- **AND** it provides a safe cancellation or retry path

#### Scenario: User uninstalls FusionCanvas
- **WHEN** the user runs the FusionCanvas uninstaller and confirms removal
- **THEN** installed application files and installer-owned shortcuts are removed
- **AND** `%LOCALAPPDATA%\FusionCanvas` is preserved
- **AND** the user can reinstall later without losing the existing workspace and settings

### Requirement: Installer candidates remain candidates rather than releases

The installer workflow SHALL publish a retained candidate artifact and SHALL not imply a public release channel or enable in-app automatic updating.

#### Scenario: Installer candidate workflow completes
- **WHEN** the installer workflow succeeds
- **THEN** the setup program is available as a workflow artifact
- **AND** no GitHub Release or update manifest is created by this capability

