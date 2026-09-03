## ADDED Requirements

### Requirement: Design Areas describe actual printable regions for one Offering
FusionCanvas SHALL provide focused management of Design Areas for one Blueprint Offering, using the existing Placeholder identity and invariants for printable regions. Each Design Area SHALL capture a user-facing name, placement, positive maximum pixel dimensions, compatible concrete Variants, optional provider reference, and recommended artwork guidance.

#### Scenario: User opens Design Area management
- **WHEN** the user opens Design Area management for a Blueprint Offering
- **THEN** FusionCanvas lists only active Design Areas belonging to that Offering
- **AND** each list item summarizes placement, maximum pixel dimensions, and compatibility
- **AND** the selected Design Area opens in a focused editor without showing Variant or Mockup Template creation forms

#### Scenario: Design Area management preserves master-detail composition
- **WHEN** Design Area management has one or more records or an active draft
- **THEN** FusionCanvas presents the Design Area collection and one focused selected-or-new editor as visually distinct peer regions
- **AND** keeps list summaries scannable while the editor prioritizes name, placement, pixel dimensions, artwork guidance, compatibility, and save actions
- **AND** may stack those regions only when available width requires it

#### Scenario: User creates a Design Area for all Variants
- **WHEN** the user creates a Design Area and accepts the common all-Variants compatibility choice
- **THEN** FusionCanvas associates the Design Area with every compatible active Variant in that Offering
- **AND** newly invalid or cross-Offering Variant references are rejected

#### Scenario: User limits a Design Area to compatible Variants
- **WHEN** the user chooses a subset of Variants for a Design Area
- **THEN** FusionCanvas persists only compatible Variants from the same Offering
- **AND** clearly distinguishes the subset from the all-Variants case

### Requirement: Design Area dimensions and artwork guidance prioritize production usefulness
FusionCanvas SHALL display maximum design dimensions in pixels as the primary measurement, SHALL show inches and millimetres as secondary information when derivable, and SHALL display recommended artwork resolution and format guidance without treating recommendations as uploaded artwork.

#### Scenario: User reviews maximum design dimensions
- **WHEN** a Design Area has valid maximum width and height in pixels
- **THEN** FusionCanvas presents the pixel dimensions first
- **AND** presents equivalent inches and millimetres secondarily using the applicable resolution assumption or provider metadata
- **AND** does not replace the stored pixel dimensions with rounded physical values

#### Scenario: User reviews recommended artwork guidance
- **WHEN** recommended artwork metadata is available
- **THEN** FusionCanvas shows the recommended minimum pixel dimensions, file format, DPI, and background guidance that are known
- **AND** distinguishes recommendations from hard Design Area maximums and validation rules

#### Scenario: Secondary physical dimensions cannot be derived
- **WHEN** no reliable DPI or physical-size metadata is available
- **THEN** FusionCanvas keeps authoritative pixel dimensions visible
- **AND** identifies physical dimensions as unavailable rather than inventing a conversion

#### Scenario: User enters invalid maximum dimensions
- **WHEN** a Design Area draft has non-positive pixel width or height
- **THEN** FusionCanvas rejects the save with recoverable field guidance
- **AND** leaves the previously confirmed Design Area unchanged

### Requirement: Provider Design Area references are advanced technical data
FusionCanvas SHALL treat a Design Area's provider reference as optional advanced technical data normally populated from Printify integration data, while Provider continues to mean the actual fulfillment partner.

#### Scenario: Imported Design Area has a provider reference
- **WHEN** Printify catalog data supplies a stable Design Area reference for a fulfillment partner's Offering
- **THEN** FusionCanvas preserves that reference without making it the user-facing Design Area name
- **AND** exposes it through secondary or Advanced disclosure

#### Scenario: Manual Design Area has no provider reference
- **WHEN** a Manual-strategy user creates a Design Area without a provider reference
- **THEN** FusionCanvas allows the Design Area to be saved when all required user-facing and compatibility data is valid
- **AND** does not fabricate a Printify or Provider identifier

### Requirement: Design Area editing preserves drafts and dependencies
FusionCanvas SHALL guard meaningful Design Area drafts and SHALL apply existing Item, Variant, and Mockup Template dependency safeguards to archival or removal.

#### Scenario: User changes selection with unsaved Design Area edits
- **WHEN** the user selects another Design Area or leaves the surface with meaningful unsaved changes
- **THEN** FusionCanvas offers to discard the changes or keep editing
- **AND** keep-editing preserves the current record, draft, and focus

#### Scenario: User removes a Design Area targeted by a Mockup Template
- **WHEN** the user requests removal of a Design Area referenced by an active Mockup Template
- **THEN** FusionCanvas blocks destructive removal or requires explicit reassignment according to authoritative lifecycle policy
- **AND** does not leave a template without its required target
