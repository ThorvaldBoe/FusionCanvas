## Context

The Store Editor import currently retrieves the selected Printify shop's products and uses each product ID as a selectable/import identity. The Integration client then loads that product's Blueprint and single Provider, fetches that Provider's catalog variants, and filters the result back to the variants enabled on the shop product. Application persistence keys local Blueprints by shop-product identity, so two products made from one Printify Blueprint can become separate local Blueprints.

The catalog client and `PrintifyCatalogImportService.ApplySelectedCatalog` are also used by the Niche-level listing import and linked-listing Variant Setup refresh. Those flows intentionally use a specific shop product and its enabled Variant subset. Store catalog import therefore needs its own Blueprint-level selection and persistence entry points; it must not change the product-level client contract or mapper those flows depend on.

The domain model already supports one Store-owned Blueprint with multiple Blueprint Offerings, each with typed Options, Option Values, concrete sellable Variants, and Placeholder compatibility. Variant management distinguishes available Option choices from actual concrete Variant combinations. The new import should use that shape: the shop identifies which Blueprints and Providers are in scope; the provider catalog defines each Offering's supported combinations.

This is an occasional setup workflow in the focused Store Editor. It remains outside the main workspace. The existing shop-product-to-Item listing import remains a separate workflow.

## Goals / Non-Goals

**Goals:**

- Present one selectable row per distinct Printify Blueprint found among all products in the selected shop.
- Discover the distinct Providers actually used by shop products for each Blueprint.
- For each selected Blueprint and each used Provider, import all catalog-reported Variants that are enabled and available, along with their Option definitions, Option Values, exact memberships, and applicable Placeholder data.
- Use explicit Printify Variant records as authoritative combinations; never generate a Color-by-Size Cartesian product.
- Persist one canonical Store Blueprint per Printify Blueprint identity and one Offering per Blueprint/Provider identity, with stable idempotent updates.
- Preserve the existing Store guards, explicit confirmation, cancellation, stale-context protection, local draft preservation, and safe failure behavior.
- Leave existing product-level imported catalog records and relationships untouched; this change does not perform a destructive or implicit catalog cleanup.

**Non-Goals:**

- Importing every Provider available in the global Printify catalog. Only Providers used by products in the selected shop are in scope.
- Importing listing Items, artwork, publication state, or external listing mappings. The Niche-level listing import remains responsible for that.
- Changing mockup-template coverage/readiness rules, auto-creating templates, or defining templates for every imported Color. This remains a separate follow-up even though importing the full sellable Variant set may expose the existing setup burden.
- Merging, archiving, or deleting existing local product-level imported Blueprints or their child records.
- Search, user sorting, paging, automatic synchronization, or removing local catalog records absent from a later shop response.

## Decisions

### Use shop products only to discover Blueprint and Provider scope

Retrieve every page of products from the persisted selected Printify shop. Group by stable numeric `blueprint_id`; within each group, collect the distinct `print_provider_id` values. Shop product identity and title remain supporting provenance and may contribute to a concise product-count/context summary, but neither defines a local catalog Blueprint or limits its sellable Variants.

This follows the user's requested meaning of “used in my store.” Loading all global Blueprints or all global Providers would include catalog items that the shop does not use.

### Load full catalog data for each selected Blueprint/used Provider pair

After explicit selection, load the authoritative Blueprint name/metadata and the complete provider catalog for every discovered Blueprint/Provider pair. Keep catalog Variants only when Printify reports them enabled and available. Resolve every retained Variant's option value IDs to stable option/value names and retain the exact combination returned by Printify. Do not infer or synthesize combinations.

Option/value metadata may be present in shop product detail payloads while full Variant combinations come from the provider catalog endpoint. The Integration mapper should union consistent option metadata across shop products sharing the same Blueprint/Provider and validate coverage of all retained catalog Variant option IDs. If required labels or relationships cannot be resolved, fail the selected import safely rather than inventing values or partially saving.

Provider catalog print areas/Placeholder metadata should be mapped with the full Variant set and preserve each Placeholder's exact Variant compatibility. Product-specific selected print areas must not remove catalog-level supported Variant combinations.

### Make Printify Blueprint and Provider IDs the local import identities

Use Store + Printify Blueprint ID to match the canonical local Blueprint. Use Store + Printify Blueprint ID + Provider ID to match a Blueprint Offering. Continue using stable Option, Option Value, Variant, and Placeholder provider identities within those parents. Do not carry a shop product ID as the Blueprint identity.

Repeated imports update matching rows in place and keep valid local relationships. Provider or Blueprint records omitted from a later shop response are not automatically deleted. Earlier product-level imports are not automatically merged or modified; any consolidation policy can be explored separately with explicit relationship-preservation rules.

### Present a Blueprint-level selection grid

Replace product-per-row selection with one fixed-height virtualized row per distinct Blueprint. Show its Blueprint name, distinct used Providers, and concise shop-product context/count. The checkbox accessible name identifies the full Blueprint and used Provider names. Selection state and submission use the stable Blueprint ID, so row recycling cannot change selected identities. Preserve the current focused Store Editor surface, keyboard use, visible loading/empty/error states, and import/cancel actions.

Do not add search/sort/paging in this module. The complete shop is retrieved and grouped before the grid appears. If large Blueprint names or Provider lists do not fit, retain the complete text in tooltip and accessible name.

### Preserve existing catalog safety and avoid legacy data migration

Validate all selected Blueprint/Provider payloads before saving the batch. Apply the normalized graph in one repository save so a malformed provider or Variant relationship does not leave a partial import. Match newly canonical records only through the new stable Blueprint/Provider identity metadata or an unambiguous legacy canonical Blueprint identity. Do not guess which of multiple product-level legacy Blueprints should own local relationships; leave those records intact and do not merge them.

No SQLite schema migration is expected because the existing normalized catalog entities already represent the target graph and provider identity metadata is flexible. Rollback is a code revert. Legacy product-level rows remain valid local data after rollback or upgrade.

### Keep mockup-template coverage as a follow-up

The imported Offering may contain many Colors and Variants. This module will not weaken, redefine, or bypass existing template readiness rules and will not create templates automatically. Import confirmation should describe catalog records/Variants imported, not claim that mockup setup is complete. A separate change can address how a template applies to colors or Variants.

## Risks / Trade-offs

- [Risk] Provider catalog Variant payloads may refer to Option Value IDs not present in one product's configured option list. → Gather option definitions across all shop products for the Blueprint/Provider, resolve against available authoritative catalog fields where present, validate every referenced ID, and fail safely if names remain unresolved.
- [Risk] A Blueprint/Provider may expose many sellable Variants and increase manual mockup-template setup. → Import the full requested sellable set and keep template coverage changes explicitly out of scope; show counts so the added setup surface is visible.
- [Risk] Older product-level imports may remain alongside a newly canonical Blueprint. → Preserve them without mutation and record this boundary; do not silently merge relationships. Any cleanup requires a separate, explicit migration design.
- [Risk] One shop may have many products but relatively few Blueprints. → Group and deduplicate before presentation, use stable identities and the existing virtualized grid pattern, and test grouping and selection under row recycling.
- [Risk] The selected shop can change while retrieval is in flight. → Preserve current scope/version checks; discard late results and perform no save against a changed shop or Store.

## Migration Plan

No database schema migration is planned. New imports use Store-scoped Blueprint and Provider catalog identities. Existing product-level imported records are retained and are not automatically merged, archived, or deleted. The previous implementation can be restored by reverting code; any new records remain valid normalized catalog records.

## Open Questions

None. Provider scope, full enabled-and-available Variant scope, explicit-combination behavior, and the separate mockup-template follow-up were confirmed during discovery.

## Implementation Plan

1. Add Application import contracts and result models for unique Blueprint choices, their distinct shop-used Providers, source shop-product context/counts, and selection by stable Blueprint ID. Retain the existing product-selection APIs for listing import and linked-listing Variant Setup.
2. Update `PrintifyCatalogClient` to retrieve all shop-product pages, group product summaries by Blueprint ID, collect distinct Provider IDs, and enrich Blueprint names. For selected Blueprint IDs, retrieve every used Provider's complete catalog Variant set and all required option/value/Placeholder metadata; filter to enabled and available Variants, validate identity consistency, and preserve exact option memberships.
3. Add a Blueprint-level mapper in `PrintifyCatalogImportService` to persist one Store-scoped Blueprint per external Blueprint ID and one Offering per Blueprint/Provider pair, with stable option/value/variant/Placeholder matching. Keep `ApplySelectedCatalog` product-specific for the separate listing import and linked-listing Variant Setup workflows. Ensure the new import is idempotent, atomic, preserves valid local relationships and local-only records, does not delete omitted records, and does not merge product-level legacy records.
4. Update the Printify import ViewModel and Store Editor virtual grid to show/select distinct Blueprints, used Providers, and shop-product context/count; submit Blueprint IDs; retain loading, empty, error, cancellation, focus, and local-draft safeguards.
5. Update/add Integration tests in `PrintifyCatalogClientTests` for grouping, provider discovery, complete enabled/available variants, option-value resolution, duplicate shop products, malformed data, paging, and failures. Update Application/persistence tests for one canonical Blueprint with multiple used Providers, complete variant combinations, idempotency, atomic failure, local relationship preservation, and legacy-row preservation. Keep or add regression coverage proving product-level listing import and linked-listing Variant Setup retain their existing product-scoped identities and variant subsets. Update ViewModel and `StoreEditorHeadlessTests` for one row per Blueprint, selection identity, accessible naming, focus, cancellation, and draft safety.
6. Review every delta-spec scenario against focused evidence; run the full solution test baseline and strict OpenSpec validation; record criterion-level results in `verification.md`.

## Acceptance-to-Verification Plan

- Store-scoped retrieval, unique Blueprint grouping, used Provider discovery, empty shop, and unsupported Store guards: Application tests with deterministic store/credential/client collaborators and Integration endpoint/fixture assertions.
- Explicit subset confirmation and cancellation: ViewModel tests; headless Store Editor coverage for grid rows, accessible labels, and selection behavior.
- Full enabled/available provider Variant set and exact Option Value memberships, with no synthesized combinations: Integration parser/client fixtures plus Application mapping and persistence tests.
- One local Blueprint, one Offering per used Provider, idempotency, and preservation of local-only/legacy data: Application and Integration persistence tests, including multiple shop products sharing a Blueprint and Provider.
- Product-level listing import and linked-listing Variant Setup remain unchanged: existing `PrintifyListingImportServiceTests` and listing lifecycle/API verification coverage, plus focused regression assertions where mapper reuse requires them.
- Malformed or incomplete metadata fails atomically; cancellation/stale Store context publishes no results: Integration and Application tests using deterministic failure/cancellation collaborators plus existing Store Editor safety coverage.
- Real-desktop Appium: no new journey planned. The workflow stays in the existing Store Editor and introduces no native OS interaction; deterministic ViewModel and Avalonia headless tests cover identity, bindings, selection, keyboard-accessible controls, and focus. Existing supplemental Appium listing-import coverage remains separate.
