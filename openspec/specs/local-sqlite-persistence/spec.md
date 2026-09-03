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
The Phase 0 SQLite persistence capability SHALL avoid storage behavior that belongs to later workflow or platform changes. Single-workspace import/export packages are no longer excluded: they are provided by the workspace-transfer capability, which reuses this persistence layer rather than extending it.

#### Scenario: Contributor reviews Phase 0 persistence scope
- **WHEN** a contributor reviews the FC-0003 implementation
- **THEN** it does not implement cloud sync, multi-user collaboration, encryption, full backup/restore, marketplace synchronization, AI provider history, plugin data stores, or advanced search optimization
- **AND** single-workspace import/export packages are understood to belong to the workspace-transfer capability, not to the persistence layer itself

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

### Requirement: Ideation rejections track optional update time
FusionCanvas SHALL persist an optional `UpdatedAt` timestamp on every ideation rejection, SHALL leave it null for rejections that have never been edited, SHALL set or advance it whenever a rejections phrase or reason is edited, and SHALL round-trip the null and non-null values through workspace save and load.

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

### Requirement: SQLite migrates the existing catalog to the Printify-aligned Store model
FusionCanvas SHALL migrate the immediately previous supported workspace schema to the fulfillment-strategy, Blueprint catalog, Option, Variant, Placeholder, Mockup Template, and revision schema in one ordered transaction, SHALL preserve existing Store and catalog identities wherever an equivalent record remains, and SHALL advance the schema version only after relationship validation succeeds.

#### Scenario: Existing Store is migrated to Manual
- **WHEN** a supported pre-change database contains one or more Stores
- **THEN** the migration assigns `Manual` to every Store
- **AND** preserves each Store ID, workspace ownership, context, archive state, timestamps, metadata, and Store-scoped child relationships

#### Scenario: Existing product blueprint is migrated
- **WHEN** an existing `product_blueprints` row is migrated
- **THEN** FusionCanvas creates the equivalent Blueprint with the same ID, Store ID, name, description, optional external identity, timestamps, and metadata
- **AND** existing offering relationships continue to reference that Blueprint identity

#### Scenario: Existing fixed-provider offering is migrated
- **WHEN** an existing offering has `FixedProvider` kind and a provider name
- **THEN** FusionCanvas creates or reuses one Store-scoped Print Provider for the same normalized provider name
- **AND** creates the fixed-Print-Provider Blueprint Offering with the existing offering ID, Blueprint relationship, descriptive values, optional external identity, timestamps, and metadata

#### Scenario: Existing Printify Choice offering is migrated
- **WHEN** an existing offering has `PrintifyChoiceNetwork` kind
- **THEN** FusionCanvas creates a Provider-Network Blueprint Offering with the existing offering ID
- **AND** assigns the stable provider-network code `printify-choice`
- **AND** does not create a fabricated Print Provider

#### Scenario: Existing inline Variant options are normalized
- **WHEN** an existing concrete Variant contains inline option name/value pairs
- **THEN** the migration creates one Offering Option per distinct normalized option name within that offering and one Offering Option Value per distinct normalized value within that Option
- **AND** maps option names equal to `Color` or `Size` without case sensitivity to `OptionKind.Color` or `OptionKind.Size`
- **AND** maps every other option name to `OptionKind.Other`
- **AND** preserves the concrete Variant ID, offering relationship, timestamps, and exact option-value membership

#### Scenario: Existing design area is migrated to a Placeholder
- **WHEN** an existing design-area row is migrated
- **THEN** FusionCanvas creates the equivalent Placeholder with the same ID, offering relationship, position, decoration method, dimensions, timestamps, and metadata
- **AND** converts explicit existing Variant IDs to Placeholder compatibility relationships
- **AND** expands an existing unrestricted area to all concrete Variants present in that offering at migration time

#### Scenario: Existing Item and design relationships are migrated
- **WHEN** Items, listing configuration, selected targets, design rows, or design slot assignments refer to existing offering or design-area identities
- **THEN** the migration preserves the offering IDs and converted Placeholder IDs those relationships reference
- **AND** loading the migrated workspace reconstructs the same valid Item-to-catalog selections without dangling references

#### Scenario: Existing database has no mockup templates
- **WHEN** a pre-change database is migrated
- **THEN** the migration creates empty Mockup Template, revision, and template-color storage
- **AND** does not fabricate templates, colors, source assets, placement configuration, or generated mockups

### Requirement: Migrated catalog relationships are validated before commit
FusionCanvas SHALL validate Store ownership, offering kind requirements, Option kinds, concrete Variant membership, Placeholder compatibility, Item references, template target ownership, template-color ownership, and active uniqueness before committing the new schema.

#### Scenario: Migrated data is valid
- **WHEN** all migrated records satisfy the new ownership and dependency invariants
- **THEN** FusionCanvas commits the migration and records the new schema version
- **AND** the workspace can be saved and reopened with equal catalog counts and preserved identities

#### Scenario: Migration encounters an invalid legacy reference
- **WHEN** an existing Variant, design area, Item target, or other catalog relationship cannot be mapped without violating ownership
- **THEN** the migration rolls back
- **AND** reports actionable migration failure without advancing the schema version or partially rewriting the database

### Requirement: New catalog and template relationships round-trip atomically
FusionCanvas SHALL persist and load Store strategies, Blueprints, Print Providers, Provider Networks, Blueprint Offerings, Options, Option Values, concrete Variants, Variant memberships, Placeholders, Placeholder compatibility, Mockup Templates, template revisions, and template-color records with stable identities and enforced foreign-key relationships.

#### Scenario: Configured Store is reopened
- **WHEN** a valid configured Store is saved and the same workspace database is reopened
- **THEN** every strategy, catalog, Placeholder, template, revision, color binding, archive state, timestamp, and relationship is reconstructed exactly
- **AND** derived compatible Variants can be recomputed from persisted Color Option Value membership

#### Scenario: Save would violate a catalog invariant
- **WHEN** a snapshot contains a cross-Store reference, cross-offering target, non-Color template binding, duplicate active template-color pair, or dangling dependent identity
- **THEN** persistence rejects the save before commit
- **AND** the database remains at the last valid complete state

#### Scenario: New database is created
- **WHEN** FusionCanvas saves workspace data to a new database path
- **THEN** the current schema includes all strategy, normalized catalog, Placeholder, Mockup Template, revision, and template-color storage
- **AND** includes no placement-coordinate, slot, compositor, per-Variant mockup override, generated-mockup, Shopify-mapping, or external-credential tables

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

### Requirement: SQLite binds data values and constrains dynamic identifiers
SQLite persistence SHALL bind data values through command parameters and SHALL validate unavoidable dynamic table or column identifiers before interpolating them into SQL.

#### Scenario: A persistence query uses a typed identifier
- **WHEN** a repository query filters or relates rows by an entity identifier
- **THEN** the identifier is supplied as a command parameter rather than interpolated into SQL text

#### Scenario: A migration requires a dynamic identifier
- **WHEN** migration code must interpolate a table or column identifier because SQLite does not accept a parameter there
- **THEN** the identifier is checked against the repository’s safe identifier rules before SQL execution

