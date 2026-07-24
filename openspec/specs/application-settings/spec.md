# Application Settings

## Purpose

Defines the user-facing application-wide Settings surface and the local preference behavior that spans workspaces, such as application appearance.

## Requirements

### Requirement: Settings uses a focused sectioned surface
FusionCanvas SHALL present application-wide preferences in a focused Settings window with a persistent section rail on the left and the selected section content on the right.

#### Scenario: User opens Settings
- **WHEN** the user activates the Settings button in the main shell
- **THEN** FusionCanvas opens one Settings window without replacing or clearing the current main-window context
- **AND** the `General` section is selected and its pane is visible

#### Scenario: User navigates settings sections
- **WHEN** the user selects `Workspace` in the vertically stacked section rail
- **THEN** the Workspace pane replaces the General pane in the content region
- **AND** the selected section remains visibly and programmatically identified

#### Scenario: User reopens Settings
- **WHEN** the user closes Settings and opens it again
- **THEN** the `General` section is selected for the new Settings session

#### Scenario: User operates Settings with a keyboard
- **WHEN** keyboard focus enters the Settings window
- **THEN** the section rail, the active section controls, and the window dismissal action are reachable in a predictable tab order
- **AND** closing the window returns focus to the Settings button when it remains available

### Requirement: General settings control application appearance
FusionCanvas SHALL provide a `Dark mode` toggle in the General section that selects an application-wide Light or Dark appearance and applies changes immediately.

#### Scenario: No appearance preference has been saved
- **WHEN** FusionCanvas starts without a saved appearance preference
- **THEN** the application uses the Light appearance
- **AND** the `Dark mode` toggle is off

#### Scenario: User enables Dark mode
- **WHEN** the user switches `Dark mode` on
- **THEN** the Settings window, main window, and every other open FusionCanvas window immediately use the Dark appearance
- **AND** windows opened later in the same session use the Dark appearance

#### Scenario: User disables Dark mode
- **WHEN** the user switches `Dark mode` off
- **THEN** all open FusionCanvas windows immediately use the Light appearance
- **AND** windows opened later in the same session use the Light appearance

#### Scenario: User closes Settings after changing appearance
- **WHEN** the user changes `Dark mode` and closes Settings
- **THEN** the selected appearance remains active without a separate Save or Apply action
- **AND** FusionCanvas does not show an unsaved-changes prompt

#### Scenario: User changes appearance repeatedly
- **WHEN** the user changes `Dark mode` repeatedly before an earlier preference write completes
- **THEN** the application appearance follows the most recent toggle state
- **AND** the most recent toggle state is the value retained for the next application start

### Requirement: Appearance preference persists locally
FusionCanvas SHALL persist the selected appearance as an application-wide local preference that is independent of workspace data.

#### Scenario: Saved Dark preference survives restart
- **WHEN** the user enables Dark mode, closes FusionCanvas, and starts it again
- **THEN** FusionCanvas loads the Dark appearance before presenting the main window
- **AND** the `Dark mode` toggle is on when Settings opens

#### Scenario: User switches active workspace
- **WHEN** the selected appearance is Dark and the active workspace changes
- **THEN** the selected appearance remains Dark
- **AND** no workspace record is changed to store the appearance preference

#### Scenario: Saved preference cannot be read
- **WHEN** the local settings file is missing, invalid, or unreadable at startup
- **THEN** FusionCanvas starts with the Light appearance
- **AND** the user can select and save an appearance preference during the session

#### Scenario: Preference cannot be saved
- **WHEN** the user changes `Dark mode` and the local preference cannot be written
- **THEN** FusionCanvas keeps the selected appearance for the current session
- **AND** the General pane reports that the preference could not be saved and may not survive restart

### Requirement: Application themes remain visually coherent
FusionCanvas SHALL use shared semantic Light and Dark resources so current application surfaces present one coherent appearance with readable primary, secondary, accent, selection, warning, and destructive states.

#### Scenario: User changes appearance with multiple windows open
- **WHEN** the main window, Settings, and another FusionCanvas window are open and the user changes `Dark mode`
- **THEN** backgrounds, surfaces, borders, text, controls, selections, and status treatments in each window resolve from the selected appearance
- **AND** no open window remains styled as the previous appearance

#### Scenario: User reviews functional color states
- **WHEN** the selected appearance changes while warning, destructive, selected, disabled, or accent content is visible
- **THEN** those states remain distinguishable and their text remains readable in the selected appearance

### Requirement: Workspace settings expose current scope and management
FusionCanvas SHALL provide a Workspace section that displays the current active workspace and delegates workspace administration to the existing workspace-management surface.

#### Scenario: Workspace section has an active workspace
- **WHEN** the user opens the Workspace settings section and an active workspace exists
- **THEN** the pane displays the active workspace name
- **AND** it provides a `Manage workspaces` button

#### Scenario: Workspace section has no active workspace
- **WHEN** the user opens the Workspace settings section and no active workspace exists
- **THEN** the pane displays `No workspace`
- **AND** `Manage workspaces` remains available so the user can create or restore a workspace

#### Scenario: Active workspace changes while Settings is open
- **WHEN** workspace management selects a different active workspace while Settings remains open
- **THEN** the Workspace pane displays the newly active workspace
- **AND** the main shell workspace indicator displays the same workspace
