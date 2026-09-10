## ADDED Requirements

### Requirement: Standalone Printify stores support catalog import
The application SHALL allow a saved, active Store using standalone `Printify`, with an available Printify credential and selected Printify shop, to retrieve and explicitly import Printify catalog data using the same workflow and safety rules as `Shopify + Printify`.

#### Scenario: Standalone Printify Store opens catalog import
- **WHEN** the user opens Catalog & mockups for a saved active Store with standalone `Printify`, an available credential, and a selected Printify shop
- **THEN** the application retrieves available Printify Blueprints and presents them for selection
- **AND** confirming selected Blueprints imports them into that Store
- **AND** the Store is not treated as Shopify-integrated or publishable

#### Scenario: Standalone Printify setup remains required
- **WHEN** a standalone `Printify` Store is unsaved, archived, lacks a selected shop, or has no usable credential
- **THEN** the application does not issue a catalog request
- **AND** it explains the required Printify setup without requiring Shopify configuration
