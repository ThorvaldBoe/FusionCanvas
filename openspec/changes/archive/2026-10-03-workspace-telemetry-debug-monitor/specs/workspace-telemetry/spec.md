## ADDED Requirements

### Requirement: Debug Mode controls workspace telemetry capture
FusionCanvas SHALL provide an off-by-default Debug Mode setting for each workspace. When enabled, the workspace telemetry service SHALL persist diagnostic events in that workspace's SQLite database; when disabled, calls to the telemetry service SHALL not persist new events. Event calls SHALL identify a stable application area and event name, and SHALL not require callers to branch on Debug Mode.

#### Scenario: Debug Mode has not been enabled
- **WHEN** a workspace is created or opened without a saved Debug Mode preference
- **THEN** Debug Mode is off
- **AND** telemetry calls create no telemetry records

#### Scenario: Event is recorded for the active workspace
- **GIVEN** Debug Mode is enabled for the active workspace
- **WHEN** an application area records a diagnostic event
- **THEN** the event is stored with its workspace identity, UTC occurrence time, stable area identifier, event name, and available outcome/context fields

#### Scenario: Workspace switching keeps telemetry isolated
- **GIVEN** two workspaces have different Debug Mode settings
- **WHEN** the active workspace changes and an event is recorded
- **THEN** the event is stored only in the newly active workspace when that workspace has Debug Mode enabled
- **AND** records from the previous workspace are not returned or shown as records for the new workspace

### Requirement: Telemetry captures useful workflow and service diagnostics
When Debug Mode is enabled, FusionCanvas SHALL record events at first-pass major workflow decision points and failure-prone application boundaries, including current OpenRouter and Printify HTTP operations. HTTP telemetry SHALL include method, sanitized endpoint, outcome/status, elapsed time, and request and response textual bodies when available. Binary bodies SHALL be represented by content type and byte length without storing binary bytes. Telemetry SHALL NOT change the result of the operation being diagnosed.

#### Scenario: Workflow decision point is reached
- **GIVEN** Debug Mode is enabled
- **WHEN** a user action reaches an instrumented major workflow decision point
- **THEN** telemetry identifies the stable application area, event, and outcome needed to understand that transition

#### Scenario: HTTP operation succeeds
- **GIVEN** Debug Mode is enabled
- **WHEN** an instrumented HTTP request receives a response
- **THEN** telemetry records sanitized request and response details, status, and elapsed time
- **AND** the original application operation receives the same result it would receive without telemetry

#### Scenario: HTTP operation fails
- **GIVEN** Debug Mode is enabled
- **WHEN** an instrumented HTTP operation times out, fails, or returns an unsuccessful response
- **THEN** telemetry records the available sanitized request details and failure outcome
- **AND** the application's existing error handling remains authoritative

#### Scenario: Telemetry persistence is unavailable
- **GIVEN** Debug Mode is enabled
- **WHEN** a telemetry write fails while an application operation is running
- **THEN** the application operation continues according to its own result
- **AND** telemetry failure does not recursively create telemetry events

### Requirement: Telemetry secrets are redacted before persistence and display
FusionCanvas SHALL sanitize telemetry before sending it to any destination. Sanitization SHALL remove credential-bearing request headers and redact recognized secret values in URLs and structured request/response bodies, including API keys, authorization tokens, passwords, and secrets. The same sanitized event SHALL be used for SQLite persistence, the live debug window, and export.

#### Scenario: HTTP credentials are present
- **GIVEN** an instrumented request contains authorization headers, secret URL parameters, or recognized credential properties
- **WHEN** the request and response telemetry is recorded
- **THEN** credential values are absent from the database, live window, and exported JSON
- **AND** non-secret request and response body content remains available for diagnosis

### Requirement: Telemetry can be searched by time and application area
FusionCanvas SHALL provide telemetry queries that filter a workspace's records by a UTC time interval and zero or more stable application-area identifiers. Queries SHALL use a start-inclusive and end-exclusive interval, return the newest matching events first by default, and support bounded paging.

#### Scenario: Search by time and area
- **GIVEN** a workspace has telemetry events from multiple areas and times
- **WHEN** the user searches with a time interval and one or more application areas
- **THEN** the results contain only events in that workspace matching both filters
- **AND** the end timestamp itself is excluded

#### Scenario: Search returns no matches
- **WHEN** a valid telemetry search has no matching records
- **THEN** the Settings surface shows an empty result state without treating the query as an error

### Requirement: Telemetry retention is configurable and automatic
FusionCanvas SHALL offer workspace telemetry retention periods of one hour, one day, and one week. New and migrated workspaces SHALL default to one day. Records older than the selected age SHALL be deleted automatically at workspace startup, after retention changes, after telemetry writes, and periodically while the workspace is open. Retention cleanup SHALL continue while Debug Mode is off.

#### Scenario: User selects a retention period
- **WHEN** the user selects one hour, one day, or one week for the active workspace
- **THEN** the selected period is persisted for that workspace
- **AND** eligible older records are removed

#### Scenario: Expired records are cleaned up
- **GIVEN** records are older than the active workspace's selected retention period
- **WHEN** startup, a telemetry write, or the periodic cleanup runs
- **THEN** expired records are deleted even if Debug Mode is off
- **AND** records within the retention period remain available

### Requirement: Users can export or delete workspace telemetry
FusionCanvas SHALL provide explicit Settings actions to export all retained telemetry for the active workspace as standalone UTF-8 JSON and to delete all retained telemetry for that workspace. Export SHALL leave stored records unchanged. Deletion SHALL require confirmation, affect only the active workspace's telemetry records, and leave Debug Mode and its future capture behavior unchanged.

#### Scenario: Export retained telemetry
- **WHEN** the user chooses Export telemetry and completes the save dialog
- **THEN** FusionCanvas writes all retained sanitized records for the active workspace as UTF-8 JSON
- **AND** leaves the workspace database unchanged

#### Scenario: User cancels telemetry export
- **WHEN** the user cancels the export destination dialog
- **THEN** no export file is written
- **AND** all database records remain unchanged

#### Scenario: User confirms telemetry deletion
- **WHEN** the user confirms Delete telemetry for the active workspace
- **THEN** all telemetry records for that workspace are deleted
- **AND** Debug Mode and retention preferences remain unchanged

#### Scenario: User cancels telemetry deletion
- **WHEN** the user cancels the delete confirmation
- **THEN** no telemetry records are deleted

### Requirement: Show Debug Window streams telemetry in a resizable floating window
FusionCanvas SHALL provide a workspace-level Show Debug Window option that opens a modeless, resizable floating text window beside the main application when screen bounds allow. Enabling Show Debug Window SHALL enable Debug Mode. Turning Debug Mode off SHALL turn Show Debug Window off and close the window. Hiding or closing the debug window alone SHALL leave Debug Mode enabled. The window SHALL show sanitized new telemetry for the active workspace and SHALL not show the previous workspace's events after a workspace switch completes.

#### Scenario: User opens the debug window
- **WHEN** the user enables Show Debug Window
- **THEN** Debug Mode is enabled for the active workspace
- **AND** a modeless resizable debug window opens and displays subsequent telemetry

#### Scenario: User turns off Debug Mode
- **WHEN** the user turns Debug Mode off while the debug window is open
- **THEN** Show Debug Window is turned off
- **AND** the window closes and subsequent telemetry calls are not persisted

#### Scenario: User hides the debug window
- **WHEN** the user closes or hides the debug window while Debug Mode is on
- **THEN** the debug window closes
- **AND** Debug Mode continues recording telemetry in SQLite

#### Scenario: Active workspace changes
- **WHEN** the active workspace changes while the debug window is open
- **THEN** the window follows the new workspace's Debug Mode and Show Debug Window settings
- **AND** previous-workspace events are cleared from the display before new-workspace events appear

### Requirement: Debug window Copy and Clear operate on displayed text only
The debug window SHALL provide keyboard-reachable Copy and Clear actions. Copy SHALL copy all text currently displayed in the window. Clear SHALL remove all currently displayed text from the window's in-memory buffer without deleting database records, changing Debug Mode, or stopping future display and capture.

#### Scenario: User copies debug output
- **WHEN** the user activates Copy
- **THEN** all text currently displayed in the debug window is placed on the clipboard in display order
- **AND** stored telemetry records remain unchanged

#### Scenario: User clears debug output
- **WHEN** the user activates Clear
- **THEN** the current debug-window display buffer becomes empty
- **AND** telemetry records remain searchable and exportable from SQLite
- **AND** later telemetry events appear in the window while it remains open
