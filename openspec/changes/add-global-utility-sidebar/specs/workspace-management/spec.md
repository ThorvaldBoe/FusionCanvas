## REMOVED Requirements

### Requirement: Settings delegates workspace administration
**Reason**: Nesting workspace administration under global Settings obscures the distinction between app-wide preferences and workspace-context actions.
**Migration**: Open workspace administration from the active workspace control in the main navigation pane. The dedicated workspace-management window remains the owner of workspace creation, editing, import/export, archive, restore, and deletion.

## ADDED Requirements

### Requirement: Main navigation exposes active workspace selection and management
FusionCanvas SHALL provide an actionable active-workspace control in the workspace navigation pane, separate from the global utility rail, for switching workspaces and opening the existing workspace-management surface.

#### Scenario: User opens the workspace selector
- **WHEN** the user activates the current workspace control in the navigation pane
- **THEN** FusionCanvas presents the available active workspaces and a `Manage workspaces` action
- **AND** the selected workspace is visibly identified

#### Scenario: User switches to an active workspace
- **WHEN** the user selects a different active workspace from the navigation-pane selector
- **THEN** FusionCanvas changes the active workspace using the existing workspace-switch behavior
- **AND** the selected store and navigation context are refreshed or cleared according to existing workspace-selection rules
- **AND** the selected workspace name updates in the navigation pane

#### Scenario: No workspace is selected
- **WHEN** no active workspace is selected
- **THEN** the navigation-pane control displays `No workspace`
- **AND** it still offers `Manage workspaces` so the user can create or select a workspace

#### Scenario: User opens workspace management
- **WHEN** the user activates `Manage workspaces` from the workspace selector
- **THEN** FusionCanvas opens or focuses one existing workspace-management window
- **AND** that window remains the only surface for workspace creation, editing, import/export, archive, restore, and deletion

#### Scenario: User closes workspace management
- **WHEN** the workspace-management window closes
- **THEN** the main window retains the current workspace and document context
- **AND** keyboard focus returns to the workspace control that opened it when that control remains available

#### Scenario: Workspace selection is available only through active choices
- **WHEN** the workspace selector lists choices
- **THEN** it offers active workspaces only
- **AND** archived workspaces remain reviewable and restorable through workspace management
