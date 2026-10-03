## MODIFIED Requirements

### Requirement: Blueprint detail supports high-impact archive with dependents
Blueprint detail SHALL provide a clearly destructive archive action that can archive an active Blueprint together with all catalog configuration owned by that Blueprint, including its Offerings, Options, Option Values, Variants, Placeholders, Mockup Templates, and catalog-owned child records. The operation SHALL preserve the records and relationships for review or future restoration, SHALL be atomic, and SHALL require an explicit confirmation that prominently explains the broad impact before mutation.

#### Scenario: User archives a Blueprint with dependent catalog data
- **WHEN** the user invokes the Blueprint archive action and explicitly confirms the high-impact warning
- **THEN** FusionCanvas marks the Blueprint archived
- **AND** marks every Offering belonging to the Blueprint archived
- **AND** marks each dependent Option, Option Value, Variant, Placeholder, and Mockup Template belonging to those Offerings archived
- **AND** marks catalog-owned child records such as template color mappings and source-image mappings archived where they have lifecycle state
- **AND** preserves all records, identities, relationships, and metadata for review or restoration

#### Scenario: Archive warning discloses broad impact
- **WHEN** the user opens the Blueprint archive confirmation
- **THEN** the confirmation uses prominent warning treatment
- **AND** explains that the Blueprint and its dependent fulfillment, variant, placeholder, and mockup configuration will leave active use
- **AND** requires a distinct confirm action and offers cancellation without changing persisted state

#### Scenario: User cancels high-impact Blueprint archive
- **WHEN** the user cancels or dismisses the Blueprint archive confirmation
- **THEN** the Blueprint and all dependent records remain unchanged
- **AND** the Blueprint remains selected in the detail surface

#### Scenario: Cascade archive fails
- **WHEN** the archive-with-dependents operation cannot be persisted
- **THEN** FusionCanvas reports a recoverable error
- **AND** leaves the Blueprint and all dependent records in their last confirmed states

#### Scenario: Blueprint archive preserves existing item relationships
- **WHEN** catalog configuration is referenced by listings or design work and the user confirms the Blueprint archive
- **THEN** FusionCanvas archives the Blueprint-owned catalog configuration without deleting or detaching those external relationships
- **AND** dependent listing or design records remain available for review according to their own lifecycle state
