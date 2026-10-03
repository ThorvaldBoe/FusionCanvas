## MODIFIED Requirements

### Requirement: Catalog import is idempotent and preserves local-only data
The application SHALL match imported records by stable Store-scoped Printify shop-product identity and SHALL normalize imported Print Provider records by trimmed, case-insensitive provider name within that Store. It SHALL update matching records in place, create missing records, preserve stable local relationships where valid, reassign offerings from same-name duplicate providers to the canonical provider, archive superseded provider records without deleting them, and SHALL NOT delete records merely because they are absent from a later shop-product response.

#### Scenario: Same shop product is imported twice
- **WHEN** the user imports the same Printify shop product twice
- **THEN** the second import updates the existing local records
- **AND** it does not create duplicate Blueprints, offerings, options, option values, variants, placeholders, or Print Providers

#### Scenario: Same provider name arrives with different external identities
- **WHEN** an import contains a Print Provider whose normalized title matches an existing Store-owned provider while its non-empty external provider ID differs
- **THEN** the import uses one canonical local Print Provider record for that name
- **AND** reassigns every affected Blueprint Offering to that provider
- **AND** archives any superseded same-name provider records
- **AND** preserves all observed external provider IDs as provider source metadata aliases

#### Scenario: Shop product data changes
- **WHEN** a later import changes a shop product title, description, selected options, sellable-variant availability, or design-area data
- **THEN** matching local records are updated by the stable shop-product identity and canonical provider identity
- **AND** local records not present in that response remain available for local review or lifecycle handling

### Requirement: Catalog import maps supported provider data safely
The import SHALL map each selected Printify shop product's product identity, Blueprint identity, Print Provider identity, options, selected variants, and print areas/placeholders into the existing local catalog model, retain the shop-product identity and all observed provider source identities needed for future updates, and reject malformed or relationship-inconsistent payloads without partially committing the import. For selected shop-product imports, the local Blueprint name SHALL be derived from the authoritative catalog Blueprint metadata using the non-empty brand and model separated by one space; when either is unavailable, the catalog Blueprint title SHALL be used.

#### Scenario: Blueprint includes options, variants, and design areas
- **WHEN** a valid provider response contains color or size options, sellable variants, and print areas applying to variant subsets
- **THEN** the local offering contains typed option values, concrete sellable variants, and design areas/placeholders with the correct compatibility relationships

#### Scenario: Selected shop product has a catalog identity
- **WHEN** a selected shop product has a `blueprint_id` and the catalog Blueprint provides brand `Gildan` and model `64000`
- **THEN** the imported local Blueprint is named `Gildan 64000`
- **AND** the shop product title is not used as the Blueprint name

#### Scenario: Catalog identity fields are incomplete
- **WHEN** a selected shop product resolves to a catalog Blueprint with an empty brand or model
- **THEN** the imported local Blueprint uses the catalog Blueprint title as its name

#### Scenario: Catalog Blueprint lookup fails
- **WHEN** the catalog Blueprint metadata required for a selected shop-product import cannot be retrieved
- **THEN** the import returns a recoverable provider error
- **AND** no local catalog records from that import are committed

#### Scenario: Malformed payload is received
- **WHEN** a response contains missing required identity, duplicate provider identity, invalid dimensions, or a variant/area reference that cannot be resolved
- **THEN** the import fails with a safe validation result
- **AND** no records from that import are committed

#### Scenario: Multiple products use one Printify Blueprint
- **WHEN** two selected shop products use the same Printify Blueprint but have different product identities or configurations
- **THEN** the import keeps them distinguishable as separate selectable and locally matchable shop products
- **AND** importing one does not update or merge the other
