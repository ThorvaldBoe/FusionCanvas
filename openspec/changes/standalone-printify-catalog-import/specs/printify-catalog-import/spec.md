## MODIFIED Requirements

### Requirement: Printify catalog retrieval is Store-scoped and read-only
The application SHALL retrieve Printify catalog data only for a saved Store using either the Store's standalone `Printify` or `Shopify + Printify` strategy, configured credential, and selected Printify shop context, and SHALL not mutate local catalog data during retrieval or preview.

#### Scenario: Saved Printify Store opens catalog import
- **WHEN** the user opens Catalog & mockups for a saved Store with either Printify strategy and an available credential
- **THEN** the application retrieves available Printify Blueprints and presents them for selection
- **AND** no local Blueprint, offering, option, variant, placeholder, or mockup-template record is changed before confirmation

#### Scenario: Unsupported Store cannot retrieve catalog
- **WHEN** the active Store is unsaved, archived, uses Manual or Shopify + Manual, lacks a selected shop, or has no usable credential
- **THEN** the application does not issue a catalog request
- **AND** it explains the specific Printify setup required before import

### Requirement: User confirms selected Blueprints before import
The application SHALL let the user select one or more retrieved Blueprints and SHALL require explicit confirmation before importing them into Store settings.

#### Scenario: User previews and confirms a subset
- **WHEN** the user selects two available Blueprints and confirms the import
- **THEN** only those two Blueprints are submitted to the local import operation
- **AND** the import reports the created and updated record counts

#### Scenario: User cancels selection
- **WHEN** the user closes or cancels the Blueprint selection surface before confirmation
- **THEN** no local catalog mutation occurs
- **AND** the existing Store catalog selection and draft state remain unchanged

### Requirement: Catalog import is idempotent and preserves local-only data
The application SHALL match imported records by stable Store-scoped provider identities, update matching records in place, create missing records, preserve stable local relationships where valid, and SHALL NOT delete records merely because they are absent from a later provider response.

#### Scenario: Same Blueprint is imported twice
- **WHEN** the user imports the same Printify Blueprint and provider offering twice
- **THEN** the second import updates the existing local records
- **AND** it does not create duplicate Blueprints, offerings, options, option values, variants, or placeholders

#### Scenario: Provider data changes
- **WHEN** a later import changes a provider title, option availability, sellable-variant availability, or design-area dimensions
- **THEN** matching local records are updated by provider identity
- **AND** local records not present in that response remain available for local review or lifecycle handling

### Requirement: Catalog import maps supported provider data safely
The import SHALL map Printify Blueprint, Print Provider, option, sellable variant, and print-area/placeholder data into the existing local catalog model, retain provider identities needed for future updates, and reject malformed or relationship-inconsistent payloads without partially committing the import.

#### Scenario: Blueprint includes options, variants, and design areas
- **WHEN** a valid provider response contains color or size options, sellable variants, and print areas applying to variant subsets
- **THEN** the local offering contains typed option values, concrete sellable variants, and design areas/placeholders with the correct compatibility relationships

#### Scenario: Malformed payload is received
- **WHEN** a response contains missing required identity, duplicate provider identity, invalid dimensions, or a variant/area reference that cannot be resolved
- **THEN** the import fails with a safe validation result
- **AND** no records from that import are committed

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
