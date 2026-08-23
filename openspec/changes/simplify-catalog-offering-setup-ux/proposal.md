## Why

Blueprint Offering setup currently concentrates too many catalog relationships in one dense surface, making the sequence from offering identity to sellable variants, printable regions, and mockup templates difficult to understand. This change turns the approved Issue #185 UX direction into a focused, progressively disclosed Store-administration workflow while preserving the existing catalog model and invariants.

## What Changes

- Provide a Blueprint-scoped Offering list with clear routes to add or open one offering.
- Replace the all-in-one offering form with a concise Offering overview that owns Basics and summary/status information and routes to focused Variant, Design Area, and Mockup Template management surfaces.
- Separate provider-catalog Option/Value availability from explicit sellable Variants, including a bulk path that adds every valid size for a newly enabled color without creating invalid combinations.
- Provide focused Design Area management for actual printable regions, with all-variant compatibility as the common case, pixel-first maximum dimensions, secondary physical dimensions, recommended artwork guidance, and advanced provider reference data.
- Provide focused Mockup Template management that links a provider mockup image, one selected Design Area, applicable Variants, and an image-space placement mapping editable visually with X/Y/width/height technical values.
- Use “Provider” only for the actual fulfillment partner, such as SwiftPOD or Monster Digital; Printify remains the integration and catalog source.
- Preserve explicit drafts, guarded transitions, empty/blocked/error states, Store isolation, archived/read-only behavior, and catalog lifecycle safeguards across the focused surfaces.
- Treat the approved wireframes as behavioral references for broad screen composition: major regions, their order, grouping, relative prominence, and list-versus-editor relationships SHALL remain recognizable. Exact geometry, dimensions, labels, styling, colors, spacing, and button text remain implementation decisions unless behaviorally essential.
- Allow the fixed Print Provider assigned to an existing Blueprint Offering to be changed from Offering Basics, using active Store-owned Provider identities and an adjacent route to create a Provider when needed.
- Exclude artwork selection by color or listing, rendering/composition execution, source-image upload, Shopify/listing publication, and unrelated integration expansion.

This is one cohesive delivery module because all five surfaces form one independently reviewable outcome: a user can configure one Blueprint Offering without confronting the entire catalog graph at once. They share the same Offering context, navigation model, draft safeguards, terminology, and deterministic headless verification surface.

## Capabilities

### New Capabilities

- `blueprint-offering-list`: Blueprint-scoped offering discovery, summary, creation entry, and opening behavior.
- `variant-management`: Focused management of provider-catalog choices and explicit sellable Variant combinations, including the color-plus-all-valid-sizes bulk workflow.
- `design-area-management`: Focused management of offering printable regions, compatibility, dimensions, artwork guidance, and advanced provider references.
- `mockup-template-management`: Focused management of provider mockup images, Design Area relationships, applicable Variants, and visual image-space mappings.

### Modified Capabilities

- `product-supplier-setup`: Replace the dense Offering detail editor with a concise Offering overview that routes to focused sub-editors while retaining the existing Blueprint Offering domain model and lifecycle behavior.

## Impact

- Primarily affects Store Editor catalog navigation, offering-oriented presentation state, and focused Avalonia management views.
- Reuses the authoritative `Blueprint`, `BlueprintOffering`, `OfferingOption`, `OfferingOptionValue`, `OfferingVariant`, `OfferingPlaceholder`/Design Area, and `MockupTemplate` identities and relationships.
- Requires application-facing orchestration for bulk valid-Variant creation and for saving image-space template mapping data through domain-approved contracts; any missing persistent mapping fields must be introduced as an extension of the existing template/revision model rather than a parallel catalog model.
- Requires deterministic ViewModel and Avalonia headless coverage for navigation, selection, focus, progressive disclosure, draft guards, bulk selection, validation, and read-only states.
- Depends on the Issue #185 normalized catalog and mockup setup model; implementation must reconcile with that change’s final accepted form before applying this UX change.
- No external service, credential, network, upload, rendering, or publication dependency is introduced.

Primary users are creators configuring Store catalog data occasionally rather than performing daily creative work. The workflow remains in the dedicated Store Editor, consumes no permanent main-workspace area, and moves complex relationship editing into focused surfaces. No unresolved high-impact product decision remains; detailed visual composition is intentionally delegated as an implementation choice within the behavioral constraints above.
