## MODIFIED Requirements

### Requirement: Imported catalog records retain provider identity and local terminology
Imported catalog records SHALL use the existing Blueprint, Blueprint Offering, Option, Option Value, Variant, and Placeholder/design-area concepts, retain stable Printify identities separately from editable display labels, preserve Store ownership, and preserve provider stock availability as a refreshable status separate from local Variant archival. Color Option Values SHALL retain the provider's approximate color value, such as its hexadecimal swatch, separately from the displayed Color name when supplied. Catalog retrieval used for Offering setup SHALL include out-of-stock provider Variants when the provider supports that request, so setup can configure the full sellable catalog rather than only currently available combinations.

#### Scenario: Import maps one provider offering
- **WHEN** a selected Printify Blueprint has one Print Provider offering
- **THEN** FusionCanvas creates or updates one Store-owned Blueprint and offering
- **AND** the offering retains the Printify Blueprint and provider identities
- **AND** provider titles remain display data rather than relationship keys

#### Scenario: Imported variant compatibility is preserved
- **WHEN** Printify assigns a design area to a subset of sellable variants
- **THEN** the corresponding local Placeholder/design area is compatible with exactly those imported variants
- **AND** the relationship remains stable after a repeated import

#### Scenario: Out-of-stock catalog variants are returned
- **WHEN** the provider supports an explicit request to include out-of-stock variants and a catalog refresh is performed
- **THEN** the catalog client requests and imports those Variants with their latest availability status
- **AND** an out-of-stock Variant remains active in local Offering setup unless the creator explicitly archives it
- **AND** a later availability refresh does not change the Variant's identity or Design Area compatibility

#### Scenario: Provider Color has an approximate swatch value
- **WHEN** Printify supplies a Color name and approximate hexadecimal color value
- **THEN** FusionCanvas stores the display name and approximate value separately on the Color Option Value
- **AND** reimport updates provider-supplied swatch metadata without changing the stable Color Value identity

#### Scenario: Provider does not return an out-of-stock Variant
- **WHEN** the provider omits a previously imported Variant from a later response
- **THEN** FusionCanvas preserves the local Variant and its last known availability status
- **AND** does not infer that the Variant was intentionally archived
