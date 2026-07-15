## ADDED Requirements

### Requirement: Main window presents compact workspace management
The main FusionCanvas window SHALL present a compact active workspace indicator and a workspace management entry point above store selection when workspace behavior is available.

#### Scenario: User views workspace shell with active workspace
- **WHEN** the main window is displayed and an active workspace exists
- **THEN** the sidebar shows the active workspace as a small top-level selected scope
- **AND** store selection and navigation are visually subordinate to the selected workspace

#### Scenario: User switches workspace from workspace management
- **WHEN** the user opens workspace management and selects a different active workspace
- **THEN** the main window refreshes store selection and navigation for the selected workspace
- **AND** stores from the previously selected workspace are not shown in the normal store selector

#### Scenario: User opens workspace management
- **WHEN** the user chooses to manage workspaces from the regular workspace UI
- **THEN** FusionCanvas opens a workspace management surface or window
- **AND** workspace lifecycle and switching actions are available from that management surface rather than inline in the navigation tree

### Requirement: Main window preserves empty workspace clarity
The main FusionCanvas window SHALL distinguish an empty workspace from an empty store.

#### Scenario: Active workspace has no stores
- **WHEN** the active workspace contains no stores
- **THEN** FusionCanvas shows the first-store prompt for that workspace
- **AND** the prompt does not imply that other workspaces have no stores

#### Scenario: Workspace management is open for an empty workspace
- **WHEN** the workspace management window is open
- **AND** the active workspace changes to a workspace with no stores
- **THEN** FusionCanvas defers the first-store prompt until workspace management closes
- **AND** the main window does not stack a store prompt over workspace management

#### Scenario: No active workspace is available
- **WHEN** no active workspace is available
- **THEN** FusionCanvas shows an empty workspace-level state with a way to create or restore a workspace
- **AND** store and niche actions that require an active workspace are unavailable
