## ADDED Requirements

### Requirement: Ideation rejections persist locally
FusionCanvas SHALL persist every confirmed ideation rejection in local SQLite with stable identity, store and niche ownership, optional group association, rejected Idea text, optional reason, generation mode, and creation timestamp.

#### Scenario: Rejection is saved and reloaded
- **WHEN** a confirmed ideation rejection is saved and the same workspace is reopened
- **THEN** its identity, store, niche, optional group, text, optional reason, mode, and timestamp are reconstructed exactly

#### Scenario: Group-scoped rejection is stored
- **WHEN** a candidate generated for a group is rejected
- **THEN** the persisted rejection retains that group association while the group exists

#### Scenario: Niche-root rejection is stored
- **WHEN** a candidate generated without a selected group is rejected
- **THEN** the persisted rejection retains its niche ownership with no group association

#### Scenario: Rejection save fails
- **WHEN** persistence fails before a rejection save completes
- **THEN** no partial rejection row or partial workspace snapshot is committed

### Requirement: SQLite migrates safely for ideation rejections
FusionCanvas SHALL add ideation-rejection storage through the next available versioned SQLite migration and SHALL preserve all pre-existing workspace records and relationships.

#### Scenario: Previous supported database is opened
- **WHEN** a database at the immediately previous supported schema version is opened
- **THEN** the migration creates ideation-rejection storage
- **AND** existing workspaces, stores, niches, groups, Items, assets, prompts, tags, links, metadata, identities, and files remain unchanged

#### Scenario: New database is created
- **WHEN** FusionCanvas creates a new workspace database
- **THEN** the current schema includes ideation-rejection storage and its store, niche, and optional group relationships

#### Scenario: Migrated rejection data is saved
- **WHEN** migration succeeds and a rejection is later saved
- **THEN** the rejection can be loaded and supplied to later Ideation context

#### Scenario: Migration fails
- **WHEN** the ideation-rejection migration cannot complete
- **THEN** the migration is rolled back
- **AND** the database is not left at the new schema version with incomplete storage
