## MODIFIED Requirements

### Requirement: Printify catalog retrieval is Store-scoped and read-only
The application SHALL retrieve Printify shop-product data only for a saved Store using either the Store's standalone `Printify` or `Shopify + Printify` strategy, configured credential, and selected Printify shop context. Retrieval SHALL use the selected shop's created products as the selectable import units, and SHALL not mutate local catalog data during retrieval or preview.

#### Scenario: Saved Printify Store opens shop-product import
- **WHEN** the user opens Catalog & mockups for a saved Store with either Printify strategy, an available credential, and a selected shop
- **THEN** the application retrieves products created in that selected Printify shop
- **AND** presents those shop products for selection using their product titles and stable product identities
- **AND** it does not present the global Printify catalog as the selectable list
- **AND** no local Blueprint, offering, option, variant, placeholder, or mockup-template record is changed before confirmation

#### Scenario: Unsupported Store cannot retrieve shop products
- **WHEN** the active Store is unsaved, archived, uses another strategy, lacks a selected shop, or has no usable credential
- **THEN** the application does not issue a shop-product request
- **AND** it explains the specific setup required before import

### Requirement: User confirms selected shop products before import
The application SHALL let the user select one or more retrieved shop products and SHALL require explicit confirmation before importing them into Store settings.

#### Scenario: User previews and confirms a subset
- **WHEN** the user selects two available shop products and confirms the import
- **THEN** only those two Printify product identities are submitted to the local import operation
- **AND** the import reports the created and updated record counts

#### Scenario: User cancels selection
- **WHEN** the user closes or cancels the shop-product selection surface before confirmation
- **THEN** no local catalog mutation occurs
- **AND** the existing Store catalog selection and draft state remain unchanged

### Requirement: Catalog import is idempotent and preserves local-only data
The application SHALL match imported records by stable Store-scoped Printify shop-product and provider identities, update matching records in place, create missing records, preserve stable local relationships where valid, and SHALL NOT delete records merely because they are absent from a later shop-product response.

#### Scenario: Same shop product is imported twice
- **WHEN** the user imports the same Printify shop product twice
- **THEN** the second import updates the existing local Blueprint and its provider offering
- **AND** it does not create duplicate Blueprints, offerings, options, option values, variants, or placeholders

#### Scenario: Shop product data changes
- **WHEN** a later import changes a shop product title, description, selected options, sellable-variant availability, or design-area data
- **THEN** matching local records are updated by the stable shop-product and provider identities
- **AND** local records not present in that response remain available for local review or lifecycle handling

### Requirement: Catalog import maps supported shop-product data safely
The import SHALL map each selected Printify shop product's product identity, Blueprint identity, Print Provider identity, options, selected variants, and print areas/placeholders into the existing local catalog model, retain the shop-product identity needed for future updates, and reject malformed or relationship-inconsistent payloads without partially committing the import.

#### Scenario: Shop product includes options, variants, and design areas
- **WHEN** a valid shop-product response contains options, selected sellable variants, and print areas applying to variant subsets
- **THEN** the local Store catalog contains a product-specific Blueprint, its provider offering, typed option values, concrete sellable variants, and design areas/placeholders with the correct compatibility relationships

#### Scenario: Multiple products use one Printify Blueprint
- **WHEN** two selected shop products use the same Printify Blueprint but have different product identities or configurations
- **THEN** the import keeps them distinguishable as separate selectable and locally matchable shop products
- **AND** importing one does not update or merge the other

#### Scenario: Malformed payload is received
- **WHEN** a response contains a missing product identity, missing Blueprint or provider identity, duplicate product identity, invalid dimensions, or a variant/area reference that cannot be resolved
- **THEN** the import fails with a safe validation result
- **AND** no records from that import are committed

### Requirement: Import failure states are safe and recoverable
The application SHALL expose loading, empty, cancellation, authentication/permission, rate-limit, timeout/network, malformed-response, and persistence-error states using user-safe messages, and SHALL never display credentials, raw response bodies, or request headers.

#### Scenario: Provider request fails
- **WHEN** shop-product retrieval fails due to authentication, permission, rate limiting, timeout, network, or service error
- **THEN** the selection surface shows a recoverable explanation and appropriate retry guidance
- **AND** no local catalog mutation occurs

#### Scenario: Selected shop has no created products
- **WHEN** the selected Printify shop returns an empty product list
- **THEN** the selection surface explains that the shop has no created products to import
- **AND** no global catalog Blueprints are substituted
- **AND** no local catalog mutation occurs

#### Scenario: Import is cancelled while running
- **WHEN** the user cancels an in-flight retrieval or import
- **THEN** the operation stops or completes without publishing stale results into a different Store context
- **AND** the UI returns to a stable non-busy state
