## MODIFIED Requirements

### Requirement: Printify catalog retrieval is Store-scoped and read-only
The application SHALL retrieve products created in the selected Printify shop only for a saved Store using either the Store's standalone `Printify` or `Shopify + Printify` strategy, configured credential, and selected Printify shop context. Before confirmation, retrieval SHALL NOT mutate local catalog data. The application SHALL group shop products by stable Printify Blueprint identity and present each distinct Blueprint once. For each Blueprint, it SHALL identify the distinct Print Providers used by products in that shop and present a summary of those Providers and the shop products represented by the Blueprint. The global Printify catalog SHALL NOT be presented as the selectable Blueprint list.

#### Scenario: Shop products share one Blueprint
- **WHEN** two or more products in the selected Printify shop use the same Blueprint
- **THEN** the selection surface presents that Blueprint exactly once
- **AND** its summary identifies each distinct Provider used by those products
- **AND** the product count or equivalent supporting context indicates that multiple shop products use it

#### Scenario: Saved Printify Store opens catalog import
- **WHEN** the user opens Catalog & mockups for a saved Store with either Printify strategy, an available credential, and a selected shop
- **THEN** the application retrieves products created in the selected Printify shop
- **AND** presents each Blueprint represented in those products as one selectable Blueprint
- **AND** no local Blueprint, offering, option, variant, placeholder, or mockup-template record is changed before confirmation

#### Scenario: Blueprint name is unavailable
- **WHEN** a retrieved shop product has no usable Blueprint brand/model name
- **THEN** the application presents a deterministic Blueprint identifier fallback as the primary label
- **AND** it retains the available shop-product title or product count as supporting context

#### Scenario: Unsupported Store cannot retrieve catalog
- **WHEN** the active Store is unsaved, archived, uses another strategy, lacks a selected shop, or has no usable credential
- **THEN** the application does not issue a shop-product request
- **AND** it explains the specific setup required before import

### Requirement: User confirms selected Blueprints before import
The application SHALL let the user select one or more distinct Blueprints represented in the selected shop, SHALL use stable Printify Blueprint IDs for selection, and SHALL require explicit confirmation before importing them into Store settings. Confirming a Blueprint SHALL import all distinct Providers used by shop products with that Blueprint.

#### Scenario: User previews and confirms a subset
- **WHEN** the user selects two available Blueprints and confirms the import
- **THEN** only those two Blueprint identities are submitted to the local import operation
- **AND** all Providers used by shop products for each selected Blueprint are included
- **AND** the import reports created and updated record counts

#### Scenario: User cancels selection
- **WHEN** the user closes or cancels the Blueprint selection surface before confirmation
- **THEN** no local catalog mutation occurs
- **AND** the existing Store catalog selection and draft state remain unchanged

#### Scenario: Selected shop has no products
- **WHEN** the selected Printify shop returns an empty product list
- **THEN** the selection surface explains that the shop has no products from which to discover Blueprints
- **AND** no global catalog Blueprints are substituted
- **AND** no local catalog mutation occurs

### Requirement: Catalog import is idempotent and preserves local-only data
The application SHALL match newly imported records by stable Store-scoped Printify Blueprint, Provider, Option, Option Value, and Variant identities. Each Printify Blueprint SHALL map to one Store-owned local Blueprint, and each Provider used by that Blueprint in the selected shop SHALL map to one Blueprint Offering. Repeated imports SHALL update matching records in place, preserve stable local relationships where valid, and SHALL NOT delete records merely because they are absent from a later shop-product response. This change SHALL NOT automatically merge or delete existing local catalog records created by earlier product-level imports.

#### Scenario: Same Blueprint and Provider are imported repeatedly
- **WHEN** the user imports the same Blueprint and used Provider set more than once
- **THEN** subsequent imports update the existing local Blueprint, offerings, options, option values, variants, and placeholders
- **AND** do not create duplicates for the same stable Printify identities

#### Scenario: Shop product configurations differ
- **WHEN** multiple shop products share a Blueprint and Provider but enable different subsets of that Provider's sellable Variants
- **THEN** the local Offering contains the Provider's full supported sellable Variant set
- **AND** the enabled-variant subset of an individual shop product does not restrict the catalog import

#### Scenario: Shop catalog later omits a Blueprint or Provider
- **WHEN** a later shop retrieval no longer includes a previously imported Blueprint or Provider
- **THEN** the import does not delete the existing local Blueprint, Offering, or related local records
- **AND** those records remain available for local review or lifecycle handling

#### Scenario: Earlier product-level imports exist
- **WHEN** local catalog records from an earlier product-level import coexist with a new Blueprint-level import
- **THEN** the import preserves those earlier records and their relationships
- **AND** it does not silently merge, archive, or delete them

### Requirement: Catalog import maps complete used Provider catalogs safely
For each selected Blueprint, the import SHALL include only distinct Print Providers used by products with that Blueprint in the selected shop. For each included Provider, it SHALL import the Provider's complete supported sellable Variant set, including the corresponding typed Option choices and explicit Variant-to-Option Value combinations, from authoritative Printify catalog data. A sellable Variant is a concrete Provider-catalog combination that Printify reports as enabled and available. The import SHALL NOT synthesize a Cartesian product of Option Values. Provider identities, Option identities, Option Value identities, Variant identities, and print-area compatibility SHALL be retained and validated before persistence. The local Blueprint name SHALL use the non-empty catalog brand and model joined by one space, falling back to the catalog Blueprint title when either is unavailable. Malformed or relationship-inconsistent selected data SHALL fail without partially committing the import.

#### Scenario: Selected Blueprint has multiple used Providers
- **WHEN** products in the selected shop use two distinct Providers for the selected Blueprint
- **THEN** the import creates or updates one local Blueprint
- **AND** creates or updates one Offering for each of the two used Providers
- **AND** does not import Providers that are not used by products in that shop for that Blueprint

#### Scenario: Provider catalog has supported option combinations
- **WHEN** the Provider catalog reports Color and Size Option Values and explicit enabled, available Variant combinations
- **THEN** the local Offering contains the reported Option Values and every enabled, available concrete Variant with its exact Option Value memberships
- **AND** it does not fabricate combinations that Printify does not report as Variants
- **AND** it does not omit a sellable Variant merely because no individual shop product currently enables it

#### Scenario: Variant is disabled or unavailable
- **WHEN** the Provider catalog reports a Variant as disabled or unavailable
- **THEN** that Variant is not imported as a sellable Variant

#### Scenario: Blueprint has a catalog identity
- **WHEN** a selected shop Blueprint resolves to catalog brand `Gildan` and model `64000`
- **THEN** the imported local Blueprint is named `Gildan 64000`

#### Scenario: Catalog identity fields are incomplete
- **WHEN** a selected shop Blueprint resolves to catalog metadata with an empty brand or model
- **THEN** the imported local Blueprint uses the catalog Blueprint title as its name

#### Scenario: Catalog metadata or option mapping cannot be resolved
- **WHEN** required Blueprint, Provider, Option, Option Value, Variant, or print-area relationship data cannot be retrieved or resolved
- **THEN** the import returns a recoverable provider or validation error
- **AND** no local catalog records from that import are committed

#### Scenario: Malformed payload is received
- **WHEN** a response contains missing required identity, duplicate stable identity, invalid dimensions, or a Variant referring to an unknown Option Value or print area
- **THEN** the import fails with a safe validation result
- **AND** no records from that import are committed

### Requirement: Printify catalog selection uses accessible virtualized rows
The Store Editor SHALL present each distinct Blueprint discovered from the selected Printify shop exactly once in a virtualized grid with fixed-height rows and aligned columns for selection, Blueprint name, Providers used, and shop-product context or count. Selection SHALL be made through a keyboard-accessible checkbox in each row and SHALL remain attached to the stable Printify Blueprint ID when rows are recycled. Each checkbox's accessible name SHALL identify the Blueprint and its used Providers. Long visible text MAY be trimmed only when the complete value remains available through a tooltip and the accessible name. The grid SHALL not add search, sorting, or paging, and SHALL preserve full-shop retrieval, explicit confirmation, Store scoping, draft preservation, and import idempotency.

#### Scenario: Many shop products resolve to fewer Blueprints
- **WHEN** Printify shop retrieval returns many products and some products share Blueprint identities
- **THEN** the grid presents one row per distinct Blueprint, with its Provider and shop-product context
- **AND** only rows needed for the current viewport are realized
- **AND** the grid does not provide search, sorting, or paging

#### Scenario: Selection survives row recycling
- **WHEN** the user selects multiple Blueprints and scrolls until their rows are recycled out of view
- **THEN** the selections remain associated with the same stable Blueprint identities
- **AND** confirming import submits exactly the selected Blueprint IDs

#### Scenario: Long Blueprint or Provider text remains available
- **WHEN** a Blueprint name or Provider summary is too long to fit in a fixed-height row
- **THEN** the visible text may be trimmed with an ellipsis
- **AND** the complete value is available from its tooltip and the checkbox accessible name

### Requirement: Printify catalog retrieval and import report progress
The Store Editor SHALL show phase-aware progress while retrieving the selected shop's paginated products, resolving Blueprint and Provider catalogs, preparing local catalog changes, and saving the import. Product scanning progress SHALL report the number of products scanned and the total when Printify supplies it. Provider-catalog progress SHALL report completed and total selected Blueprint/Provider pairs. A failed post-request import SHALL record a safe diagnostic event containing the failing local phase and exception summary in the existing technical telemetry monitor; it SHALL NOT include credentials, authorization headers, or raw provider response bodies.

#### Scenario: Shop products are scanned from paginated responses
- **WHEN** the selected Printify shop contains many products
- **THEN** each product's Blueprint ID, Provider ID, and available Option/value definitions are read from the paginated product response
- **AND** the importer does not issue a separate product-detail request for every product solely to retrieve Option/value definitions
- **AND** progress reports the number of products scanned and total products when available

#### Scenario: Selected Provider catalogs are loaded
- **WHEN** the user confirms one or more Blueprints
- **THEN** progress reports completion across the distinct selected Blueprint/Provider pairs
- **AND** each pair's authoritative catalog Variant and Placeholder data is retrieved once for the import

#### Scenario: Local import fails after successful provider requests
- **WHEN** provider requests succeed but snapshot mapping or persistence fails
- **THEN** the user sees a safe recoverable failure message
- **AND** the technical telemetry monitor records the local failure phase and sanitized exception summary
- **AND** no imported catalog records are partially committed
