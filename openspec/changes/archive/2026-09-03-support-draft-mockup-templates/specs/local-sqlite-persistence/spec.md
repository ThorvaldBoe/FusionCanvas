## ADDED Requirements

### Requirement: SQLite persists partial Mockup Template configuration
FusionCanvas SHALL persist partial Mockup Templates and attributable revisions with nullable target Design Area, image-source, and image-space mapping configuration, while retaining required template and Offering identities and preserving validation for every supplied value. Readiness SHALL be reconstructed from persisted configuration and current catalog relationships rather than stored as an independent mutable column.

#### Scenario: Partial Draft is saved and reopened
- **WHEN** a named Mockup Template with absent target Design Area, Colors, image, and mapping is saved and the workspace is reopened
- **THEN** SQLite reconstructs the same stable template identity, Offering relationship, name, revision number, and nullable configuration
- **AND** the application derives Draft after reload

#### Scenario: Complete template is saved and reopened
- **WHEN** a Ready-for-use Mockup Template is saved and the workspace is reopened
- **THEN** SQLite preserves its target Design Area, Color bindings, image reference and dimensions, in-bounds mapping, and revision attribution
- **AND** the application derives Ready for use when current catalog compatibility remains valid

#### Scenario: Readiness-related field is cleared
- **WHEN** a user saves an edit that explicitly removes a target Design Area, image, mapping, or all Color applicability
- **THEN** SQLite persists the cleared nullable relationship or empty binding set atomically
- **AND** the prior revision snapshot remains unchanged

### Requirement: SQLite migrates existing Mockup Templates to partial-draft storage
FusionCanvas SHALL introduce partial Mockup Template storage through the next available versioned SQLite migration. The migration SHALL make readiness-related target fields nullable without changing existing non-null values, identities, revision numbers, Color bindings, image mappings, timestamps, archive state, or unrelated workspace data.

#### Scenario: Previous supported database is opened
- **WHEN** a database at the immediately previous supported schema version contains existing Mockup Templates and revisions with required target Design Areas
- **THEN** the migration rebuilds or alters the affected structures transactionally to permit nullable Draft configuration
- **AND** every existing complete and partial value is preserved exactly
- **AND** schema version advances only after validation succeeds

#### Scenario: New database is created
- **WHEN** FusionCanvas creates a new workspace database
- **THEN** the current schema supports nullable readiness configuration for Mockup Templates and revisions
- **AND** foreign keys still protect every non-null Design Area, Color, image-asset, and Offering relationship

#### Scenario: Migration fails
- **WHEN** the partial-template migration cannot rebuild, copy, validate, or replace an affected table
- **THEN** the migration transaction rolls back
- **AND** the prior database remains at the prior supported schema without partial structural changes

#### Scenario: Existing complete template is migrated
- **WHEN** a pre-migration template already has valid target, image, mapping, Color, and revision data
- **THEN** migration does not downgrade, rewrite, or duplicate its configuration
- **AND** its post-migration readiness is derived from the preserved values

#### Scenario: Workspace package contains partial templates
- **WHEN** a workspace containing Draft and Ready Mockup Templates is exported and imported through the supported package flow
- **THEN** nullable configuration, relationships, revisions, and stable identities round-trip without requiring provider connectivity
- **AND** readiness is re-derived after import

