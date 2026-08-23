## Why

FusionCanvas's current Store catalog uses partially Printify-compatible concepts but cannot represent a store's integration strategy, stable option semantics, durable provider-network identity, or reusable color-level mockup templates. GitHub issue #185 needs one manual-first configuration module that replaces those weak boundaries with a Printify-aligned local model, so later Listing, Printify, and Shopify modules can build on stable relationships instead of migrating another provisional catalog.

## What Changes

- Add a Store-level fulfillment strategy with `Manual`, `ShopifyManual`, and `ShopifyPrintify`; migrate every existing Store to `Manual`, expose all three choices in the focused Store Editor, and keep the two integration strategies disabled until their later modules exist.
- Replace user-facing and domain catalog language with Printify terminology: Blueprint, Print Provider, Blueprint Offering, Option, Variant, and Placeholder. Non-intuitive terms receive visible helper text or accessible tooltips rather than alternate names.
- Reshape the Store catalog around explicit Blueprints, fixed-Print-Provider or Provider-Network offerings, stable provider-network codes, typed Options (`Color`, `Size`, `Other`), Option Values, concrete Variants, and variant-compatible Placeholders.
- Preserve Printify Choice as a named Provider Network with a stable identity independent of its mutable display label; do not fabricate an ordinary Print Provider for it.
- Add Store-level Mockup Template configuration. Every template targets one concrete Placeholder from its own Blueprint Offering and maintains color-level template records bound to Color Option Values from that offering.
- Make one active template-color record the primary future mockup source for all compatible concrete variants sharing that color. Do not add size-specific, concrete-variant, or generalized override structures.
- Add template revision attribution and lifecycle rules so later generated mockups can remain attributable to the exact template revision and color value used; template changes affect future generation only.
- Prefer archive/deactivation for catalog records with dependents and block destructive removal of referenced Placeholders, Color Option Values, offerings, or templates until dependents are reassigned, archived, or removed safely.
- Provide a clearly empty future template-source-image state. Source-image upload/import, asset editing, coordinates, slots, transforms, rendering, composition, and placement UI are not part of this module.
- Define and verify a concrete migration from existing products, offerings, inline option combinations, and design areas into the new Manual-strategy model without losing valid identities, relationships, or Item target selections.
- Reconcile durable UI/data-model guidance that currently suggests numeric placement configuration or older Product/design-area terminology with the approved visual-editor deferral and Printify terminology.
- Keep Listing-stage color selection, mockup generation, rendering/composition, Printify synchronization, credentials/API calls, Shopify publication, and cross-system option mapping as future work. Document only that a future Shopify adapter must map FusionCanvas color-level mockups explicitly rather than assuming matching labels or identifiers.

This is one cohesive module because fulfillment strategy, catalog normalization, template-color binding, Store Editor setup, and migration share the same Store-scoped data graph, editing surface, persistence transaction, fixtures, and acceptance pass. It ends with a manually configurable and independently verifiable Store foundation; no listing or external-service outcome is included.

The primary workflow is occasional Store administration by a creator, so it remains in the dedicated Store Editor and uses progressive disclosure rather than consuming the daily creative workspace. Verification will combine focused domain invariant tests, application orchestration tests, isolated SQLite migration/round-trip tests, and Avalonia headless tests for the Store Editor's bindings, selection, disabled strategies, drafts, focus, blocked actions, and explanatory states.

## Capabilities

### New Capabilities

- `store-fulfillment-strategy`: Store-scoped strategy selection, availability, persistence, transition behavior, and integration gating.
- `mockup-template-setup`: Placeholder-targeted template configuration, color-level bindings, revisions, lifecycle integrity, and the deliberately empty future asset state.

### Modified Capabilities

- `product-supplier-setup`: Replace the provisional Product/offering/inline-variant/design-area structure and terminology with the Printify-aligned Blueprint catalog, stable Provider Network identity, typed Options and Values, explicit Variants, and concrete Placeholders.
- `store-management`: Rename and extend the focused Store Editor setup surface so it owns fulfillment strategy, Blueprint catalog, and Mockup Template administration using the approved terminology and read-only archived behavior.
- `design-area-target-selection`: Preserve Item target selection behavior while changing its selectable catalog target from the provisional design-area concept to concrete Offering Placeholders.
- `basic-product-workflow`: Present selected Offering Placeholder guidance in the Design tool using the new catalog relationships and terminology.
- `local-sqlite-persistence`: Add an ordered backward-compatible migration and persistence mappings for the normalized catalog, strategy, templates, revisions, and relationship integrity.

## Impact

- Affects Domain catalog and Store types, Application setup contracts/services, Integration SQLite schema and migration logic, and the App Store Editor view model and Avalonia surface.
- Requires replacing or migrating current `StoreProduct`, `FulfillmentOffering`, `ProductVariant`, `VariantOption`, and `DesignArea` responsibilities while preserving existing Item listing/design target relationships.
- Adds normalized Store-scoped catalog and mockup-template tables and advances the workspace schema version; older local databases and workspace-package databases remain migratable through the same ordered chain.
- Adds no external SDK, network dependency, credential storage, renderer, image editor, or marketplace API.
- Requires updates to affected documentation and deterministic Domain, Application, Integration, view-model, and Avalonia headless tests.
