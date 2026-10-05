## ADDED Requirements

### Requirement: Update discovery does not block the workspace
FusionCanvas SHALL check the configured stable Windows update source in the background after the primary workspace is available, SHALL compare releases using canonical product versions, and SHALL keep the current application usable when the source is unavailable.

#### Scenario: A newer release is available
- **WHEN** the background check receives valid metadata with a product version newer than the running version
- **THEN** the application exposes an `Update available` action in the main shell
- **AND** the current workspace remains usable while the check completes

#### Scenario: No newer release is available
- **WHEN** the check succeeds and the advertised version is equal to or older than the running version
- **THEN** the main-shell update action is hidden
- **AND** the About/settings surface reports that FusionCanvas is up to date when the user manually checks

#### Scenario: Update discovery cannot complete
- **WHEN** the network, source, manifest, or version comparison cannot be used
- **THEN** the application remains usable
- **AND** the main shell does not claim that an update is available
- **AND** the About/settings surface exposes a retryable, actionable failure state

### Requirement: Update controls remain compact and explicit
The application SHALL keep update discovery out of the permanent workspace footprint until an update exists, SHALL place the visible `Update available` action in the global shell beside existing application controls, and SHALL expose a manual check in About/settings.

#### Scenario: User sees the available update action
- **WHEN** a newer compatible release has been discovered
- **THEN** the user can activate a compact button labeled `Update available`
- **AND** the button has an accessible name and does not obscure navigation or the active work surface

#### Scenario: User manually checks for updates
- **WHEN** the user activates the About/settings update check
- **THEN** the surface shows an explicit checking state followed by up-to-date, available, or actionable failure state
- **AND** the user can retry after a recoverable failure

### Requirement: Installer downloads are verified before handoff
FusionCanvas SHALL download the candidate installer only after explicit user action, SHALL require HTTPS and a supported release source, SHALL verify the downloaded bytes against the manifest SHA-256 digest, and SHALL not launch an installer when verification fails.

#### Scenario: User downloads a verified update
- **WHEN** the user activates the update action and the installer download completes
- **AND** the SHA-256 digest matches the manifest
- **THEN** the application presents the update as ready to install
- **AND** it identifies that FusionCanvas will close and restart through the existing installer

#### Scenario: Download or verification fails
- **WHEN** the download is cancelled, interrupted, too large, unavailable, or has a digest mismatch
- **THEN** the application does not launch the installer
- **AND** it reports an actionable failure with retry or cancellation
- **AND** the existing installation remains unchanged

### Requirement: Applying an update preserves user control and data
The update flow SHALL require an explicit final install action, SHALL flush pending application settings before handing off to the NSIS installer, SHALL close FusionCanvas before installer launch, and SHALL rely on the installer contract to preserve `%LOCALAPPDATA%\FusionCanvas` user data.

#### Scenario: User confirms installation
- **WHEN** a verified installer is ready and the user confirms installation
- **THEN** FusionCanvas flushes pending settings, launches the verified NSIS installer, and requests application shutdown
- **AND** the installer can replace application files without the running application holding them open

#### Scenario: User cancels before installation
- **WHEN** the user cancels from the ready-to-install state
- **THEN** FusionCanvas remains open and usable
- **AND** the downloaded installer is not applied automatically

#### Scenario: Update is unavailable on the current runtime
- **WHEN** the application is not running on supported Windows installer mode or the release does not provide a compatible asset
- **THEN** the update action is not offered as an installable update
- **AND** the About/settings surface explains that no compatible update is available
