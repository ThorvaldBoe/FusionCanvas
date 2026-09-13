## Context

The current integration loads `/v1/catalog/blueprints.json`, which is the global Printify product catalog. The selected shop ID is already persisted in Store context, but it is only used as a prerequisite and is not sent to the catalog client. Printify's shop product resource is the correct source: `GET /v1/shops/{shop_id}/products.json`, paginated with `limit` and `page`, returns the products created in that shop, including each product's string ID, title, description, Blueprint ID, Print Provider ID, options, variants, and print areas.

The user-facing workflow remains an occasional action in the focused Store Editor's Catalog & mockups tab. The panel continues to load on demand, show explicit loading/empty/error states, allow multi-selection, require confirmation, and preserve local drafts and Store context during asynchronous work.

## Goals / Non-Goals

**Goals:**

- Retrieve all pages of products from the persisted selected Printify shop.
- Show shop products—not global catalog Blueprints—as the selectable import units.
- Keep products with the same underlying Blueprint distinct by their stable Printify product IDs.
- Import the selected product configurations into the existing local Blueprint/offering/variant/placeholder model safely and idempotently.
- Preserve current Store strategy guards, credential handling, cancellation, confirmation, failure messaging, and local-only data.
- Cover pagination, product identity, response validation, selection, persistence, and the relevant headless UI state.

**Non-Goals:**

- No Printify product creation, update, deletion, publishing, or mockup download.
- No automatic synchronization, background refresh, filtering by publish status, or deletion of local records absent from a later shop response.
- No database schema migration or change to Shopify behavior.
- No attempt to import every global catalog Blueprint as a fallback when the shop has no products.

## Decisions

### Use the shop product endpoint and persisted shop ID

The catalog client will accept the selected shop ID and call `/v1/shops/{shopId}/products.json`. It will request the maximum supported page size and follow the API's pagination metadata until all pages are collected. This is preferred over filtering the global catalog because only the shop endpoint represents products the user actually created.

### Treat a Printify shop product as the import identity

The product's stable string `id` is the primary external identity. The numeric `blueprint_id` remains provider metadata, but it is not sufficient for matching because multiple shop products can share one Blueprint. Imported local Blueprint metadata will retain the product ID (and blueprint ID for traceability); offering identity will be product/provider scoped.

### Use the product payload as the import source

The shop product response already contains the selected product's options, variants, and print areas. The integration maps those fields into the existing application models. Provider titles or additional catalog metadata that are not present in the product payload will use a safe deterministic label or be obtained through the existing catalog detail boundary only if the current model requires it; no global catalog list will be shown to the user.

### Keep selection identifiers opaque strings

The import session will carry Printify product IDs as strings rather than coercing them to integers. The UI may display the product title and a concise provider/product reference, but it will submit only the selected stable IDs to the application service.

### Fail the import atomically on malformed selected data

Required product, Blueprint, provider, variant, option, and print-area relationships will be validated before persistence. Invalid records produce a safe error and no partial local save. An empty shop result is a distinct successful retrieval state with guidance to create a product in Printify.

## Risks / Trade-offs

- [Risk] A shop can contain more products than one API page. → Follow `last_page`/`next_page_url` with a bounded maximum response size and cancellation-aware requests; add multi-page fixtures.
- [Risk] Printify product IDs are strings while current local matching helpers assume integer IDs. → Introduce product-specific metadata matching and keep existing integer catalog IDs only for Blueprint/provider/variant relationships.
- [Risk] Product payloads may omit data required by the local model or vary by product type. → Validate explicitly, map optional fields conservatively, and return a safe recoverable response without committing partial data.
- [Risk] Existing imported global Blueprints may collide with the new product identity scheme. → Preserve old records, use a new product identity marker for new imports, and document that re-import matching applies to product-origin records; do not delete or silently merge legacy records.

## Migration Plan

No database migration is required. Existing local catalog rows remain valid. New imports use shop-product identity metadata; rollback is a code revert. Existing global-catalog imports may remain visible as local records and are not automatically removed.

## Open Questions

None blocking. The selected shop is the authoritative scope, the shop product ID is the authoritative import identity, and the existing explicit-selection/persistence workflow remains the product behavior.

## Implementation Plan

1. Update the Application Printify models and interfaces so retrieval and selected import use a `StoreCredentialScope`/shop ID plus string product IDs, while keeping credential lookup in the application service.
2. Replace the Integration client's global blueprint list request with paginated shop-product retrieval, parse the response envelope, map product payloads into import models, and preserve safe HTTP/JSON classification.
3. Update selected-product loading and application import matching so product identity prevents collisions between products sharing a Blueprint and local-only rows remain untouched.
4. Update the import session view model and Store Editor copy/accessibility text from “Blueprints” to shop products while retaining multi-selection, focus, cancellation, and draft-safety behavior.
5. Add focused Integration tests for endpoint paths, shop ID, pagination, string IDs, empty results, malformed payloads, and safe provider failures; add Application persistence/idempotency tests for same-Blueprint distinct products; update App/headless tests for selection and labels.
6. Run criterion-level tests, `dotnet test .\\FusionCanvas.sln`, and strict `openspec validate`; record evidence for every scenario in `verification.md`.

## Acceptance-to-Verification Plan

- Store-scoped shop retrieval and unsupported-store guards: Application tests with credential/client stubs and Integration request assertions.
- Product list presentation, subset confirmation, cancellation, focus, and draft safety: existing focused App view-model/headless tests updated to use product summaries.
- Pagination and safe parsing/failures: Integration client tests with two-page, empty, malformed, and classified HTTP fixtures.
- Product identity, mapping, idempotency, and preservation of local-only data: Application and Integration persistence tests using two products sharing one Blueprint.
