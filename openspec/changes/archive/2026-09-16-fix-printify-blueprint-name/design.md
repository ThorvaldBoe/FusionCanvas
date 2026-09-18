## Context

The shop-product endpoint returns a user-facing product title. That title is not the catalog Blueprint identity: a product can be titled “Gildan 64000 t-shirt” while the catalog Blueprint exposes brand `Gildan` and model `64000`. The current import constructs and updates local Blueprints directly from the shop title, so repeat imports also overwrite a local Blueprint name with that title.

## Goals / Non-Goals

**Goals:**

- Resolve selected shop products to the authoritative catalog Blueprint record.
- Use `brand + model` as the local Blueprint name when both are available.
- Fall back to the catalog title when either identity field is unavailable.
- Mirror the corrected normalized Blueprint name into the legacy StoreProduct projection so the current Store Management screen reflects the imported name after refresh.
- Keep stable matching, offering names, provider data, and idempotent persistence behavior unchanged.

**Non-Goals:**

- Do not rename user-created or local-only Blueprints that are not matched to the imported Printify identity.
- Do not change the Printify shop product title.
- Do not add a new UI control or database migration.
- Do not change Blueprint Offering naming in this module.

## Decisions

1. **Resolve catalog metadata during selected import.** The shop-product payload includes `blueprint_id`, so the integration client will request `/catalog/blueprints/{blueprint_id}.json` for each distinct selected Blueprint. This avoids guessing from title suffixes and uses the provider's documented identity fields.

2. **Name from brand and model, with title fallback.** The application import service owns the local naming rule: trimmed non-empty brand and model become `"{brand} {model}"`; otherwise the resolved catalog title is used. This keeps provider-specific naming policy out of the UI and makes it directly testable.

3. **Refresh the legacy projection from normalized catalog state.** After import, use the existing `CatalogCompatibilitySynchronizer` so Store Management consumers see the same corrected Blueprint name as the normalized catalog without introducing a second naming rule.

4. **Fail safely if selected Blueprint metadata cannot be resolved.** A selected import depends on authoritative Blueprint identity. Existing client error classification is returned and no local repository write occurs when a required catalog lookup fails.

## Risks / Trade-offs

- [Additional API calls] Selected imports make one catalog request per distinct Blueprint. → Deduplicate by Blueprint ID and keep the initial shop-product preview unchanged.
- [Provider metadata changes] A later import can change brand/model. → Matching remains based on stable Printify identity metadata; the imported local name follows the latest authoritative catalog identity.
- [Legacy records] Older imports may have title-based names. → Repeat import updates the matching normalized Blueprint to the corrected identity without changing its ID or relationships.

## Implementation Plan

1. Update `PrintifyCatalogClient` to enrich selected shop-product details with catalog Blueprint summaries, preserving each selected product ID and existing variants/placeholders.
2. Add a small application-level naming helper in `PrintifyCatalogImportService` and use it for Blueprint create/update only.
3. Mirror normalized import results through `CatalogCompatibilitySynchronizer` so the visible legacy product projection is refreshed.
4. Add integration-client tests for the catalog lookup and application tests for brand/model naming, title fallback, projection synchronization, and repeat-import update behavior.
5. Run focused tests, full solution tests, and strict OpenSpec validation; record criterion-level evidence in `verification.md`.

## Verification Mapping

- Authoritative catalog identity is used: integration-client test plus application import test with `Gildan`/`64000` expects `Gildan 64000`.
- Missing brand/model falls back to title: application import test with null identity fields expects the catalog title.
- Existing identities and relationships remain stable: existing repeated-import and persistence tests, plus full solution baseline.
- Lookup failures do not persist partial imports: integration/client failure test and repository assertion in the application boundary test.
