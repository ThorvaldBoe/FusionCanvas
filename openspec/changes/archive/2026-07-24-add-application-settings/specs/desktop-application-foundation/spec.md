## MODIFIED Requirements

### Requirement: Main window presents compact workspace management
The main FusionCanvas window SHALL present a compact active workspace indicator and a Settings entry point in the navigation-pane header above store selection when workspace behavior is available.

#### Scenario: User views workspace shell with active workspace
- **WHEN** the main window is displayed and an active workspace exists
- **THEN** the header row shows `FusionCanvas`, a compact workspace icon and active workspace name, and a Settings button
- **AND** the workspace name truncates rather than displacing the application identity or Settings button at the navigation pane's minimum supported width
- **AND** the workspace indicator tooltip says `Manage workspaces in settings`
- **AND** store selection and navigation are visually subordinate to the selected workspace
- **AND** the former `Workspaces` label and workspace-management cog row are not shown

#### Scenario: User views workspace shell without an active workspace
- **WHEN** the main window is displayed and no active workspace exists
- **THEN** the header workspace indicator shows its workspace icon and `No workspace`
- **AND** the Settings button remains available

#### Scenario: User switches workspace from workspace management
- **WHEN** the user opens workspace management from Settings and selects a different active workspace
- **THEN** the main window refreshes its workspace indicator, store selection, and navigation for the selected workspace
- **AND** stores from the previously selected workspace are not shown in the normal store selector

#### Scenario: User opens workspace management
- **WHEN** the user opens Settings, selects `Workspace`, and activates `Manage workspaces`
- **THEN** FusionCanvas opens the existing workspace-management surface or window
- **AND** workspace lifecycle and switching actions are available from that management surface rather than inline in Settings or the navigation tree

