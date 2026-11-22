## Why

The Design workflow currently couples artwork variants to shirt colors, so creators either configure every color in advance or interrupt design work to add a rarely used color. Separating artwork alternatives from Listing choices lets creators keep a small favorite set in the normal path while retaining access to every configured provider color when preparing a listing.

## What Changes

- Make Design artwork variants represent alternate artwork files, with optional contrast intent (`light artwork for dark products` or `dark artwork for light products`) and no color assignments.
- Keep the selected Blueprint Offering and fixed Provider in Design because they define the available Design Areas; switching the provider requires a new Design context or manually adapted artwork.
- Move per-Item color and size selection into Listing. Default the first artwork variant to each selected color, with explicit reassignment to another artwork variant.
- Add Offering-scoped favorite Color values for the common Listing path and a focused **More Options** picker for all other configured Colors. Show an approximate solid-color swatch, preview the active artwork on it, and let the creator add multiple Colors to the Item.
- Keep configured sellable Variants for all relevant provider Colors and sizes, including out-of-stock variants. Selecting a Color selects all its configured Sizes; current stock is visible but does not exclude a variant.
- Prompt for missing Mockup Templates when an additional Color is selected. Ready Colors can continue through mockup generation while the missing Color is unresolved; no mockup is fabricated for it.
- Before every Printify publish or republish, remind the creator to check Printify's per-product availability setting because FusionCanvas cannot read or set it through the supported API.
- Migrate existing Item color-to-artwork assignments into Listing selections while preserving their artwork mapping. Do not mutate existing remote Printify products as part of local migration.

## Capabilities

### New Capabilities

- `artwork-variant-management`: Color-independent Design artwork variants, optional contrast intent, and per-Design-Area artwork assignments.
- `listing-variant-selection`: Offering-scoped favorites, Listing-stage Color/Size selection, color preview, and artwork-variant assignment for an Item.

### Modified Capabilities

- `printify-listing-lifecycle`: Build Printify product variants from Listing selections and include a manual out-of-stock availability check before publish actions.
- `printify-listing-import`: Downloaded Printify variant setup populates Listing Color selections and color-independent artwork variants.
- `listing-mockup-generation`: Consume Listing Color selections and their assigned artwork variants, and allow ready Colors to proceed when another selected Color lacks a template.
- `product-supplier-setup`: Retain provider catalog Variants that are out of stock so they remain selectable for future orders.

## Impact

- **Design workflow:** Simplifies artwork management and preserves Offering/provider selection for Design Area guidance.
- **Listing workflow:** Adds the primary Color/Size selector, favorite-first progressive disclosure, artwork assignment, stock indicators, and missing-template guidance.
- **Store settings:** Adds favorite management to the Offering's Color configuration and ensures all provider sellable Variants can be configured independently of current stock.
- **Application/domain:** Replaces color-keyed Design variant rows with color-independent artwork variants and introduces Item-scoped Listing selections and assignments.
- **Persistence:** Requires an idempotent workspace migration that preserves current artwork assets and existing color mappings while moving the authoritative selection to Listing.
- **Printify integration:** Uses catalog responses including out-of-stock Variants; product creation/update includes only explicitly selected Listing variants. Publishing receives a manual acknowledgement step, not a claimed API verification.
- **UX and verification:** The frequent path remains compact in the Listing workspace; the less frequent full Color collection and favorite management use focused/progressive surfaces. Plan focused application tests and Avalonia headless coverage for selection, preview, assignment, focus, and modal state. A real-desktop Appium journey is warranted for the end-to-end add-extra-color, setup-template, and publish-reminder workflow.
- **Dependencies and sequencing:** The active `printify-listing-lifecycle` change currently specifies Design-owned Colors. Its implementation and deterministic verification are complete; only its supplemental supervised showcase-store smoke check remains. This module replaces the Color-ownership assumption and must keep one authority throughout the migration. Do not block local implementation on an external smoke check.
- **Non-goals:** Fabric-texture simulation, automatic Printify Order Routing, provider switching within an existing Design, direct editing of Printify's availability policy, and automatic mutation of already published remote products.
- **Risks:** Legacy color-to-row mappings and generated mockup attribution must survive migration; catalog availability must not be confused with a locally archived Variant; a missing template must not block unrelated ready Colors; Publish acknowledgement must remain clearly manual.
- **Verification approach:** Map every scenario to domain/application, persistence migration, Printify contract, view-model, headless view, or scoped Appium evidence in `verification.md`; run strict OpenSpec validation and `dotnet test .\FusionCanvas.sln` before completion.
