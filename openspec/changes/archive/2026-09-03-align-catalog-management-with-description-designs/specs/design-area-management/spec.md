## MODIFIED Requirements

### Requirement: Design Areas describe actual printable regions for one Offering
FusionCanvas SHALL provide focused management of Design Areas for one Blueprint Offering, using the existing Placeholder identity and invariants for printable regions. It SHALL present a scannable Design Area collection beside one grouped selected-or-new editor. Each Design Area SHALL capture a user-facing name, placement, positive maximum pixel dimensions, compatible concrete Variants, optional provider reference, and recommended artwork guidance.

#### Scenario: User opens Design Area management
- **WHEN** the user opens Design Area management for a Blueprint Offering
- **THEN** FusionCanvas lists only active Design Areas belonging to that Offering
- **AND** each list item summarizes name, placement, maximum pixel dimensions, compatibility, and a clear edit action
- **AND** the selected Design Area opens in a focused editor without showing Variant or Mockup Template creation forms

#### Scenario: Design Area management preserves master-detail composition
- **WHEN** Design Area management has one or more records or an active draft
- **THEN** FusionCanvas presents the Design Area collection and one focused selected-or-new editor as visually distinct peer regions
- **AND** groups the editor into identity, maximum design size, recommended artwork, compatibility, and Advanced provider data
- **AND** may stack those regions only when available width requires it

#### Scenario: User reviews maximum size and artwork guidance
- **WHEN** a Design Area with known maximum dimensions and artwork recommendations is selected
- **THEN** FusionCanvas presents authoritative maximum pixel dimensions before secondary physical measurements
- **AND** presents recommended artwork in a separate advisory group rather than as part of the hard maximum

#### Scenario: User creates a Design Area for all Variants
- **WHEN** the user creates a Design Area and accepts the common all-Variants compatibility choice
- **THEN** FusionCanvas associates the Design Area with every compatible active Variant in that Offering
- **AND** keeps individual Variant-selection controls hidden
- **AND** newly invalid or cross-Offering Variant references are rejected

#### Scenario: User limits a Design Area to compatible Variants
- **WHEN** the user chooses a subset of Variants for a Design Area
- **THEN** FusionCanvas reveals selection controls for compatible Variants from the same Offering
- **AND** persists only the selected compatible Variants
- **AND** clearly distinguishes the subset from the all-Variants case

#### Scenario: User reviews a lifecycle action
- **WHEN** a Design Area summary exposes an archive or destructive action
- **THEN** FusionCanvas presents that action as secondary to opening and editing the Design Area
- **AND** applies the authoritative confirmation and dependency safeguards

### Requirement: Design Area dimensions and artwork guidance prioritize production usefulness
FusionCanvas SHALL display maximum design dimensions in pixels as the primary measurement, SHALL place inches and millimetres immediately after those dimensions as secondary information when derivable, and SHALL group recommended artwork resolution and format guidance separately from hard Design Area maximums and validation rules.

#### Scenario: User reviews maximum design dimensions
- **WHEN** a Design Area has valid maximum width and height in pixels
- **THEN** FusionCanvas presents the pixel dimensions first
- **AND** presents equivalent inches and millimetres secondarily using the applicable resolution assumption or provider metadata
- **AND** does not replace the stored pixel dimensions with rounded physical values

#### Scenario: User reviews recommended artwork guidance
- **WHEN** recommended artwork metadata is available
- **THEN** FusionCanvas shows the recommended minimum pixel dimensions, file format, DPI, and background guidance that are known in a distinct advisory group
- **AND** distinguishes recommendations from hard Design Area maximums and validation rules

#### Scenario: Secondary physical dimensions cannot be derived
- **WHEN** no reliable DPI or physical-size metadata is available
- **THEN** FusionCanvas keeps authoritative pixel dimensions visible
- **AND** identifies physical dimensions as unavailable rather than inventing a conversion

#### Scenario: User enters invalid maximum dimensions
- **WHEN** a Design Area draft has non-positive pixel width or height
- **THEN** FusionCanvas rejects the save with recoverable field guidance
- **AND** leaves the previously confirmed Design Area unchanged
