## 1. Presentation Read Models

- [x] 1.1 Add immutable catalog screen projections for Blueprint Offering cards, semantic sellable-Variant rows, Design Area cards, and Mockup Template cards under `FusionCanvas.App/Stores`.
- [x] 1.2 Build Blueprint Offering cards from `BlueprintOfferingSetupSummary`, including fulfillment context, archived/ready/incomplete status, and all three active setup counts.
- [x] 1.3 Build sellable-Variant rows by resolving stable Option Value identities through parent `OfferingOption.OptionKind`, including truthful missing-identity fallback without label inference.
- [x] 1.4 Build Design Area cards from `DesignAreaSetupSummary`, including placement, maximum pixel size, all/subset compatibility summary, and secondary lifecycle action state.
- [x] 1.5 Build Mockup Template cards from `MockupTemplateSetupSummary`, stable target/Color identities, compatible Variant summary, revision, and lifecycle state.
- [x] 1.6 If necessary, extend the Application mockup-template setup read model and mapping with already-stored current revision data; add no persistence field or migration.
- [x] 1.7 Add focused view-model/Application tests for every projection, including fixed Provider, Provider Network, incomplete/ready/archived Offerings, Color-only Variants, Color/Size Variants, unresolved identities, all/subset Design Areas, and archived/revisioned templates.

## 2. Blueprint Page and Offering Overview

- [x] 2.1 Load and refresh normalized Blueprint Offering-card summaries for the selected Store and Blueprint without replacing existing stable route identities.
- [x] 2.2 Add explicit same-page Blueprint Basic disclosure state while preserving existing Blueprint draft, validation, save, archive/delete, and discard behavior.
- [x] 2.3 Recompose the Blueprint detail AXAML so compact identity and Basic disclosure precede the dominant Offering collection and no separate Blueprint editor/window is introduced.
- [x] 2.4 Bind Offering cards to normalized fulfillment, lifecycle/readiness, Variant, Design Area, and Mockup Template summaries; retain one keyboard-accessible open interaction and one Blueprint-scoped add route.
- [x] 2.5 Add the Offering lifecycle/readiness status to the overview heading and keep one primary owner for saving the Basics draft.
- [x] 2.6 Preserve one consolidated Setup region with Variant, Design Area, and Mockup Template counts/routes and keep provider-network and external identifier content secondary.
- [x] 2.7 Refresh Offering-card and overview summaries after save, archive, or return from focused management, retaining Store/Blueprint/Offering context and meaningful focus.
- [x] 2.8 Add view-model and headless tests for Basic disclosure, list dominance/order, empty Blueprint Offerings, archived read-only state, normalized card content, overview status, single save owner, setup routes, blocked prerequisites, and focus return.

## 3. Variant Management

- [x] 3.1 Add explicit Option Value management disclosure state scoped to one selected Option, with open/close commands, draft reset, command notification, and focus-return signaling.
- [x] 3.2 Add explicit bulk Variant draft state and a shared transition helper that makes individual and bulk Variant drafts mutually exclusive and clears stale input/preview state.
- [x] 3.3 Recompose Available choices as compact Option-kind groups with on-demand value management while retaining provider-catalog unavailable guidance and confirmed local values.
- [x] 3.4 Recompose Sellable Variants with count, compact peer individual/bulk actions, and structured semantic Variant rows; omit provider SKU/availability when no authoritative descriptor supplies them.
- [x] 3.5 Reveal only the invoked individual or color-plus-valid-sizes bulk draft within the Sellable Variants region and preserve existing preview, atomic confirmation, duplicate-skip, and invalid-combination behavior.
- [x] 3.6 Keep archive/dependency actions secondary to Variant identity and preserve draft-discard behavior when selection or navigation changes.
- [x] 3.7 Add view-model tests for Option-kind grouping, Option Value disclosure, individual/bulk exclusivity, cancellation/reset, provider-unavailable state, and structured Variant projections.
- [x] 3.8 Add headless tests proving Available choices precede Sellable Variants, editors are hidden until invoked, only one Variant draft is visible, semantic values are readable, and cancel returns to compact summaries.

## 4. Design Area Management

- [x] 4.1 Bind the Design Area collection to normalized summary cards showing name, placement, maximum pixel dimensions, compatibility, Edit/open, and a secondary archive action.
- [x] 4.2 Recompose the selected/new Design Area editor into Identity, Maximum design size, Recommended artwork, Compatibility, Advanced provider data, and Save/Cancel groups.
- [x] 4.3 Keep pixels primary, place reliable inches/millimetres immediately second, and show an explicit unavailable physical-size state when DPI metadata is insufficient.
- [x] 4.4 Preserve all-active-Variants as the default concise compatibility state and reveal concrete compatible Variant choices only in subset mode.
- [x] 4.5 Preserve existing validation, same-Offering compatibility, draft guards, archive confirmation, and dependent-template safeguards.
- [x] 4.6 Add view-model and headless tests for card summaries, master-detail peers, editor group hierarchy, pixels-first presentation, advisory artwork separation, physical-size fallback, all/subset disclosure, and secondary lifecycle actions.

## 5. Mockup Template Management

- [x] 5.1 Bind the Mockup Template collection to normalized summary cards showing name, target Design Area, Color/derived Variant applicability, revision, lifecycle state, Edit/open, and secondary archive action.
- [x] 5.2 Recompose the selected/new template editor into a prominent provider-image/placement region and a supporting configuration region at normal desktop width.
- [x] 5.3 Show a truthful empty/unavailable preview state when Manual strategy or unavailable provider-catalog data supplies no mockup image; add no upload or fabricated-image behavior.
- [x] 5.4 Preserve the existing `MockupPlacementEditor`, keyboard interaction, numeric X/Y/width/height synchronization, bounds validation, target Design Area compatibility, and color-level applicability.
- [x] 5.5 Keep provider mockup reference in Advanced disclosure and preserve revision creation, draft guards, archived read-only behavior, and the no-Design-Area blocked route.
- [x] 5.6 Add view-model and headless tests for complete cards, master-detail peers, preview/configuration prominence, unavailable image state, visual/numeric mapping retention, blocked prerequisites, compatibility errors, and archived read-only state.

## 6. Verification and Delivery Evidence

- [x] 6.1 Run focused Application and App test projects after each screen group and correct implementation or approved artifacts for every failed acceptance criterion.
- [x] 6.2 Run `dotnet test .\FusionCanvas.sln` and record the deterministic baseline result, distinguishing any proven unrelated baseline defect.
- [x] 6.3 Run `openspec validate align-catalog-management-with-description-designs --strict` and correct every validation error.
- [x] 6.4 Review the changed scope for domain, persistence, provider-identity, lifecycle, accessibility, keyboard/focus, and OpenSpec drift against the authority hierarchy and non-goals.
- [x] 6.5 Complete `verification.md` by mapping every scenario to its result and concrete test/command evidence, including limitations and any optional desktop observations.
