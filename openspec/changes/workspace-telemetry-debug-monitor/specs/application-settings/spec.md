## ADDED Requirements

### Requirement: Workspace Settings exposes diagnostic controls
FusionCanvas SHALL expose the active workspace's Debug Mode, retention period, Show Debug Window, telemetry search, export, and delete controls in a progressively disclosed Diagnostics group in Settings → Workspace. Workspace-specific controls SHALL be unavailable when no workspace is active, and the pane SHALL explain why.

#### Scenario: Active workspace is available
- **WHEN** the user opens Settings → Workspace with an active workspace
- **THEN** the Diagnostics group displays that workspace's Debug Mode and retention settings
- **AND** Show Debug Window, telemetry search, export, and delete actions are available according to their state

#### Scenario: No workspace is active
- **WHEN** the user opens Settings → Workspace without an active workspace
- **THEN** the Diagnostics group explains that a workspace is required
- **AND** workspace-specific capture, retention, search, export, delete, and window controls are disabled

#### Scenario: User searches telemetry
- **WHEN** the user enters a time range and selects one or more application areas, then activates Search
- **THEN** matching telemetry results and an empty or error state appear in the Diagnostics group

#### Scenario: User confirms destructive deletion
- **WHEN** the user requests deletion of telemetry
- **THEN** Settings asks for explicit confirmation identifying the active workspace before deletion proceeds
- **AND** cancellation returns to the Diagnostics group without changing records
