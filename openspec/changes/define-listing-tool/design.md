## Context

Issue #165 asks for a Listing tool that works for manual and future Shopify workflows. Issues #135 and #136 describe Shopify plus Printify and Shopify plus manual paths, but their “Printify tool” and “Shopify tool” language must be reconciled with the settled principle of one shared Listing tool and one persistent listing-data model.

Today the ListingStageToolViewModel is a read-only lifecycle summary. Item already owns the canonical working title, generic description, reusable tag links, assets, metadata, workflow stage, and lifecycle status. The Store catalog already owns provider-neutral products, offerings, variants, and design areas, including Printify Choice as a variable fulfillment network. The new module must compose those existing records rather than copy them or turn catalog fulfillment kinds into listing strategies.

The first delivery outcome is local listing preparation: a creator can prepare and validate a listing manually, see strategy-aware capability state, and later select a Shopify or Shopify plus Printify strategy without losing local data. Network connectors and remote synchronization are intentionally deferred.

## Goals / Non-Goals

**Goals:**

- Define one logical listing-preparation aggregate associated one-to-one with an Item.
- Keep Item title/description, tag links, assets, and catalog references canonical; add only listing-specific local data such as price/currency, readiness, strategy, ownership, and optional provider state.
- Support manual, Shopify plus manual, and Shopify plus Printify as strategy states on the same record.
- Make common fields stable and provider-neutral, with progressive disclosure and enabled-state rules for strategy-specific capabilities.
- Define source/inherited, manual-override, and provider-managed ownership without silently overwriting overrides.
- Persist and migrate the data additively with stable Item identities and no connector requirement.
- Provide deterministic Domain/Application/Integration coverage and focused Avalonia headless coverage for meaningful Listing-stage behavior.

**Non-Goals:**

- Shopify or Printify credentials, HTTP clients, API calls, publication, pull/push synchronization, remote conflict resolution, or live connector tests.
- A second Shopify or Printify Listing tool, provider-specific duplicate common fields, or a generic marketplace-plugin abstraction.
- Marketplace-specific schema design beyond the provider/channel extension boundary needed to represent optional identity and diagnostics.
- Mockup generation, image transformation, shipping calculation, inventory synchronization, or new catalog administration. Existing media, variant, product, offering, and design-area records are referenced or surfaced.
- Treating `FulfillmentKind.FixedProvider` or `PrintifyChoiceNetwork` as the Listing strategy. Those remain Store catalog offering semantics.

## Decisions

### One logical listing aggregate, with existing Item data as canonical

Represent listing preparation as a one-to-one `ItemListingProfile` (name provisional) associated with the existing Item. The logical listing record is the Item plus this profile and existing relationship tables; it is not a replacement Item and does not copy Item.Name, Item.Description, tags, assets, or catalog references. A profile may be materialized for existing Items during migration with empty optional values.

Alternative rejected: put all values in Item.MetadataJson. That would make price/currency, ownership, strategy transitions, and provider state difficult to validate and query, while allowing unrelated metadata edits to corrupt listing semantics.

Alternative rejected: create separate ManualListing, ShopifyListing, and PrintifyListing records. That would violate the settled continuity principle and make strategy changes a data migration between duplicate records.

### Common fields remain shared; strategy state is an extension

The common surface includes the existing title, description, tags, media references, and product/variant references plus listing-specific price/currency, local readiness/publication state, and genuinely shared metadata. Strategy state selects Manual, ShopifyManual, or ShopifyPrintify and controls capability visibility. Shopify identity, publication-channel bindings, provider metadata, and diagnostics are optional child data keyed by provider/channel.

Price is provider-neutral in this module, but its exact future granularity is deliberately fixed to one local listing price/currency value for Phase 1. Variant- or channel-specific prices are deferred and must not be invented by implementation.

### Ownership is explicit and overrides are durable

Every field that can be inherited or synchronized has a source classification: `ItemOrCatalog`, `ManualOverride`, or `ProviderManaged`, with an optional provider/channel scope. Existing Item fields remain canonical and can be presented as inherited shared values; a local listing profile stores only explicit listing-specific values and ownership markers. A strategy switch changes capability and provider state, not common values. A future connector may update provider-managed fields only through an explicit sync result and must preserve manual overrides.

### Strategy transitions are guarded state changes

- Manual is always locally usable and marketplace-agnostic.
- Shopify plus manual requires a user-selected Shopify item identity before Shopify actions become enabled. The identity is stored in the same profile and is visible/copyable.
- Shopify plus Printify represents the future Printify publication path. A successful publication supplies the Shopify identity; after that, the Printify surface is locked by default and requires an explicit warning-confirmed unlock for exceptional edits.
- The local Phase 1 implementation can represent these states and prerequisite messages without executing a connector operation. No state is marked externally published unless a future connector result supplies confirmation.

### Readiness and external publication are separate

Use local readiness checks for manual preparation. Keep external publication state and provider operation diagnostics optional and separate from local readiness. A locally ready manual listing has no implied Shopify or Printify identity.

### Persistence uses additive snapshot/schema evolution

Extend `WorkspaceSnapshot` with listing profile and provider-state collections using empty compatibility defaults. Add SQLite tables and foreign keys in a new schema version. Save profile/provider rows after their Item/Store/catalog parents and delete in reverse dependency order. Existing versioned workspaces migrate transactionally with empty optional provider state. Cross-store and invalid references fail before commit; existing snapshot remains unchanged.

### UI uses the existing stage host and progressive disclosure

Extend `ListingStageToolViewModel` and the Listing-stage view in the existing MainWindow stage host. Keep the common editor visible in all strategies. Use compact strategy selection and collapsible strategy-specific sections. Disabled actions must explain their prerequisite. Empty, loading, success, validation-error, unavailable-connector, inactive/read-only, and post-publish-locked states are explicit. Field edits follow existing automatic-save/commit-drain conventions; strategy changes and provider binding are deliberate actions with recoverable failure states.

The main workspace remains the right surface because Listing preparation is a frequent item-bound workflow, while occasional credential or connector administration remains outside this module. No second dialog or tool selector is introduced for Shopify or Printify.

## Risks / Trade-offs

- [Common fields drift into Shopify-specific storage] → Keep Item fields and existing relationship tables canonical; prohibit duplicate title/description/tags/price fields in provider extensions and add persistence assertions.
- [Strategy changes silently overwrite user work] → Persist ownership markers, apply guarded transitions, and test that manual overrides survive every local strategy transition.
- [Current product catalog semantics are confused with connector strategy] → Document and test the distinction between listing strategy and `FulfillmentKind`, especially Printify Choice.
- [Provider schemas change] → Keep provider data behind provider/channel-scoped extension records and Application ports; do not place Shopify DTOs in Domain.
- [Migration corrupts existing workspaces] → Use additive tables, transaction boundaries, empty defaults, isolated upgrade fixtures, and rollback tests.
- [UI becomes too dense] → Use progressive disclosure, keep common fields compact, and put diagnostics/advanced ownership details behind expandable sections.
- [Future connector scope expands this module] → Phase 1 tasks must stop at deterministic local state and port definitions; actual connector operations require a later OpenSpec change.

## Migration Plan

1. Add Domain profile, strategy/state, ownership, and provider-diagnostic types with compatibility-friendly empty defaults.
2. Add Application contracts and local preparation service; ensure transitions and saves are atomic and connector-independent.
3. Add SQLite schema version migration and snapshot round trips. Existing Items become valid manual profiles without altering existing values or requiring settings.
4. Wire the Listing-stage VM/view and shared Item context. Surface existing Item/catalog references rather than copying them.
5. Verify upgrade, round-trip, invalid-reference, failure-rollback, strategy-transition, and headless UI scenarios.
6. Rollback strategy: because the migration is additive and transactional, a failed upgrade leaves the prior schema/data untouched; a future connector remains absent and does not affect local listing use.

## Open Questions

No product decision blocks the bounded local phase. The following are explicitly deferred to a later connector/integration proposal: Shopify API credential flow, exact remote field mapping, channel mutation semantics, push/pull direction, conflict merge policy, and Printify publication transport. The local model must retain enough provider/channel identity and diagnostics to support those later decisions without replacing the shared listing record.

## Implementation Plan

### Affected layers and likely types

1. **Domain** — add `ItemListingProfile`, strategy/readiness/publication enums, field-source/override records, provider/channel identity and diagnostic records, and pure validation. Reuse `Item`, `ItemTag`, `AssetLink`, `ItemListingConfiguration`, `StoreProduct`, and `ProductVariant` relationships.
2. **Application** — add listing preparation state/results, local repository/use-case contract, guarded strategy-transition and update requests, and atomic save orchestration. Keep future connector ports minimal and unused in Phase 1.
3. **Integration** — extend `WorkspaceSnapshot`, `SqliteWorkspaceRepository`, schema initialization/migrations, insert/load/delete ordering, and validation for profile/provider rows. Add isolated upgrade and round-trip fixtures.
4. **App** — replace the informational `ListingStageToolViewModel` behavior with shared listing state, strategy controls, common-field bindings, readiness/diagnostic presentation, and capability gating in the existing Listing-stage XAML/stage host. Coordinate with `ItemInspectorViewModel` so common Item fields have one owner.

### Data and algorithms

- Resolve a listing's effective store from its Item and validate every referenced product, variant, asset, and provider/channel record against that store.
- On load, compose canonical Item fields and relationship references with profile-owned listing fields and ownership state; never copy common fields into provider extensions.
- On local update, validate price/currency, references, strategy prerequisites, and lifecycle editability before producing one snapshot save. A failed save returns the existing draft and leaves the repository snapshot unchanged.
- On strategy transition, preserve common data, update strategy/capability state, clear only explicitly invalid provider bindings with a recoverable explanation, and never fabricate external publication.
- Model Shopify manual binding and future Printify-produced binding through the same provider/channel identity shape. Keep post-publish Printify lock state explicit.

### Sequencing and tests

- Implement Domain and Application state/validation first; add focused tests before UI wiring.
- Implement SQLite migration/round trip and atomic failure tests before exposing the new persisted state.
- Implement the Listing-stage UI and view-model tests, then Avalonia headless tests for construction, bindings, enabled/disabled capability state, strategy transitions, inactive/read-only state, and post-publish lock presentation.
- Run strict OpenSpec validation and `dotnet test .\\FusionCanvas.sln` as completion gates. Live connector tests are not part of this module.

### Acceptance-to-verification mapping

- One model and strategy continuity: Domain profile/strategy tests plus Application transition tests.
- Common fields and ownership: Application save/override tests and Item relationship preservation tests.
- Strategy visibility/prerequisites: App view-model tests and focused Avalonia headless Listing-stage tests.
- Shopify manual binding and Printify post-publish state: deterministic local state tests; connector transport is deferred.
- External IDs/diagnostics: Domain validation and SQLite round-trip tests.
- Readiness/publication distinction: Domain/Application state tests and UI state assertions.
- Additive migration/atomicity: isolated SQLite upgrade, round-trip, invalid-reference, and rollback tests.
