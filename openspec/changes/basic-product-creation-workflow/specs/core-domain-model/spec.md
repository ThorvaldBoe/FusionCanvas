## MODIFIED Requirements

### Requirement: Listing is the primary item and product concept
An `Item` SHALL replace `Listing` as the universal stage-agnostic product record that carries creative work inside a store topic structure; Listing SHALL refer to the final workflow stage or future marketplace-specific data rather than the universal record.

#### Scenario: Contributor identifies item entities
- **WHEN** a contributor classifies core domain entities
- **THEN** Item is treated as the primary item entity
- **AND** Store, Niche, and Group are not treated as item entities
- **AND** Listing is not used as the universal record name

#### Scenario: Item belongs to a store topic context
- **WHEN** an Item is represented in the domain model
- **THEN** it belongs to a store
- **AND** it can be associated with a niche or group topic context
- **AND** it is not required to be a published marketplace product

## ADDED Requirements

### Requirement: Item identity is the only universally required Item value
Every Item SHALL receive a non-empty automatically assigned stable unique ID, while working title and all workflow-stage content remain optional.

#### Scenario: Item is created without user-entered content
- **WHEN** the user creates an Item in a valid active topic without entering a title or stage data
- **THEN** FusionCanvas persists one Item with a non-empty unique ID
- **AND** starts it at Idea with Draft status and `IsArchived = false`

#### Scenario: Empty Item needs a display label
- **WHEN** an Item has an empty working title
- **THEN** presentation surfaces show `Untitled item · ` followed by the first eight lowercase hexadecimal ID characters
- **AND** do not persist that fallback as the working title or creative metadata

### Requirement: Item terminology migration preserves identity and relationships
FusionCanvas SHALL migrate every existing universal Listing record to Item terminology without changing record identity or related creative data.

#### Scenario: Existing workspace is upgraded
- **WHEN** a workspace created by the universal Listing model is opened after the Item migration
- **THEN** every record has the same ID, placement, timestamps, stage, status, archive state, text, metadata, tags, prompts, assets, and managed files
- **AND** no duplicate Item is created
