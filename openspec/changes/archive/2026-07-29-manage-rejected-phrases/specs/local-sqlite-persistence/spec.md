## ADDED Requirements

### Requirement: Ideation rejections track optional update time
FusionCanvas SHALL persist an optional `UpdatedAt` timestamp on every ideation rejection, SHALL leave it null for rejections that have never been edited, SHALL set or advance it whenever a rejection's phrase or reason is edited, and SHALL round-trip the null and non-null values through workspace save and load.

#### Scenario: Never-edited rejection round-trips with null update time
- **WHEN** a rejection captured by Ideation is saved without any later edit and the workspace is reopened
- **THEN** the loaded rejection preserves a null `UpdatedAt`
- **AND** its `CreatedAt` is unchanged

#### Scenario: Edited rejection round-trips with update time
- **WHEN** a rejection whose phrase or reason has been edited is saved and the workspace is reopened
- **THEN** the loaded rejection preserves the most recent `UpdatedAt` value
- **AND** its `CreatedAt`, identity, store, niche, optional group, mode, phrase, and reason remain unchanged

### Requirement: SQLite migrates safely to add ideation-rejection update time
FusionCanvas SHALL add the `ideation_rejections.updated_at` column through the next versioned SQLite migration, SHALL create the column as nullable, SHALL preserve every existing rejection row and every existing column value, and SHALL leave pre-existing workspace, store, niche, group, item, tag, and asset records intact.

#### Scenario: Pre-migration database is opened
- **WHEN** a local database at the previous schema version contains existing ideation rejections without an `updated_at` column
- **AND** the application opens or saves that database
- **THEN** the migration applies that adds the nullable `ideation_rejections.updated_at` column
- **AND** existing rejections receive a null `updated_at`
- **AND** existing rejection identities, scope, text, reason, mode, `created_at`, and all unrelated workspace records remain intact

#### Scenario: New database is created
- **WHEN** workspace data is saved to a new local database path
- **THEN** the schema is created at the current schema version including `ideation_rejections.updated_at`
- **AND** rejections can be loaded from that database afterward

#### Scenario: Migration fails
- **WHEN** any migration operation fails
- **THEN** the transaction rolls back
- **AND** the prior database remains readable by the prior schema implementation
- **AND** no partially migrated schema is reported as current
