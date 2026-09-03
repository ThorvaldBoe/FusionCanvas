## Why

The Issue #185 catalog screens now expose the required workflows, but several screens still present normalized catalog data as dense forms or weak name-only rows. Aligning them with the approved description designs will make Blueprint Offering setup scannable and task-focused while retaining the authoritative catalog model and implemented behavior.

## What Changes

- Keep Blueprint editing on the Blueprint page as a compact, progressively disclosed **Basic** section; do not introduce a separate Blueprint window or route.
- Make the Blueprint-scoped Offering collection the dominant page content and summarize each Offering from normalized identity, fulfillment context, lifecycle/readiness, and setup counts.
- Refine the Offering overview so identity and lifecycle/readiness are immediately visible, Basics remains concise, and Variants, Design Areas, and Mockup Templates remain one consolidated set of focused setup routes.
- Present Variant management as **Available choices** followed by **Sellable Variants**; reveal Option Value editing, individual Variant drafting, and bulk drafting only when invoked, and show explicit Variants through stable Option-kind projections rather than name-only rows.
- Present Design Areas as scannable summary cards beside one grouped selected-or-new editor, with pixels first, physical measurements second, artwork guidance separate from hard constraints, and subset compatibility disclosed only when selected.
- Present Mockup Templates as informative summary cards beside an editor that gives the provider image and visual placement mapping priority over supporting configuration fields.
- Preserve existing draft guards, archive/read-only behavior, dependency safeguards, normalized identities, color-level template semantics, visual placement behavior, and Provider terminology.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `blueprint-offering-list`: Refine the Blueprint page hierarchy so a compact Basic section coexists with a dominant, normalized Offering list.
- `product-supplier-setup`: Refine the Offering overview's identity, lifecycle/readiness, primary action, and consolidated setup presentation.
- `variant-management`: Require on-demand choice and Variant drafts plus structured sellable-Variant summaries based on stable Option kinds.
- `design-area-management`: Require scannable Design Area summaries and a grouped editor information hierarchy.
- `mockup-template-management`: Require complete template summaries and a preview-first editor composition.

## Impact

- Primarily affects Avalonia presentation state, view models, AXAML composition, and focused App-layer/headless UI tests for Store Management catalog screens.
- May add small immutable screen-specific projections derived from existing normalized catalog identities and setup summaries; it does not add or migrate domain or persistence data.
- Depends on the behavior and model delivered by `support-printify-store-catalog-mockup-setup` and `simplify-catalog-offering-setup-ux`.
- No external API, credential, image-upload, rendering, listing, Shopify publication, or provider-catalog synchronization behavior is added.

## Delivery Scope and Verification

This is one cohesive, independently reviewable UI-alignment module because the five screens form one Offering-setup journey and share the same presentation projections, progressive-disclosure rules, and verification surface. Verification will combine focused view-model tests, Avalonia headless tests for hierarchy, disclosure, selection, and read-only behavior, the full deterministic solution test suite, and strict OpenSpec validation. The semantic UI descriptions and wireframes are illustrative references; exact geometry, colors, labels, and button placement remain implementation decisions unless required by a scenario.

No high-impact product, UX, data, or architecture question remains. The confirmed Blueprint decision is authoritative: Blueprint editing stays in a compact Basic section on the Blueprint page and does not open a separate window.
