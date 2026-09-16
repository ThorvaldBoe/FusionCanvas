## MODIFIED Requirements

### Requirement: Catalog import maps supported provider data safely
The import SHALL map Printify Blueprint, Print Provider, option, sellable variant, and print-area/placeholder data into the existing local catalog model, retain provider identities needed for future updates, and reject malformed or relationship-inconsistent payloads without partially committing the import. For selected shop-product imports, the local Blueprint name SHALL be derived from the authoritative catalog Blueprint metadata using the non-empty brand and model separated by one space; when either is unavailable, the catalog Blueprint title SHALL be used.

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
