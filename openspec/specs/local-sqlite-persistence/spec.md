# Local SQLite Persistence

## Purpose

Defines the local SQLite persistence expectations for structured FusionCanvas workspace data, including entity storage, relationship preservation, schema initialization, and version safeguards.

## Requirements

### Requirement: Local SQLite is the structured workspace data store
FusionCanvas SHALL use a local SQLite database as the primary store for structured workspace data.

#### Scenario: Contributor inspects persistence implementation
- **WHEN** a contributor reviews the structured persistence adapter
- **THEN** workspace data is stored in a local SQLite database
- **AND** the implementation does not require cloud services, remote accounts, or network access for primary workspace data

### Requirement: Persistence is exposed through application contracts
Structured workspace persistence SHALL be accessed through application-facing contracts rather than directly from UI or domain code.

#### Scenario: Contributor reviews layer dependencies
- **WHEN** a contributor inspects persistence-related project references
- **THEN** the SQLite implementation is owned by the integration layer
- **AND** the UI layer does not issue SQLite commands directly
- **AND** the domain layer does not reference SQLite packages or database abstractions

### Requirement: Core workspace entities are persistable
FusionCanvas SHALL persist the active core workspace entities included in the active model.

#### Scenario: Workspace snapshot is saved
- **WHEN** a workspace snapshot contains workspaces, stores, niches, groups, listings, assets, prompts, and tags
- **THEN** each included entity type can be saved to the local SQLite database
- **AND** stable identity, name, optional description, timestamps, archive state, and flexible metadata are preserved where present in the active model
- **AND** each store's workspace ownership is preserved

#### Scenario: Empty workspace is loaded
- **WHEN** no local workspace database exists at the configured path
- **THEN** loading workspace data returns an empty structured workspace instead of failing because the file is missing

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

### Requirement: Local storage separates structured data from file contents
FusionCanvas SHALL store references to workspace files or external resources instead of embedding large file contents in SQLite.

#### Scenario: Asset record is persisted
- **WHEN** an asset record references a managed workspace file or original source location
- **THEN** SQLite stores the file reference data needed to reconnect the asset
- **AND** the database does not embed the binary asset file contents

### Requirement: Flexible metadata is supported
FusionCanvas SHALL support flexible metadata for persisted core workspace records.

#### Scenario: Entity metadata is round-tripped
- **WHEN** a core workspace entity includes metadata supported by the active model
- **AND** the entity is saved and loaded again
- **THEN** the metadata value is preserved with the entity

### Requirement: Save operations protect against partial writes
FusionCanvas SHALL save structured workspace data in a way that avoids partially written workspace state when a save operation fails.

#### Scenario: Save is interrupted by an error
- **WHEN** a save operation fails before all structured data changes are written
- **THEN** the local SQLite database is not left with only part of the new workspace snapshot committed

### Requirement: Schema initialization is automatic
FusionCanvas SHALL initialize the local SQLite schema needed for structured workspace data when a workspace database is first used.

#### Scenario: New database path is saved
- **WHEN** workspace data is saved to a new local database path
- **THEN** the required SQLite schema is created automatically
- **AND** the workspace data can be loaded from that database afterward

### Requirement: Schema versioning and migration boundaries are defined
FusionCanvas SHALL track the local SQLite schema version and define a safe boundary for future migrations.

#### Scenario: Database is current version
- **WHEN** the local database schema version matches the current application schema version
- **THEN** the persistence layer can load and save structured workspace data

#### Scenario: Database version is older
- **WHEN** the local database schema version is older than the current application schema version
- **THEN** the persistence layer applies known migrations or reports that the database cannot be opened safely

#### Scenario: Database version is newer
- **WHEN** the local database schema version is newer than the current application schema version
- **THEN** the persistence layer refuses unsafe writes and reports that the database requires a newer application version

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

### Requirement: Phase 0 persistence avoids advanced storage scope
The Phase 0 SQLite persistence capability SHALL avoid storage behavior that belongs to later workflow or platform changes.

#### Scenario: Contributor reviews Phase 0 persistence scope
- **WHEN** a contributor reviews the FC-0003 implementation
- **THEN** it does not implement cloud sync, multi-user collaboration, encryption, full backup/restore, import/export packages, marketplace synchronization, AI provider history, plugin data stores, or advanced search optimization

### Requirement: Tag color is persisted with a versioned migration
FusionCanvas SHALL persist an optional color on every tag in a dedicated `tags.color` column, SHALL round-trip that color through workspace save and load, and SHALL introduce the column through a versioned SQLite migration from schema version 3 to 4 with safe backfill for existing tags.

#### Scenario: Tag color is saved and reloaded
- **WHEN** a tag with a normalized `#RRGGBB` color is saved to the local SQLite database
- **AND** the application later loads from the same database
- **THEN** the loaded tag preserves the exact normalized color value

#### Scenario: Tag with no color is round-tripped
- **WHEN** a tag with no color is saved and loaded again
- **THEN** the loaded tag preserves the null color
- **AND** the application renders the tag in the default accent color

#### Scenario: Pre-migration database is opened
- **WHEN** a local database at schema version 3 contains existing tags without a color column
- **AND** the application opens or saves that database
- **THEN** the persistence layer applies the 3 → 4 migration that adds the nullable `tags.color` column
- **AND** existing tags receive a null color
- **AND** existing `ListingTag` rows, store ownership, archive state, metadata, and stable identities remain intact

#### Scenario: New database is created
- **WHEN** workspace data is saved to a new local database path
- **THEN** the schema is created at the current schema version including `tags.color`
- **AND** tags and listing tag links can be loaded from that database afterward

#### Scenario: Database newer than supported is refused
- **WHEN** the local database schema version is newer than the current application schema version
- **THEN** the persistence layer refuses unsafe writes and reports that the database requires a newer application version
- **AND** the database is not partially written
