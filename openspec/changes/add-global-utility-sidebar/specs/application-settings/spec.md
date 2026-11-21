## REMOVED Requirements

### Requirement: Workspace settings expose current scope and management
**Reason**: Workspace selection and administration belong beside workspace-scoped navigation, not in the application-wide Settings surface.
**Migration**: Use the active workspace control in the main navigation pane to switch workspaces or open the existing workspace-management window. Workspace diagnostics remain discoverable in Settings with their active workspace scope clearly shown.

## ADDED Requirements

### Requirement: Workspace diagnostics identify their data scope
FusionCanvas SHALL present workspace-specific diagnostics as a clearly labeled section of the application Settings window and SHALL identify the active workspace whose diagnostics are being viewed or changed.

#### Scenario: User opens diagnostics with an active workspace
- **WHEN** the user opens Workspace Diagnostics in Settings and an active workspace exists
- **THEN** the section identifies the active workspace by name
- **AND** telemetry controls and records apply only to that workspace

#### Scenario: Active workspace changes while diagnostics are open
- **WHEN** the active workspace changes while Settings remains open on Workspace Diagnostics
- **THEN** the section updates its workspace identity and loads the newly active workspace's diagnostic settings
- **AND** it does not display or modify telemetry from the previous workspace as if it belonged to the new one

#### Scenario: No active workspace exists
- **WHEN** the user opens Workspace Diagnostics without an active workspace
- **THEN** the section identifies that no workspace is selected
- **AND** workspace-specific diagnostic controls remain unavailable until a workspace is selected

### Requirement: Application Settings sections distinguish global preferences from scoped diagnostics
FusionCanvas SHALL present one application-wide Settings window with navigable sections for application preferences and information, and SHALL label Workspace Diagnostics as scoped to the active workspace.

#### Scenario: User navigates application Settings sections
- **WHEN** the user selects a Settings section
- **THEN** the selected section replaces the previous content in the same Settings window
- **AND** the section's title and scope make clear whether it controls app-wide preferences or active-workspace diagnostics
