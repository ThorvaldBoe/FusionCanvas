## ADDED Requirements

### Requirement: Schema version migration renames universal Listing storage to Item
FusionCanvas SHALL transactionally migrate schema-version-4 Listing tables, columns, and relationships to physical Item terminology while preserving every record ID and value.

#### Scenario: Version 4 workspace is opened
- **WHEN** the repository opens a version 4 database containing Listings, Listing Tags, Prompt references, and generic Asset links
- **THEN** it creates the approved Item-named structures and copies rows with unchanged values
- **AND** recreates foreign keys and indexes
- **AND** retains the persisted generic entity-kind numeric value for Item links
- **AND** advances the schema version only after successful migration

#### Scenario: Item migration fails
- **WHEN** any migration operation fails
- **THEN** the transaction rolls back
- **AND** the prior database remains readable by the prior schema implementation
- **AND** no partially renamed schema is reported as current

#### Scenario: Migrated workspace is saved and reopened
- **WHEN** migration succeeds and the workspace is saved and reopened
- **THEN** Item counts, IDs, values, Tags, Prompts, Asset links, stage, status, archive state, metadata, and topic relationships remain equal to the pre-migration state

