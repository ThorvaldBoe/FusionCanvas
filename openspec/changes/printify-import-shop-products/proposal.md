## Why

The current Printify import retrieves the global catalog of available Blueprints, not the products created in the selected Printify shop. That forces users to sift through unrelated test products, gifts, and experiments, and can lead them to offer products that do not belong to the shop they are configuring.

## What Changes

- Change the Printify import source to the selected shop's created products.
- Use the persisted Printify shop ID as part of the remote retrieval request.
- Present the shop's products for explicit selection before local import, while preserving the existing confirmation and idempotent persistence flow.
- Map each selected shop product's Blueprint/provider/variant/print-area data into the existing local catalog model.
- Update labels and guidance so the surface clearly describes shop products rather than the complete Printify catalog.
- Preserve safe failure handling, cancellation, Store scoping, and support for both Printify fulfillment strategies.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `printify-catalog-import`: Retrieve and select products created in the selected Printify shop instead of global catalog Blueprints.
- `store-management`: Clarify the Catalog & mockups import action and selection surface as a shop-product import workflow.

## Impact

The Printify application contract and integration client will accept the selected shop ID and parse shop-product payloads. The application import mapping and view-model terminology will change from global Blueprint summaries to shop-product summaries while retaining the existing local Blueprint catalog model. Focused integration, application, and Avalonia headless tests will need updated fixtures and endpoint assertions. No database migration is expected because imported records continue to use the existing Store-scoped catalog entities and stable Printify identities.
