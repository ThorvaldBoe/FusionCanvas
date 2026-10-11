## Origin

- Primary issue: [#915](https://github.com/ThorvaldBoe/FusionCanvas/issues/915)

## Why

An Item cannot manage a Printify product that already exists until its external product ID is explicitly linked, and the Listing tool currently verifies the selected shop through an unsupported shop-detail route. This module safely attaches an existing product to an Item in the selected Store and confirms shop readiness through Printify's supported account shop list.

## What Changes

- Verify the selected Printify shop through `GET /v1/shops.json` and require an exact shop ID match before enabling product operations.
- Display the saved Printify product ID in the Listing tool and allow entering an existing product ID for verification.
- Read the product through the selected shop-scoped endpoint, verify the returned product identity and shop identity when provided, and show a clear confirmation before the local mapping is created or replaced.
- Preserve the existing mapping when the product cannot be verified or the user cancels; reject a product already mapped to another Item in the same Store.
- Add mocked API, application, persistence, and Avalonia headless coverage. Do not infer product identity from title, description, or SKU, or mutate the Printify product while linking it.

## Capabilities

### New Capabilities

- `printify-existing-product-linking`: verify and link a product from the selected Printify shop to an Item without changing the remote product.

### Modified Capabilities

None. This is a separate adoption workflow; the existing Printify lifecycle change explicitly excludes adoption.

## Impact

- Application: add a verified-linking use case that validates Store/Item/shop scope, prevents duplicate mappings, and persists only after explicit confirmation.
- Integration: correct the listing readiness probe to use the account shops endpoint and reuse the existing selected-shop-scoped product read for verification.
- Persistence: update the existing Store/Item Printify mapping atomically; no schema migration is expected unless current mapping invariants require it.
- App: add a compact ID entry and confirmation state to the Printify Listing tool, retaining the current ID in a selectable/copyable control.
- Verification: mocked transport and application coverage, persistence boundary coverage, and headless UI tests for validation, confirmation, cancellation, and visible/copyable identity.
- Non-goals: product search or automatic matching, remote product edits, mapping history UI, shop migration, and changes to existing product creation or synchronization behavior.
