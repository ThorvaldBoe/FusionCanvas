## ADDED Requirements

### Requirement: Combined schema version and snapshot ownership remain coherent
FusionCanvas SHALL use SQLite schema version 7 as the shared current version for workspace, Ideation rejection, and global Snowclone persistence, SHALL preserve global Snowclone tables during full workspace-snapshot saves, and SHALL preserve every supplied Ideation rejection during filtering, merging, saving, loading, and workspace package migration.

#### Scenario: Current database opens
- **WHEN** a schema-v7 database containing workspace records, Ideation rejections, and global Snowclones is opened
- **THEN** all records load through their owning repositories without a schema mutation

#### Scenario: Workspace snapshot is saved
- **WHEN** a full workspace snapshot containing Ideation rejections is saved
- **THEN** those rejections round-trip with their relationships intact
- **AND** global Snowclone records and initialization state remain unchanged

#### Scenario: Workspace package database is created
- **WHEN** a filtered workspace snapshot is written to a package database
- **THEN** the embedded database is schema v7
- **AND** it contains only the filtered workspace-owned rejection history and no global Snowclone records

#### Scenario: Older package database is opened
- **WHEN** an older supported embedded database is opened during import
- **THEN** the normal migration chain advances it to schema v7
- **AND** absence of a historical rejection table or rows does not alter destination rejection history

