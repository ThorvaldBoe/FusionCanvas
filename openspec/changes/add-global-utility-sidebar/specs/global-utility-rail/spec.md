## ADDED Requirements

### Requirement: Main shell provides a global utility rail
FusionCanvas SHALL provide a compact persistent utility rail at the leading edge of the main shell for application-wide destinations and actions, visually separate from workspace-scoped navigation.

#### Scenario: User opens global Settings
- **WHEN** the user activates the Settings control in the utility rail
- **THEN** FusionCanvas opens or focuses the single application-wide Settings window
- **AND** the current workspace and document context remain available
- **AND** keyboard focus returns to the rail Settings control when Settings closes and the control remains available

#### Scenario: Rail contains no workspace-scoped actions
- **WHEN** the main shell displays the utility rail
- **THEN** the rail presents only application-wide destinations or actions
- **AND** workspace selection and workspace administration remain in the workspace navigation pane

### Requirement: Utility rail exposes actionable updates progressively
FusionCanvas SHALL place the existing update action in the global utility rail and SHALL show it only when an update is available or downloading for the selected Stable or Preview update track.

#### Scenario: Update is available on the selected track
- **WHEN** update discovery finds a compatible newer release for the selected track
- **THEN** the rail exposes an accessible update action that starts the existing explicit download flow
- **AND** the action does not cover or replace the Settings control

#### Scenario: No actionable update is available
- **WHEN** no compatible update is available for the selected track
- **THEN** the rail hides the update action without reserving a permanent update-button footprint

#### Scenario: Track changes
- **WHEN** the user changes the update track in application Settings
- **THEN** update discovery and rail visibility follow the newly selected track
- **AND** Stable/Preview release and install policy remains owned by the update capability

### Requirement: Utility rail remains keyboard and accessibility reachable
FusionCanvas SHALL expose a stable accessible name and keyboard focus target for each utility-rail action and SHALL preserve a usable workspace navigation and document area at the supported main-window minimum size.

#### Scenario: User navigates the rail with the keyboard
- **WHEN** keyboard focus enters the main shell utility controls
- **THEN** visible rail actions are reachable in a predictable tab order
- **AND** each icon-only action exposes an accessible name and descriptive tooltip

#### Scenario: Main window is at minimum supported size
- **WHEN** the main window is at its supported minimum dimensions
- **THEN** the utility rail, workspace navigation pane, and document area remain usable
- **AND** the workspace pane remains resizable within the remaining layout constraints
