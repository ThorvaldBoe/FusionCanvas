## MODIFIED Requirements

### Requirement: Store Editor exposes strategy-gated Printify catalog import
The Store Editor SHALL show an Import from Printify action beside New Blueprint on Catalog & mockups only when the selected Store uses standalone `Printify` or `Shopify + Printify`, and SHALL keep the action out of the regular workspace surface. The action and its selection surface SHALL explain that it retrieves products created in the selected Printify shop.

#### Scenario: Printify Store shows shop-product import action
- **WHEN** the user opens Catalog & mockups for a saved Store using standalone `Printify` or `Shopify + Printify`
- **THEN** an Import from Printify action is visible beside New Blueprint
- **AND** its accessible name explains that it retrieves products from the selected Printify shop

#### Scenario: Manual Store hides import action
- **WHEN** the user opens Catalog & mockups for a Manual or Shopify + Manual Store
- **THEN** the Printify import action is hidden or unavailable with explanatory guidance
- **AND** no provider request can be started from that Store

### Requirement: Catalog import preserves Store Editor safety
The Store Editor SHALL preserve selection, focus, and unsaved local catalog drafts while the shop-product selection surface is opened, cancelled, completed, or fails.

#### Scenario: Import succeeds while local draft exists
- **WHEN** the user has an unsaved local catalog draft and completes a shop-product import
- **THEN** the imported persisted records are refreshed without silently discarding the local draft

#### Scenario: User changes Store during retrieval
- **WHEN** the user changes Store or closes the editor while a shop-product retrieval is in flight
- **THEN** the operation is cancelled or its late result is discarded
- **AND** no result is applied to the newly selected Store
