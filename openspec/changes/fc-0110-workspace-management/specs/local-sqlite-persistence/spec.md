## MODIFIED Requirements

### Requirement: Core workspace entities are persistable
FusionCanvas SHALL persist the active core workspace entities included in the active model.

#### Scenario: Workspace snapshot is saved
- **WHEN** a workspace snapshot contains workspaces, stores, niches, groups, listings, assets, prompts, and tags
- **THEN** each included entity type can be saved to the local SQLite database
- **AND** stable identity, name, optional description, timestamps, archive state, and flexible metadata are preserved where present in the active model
- **AND** each store's workspace ownership is preserved

### Requirement: Core workspace entities are loadable after save
FusionCanvas SHALL load previously saved structured workspace data from the local SQLite database.

#### Scenario: Workspace data is reopened
- **WHEN** structured workspace data has been saved locally
- **AND** the application later loads from the same database
- **THEN** the saved workspaces, stores, niches, groups, listings, assets, prompts, and tags are reconstructed with their persisted values
- **AND** stores are reconstructed with their workspace ownership

### Requirement: Entity relationships are preserved
FusionCanvas SHALL preserve relationships between persisted core workspace entities.

#### Scenario: Workspace and store relationships are loaded
- **WHEN** a workspace contains a store
- **AND** the workspace is saved and loaded again
- **THEN** the loaded records preserve the relationship between the workspace and store

#### Scenario: Topic and listing relationships are loaded
- **WHEN** a store contains a niche, a nested group, and a listing associated with that topic context
- **AND** the workspace is saved and loaded again
- **THEN** the loaded records preserve the store, niche, group, and listing relationships

#### Scenario: Context relationships are loaded
- **WHEN** assets, prompts, and tags are connected to relevant workspace entities
- **AND** the workspace is saved and loaded again
- **THEN** the loaded records preserve those context relationships

## ADDED Requirements

### Requirement: Persistence migrates existing stores into a default workspace
FusionCanvas SHALL migrate pre-workspace SQLite databases by creating a default workspace and assigning existing stores to it.

#### Scenario: Older database contains stores
- **WHEN** the local database schema version predates workspace support
- **AND** the database contains one or more stores
- **THEN** the persistence layer creates a default workspace during migration
- **AND** every existing store is assigned to that default workspace
- **AND** existing store-scoped child records retain their relationships

#### Scenario: Older database contains no stores
- **WHEN** the local database schema version predates workspace support
- **AND** the database contains no stores
- **THEN** the persistence layer creates the workspace-capable schema safely
- **AND** loading structured workspace data succeeds without requiring manual repair

### Requirement: Persistence enforces store workspace ownership
FusionCanvas SHALL persist and load every store with a valid workspace identity.

#### Scenario: Store without workspace identity cannot be saved
- **WHEN** a workspace snapshot contains a store without valid workspace ownership
- **THEN** the persistence layer rejects or prevents saving invalid store data
- **AND** the database is not left partially written

#### Scenario: Workspace delete cascades only through explicit application behavior
- **WHEN** a workspace exists in SQLite
- **THEN** the schema preserves store workspace ownership through a database relationship
- **AND** permanent workspace deletion with owned stores occurs only through explicit application behavior that removes the workspace and its owned store-scoped records together
