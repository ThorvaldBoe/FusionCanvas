# Issue #185 design-to-code exploration

This exploration compares the approved Issue #185 wireframes, the accepted catalog UX requirements, deterministic UI descriptions, and the current Avalonia implementation after commit `8f96b53`. It does not change production behavior.

## Authority and method

The comparison applies this authority order:

1. Existing domain identities and invariants.
2. Accepted OpenSpec behavioral requirements in `simplify-catalog-offering-setup-ux`.
3. The semantic hierarchy expressed by the `.ui.yaml` designs.
4. Wireframe details as illustrative evidence rather than pixel-perfect instructions.
5. Current AXAML as the implementation being evaluated, not the source of intended behavior.

The design language can express hierarchy, semantic controls, relative sizing, representative content, and narrow states. It cannot define scrolling, bindings, command behavior, accessibility, responsive breakpoints, real images, or the interaction mechanics of the placement rectangle. Those remain Avalonia and behavioral-verification concerns.

## Generated design set

| Screen | Semantic source | Rendered states |
| --- | --- | --- |
| Blueprint Offering List | `blueprint-offering-list.ui.yaml` | `default`, `empty-collection` |
| Offering Overview | `offering-overview.ui.yaml` | `default`, `archived-store` |
| Variant Management | `manage-variants.ui.yaml` | `default`, `provider-unavailable` |
| Design Area Management | `manage-design-areas.ui.yaml` | `default`, `empty-collection` |
| Mockup Template Management | `manage-mockup-templates.ui.yaml` | `default`, `empty-collection` |

The expanded designs intentionally use actual fulfillment-partner names such as SwiftPOD and Monster Digital. They do not label Printify as the fixed Provider. The Offering Overview omits the wireframe's Publishing section because listing readiness and publication remain outside this module.

## Screen comparison

### Blueprint Offering List

The design makes the Offering collection the dominant Blueprint-detail task: compact Blueprint identity, one add action, scannable Offering cards, and no relationship editing.

The current view correctly scopes the list to the selected Blueprint, provides one add route, and opens an Offering without a second selector. However, a complete editable Blueprint details form precedes the collection, so the Offering list is visually secondary and may start below the first viewport. Current Offering items show name, fulfillment context, and a partial setup summary, but omit lifecycle status and Mockup Template count even though the accepted requirement calls for status plus relevant setup completeness.

Recommended adjustment:

- Make Blueprint identity compact on this screen and move Blueprint editing behind an explicit secondary action or concise expandable Basics section.
- Render Offering cards from the normalized Offering setup summary so Provider or Provider-Network context, lifecycle state, Variant count, Design Area count, and Mockup Template count come from one authoritative projection.
- Keep the whole card keyboard-openable; an extra literal `Open` button is optional and not behaviorally required.

Priority: high. The current hierarchy and missing summary information materially weaken the screen's intended purpose.

### Offering Overview

The current implementation is close to the semantic design. Basics appears before one consolidated Setup region; Provider is editable by stable Store-owned identity; all three setup routes are grouped with counts; Provider-Network guidance and Advanced identifiers remain secondary.

The main differences are that lifecycle/readiness status is not visible beside the Offering identity, and the primary save action is buried at the bottom of expanded Basics rather than aligned with the overview identity. Basics is expanded by default, so the ToggleButton itself is not a serious divergence.

Recommended adjustment:

- Add explicit Active, Draft/incomplete, or Archived/read-only status beside the Offering heading using the existing setup summary/lifecycle information.
- Consider one primary Save action in the overview header while keeping field-level validation near Basics. Avoid duplicate primary save actions.
- Preserve the current consolidated Setup rows and Advanced disclosure; they already capture the design's essence.

Priority: medium-low. This is refinement rather than a structural rewrite.

### Variant Management

The current screen now preserves the most important top-level order: Available choices first and Sellable Variants second. Color/Size/Other values are grouped in cards and the former inert section toggle is gone.

Three material differences remain:

- The Option Value editor is always expanded whenever any Option exists. That turns the compact Available choices region into a second general-purpose form instead of revealing value management only after `Manage values`.
- Sellable Variants are shown as name-only rows with Archive actions. The design communicates concrete combinations through stable Color and Size columns, a count, and compact row actions.
- The bulk workflow is always rendered as a large nested panel. In the design, Add Variant and Bulk add are compact peer actions, with their detailed drafts revealed only after invocation.

Recommended adjustment:

- Introduce explicit `IsManagingOptionValues` presentation state and show the selected Option Value editor only after a choice card's Manage action.
- Add a presentation-only Variant row projection that resolves each stable Option Value identity into columns grouped by `OptionKind`. Do not infer Color or Size from editable labels.
- Do not invent Provider SKU or availability fields. Show them only when a future provider-catalog descriptor supplies authoritative values; otherwise omit those columns or show a truthful unavailable state.
- Place Add Variant and Bulk add together in the Sellable Variants header and reveal only the active draft below the header/table.

Priority: high. The current screen has the correct regions but not yet the wireframe's scanning and action economy.

### Design Area Management

The current view has the correct two-region master-detail skeleton and keeps one selected/new editor separate from the collection. It also preserves pixel fields, optional physical-size derivation, artwork guidance, compatibility, Advanced provider data, and save/cancel behavior.

The collection remains much denser than the design: each item is a single horizontal line containing Edit, identity, placement, dimensions, and Archive. It does not show compatibility count. The editor presents raw width/height and artwork fields as a long form, while the intended information hierarchy makes maximum pixels the primary production fact, physical dimensions immediately secondary, recommended artwork a distinct advisory group, and the common all-Variants compatibility case concise.

Recommended adjustment:

- Bind the collection to `DesignAreaSetupSummary`-shaped rows and present name, placement, maximum pixel dimensions, compatibility summary, and Edit as a scannable card. Keep Archive secondary or in an overflow/advanced action area.
- Group the editor into Identity, Maximum design size, Recommended artwork, Compatibility, and Advanced provider data sections.
- Keep pixels visually first and place derived inches/millimetres immediately below them. Keep recommendation inputs visually distinct from hard maximums.
- Preserve the all-current-Variants default and reveal concrete Variant selection only when the user chooses a subset.

Priority: high. The data and behavior exist; the remaining issue is information hierarchy.

### Mockup Template Management

The current screen uses a collection-focused parent with Add/Edit opening the same focused dialog. The dialog preserves the hardest behavior: an accessible visual placement editor synchronized with X, Y, width, and height fields. Provider image selection, Design Area selection, Color-level applicability, Advanced provider reference, revision creation, meaningful-draft protection, and blocked-no-Design-Area behavior are also present.

The collection cards expose template name, target Design Area, Color applicability, derived Variant summary, revision, and lifecycle state. The focused dialog gives the provider image and placement rectangle a prominent left region and groups identity, Design Area, Color applicability, numeric mapping, and provider reference beside it without constraining the default collection surface.

Recommended adjustment:

- Render template cards from a setup-summary projection containing name, target Design Area, applicable Colors or derived Variant summary, revision/lifecycle state, and Edit.
- Use an internal editor grid: provider image/placement preview on the left, configuration and numeric mapping on the right. Stack only when available width requires it.
- Preserve the existing `MockupPlacementEditor` and numeric two-way synchronization; this is behaviorally stronger than the static design and should not be replaced.
- Show a clear unavailable/empty provider-image state inside the preview region when Manual strategy has no provider catalog data.

Priority: high. The outer composition is correct, but both summary content and editor prominence differ materially.

## Cross-cutting implementation direction

The recurring issue is not the domain model. The normalized model and application services already contain most required identities and summaries. The presentation layer frequently binds raw entities or legacy `FulfillmentOfferingSummary` values directly, which forces AXAML to produce weak name-only rows.

A follow-up module should prefer small immutable presentation projections:

```text
normalized identities and setup summaries
                  |
                  v
       screen-specific row projections
                  |
                  v
 compact list/table + explicit edit draft
```

The next module can remain UI/application-presentation work. It should not add persistence fields, provider communication, listing publication, uploads, rendering/composition, per-size mockup overrides, or inferred Option semantics.

## Suggested next delivery module

Working name: `align-catalog-management-with-description-designs`.

Outcome: make all five Issue #185 catalog screens preserve the approved information hierarchy and action economy while retaining the implemented domain behavior.

Included work:

- normalized Offering-card summary and compact Blueprint context;
- visible Offering lifecycle/readiness status;
- Variant row projections and on-demand choice/bulk drafts;
- Design Area summary cards and grouped editor hierarchy;
- Mockup Template summary cards and internal preview/configuration split;
- deterministic headless tests for region ordering, summary content, draft disclosure, and responsive peer-region behavior.

Non-goals:

- new catalog persistence or API fields;
- fabricated Provider SKU or availability;
- Printify network calls or credentials;
- image upload, artwork rendering, listing publication, or Shopify behavior;
- pixel-perfect reproduction of the SVGs.

No high-impact domain or architecture decision remains. The only product-level choice worth confirming before a proposal is whether Blueprint editing should move to a separate action or remain as a collapsed Basics section above the Offering list.

## Validation note

All five UI descriptions validate, and all six newly generated default/alternate SVG outputs render successfully through the repository CLI. Strict OpenSpec validation passes for both `prototype-avalonia-ui-description-language` and `simplify-catalog-offering-setup-ux`.

The restored UI-description tooling suite currently reports 26 passing tests and one pre-existing failure on the merged `main` implementation: `Validator_rejects_unknown_state_targets_and_incompatible_overrides`. The test's string replacement no longer matches `TestSupport.MinimalYaml` because the fixture now includes an additional `text: Unavailable` line, so it validates the unchanged source and receives no diagnostics. This is an independent test-fixture defect; no tooling or production code was changed during this exploration.
