## ADDED Requirements

### Requirement: Update failures are diagnosable in workspace Debug Mode
FusionCanvas SHALL record update-check failures and update-package download outcomes through the existing workspace telemetry service. Records SHALL be scoped to the active workspace and SHALL only persist when that workspace's Debug Mode is enabled. Package-download failures SHALL identify the stage that failed using a stable, bounded stage identifier and include safe diagnostic context such as target version, track, exception type, HRESULT, and HTTP status when available. Telemetry SHALL NOT include raw exception messages, request or response bodies, full URLs, local file paths, package hashes, or installer bytes.

#### Scenario: Update check fails while Debug Mode is enabled
- **GIVEN** Debug Mode is enabled for the active workspace
- **WHEN** an update check fails
- **THEN** the workspace telemetry records an `UpdateCheckFailed` event with the exception type and available HTTP status
- **AND** the Debug Monitor displays the event
- **AND** the existing user-facing error and retry behavior remain unchanged

#### Scenario: Update package download fails while Debug Mode is enabled
- **GIVEN** Debug Mode is enabled for the active workspace
- **WHEN** an update package download fails during installer download, size enforcement, checksum verification, provenance-bundle download, provenance verification, or package finalization
- **THEN** the workspace telemetry records an `UpdateDownloadFailed` event with the corresponding stable failure stage and safe diagnostic context
- **AND** the Debug Monitor displays enough information to distinguish the failure stage
- **AND** FusionCanvas does not launch the installer

#### Scenario: Verified update package is ready
- **GIVEN** Debug Mode is enabled for the active workspace
- **WHEN** the installer checksum and provenance verification both succeed
- **THEN** the workspace telemetry records an `UpdateDownloadSucceeded` event with the target version and track
- **AND** the update surface enters its existing ready-to-install state

#### Scenario: Debug Mode is disabled
- **GIVEN** Debug Mode is disabled for the active workspace
- **WHEN** an update check fails or an update package download completes
- **THEN** no updater telemetry record is persisted
- **AND** the update operation follows its existing result and error handling

#### Scenario: Telemetry recording fails
- **GIVEN** an update check or download is being processed
- **WHEN** writing its telemetry event fails
- **THEN** telemetry does not replace or alter the update operation's result

#### Scenario: User cancels an update download
- **GIVEN** an update package is downloading
- **WHEN** the user cancels the download
- **THEN** the operation returns through the existing cancellation path
- **AND** cancellation is not recorded as a package verification failure
