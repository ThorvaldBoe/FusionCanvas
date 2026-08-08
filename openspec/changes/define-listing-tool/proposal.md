## Why

Issue #165 exposes a gap between the current read-only Listing-stage status summary and the product's need to prepare sellable listings for manual fulfillment or future marketplace integrations. The application needs one stable, provider-neutral listing model and one shared Listing-stage workflow so creators can prepare data manually today and move to Shopify or Shopify plus Printify later without duplicate tools, records, or lost work.

## What Changes

- Add a persistent, provider-neutral listing-preparation model attached to the existing Item, with common title, description, tags, price/currency, media references, variant/product references, readiness/publication state, and genuinely shared marketplace metadata.
- Add one shared Listing-stage UI that presents common fields in every strategy and progressively reveals or enables strategy-specific capabilities.
- Define manual fulfillment as marketplace-agnostic local preparation; it may prepare listings for any marketplace, including Shopify before connector support is enabled.
- Define Shopify plus manual as a Shopify channel binding selected by the user; persist the Shopify item identity and enable Shopify management only after the binding is established.
- Define Shopify plus Printify as a Printify publication path that acquires the Shopify identity after successful publication, with the post-publish Printify lock and explicit unlock warning established by issue #135.
- Extend the same listing record with provider/channel identities, provider metadata, source/override ownership, publication-channel state, and sync/publish diagnostics without duplicating common fields.
- Define strategy transitions, per-field source and manual-override preservation, readiness/publication states, external-ID scope, validation, recoverable errors, and conflicts before implementation.
- Add additive, no-data-loss persistence migration behavior so existing Items remain valid manual listings with stable identities and empty optional integration state.
- Bound the first implementation phase to local manual preparation and deterministic state/validation behavior; defer actual Shopify/Printify connector transport, remote synchronization, and conflict resolution algorithms to a later integration module.

## Capabilities

### New Capabilities

- `listing-preparation`: One shared Listing-stage listing model and user workflow supporting provider-neutral manual preparation and strategy-specific capability visibility for manual, Shopify plus manual, and Shopify plus Printify.

### Modified Capabilities

- `listing-inspector`: Extend the Item document surface so the shared Listing-stage experience can present listing preparation state and common listing fields without introducing a competing tool or duplicate status/metadata ownership.
- `local-sqlite-persistence`: Persist listing-preparation records, strategy/channel state, ownership metadata, and optional provider diagnostics with additive migration and stable existing Item identity.

## Impact

- Domain: new provider-neutral listing records/value objects, strategy and publication/readiness state, field ownership/override rules, and validation; existing Item identity and catalog relationships remain authoritative.
- Application: listing preparation use cases, repository contracts, strategy transition guards, readiness/error results, and future connector ports without implementing remote transport in this module.
- Integration: SQLite snapshot/schema/migration and round-trip support for the new optional listing data; provider DTOs and network clients remain out of scope.
- App: the existing ListingStageToolViewModel and Listing-stage host become the single shared editor with common fields, progressive disclosure, empty/read-only/error states, and strategy-aware enabled state.
- Tests: Domain validation, Application deterministic workflow tests, isolated SQLite migration/round-trip tests, and Avalonia headless tests for the meaningful Listing-stage bindings and state transitions.
- Dependencies: existing Item management/inspector, Store product and fulfillment catalog, workspace snapshot persistence, stage navigation, and current automatic-save/atomic-operation conventions.
- Risks: unclear ownership between Item creative fields and listing fields, accidental duplication of common data in Shopify projections, and strategy transitions that silently overwrite manual overrides. The delta specs and design must resolve these before implementation begins.
