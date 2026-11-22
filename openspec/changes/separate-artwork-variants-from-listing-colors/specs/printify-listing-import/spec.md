## ADDED Requirements

### Requirement: Downloaded Printify variant setup populates Listing and artwork variants
When the creator downloads variant setup for a linked Printify product, FusionCanvas SHALL place that product's selected Color and compatible sellable Variant setup in the Item's Listing selection and SHALL create color-independent artwork variants grouped by their imported Design Area artwork assignments. It SHALL preserve stable imported Option Value, Variant, Asset, and Design Area identities where available, SHALL include enabled Variants even when `is_available` is false, and SHALL NOT overwrite an existing local Design or Listing configuration without an explicit resolution path.

#### Scenario: Creator downloads setup for a linked product
- **WHEN** the Item has no local Offering configuration and the creator downloads its linked Printify variant setup
- **THEN** FusionCanvas attaches the resolved Offering, creates Listing Color selections from the linked product's enabled Color Variants, and includes their compatible Sizes
- **AND** imported artwork is represented as one or more color-independent variants according to distinct Design Area artwork assignments
- **AND** each selected Color is assigned to the corresponding imported artwork variant

#### Scenario: Linked product has an out-of-stock enabled Variant
- **WHEN** the linked Printify product reports an enabled Variant as out of stock
- **THEN** FusionCanvas retains that Variant as active provider setup and includes its Color in the imported Listing selection when selected on the product
- **AND** records the out-of-stock state separately from local archival

#### Scenario: Item already has local variant or artwork setup
- **WHEN** the creator requests setup download for an Item that already has local Design or Listing configuration
- **THEN** FusionCanvas preserves the existing configuration and reports that it must be explicitly resolved
- **AND** does not replace artwork assignments, Color selections, or existing remote mappings

#### Scenario: Printify artwork import is partial or ambiguous
- **WHEN** a linked product has unsupported or ambiguous artwork assignments for one or more Design Areas
- **THEN** FusionCanvas preserves successfully imported artwork variants and leaves unsupported assignments empty with a per-area diagnostic
- **AND** keeps all imported Listing Colors visible with readiness guidance
