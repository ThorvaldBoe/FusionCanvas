## Why

The Printify shop-products response contains the seller-facing product title, such as “Gildan 64000 t-shirt”, while the catalog Blueprint has the authoritative blank-product identity, including brand and model (“Gildan”, “64000”). Importing the shop title currently gives the local Blueprint the wrong name and can overwrite a corrected local Blueprint name on repeat imports.

## What Changes

- Resolve selected shop products to their Printify catalog Blueprint metadata before importing them.
- Name imported Blueprints from the catalog Blueprint identity (brand + model when available), with the catalog title as the fallback.
- Mirror the corrected normalized Blueprint name into the legacy product projection used by the current Store Management screen.
- Preserve the existing stable identity matching, offering synchronization, and idempotent import behavior.
- Add regression coverage proving that a shop product title does not become the local Blueprint name.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `printify-catalog-import`: imported Blueprint names must use the authoritative Printify catalog Blueprint identity rather than a shop product title.

## Impact

- `FusionCanvas.Integration`: selected shop-product imports make additional read-only catalog Blueprint requests.
- `FusionCanvas.Application`: import naming consumes the resolved catalog identity and falls back safely when brand/model data is unavailable.
- Printify client and application/integration tests.
- No database migration or UI surface change is required; the existing projection synchronization is extended so the current screen reflects the corrected name.
