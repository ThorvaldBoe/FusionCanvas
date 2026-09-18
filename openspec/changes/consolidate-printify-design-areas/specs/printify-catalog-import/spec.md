## MODIFIED Requirements

### Requirement: Catalog import maps supported provider data safely
The import SHALL map each selected Printify shop product's product identity, Blueprint identity, Print Provider identity, options, selected variants, and print areas/placeholders into the existing local catalog model, retain the shop-product and provider identities needed for future updates, and reject malformed or relationship-inconsistent payloads without partially committing the import. For selected shop-product imports, the local Blueprint name SHALL be derived from the authoritative catalog Blueprint metadata using the non-empty brand and model separated by one space; when either is unavailable, the catalog Blueprint title SHALL be used. Printify placeholder observations SHALL be consolidated into one active local Design Area per logical provider position and decoration method; that area's maximum width and height SHALL be the maximum width and maximum height reported across its compatible imported variants, and its compatible variant set SHALL include every imported variant that exposes that logical position and decoration method.

#### Scenario: Variant-specific geometries share one logical area
- **WHEN** a valid provider response reports `front`/`dtg` placeholders at multiple dimensions for different variants
- **THEN** the local offering contains one active `front`/`dtg` Design Area for that import
- **AND** its width and height are the respective maximum dimensions reported by the provider
- **AND** its compatibility includes every variant that reported the position and decoration method

#### Scenario: Existing geometry-split import is consolidated
- **WHEN** a later import finds multiple active imported Design Areas for the same offering, position, and decoration method but different dimensions
- **THEN** one deterministic canonical area remains active with the aggregate maximum dimensions and unioned imported compatibility
- **AND** superseded imported duplicates are archived rather than deleted
- **AND** existing offering, mockup, revision, and design-slot references to a superseded area are moved to the canonical area

#### Scenario: Primary selection is preserved
- **WHEN** an offering has an explicit primary Design Area before consolidation
- **THEN** consolidation moves that designation only if it pointed to a superseded duplicate
- **AND** an offering without a primary remains without a primary

### Requirement: Catalog import is idempotent and preserves local-only data
The application SHALL match imported records by stable Store-scoped Printify shop-product and provider identities, update matching records in place, create missing records, preserve stable local relationships where valid, and SHALL NOT delete records merely because they are absent from a later shop-product response. Imported geometry-split duplicates that are superseded by the same logical Printify position may be archived as part of consolidation, while unrelated local-only Design Areas remain unchanged.

#### Scenario: Same shop product is imported twice
- **WHEN** the user imports the same shop product twice
- **THEN** the second import leaves one active Design Area per imported logical position and decoration method
- **AND** it does not create additional active geometry-specific areas
