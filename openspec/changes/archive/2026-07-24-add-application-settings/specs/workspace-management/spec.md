## ADDED Requirements

### Requirement: Settings delegates workspace administration
FusionCanvas SHALL launch the existing workspace-management workflow from the Workspace settings section without duplicating workspace selection, creation, editing, archive, restore, or deletion behavior in Settings.

#### Scenario: User activates Manage workspaces
- **WHEN** the user activates `Manage workspaces` in the Workspace settings section
- **THEN** FusionCanvas opens one workspace-management window using the current workspace-management state
- **AND** the Settings window remains available behind the focused management window

#### Scenario: User attempts to open workspace management again
- **WHEN** the workspace-management window is already open
- **THEN** FusionCanvas does not create a duplicate workspace-management window
- **AND** the existing management window remains the active management surface

#### Scenario: User closes workspace management
- **WHEN** the user closes the workspace-management window
- **THEN** the Settings window returns to the foreground with the Workspace section still selected
- **AND** keyboard focus returns to `Manage workspaces`

#### Scenario: Workspace management changes active workspace
- **WHEN** the user selects a different active workspace in workspace management
- **THEN** the existing workspace selection, store reset, navigation refresh, and empty-state rules are preserved
- **AND** Settings adds no second workspace mutation path
