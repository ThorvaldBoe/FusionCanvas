## Why

Standalone Printify stores expose the same Printify credential and shop configuration as Shopify + Printify stores, but catalog import currently rejects them. This makes the newly supported standalone strategy unusable for its primary Printify catalog workflow.

## What Changes

- Allow saved, active standalone `Printify` stores with a selected shop to retrieve and import Printify catalog data.
- Keep the existing credential, shop-selection, confirmation, idempotency, and safe-failure behavior unchanged.
- Preserve the distinction that standalone Printify does not enable Shopify integration or publishing.
- Update user-facing blocked-state guidance to name both supported Printify strategies.

## Capabilities

### New Capabilities

- `standalone-printify-catalog-import`: Catalog retrieval and confirmed import for standalone Printify stores.

### Modified Capabilities

- `printify-catalog-import`: Expand the supported Store strategies from only Shopify + Printify to both Printify strategies.

## Impact

The application Printify catalog import service and import-session messaging change, with focused Application and Avalonia headless regression coverage. No persistence schema, external API, or Shopify publishing behavior changes.
