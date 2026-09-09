## Why

When a Store uses Shopify + Printify, its local Blueprint catalog must otherwise be recreated by hand even though Printify already exposes the authoritative catalog structure. Issue #322 adds a focused, confirm-before-import workflow so creators can select the Blueprints they want and populate local catalog settings with provider, option, sellable-variant, and design-area data while retaining local ownership.

## What Changes

- Add a Store-scoped Printify catalog retrieval boundary using the already configured Printify credentials and selected shop context.
- Add a Catalog & mockups action beside New Blueprint that is visible only for Shopify + Printify Stores and opens a selectable list of available Printify Blueprints before any local mutation.
- Import selected Blueprints and their available provider offerings, typed option choices, sellable variants, and design areas into the existing local catalog model.
- Make repeated imports idempotent: matching Printify identities update existing local records and do not create duplicates; records no longer returned remain local and are not silently deleted.
- Keep mockup templates, source images, artwork, placement, and generated products out of scope because they depend on local files or later workflows.
- Surface safe loading, empty, authentication/permission, rate-limit, timeout, malformed-response, cancellation, and persistence-error states without exposing credentials or raw provider responses.

## Capabilities

### New Capabilities

- `printify-catalog-import`: Retrieve, preview, select, and idempotently import Printify catalog data into one Store.

### Modified Capabilities

- `store-management`: Add the strategy-gated Catalog & mockups import entry point and confirmation workflow.
- `product-supplier-setup`: Define how provider catalog identities map to existing Blueprints, offerings, Options, Variants, and Placeholders/design areas during import.
- `local-sqlite-persistence`: Persist provider identities and imported catalog updates transactionally without duplicating records or deleting locally retained data.

## Impact

The change affects Application catalog/import contracts and orchestration, Integration Printify HTTP access and response mapping, existing Domain catalog identity fields or import invariants if required, SQLite snapshot persistence, and Store Editor Avalonia presentation. It depends on the completed per-Store Printify credential and shop-selection capability from issue #320. Verification will use fake HTTP responses, deterministic application/persistence tests, and Avalonia headless tests for visibility, selection, confirmation, busy/error states, and keyboard-safe cancellation; no live Printify account is required.
