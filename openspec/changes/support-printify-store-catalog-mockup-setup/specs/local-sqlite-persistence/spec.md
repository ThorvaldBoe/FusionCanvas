## ADDED Requirements

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
