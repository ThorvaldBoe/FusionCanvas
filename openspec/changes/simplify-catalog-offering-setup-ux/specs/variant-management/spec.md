## ADDED Requirements

### Requirement: Variant management separates possible choices from sellable Variants
FusionCanvas SHALL distinguish provider-catalog Options and Option Values that may participate in combinations from explicit sellable Variants enabled for one Blueprint Offering. It SHALL preserve stable Option kinds and explicit Variant identities from the authoritative catalog model.

#### Scenario: User opens Variant management
- **WHEN** the user opens Variant management for a Blueprint Offering
- **THEN** FusionCanvas shows the Offering's available Options and Values separately from its explicit sellable Variants
- **AND** identifies the actual fulfillment Provider or Provider-Network context
- **AND** does not present these choices as global Store configuration

#### Scenario: User enables provider-catalog choices
- **WHEN** the user selects Color, Size, or other Option Values available from the provider catalog for the Offering
- **THEN** FusionCanvas records those values as possible choices for that Offering
- **AND** does not automatically treat every mathematical combination as sellable

#### Scenario: User creates one sellable Variant
- **WHEN** the user selects one valid combination of enabled Option Values and explicitly adds it as sellable
- **THEN** FusionCanvas persists one concrete Offering Variant with a stable identity
- **AND** rejects duplicate or provider-invalid combinations without changing confirmed Variants

### Requirement: Variant management supports color-plus-valid-sizes bulk creation
FusionCanvas SHALL provide an efficient bulk workflow for adding every valid enabled Size combination for a newly offered Color while respecting provider-catalog validity and existing sellable Variants.

#### Scenario: User bulk-adds all valid sizes for a Color
- **WHEN** the user chooses an enabled Color and invokes the all-valid-sizes workflow
- **THEN** FusionCanvas previews or clearly identifies the valid enabled Sizes that will become sellable Variants
- **AND** creates only combinations the provider catalog declares valid for that Offering
- **AND** skips already-existing equivalent Variants without duplicating them

#### Scenario: Some enabled Sizes are invalid for the Color
- **WHEN** one or more enabled Sizes cannot be combined with the selected Color according to provider-catalog data
- **THEN** FusionCanvas excludes those combinations and reports which Sizes were not added and why
- **AND** still permits the remaining valid combinations to be confirmed atomically

#### Scenario: No new valid combinations remain
- **WHEN** every valid Size combination for the selected Color already exists or is unavailable
- **THEN** FusionCanvas performs no mutation
- **AND** explains that there are no new valid Variants to add

### Requirement: Variant drafts and lifecycle actions preserve confirmed setup
FusionCanvas SHALL keep Variant and choice edits scoped to the current Offering, SHALL guard meaningful drafts, and SHALL apply existing archive, dependency, and integrity policies to sellable Variants.

#### Scenario: User cancels a Variant draft
- **WHEN** the user starts an individual or bulk Variant draft and cancels before confirmation
- **THEN** FusionCanvas persists no new Variant
- **AND** returns focus to the invoking action or current Variant selection

#### Scenario: User leaves with unsaved Variant changes
- **WHEN** the user attempts to leave Variant management with meaningful unconfirmed changes
- **THEN** FusionCanvas offers to discard the changes or keep editing
- **AND** keep-editing preserves current selections and keyboard focus

#### Scenario: User retires a referenced Variant
- **WHEN** the user requests retirement or removal of a Variant referenced by a Design Area, Item, or other dependent record
- **THEN** FusionCanvas applies the authoritative dependency and archival safeguards
- **AND** reports required resolution rather than silently breaking relationships

#### Scenario: Provider catalog is unavailable
- **WHEN** provider-catalog choices cannot be loaded and no locally persisted choices are available
- **THEN** FusionCanvas shows a recoverable unavailable state
- **AND** leaves confirmed Variants unchanged
