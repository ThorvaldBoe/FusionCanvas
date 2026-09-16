## 1. Contracts and shop-product models

- [x] 1.1 Replace the global-catalog import contract with shop-product retrieval that accepts the selected Printify shop ID and uses opaque string product IDs.
- [x] 1.2 Add product-specific summaries and payload models carrying product ID, title, description, Blueprint ID, provider ID, options, variants, and print areas while preserving compatibility with the local catalog import model.

## 2. Printify integration

- [x] 2.1 Implement paginated `GET /v1/shops/{shop_id}/products.json` retrieval with the maximum supported page size, cancellation, response-size limits, and safe status/error classification.
- [x] 2.2 Parse the paginated response envelope and validate required product identities and relationships; represent an empty shop without falling back to the global Blueprint catalog.
- [x] 2.3 Update selected-product retrieval to resolve only the selected shop products and retain stable product IDs through the import operation.

## 3. Application import and persistence

- [x] 3.1 Pass the Store's persisted Printify shop ID into retrieval and retain the existing saved-store, strategy, credential, and selected-shop guards.
- [x] 3.2 Map selected shop products into product-specific local Blueprints, provider offerings, options, variants, and placeholders, including product identity metadata and safe validation before save.
- [x] 3.3 Make repeated imports idempotent by shop-product/provider identity and prove two products sharing one Blueprint remain distinct; preserve local-only catalog records.

## 4. Store Editor workflow

- [x] 4.1 Update import-session models, selection commands, confirmation payloads, and user-facing text from global Blueprints to products created in the selected Printify shop.
- [x] 4.2 Preserve loading, empty, error, cancellation, focus, stale-result protection, and unsaved local catalog draft behavior; update accessible names and empty guidance.
- [x] 4.3 Update or add deterministic Avalonia headless coverage for the revised import action, product selection, confirmation, empty state, and strategy gating.

## 5. Verification and delivery

- [x] 5.1 Add Integration tests for shop endpoint paths, shop IDs, pagination, product string IDs, empty responses, malformed payloads, and safe provider failures.
- [x] 5.2 Add Application and persistence tests mapping selected shop products, handling duplicate underlying Blueprints, repeated import, and unsupported Store contexts.
- [x] 5.3 Run criterion-level verification for every scenario in both delta specs and record results in `verification.md`.
- [x] 5.4 Run `openspec validate` and resolve any strict validation errors.
- [x] 5.5 Run `dotnet test .\\FusionCanvas.sln` and record the final baseline result in `verification.md`.
