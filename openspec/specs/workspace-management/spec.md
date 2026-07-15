# Workspace Management

## Purpose

Defines user-facing workspace lifecycle and active workspace selection behavior for separating personal, client, portfolio, and project settings.

## Requirements

### Requirement: Workspace management creates workspaces
FusionCanvas SHALL allow users to create workspaces as top-level organizational scopes for separate personal, client, brand portfolio, or project settings.

#### Scenario: User creates first workspace
- **WHEN** no workspace exists and the user creates a workspace with a valid name
- **THEN** FusionCanvas persists a new active workspace with stable identity, name, timestamps, archive state, optional description, and flexible metadata
- **AND** the workspace becomes available as the top-level scope for store organization

#### Scenario: User creates another workspace
- **WHEN** at least one workspace exists and the user creates another workspace with a valid name
- **THEN** FusionCanvas persists the new workspace without moving stores from existing workspaces
- **AND** workspace selection can switch between the available active workspaces

#### Scenario: User attempts duplicate active workspace name
- **WHEN** an active workspace already uses a normalized name
- **AND** the user creates another active workspace with the same normalized name
- **THEN** FusionCanvas rejects the create operation
- **AND** the existing workspace remains unchanged

### Requirement: Workspace management edits workspace context
FusionCanvas SHALL allow users to rename workspaces and edit basic workspace-level context while preserving workspace identity and contained stores.

#### Scenario: User renames a workspace
- **WHEN** the user renames an existing workspace to a valid new name
- **THEN** FusionCanvas persists the new workspace name
- **AND** stores, niches, groups, listings, assets, prompts, and tags below that workspace remain associated with the same workspace through their stores

#### Scenario: User edits workspace notes
- **WHEN** the user updates workspace description or metadata-backed notes
- **THEN** FusionCanvas persists the updated workspace context
- **AND** the update does not require changes to contained stores or child records

### Requirement: Workspace management distinguishes active and archived workspaces
FusionCanvas SHALL distinguish active workspaces from archived workspaces and SHALL keep archived workspaces out of normal active workspace selection by default.

#### Scenario: User archives workspace
- **WHEN** the user archives an active workspace
- **THEN** FusionCanvas marks the workspace as archived
- **AND** the workspace no longer appears in the normal active workspace selector
- **AND** the workspace's contained stores and child work remain preserved

#### Scenario: User reviews archived workspaces
- **WHEN** the user opens workspace management
- **THEN** FusionCanvas shows archived workspaces separately from active workspaces
- **AND** each archived workspace remains available for review or restoration

### Requirement: Workspace management restores archived workspaces
FusionCanvas SHALL allow users to restore archived workspaces so inactive top-level scopes can return to normal work.

#### Scenario: User restores archived workspace
- **WHEN** the user restores an archived workspace
- **THEN** FusionCanvas marks the workspace as active
- **AND** the workspace appears in the normal active workspace selector
- **AND** contained stores and child work retain their existing associations

#### Scenario: User restores workspace with conflicting name
- **WHEN** the user attempts to restore an archived workspace
- **AND** an active workspace already uses the same normalized name
- **THEN** FusionCanvas blocks restoration until the conflict is resolved
- **AND** the archived workspace remains archived

### Requirement: Workspace management deletes workspaces with explicit confirmation
FusionCanvas SHALL allow permanent workspace deletion only after explicit confirmation, and SHALL require typed workspace-name confirmation before deleting a workspace that contains stores or child data.

#### Scenario: User deletes empty workspace
- **WHEN** the user requests permanent deletion for a workspace with no stores
- **AND** the user confirms the deletion warning
- **THEN** FusionCanvas permanently removes the workspace
- **AND** the workspace no longer appears in active or archived workspace lists

#### Scenario: User attempts to delete non-empty workspace without typed confirmation
- **WHEN** the user requests permanent deletion for a workspace that contains active or archived stores
- **AND** the typed confirmation does not exactly match the workspace name
- **THEN** FusionCanvas blocks the deletion
- **AND** FusionCanvas explains that deleting a non-empty workspace requires typing the workspace name

#### Scenario: User attempts to delete the last workspace
- **WHEN** only one workspace exists
- **AND** the user requests permanent deletion for that workspace
- **THEN** FusionCanvas blocks the deletion
- **AND** at least one workspace remains available

#### Scenario: User deletes non-empty workspace with typed confirmation
- **WHEN** the user requests permanent deletion for a workspace that contains active or archived stores
- **AND** the user types the workspace name exactly to confirm
- **THEN** FusionCanvas permanently removes the workspace and all stores and child records owned through that workspace
- **AND** the deleted workspace no longer appears in active or archived workspace lists

### Requirement: Workspace management selects active workspace scope
FusionCanvas SHALL allow users to select an active workspace as the top-level scope for store browsing and context-aware work.

#### Scenario: User selects active workspace
- **WHEN** the user selects an active workspace
- **THEN** FusionCanvas sets that workspace as the active workspace scope
- **AND** normal store selection, store management, navigation, and context-aware work are scoped to that workspace

#### Scenario: User cannot select archived workspace as active scope
- **WHEN** the user attempts to select an archived workspace through the normal active workspace flow
- **THEN** FusionCanvas does not set that workspace as the active workspace scope
- **AND** FusionCanvas requires the workspace to be restored before normal use

#### Scenario: Active child scope resets when workspace changes
- **WHEN** the active workspace changes
- **THEN** FusionCanvas clears or reselects the active store and active niche only from records that belong to the newly active workspace
- **AND** stores from the previously active workspace do not remain selected
