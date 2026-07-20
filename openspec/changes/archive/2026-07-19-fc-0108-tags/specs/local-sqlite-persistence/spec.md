## ADDED Requirements

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
