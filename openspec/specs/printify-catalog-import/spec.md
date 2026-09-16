## Purpose

Defines the read-only Printify catalog retrieval, explicit Blueprint selection, safe Store-scoped import, and recoverable failure behavior for Shopify + Printify Stores.
## Requirements
### Requirement: Printify catalog retrieval is Store-scoped and read-only
The application SHALL retrieve Printify shop-product data only for a saved Store using either the Store's standalone `Printify` or `Shopify + Printify` strategy, configured credential, and selected Printify shop context, and SHALL not mutate local catalog data during retrieval or preview. Each selectable shop product SHALL expose the name of the Blueprint it is built on as its primary display label when Printify provides that name, while retaining the shop product title as secondary context.

#### Scenario: Saved Printify Store opens catalog import
- **WHEN** the user opens Catalog & mockups for a saved Store with either Printify strategy, an available credential, and a selected shop
- **THEN** the application retrieves products created in the selected Printify shop and presents each product primarily by its Blueprint name, such as “Gildan 64000”
- **AND** the shop product title remains available as supporting context
- **AND** the global Printify catalog is not presented as the selectable list
- **AND** no local Blueprint, offering, option, variant, placeholder, or mockup-template record is changed before confirmation

#### Scenario: Blueprint name is unavailable
- **WHEN** a retrieved shop product has no usable Blueprint brand/model name
- **THEN** the application presents a deterministic Blueprint identifier fallback as the primary label
- **AND** it still presents the shop product title as supporting context

#### Scenario: Unsupported Store cannot retrieve catalog
- **WHEN** the active Store is unsaved, archived, uses another strategy, lacks a selected shop, or has no usable credential
- **THEN** the application does not issue a catalog request
- **AND** it explains the specific setup required before import

### Requirement: User confirms selected Blueprints before import
The application SHALL let the user select one or more retrieved shop products, SHALL use their stable opaque Printify product IDs for selection, and SHALL require explicit confirmation before importing them into Store settings.

#### Scenario: User previews and confirms a subset
- **WHEN** the user selects two available shop products and confirms the import
- **THEN** only those two products are submitted to the local import operation
- **AND** the import reports the created and updated record counts

#### Scenario: User cancels selection
- **WHEN** the user closes or cancels the shop-product selection surface before confirmation
- **THEN** no local catalog mutation occurs
- **AND** the existing Store catalog selection and draft state remain unchanged

#### Scenario: Selected shop has no created products
- **WHEN** the selected Printify shop returns an empty product list
- **THEN** the selection surface explains that the shop has no created products to import
- **AND** no global catalog Blueprints are substituted
- **AND** no local catalog mutation occurs

### Requirement: Catalog import is idempotent and preserves local-only data
The application SHALL match imported records by stable Store-scoped Printify shop-product and provider identities, update matching records in place, create missing records, preserve stable local relationships where valid, and SHALL NOT delete records merely because they are absent from a later shop-product response.

#### Scenario: Same shop product is imported twice
- **WHEN** the user imports the same Printify shop product twice
- **THEN** the second import updates the existing local records
- **AND** it does not create duplicate Blueprints, offerings, options, option values, variants, or placeholders

#### Scenario: Shop product data changes
- **WHEN** a later import changes a shop product title, description, selected options, sellable-variant availability, or design-area data
- **THEN** matching local records are updated by the stable shop-product and provider identities
- **AND** local records not present in that response remain available for local review or lifecycle handling

### Requirement: Catalog import maps supported provider data safely
The import SHALL map each selected Printify shop product's product identity, Blueprint identity, Print Provider identity, options, selected variants, and print areas/placeholders into the existing local catalog model, retain the shop-product and provider identities needed for future updates, and reject malformed or relationship-inconsistent payloads without partially committing the import. For selected shop-product imports, the local Blueprint name SHALL be derived from the authoritative catalog Blueprint metadata using the non-empty brand and model separated by one space; when either is unavailable, the catalog Blueprint title SHALL be used.

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

### Requirement: Import failure states are safe and recoverable
The application SHALL expose loading, empty, cancellation, authentication/permission, rate-limit, timeout/network, malformed-response, and persistence-error states using user-safe messages, and SHALL never display credentials, raw response bodies, or request headers.

#### Scenario: Provider request fails
- **WHEN** catalog retrieval fails due to authentication, permission, rate limiting, timeout, network, or service error
- **THEN** the selection surface shows a recoverable explanation and appropriate retry guidance
- **AND** no local catalog mutation occurs

#### Scenario: Import is cancelled while running
- **WHEN** the user cancels an in-flight retrieval or import
- **THEN** the operation stops or completes without publishing stale results into a different Store context
- **AND** the UI returns to a stable non-busy state
