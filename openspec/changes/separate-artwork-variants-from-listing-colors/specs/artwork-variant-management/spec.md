## ADDED Requirements

### Requirement: Design artwork variants are independent of product Colors
FusionCanvas SHALL represent a Design's final artwork as one or more stable artwork variants that are not assigned to provider Color values. Each artwork variant SHALL contain its own Design Area-to-Asset assignments, optional contrast intent (`light artwork for dark products` or `dark artwork for light products`), and stable ordering. The selected Blueprint Offering and fixed Provider SHALL remain part of Design context because they determine compatible Design Areas. Contrast intent SHALL describe artwork treatment only and SHALL NOT infer or select product Colors.

#### Scenario: Design has one artwork variant
- **WHEN** an Item enters Design with a selected Blueprint Offering and one or more Design Areas
- **THEN** FusionCanvas presents one default artwork variant with the Item's Design Area assignment slots
- **AND** it does not require any product Color selection to import, generate, or assign artwork

#### Scenario: Creator adds a manually prepared alternative
- **WHEN** the creator adds an artwork variant and imports artwork they prepared outside FusionCanvas
- **THEN** FusionCanvas stores the imported artwork as a distinct variant with its own Design Area assignments
- **AND** leaves existing variants and their assignments unchanged
- **AND** does not require the creator to name a shirt Color for the alternative

#### Scenario: Creator sets contrast intent
- **WHEN** the creator records a contrast intent for an artwork variant
- **THEN** FusionCanvas persists the selected light-on-dark or dark-on-light intent with that variant
- **AND** presents the intent as guidance without automatically selecting Listing Colors

#### Scenario: Creator assigns different artwork to multiple Design Areas
- **WHEN** an artwork variant has multiple selected Design Areas
- **THEN** each Design Area can retain an independent Asset assignment within that variant
- **AND** another artwork variant can have different assignments for the same Design Areas

#### Scenario: Creator changes the Design Offering
- **WHEN** the creator requests a different Blueprint Offering or Provider
- **THEN** FusionCanvas warns that Design Area compatibility may differ and requires the creator to review the new Design context
- **AND** does not silently present the prior artwork as verified for the new Provider
- **AND** preserves the prior artwork files for manual adaptation or reassignment

#### Scenario: Creator removes an artwork variant used by a Listing Color
- **WHEN** a Listing Color is assigned to an artwork variant that the creator attempts to remove
- **THEN** FusionCanvas blocks removal or requires reassignment of every dependent Color before completion
- **AND** preserves the prior variant and assignments if the operation is cancelled

### Requirement: Existing color-partitioned Design artwork migrates without losing assignments
When a workspace is upgraded, FusionCanvas SHALL convert each existing Design variant row into a color-independent artwork variant, preserve its Design Area-to-Asset assignments and optional contrast metadata, and move each existing Color-to-row relationship into the Item's Listing Color-to-artwork assignment. The migration SHALL resolve legacy Color labels to stable same-Offering Color Value identities when unambiguous. Unmatched or ambiguous legacy labels SHALL remain visible as unresolved Listing selections with their artwork assignment intact until the creator resolves them. The migration SHALL be idempotent and SHALL NOT mutate a remote Printify product.

#### Scenario: Existing color mapping resolves uniquely
- **WHEN** a legacy Design Color label matches exactly one active Color Value in the Item's Offering
- **THEN** the Color Value becomes selected for that Item in Listing
- **AND** its prior artwork row becomes the assigned artwork variant
- **AND** all Design Area assignments and asset identities are preserved

#### Scenario: Existing color mapping is unmatched or ambiguous
- **WHEN** a legacy Design Color label has no unique active Color Value match
- **THEN** FusionCanvas retains the label and its prior artwork assignment in an unresolved Listing selection
- **AND** identifies the need for creator resolution without dropping the artwork or blocking unrelated resolved Colors

#### Scenario: Workspace migration is repeated
- **WHEN** an already migrated workspace is opened or migrated again
- **THEN** FusionCanvas does not duplicate artwork variants, Listing Color selections, or assignments
- **AND** retains stable identities and existing remote product mappings
