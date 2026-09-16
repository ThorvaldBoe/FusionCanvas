## Context

The standalone Printify strategy and its credential/shop controls already exist. Catalog import has a narrower guard in `PrintifyCatalogImportService` and a matching view-model error message that only recognize Shopify + Printify. The desired behavior is a strategy-policy correction, not a new API or persistence model.

The primary workflow is occasional store setup in the existing focused Store Editor. The import panel remains progressively disclosed behind the existing Import from Printify command, with existing loading, empty, error, confirmation, cancellation, and focus behavior preserved.

## Goals / Non-Goals

**Goals:**

- Permit standalone Printify through the same credential, shop, retrieval, selection, confirmation, and persistence path.
- Keep strategy checks Store-scoped and keep Shopify behavior excluded for standalone Printify.
- Provide accurate blocked-state guidance.

**Non-Goals:**

- No Shopify publishing or integration changes.
- No database migration, API contract change, or redesign of the import panel.

## Decisions

- Use `FulfillmentStrategyPolicy.RequiresPrintifyKey` as the shared definition of Printify-capable strategies, rather than duplicating an enum comparison. This keeps standalone Printify and Shopify + Printify aligned.
- Keep the selected-shop guard and credential lookup unchanged. Both are required for either Printify strategy.
- Update the application service message and view-model precondition message to say “Printify Store” so the user is not incorrectly told to configure Shopify.
- Verify decision logic in framework-free Application tests for both strategies and retain the existing headless Store Editor coverage for the user-facing workflow. No live desktop check is needed for this equivalent low-risk path.

## Risks / Trade-offs

- [Risk] A future Printify-capable strategy could be added to policy but lack catalog compatibility. → The policy remains the single intentional capability boundary; future additions must satisfy this contract and tests.
- [Risk] Existing tests may only cover Shopify + Printify. → Add standalone retrieval and import regression cases.

## Migration Plan

No migration is required. Existing stores and credentials remain compatible. Rollback is a code revert.

## Open Questions

None. Standalone Printify’s credential, shop, catalog, and non-Shopify boundaries are already defined by the accepted standalone fulfillment specification.

## Implementation Plan

1. Update `PrintifyCatalogImportService` to accept any strategy for which `FulfillmentStrategyPolicy.RequiresPrintifyKey` is true and use strategy-neutral invalid-context text.
2. Update `PrintifyCatalogImportViewModel` precondition text to refer to a saved active Printify Store.
3. Add Application tests proving standalone Printify passes the same guards and imports selected catalog data; retain rejection tests for non-Printify strategies.
4. Run focused tests, the full solution test baseline, and strict OpenSpec validation. Record each acceptance scenario in `verification.md`.
