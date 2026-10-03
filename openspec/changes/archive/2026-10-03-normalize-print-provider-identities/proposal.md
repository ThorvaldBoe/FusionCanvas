## Why

The Store Editor can show the same Print Provider more than once when imported or legacy records share a provider name but carry different external Printify IDs. This is confusing in the fixed-provider offering picker and leaves offerings attached to competing local identities. The product decision is that a Store has one provider identity for a provider name such as `SwiftPOD`; existing references must be consolidated safely.

## What Changes

- Define Store-scoped Print Provider name normalization for imported and manually created providers.
- Merge same-name provider records even when their non-empty external provider IDs differ.
- Reassign every affected Blueprint Offering to the surviving provider identity before archiving duplicate records.
- Preserve duplicate records as archived history rather than deleting them.
- Preserve all known external Printify provider IDs as provider metadata aliases while retaining the local provider ID as the relationship key.
- Repair stale duplicate providers when Catalog setup loads, so the reported dropdown issue is fixed without requiring a new import.
- Prevent creation of another active Print Provider with a name already used in the Store.
- Keep Printify shop-product and offering identities idempotent and independent from provider display labels.
- Add deterministic application and UI-facing regression coverage for same-name providers with different external IDs.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `product-supplier-setup`: Define one Store-scoped Print Provider identity per normalized provider name, safe consolidation of duplicate records, reassignment of offering references, and exclusion of archived duplicates from active selections.
- `printify-catalog-import`: Normalize imported providers by Store-scoped name while preserving external identity aliases, and maintain idempotent imports after consolidation.

## Impact

- Application catalog setup and Printify import services will share provider-normalization behavior.
- The Store Editor's available-provider projection will consume the normalized active provider set; no new management surface is required.
- Existing `BlueprintOffering.PrintProviderId` relationships will be rewritten transactionally in the in-memory snapshot before persistence, with compatibility projections synchronized afterward.
- Provider metadata will carry additional Printify external-ID aliases; no database schema migration is expected.
- Affected tests include `FusionCanvas.Application.Tests` catalog/import tests, `FusionCanvas.App.Tests` provider-picker coverage, and relevant persistence round-trip coverage.
- The module is intentionally limited to Store-owned Print Providers and fixed Blueprint Offerings. It does not change Provider Network identity, shop-product identity, Printify API retrieval, or unrelated catalog records.
