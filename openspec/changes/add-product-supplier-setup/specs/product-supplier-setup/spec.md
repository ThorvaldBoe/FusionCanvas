## ADDED Requirements

### Requirement: Store catalog maintains product and fulfillment structure
FusionCanvas SHALL let a user maintain product blueprints and one or more fulfillment offerings within an active Store, and SHALL preserve that catalog across workspace reload.

#### Scenario: User adds a product and fixed-provider offering
- **WHEN** the user saves a valid product blueprint and a valid named fixed-provider offering in Store Management
- **THEN** FusionCanvas persists both records with stable identities scoped to the selected Store
- **AND** the offering remains associated with that product after reload

#### Scenario: Catalog is isolated by Store
- **WHEN** the user opens product setup for another Store
- **THEN** FusionCanvas shows only that Store's products and offerings
- **AND** it does not expose or permit editing catalog records from another Store

### Requirement: Offerings retain provider-compatible variants and design areas
FusionCanvas SHALL allow an offering to hold concrete option combinations and printable areas containing position, decoration method, positive pixel width and height, and applicable variants.

#### Scenario: User creates a variant-specific printable area
- **WHEN** the user saves an offering with variants and selects applicable variants for a printable area
- **THEN** FusionCanvas persists the area with its position, decoration method, dimensions, and variant applicability
- **AND** the area remains associated only with variants from that offering

#### Scenario: User enters invalid printable dimensions or references
- **WHEN** the user attempts to save a printable area with non-positive dimensions or a variant from another offering
- **THEN** FusionCanvas rejects the save with recoverable guidance
- **AND** it leaves confirmed catalog data unchanged

### Requirement: Printify Choice is represented as a variable network
FusionCanvas SHALL represent Printify Choice as a fulfillment-network offering rather than as a fixed provider and SHALL disclose that exact provider selection and design consistency can vary.

#### Scenario: User configures a Choice offering
- **WHEN** the user creates a Printify Choice offering
- **THEN** FusionCanvas does not require or display a fixed provider identity
- **AND** it identifies the offering as a variable fulfillment network

#### Scenario: User reviews Choice design areas
- **WHEN** a configured Choice offering provides selectable printable areas
- **THEN** FusionCanvas shows a consistency warning with those areas
- **AND** the areas remain eligible for design-target selection

### Requirement: Catalog edits preserve selected Item targets
FusionCanvas SHALL require explicit safe handling before a user permanently removes a catalog record that is selected by an Item.

#### Scenario: User removes an unreferenced area
- **WHEN** the user confirms removal of a printable area that no Item has selected
- **THEN** FusionCanvas removes that area
- **AND** it preserves unrelated products, offerings, variants, and Items

#### Scenario: User removes a referenced record
- **WHEN** the user requests removal of a product, offering, or printable area that is selected by one or more Items
- **THEN** FusionCanvas blocks the removal
- **AND** explains that the target must first be cleared or replaced on the affected Items
