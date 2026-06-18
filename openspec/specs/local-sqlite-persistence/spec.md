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
FusionCanvas SHALL persist the Phase 0 core workspace entities included in the active model.

#### Scenario: Workspace snapshot is saved
- **WHEN** a workspace snapshot contains stores, niches, groups, listings, assets, prompts, and tags
- **THEN** each included entity type can be saved to the local SQLite database
- **AND** stable identity, name, optional description, timestamps, archive state, and flexible metadata are preserved where present in the active model

#### Scenario: Empty workspace is loaded
- **WHEN** no local workspace database exists at the configured path
- **THEN** loading workspace data returns an empty structured workspace instead of failing because the file is missing

### Requirement: Core workspace entities are loadable after save
FusionCanvas SHALL load previously saved structured workspace data from the local SQLite database.

#### Scenario: Workspace data is reopened
- **WHEN** structured workspace data has been saved locally
- **AND** the application later loads from the same database
- **THEN** the saved stores, niches, groups, listings, assets, prompts, and tags are reconstructed with their persisted values

### Requirement: Entity relationships are preserved
FusionCanvas SHALL preserve relationships between persisted core workspace entities.

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

### Requirement: Phase 0 persistence avoids advanced storage scope
The Phase 0 SQLite persistence capability SHALL avoid storage behavior that belongs to later workflow or platform changes.

#### Scenario: Contributor reviews Phase 0 persistence scope
- **WHEN** a contributor reviews the FC-0003 implementation
- **THEN** it does not implement cloud sync, multi-user collaboration, encryption, full backup/restore, import/export packages, marketplace synchronization, AI provider history, plugin data stores, or advanced search optimization
