## Context

Issue #185 established the normalized catalog model and then separated the former giant setup form into a Blueprint Offering list, Offering overview, Variant management, Design Area management, and Mockup Template management. The current Avalonia implementation contains those routes and most required behavior, but it often binds raw entities or legacy `FulfillmentOfferingSummary` data directly. As a result, the screens expose long forms, name-only rows, incomplete summaries, and always-visible drafts even though normalized application summaries already contain most of the needed information.

This change is presentation-focused. It retains `Blueprint`, `BlueprintOffering`, typed Options and Option Values, explicit `OfferingVariant` identities, `OfferingPlaceholder` Design Areas, Mockup Templates, color-level applicability, revision behavior, and all existing lifecycle and compatibility policies.

### Authority hierarchy

Implementation and review SHALL use this order when sources differ:

1. Existing domain identities and invariants.
2. Accepted OpenSpec behavioral requirements, including the completed `support-printify-store-catalog-mockup-setup` and `simplify-catalog-offering-setup-ux` delivery packages.
3. This change's behavioral requirements.
4. Semantic `.ui.yaml` descriptions as illustrative information-hierarchy references.
5. Low-fidelity wireframes as illustrative workflow references.
6. Detailed labels, styling, exact placement, colors, button text, and geometry as implementation decisions unless a requirement makes them behaviorally significant.

The SVGs and wireframes are not pixel-layout contracts. Their value is the ordering of information, progressive disclosure, scannable summaries, focused editors, and recognizable user journeys.

### UX preflight

- **User and objective:** A creator or store administrator configures one Blueprint and its fulfillment Offerings, then completes the Offering's sellable Variants, printable Design Areas, and Mockup Templates.
- **Frequency:** Opening and scanning Offerings and setup completeness is relatively frequent during catalog setup. Editing Blueprint Basics, Option Values, provider references, and archive actions is occasional.
- **Surface ownership:** All work remains in the existing focused Store Management editor. Blueprint Basics stays on the Blueprint page; no separate Blueprint window is introduced. Offering relationship editing remains in the three focused Offering-scoped management surfaces.
- **Workspace footprint:** The Blueprint page gives most space to Offerings. Design Area and Mockup screens retain peer collection/editor regions. Variant management uses sequential Available choices and Sellable Variants regions. Long data remains reachable through the existing scrollable editor without permanently expanding occasional forms.
- **Progressive disclosure:** Blueprint Basics, Option Value management, individual Variant creation, bulk Variant creation, compatibility subsets, and advanced provider identifiers are disclosed only when invoked or selected.
- **States:** Empty Blueprint Offerings, incomplete setup, archived/read-only Stores, unavailable provider catalogs, no Design Areas, selected/new drafts, validation errors, and dependency-blocked archive actions remain explicit.
- **Selection and focus:** Opening a row retains Store/Blueprint/Offering context. Starting a draft focuses its first required control. Completing or cancelling returns focus to the invoking action or current row. Returning from a focused setup screen refreshes the Offering summary.
- **Drafts and destructive actions:** Existing meaningful-draft guards remain authoritative. Cancellation does not persist partial records. Archive and destructive controls stay secondary and retain confirmations and dependency checks.

## Goals / Non-Goals

**Goals:**

- Make the Blueprint Offering list the dominant Blueprint-page task while retaining Blueprint editing as a compact same-page Basic section.
- Make all five catalog screens reflect normalized catalog identities and setup summaries rather than weak legacy/name-only presentation.
- Preserve the general composition and action economy of the approved wireframes without prescribing pixel-perfect layouts.
- Make editing states explicit and on demand, with predictable cancellation, focus return, and read-only behavior.
- Keep implementation within existing Application and App responsibilities and verify framework-sensitive behavior headlessly.

**Non-Goals:**

- A separate Blueprint window, dialog, or navigation destination.
- New domain concepts, database columns, schema migrations, or compatibility semantics.
- Printify API calls, credentials, provider-catalog synchronization, or fabricated provider SKU/availability values.
- Image upload/import, rendering, artwork composition, listing selection, Shopify publication, or marketplace behavior.
- Per-size mockup overrides or changes to color-level template applicability.
- Pixel-perfect reproduction of semantic SVGs or wireframes.
- Refactoring unrelated Store Management screens or the broader application shell.

## Decisions

### 1. Keep Blueprint Basics on the Blueprint page, subordinate to Offerings

The Blueprint detail route remains the owner of Blueprint identity and editing. Its editable fields move into a concise Basic disclosure above the Offering collection. The current Blueprint remains visible while the section is collapsed, and the Offering list follows immediately as the dominant content.

This avoids an extra window and preserves the user's current Store Management context. A permanently expanded form was rejected because it pushes the frequent Offering task below occasional Blueprint fields. A separate Blueprint editor was rejected by explicit product decision.

### 2. Build immutable screen projections from normalized application state

The App layer will expose small immutable presentation records, preferably in a focused file such as `CatalogPresentationModels.cs`, rather than putting formatting and identity joins into AXAML. Expected projections are:

- `BlueprintOfferingCardViewModel`: stable Offering identity, name, fulfillment display, provider-network flag, lifecycle/readiness label, and active Variant/Design Area/Mockup Template counts.
- `SellableVariantRowViewModel`: stable Variant identity/name plus resolved Color, Size, and Other value summaries based on each `OfferingOption.OptionKind` and stable Option Value IDs.
- `DesignAreaCardViewModel`: stable Design Area identity, name, placement, maximum pixel dimensions, compatibility summary, and secondary archive availability.
- `MockupTemplateCardViewModel`: stable template identity, name, target Design Area, applicable Color/derived Variant summary, current revision, and lifecycle label.

`IOfferingManagementService.LoadForBlueprintAsync` and `OfferingManagementState` remain authoritative inputs. `DesignAreaSetupSummary` and `MockupTemplateSetupSummary` are preferred over raw entities for screen summaries. If current revision is absent from `MockupTemplateSetupSummary`, extend that application read model from existing persisted revision data; do not add persistence fields. If an archived record is intentionally excluded from an active collection, do not invent an active status for it.

Presentation strings are derived deterministically. Offering readiness is `Archived` when archived, `Ready` when all three setup counts are complete, and `Setup incomplete` otherwise. Variant semantics come only from `OptionKind`; editable Option names never determine Color or Size. Provider SKU and availability are omitted unless an authoritative provider descriptor supplies them.

Binding raw catalog entities directly was rejected because it repeats joins in AXAML and cannot provide truthful setup summaries. Adding UI concerns to Domain was rejected because formatting and disclosure are presentation responsibilities.

### 3. Model disclosure as mutually coherent presentation state

`CatalogSetupViewModel` will gain explicit state for Option Value management and bulk Variant drafting. Existing `IsAddingVariant` remains the individual draft state. Starting one Variant draft closes the other; cancelling or confirming closes only the active draft and resets its temporary input. Selecting `Manage values` identifies one Option and opens its value editor; completion/cancellation restores the compact choice-card view.

Suggested state and command additions:

- `IsManagingOptionValues`, `BeginManageOptionValuesCommand`, and `CloseOptionValueManagementCommand` (or equivalent names using existing `ManageOptionCommand`).
- `IsAddingBulkVariants` and `StartBulkVariantsCommand`; existing preview/confirm/cancel commands operate only while this state is active.
- A single helper that transitions between `None`, `IndividualVariant`, and `BulkVariants`, clears stale draft values, raises dependent properties, and refreshes command enablement.

The UI keeps `Add Variant` and `Bulk add` as compact peer actions in the Sellable Variants header. The selected draft appears below that header and above or after the confirmed rows according to the simplest accessible reading order. Exact button labels and column widths are not normative.

Leaving both draft panels permanently visible was rejected because it undermines scanning and creates competing primary actions. Allowing both drafts simultaneously was rejected because draft-guard and focus behavior would be ambiguous.

### 4. Preserve Available choices before explicit Sellable Variants

The Variant screen retains two vertically ordered regions:

1. Available choices, grouped by stable Option kind and showing compact values plus a manage action.
2. Sellable Variants, showing count, compact individual/bulk actions, and structured rows.

Rows display Color, Size, and Other summaries only when those kinds exist. A product with no Size Option must not receive a blank mandatory Size concept; the layout adapts to available stable kinds. Archived or dependency-blocked actions remain secondary to the row identity.

A grid-like presentation is preferred for desktop scanning, but the behavior requires semantic columns/labels rather than a specific Avalonia control. A `DataGrid` is not mandated; an `ItemsControl` with aligned columns is acceptable if keyboard access, automation identity, and readable empty states are preserved.

### 5. Use summary-first Design Area master-detail composition

The existing Design Area collection/editor Grid remains. Collection rows become cards bound to `DesignAreaCardViewModel` so name, placement, maximum pixels, and compatibility are readable without entering edit mode. Archive remains secondary to Edit/open.

The selected/new editor is regrouped without changing persisted fields:

1. Identity: name, placement, decoration method, optional description.
2. Maximum design size: width/height pixels first; derived inches/millimetres immediately below or adjacent as secondary read-only information.
3. Recommended artwork: advisory dimensions, DPI, format, and background.
4. Compatibility: all active Variants as the default; concrete Variant choices visible only for subset mode.
5. Advanced provider data.
6. One Save/Cancel action row.

This is a visual and presentation-state rearrangement. Existing validation, all-Variant expansion, cross-Offering rejection, and dependency safeguards remain unchanged.

### 6. Use summary-first Mockup Template master-detail with a preview-first editor

The existing template collection/editor Grid remains. Template cards use `MockupTemplateCardViewModel` and include target Design Area, Color/derived Variant summary, revision, and lifecycle state.

Inside the selected/new editor, use two semantic regions:

- **Preview/mapping:** provider image or truthful unavailable state plus the existing `MockupPlacementEditor`.
- **Configuration:** name, image selector, target Design Area, Color applicability, numeric X/Y/width/height, Advanced provider reference, and Save/Cancel.

The preview and configuration are peers at the normal desktop width. They may stack when the usable width cannot support both. The existing two-way numeric/visual mapping and bounds validation are retained. No upload affordance or fabricated image is added for Manual mode.

Replacing `MockupPlacementEditor` with a static preview was rejected because the implemented interaction is behaviorally stronger and already accepted. Moving numeric values into Advanced was rejected because they are supporting technical values required for accessible precise editing.

### 7. Keep navigation, draft guards, and destructive policy with their current owners

`StoreManagementViewModel` continues to own Blueprint/Offering route transitions, Blueprint and Offering drafts, discard prompts, and focus requests. `CatalogSetupViewModel` continues to own normalized Offering setup drafts and commands. Domain/Application services continue to own validation, compatibility, archive, and persistence behavior.

The views do not implement business rules. A blocked prerequisite remains a visible explanation plus safe route. Archived Store state disables mutation without hiding confirmed data. Refreshing after save/archive or returning to the Offering overview rebuilds projections from authoritative state rather than editing projection instances in place.

### 8. Preserve design references without making them runtime assets

The semantic UI descriptions and generated SVGs remain documentation/exploration artifacts. The application does not load them, and the prior low-fidelity wireframes are not copied into the repository. Implementation uses theme resources and existing control styles from `StoreEditorWindow.axaml` and follows `docs/ui-guidelines.md`.

## Implementation Plan

### Affected layers and likely files

- **Domain:** no changes expected.
- **Application:** normally unchanged except a read-model-only addition such as `CurrentRevision` to `MockupTemplateSetupSummary` and its mapping in `OfferingManagementService` if required for truthful template cards. No interfaces or persistence schema change unless an existing read method must return already-stored revision data.
- **App presentation models:** add focused immutable records under `src/FusionCanvas.App/Stores/`, preferably `CatalogPresentationModels.cs`.
- **App view models:** update `CatalogSetupViewModel.cs` to build/refresh projections, manage explicit disclosure state, and expose counts/status/empty-state properties. Update `StoreManagementViewModel.cs` only where Blueprint Basic disclosure, normalized Offering cards, summary refresh, navigation, or focus ownership requires it.
- **Avalonia view:** reorganize catalog sections in `StoreEditorWindow.axaml`; preserve existing automation identifiers where they remain meaningful and add stable identifiers for newly testable regions/actions.
- **Tests:** extend `CatalogSetupViewModelTests.cs` for projection and disclosure logic and `StoreEditorHeadlessTests.cs` for visual-tree order, visibility, read-only state, focus, and master-detail composition. Extend Application tests only if an application summary record changes.

### Sequencing

1. Add projection records and deterministic builders/tests from normalized fixtures.
2. Add Blueprint Offering-card loading and summary refresh without changing routes.
3. Add explicit Option Value/individual/bulk disclosure state and cancellation/focus hooks.
4. Recompose Blueprint and Offering overview AXAML and verify context/read-only behavior.
5. Recompose Variant management and verify ordering, semantic row content, and draft exclusivity.
6. Recompose Design Area cards/editor groups and verify all/subset disclosure.
7. Recompose Mockup Template cards and preview/configuration regions while preserving placement synchronization.
8. Run focused tests, the full solution suite, strict OpenSpec validation, and complete `verification.md` with criterion-level evidence.

### Algorithms and edge cases

- Join a Variant's stable Option Value IDs to active/included values and their parent Option. Group display values by `OptionKind`; unknown/missing identities produce a truthful fallback and never infer from names.
- Derive Design Area compatibility from `DesignAreaSetupSummary`; display `All active Variants` for the common case and a count/subset summary otherwise.
- Resolve template Color names by stable IDs, target Design Area by stable ID, compatible Variant count from the setup summary, and revision from existing template revision state.
- When provider catalog data is unavailable, keep persisted choices and confirmed Variants visible. Show unavailable guidance only where no authoritative choice/image data exists.
- When a selected record is archived or disappears after refresh, select a sensible remaining record or show the existing empty/editor prompt; do not retain a dangling projection.
- Draft-state transitions must clear stale preview results and selection values, raise visibility properties, and re-evaluate commands.
- The archived Store path builds the same projections but exposes no mutation commands or editable controls.

### Compatibility and migration

No database or workspace migration is required. Existing records are re-presented through new projections. Rollback consists of reverting App/Application presentation changes; stored catalog data remains compatible because no schema or domain invariant changes.

### Decisions implementers must not reopen

- Blueprint Basics stays on the Blueprint page; no separate window.
- Available choices appear before Sellable Variants.
- Variant semantics are based on `OptionKind`, not editable names.
- Provider means the actual fulfillment partner; Printify is the catalog/integration source.
- Do not fabricate Provider SKU, availability, images, or external identifiers.
- Design Area pixels remain primary; physical values are secondary; artwork recommendations are advisory.
- Mockup applicability remains color-level and concrete compatible Variants remain derived.
- The existing visual placement editor and numeric synchronization remain.
- Exact wireframe geometry is not normative.

## Planned Verification

Every scenario in this change maps to deterministic evidence as follows. A small optional live desktop review may supplement visual judgment but is not a completion gate.

| Capability / scenarios | Planned verification |
| --- | --- |
| Blueprint Offering List — `User opens a Blueprint with Offerings`, `User opens Blueprint Basics`, `User opens a Blueprint without Offerings`, `User reviews an archived Store` | View-model tests for normalized card content and status/count derivation; headless view tests for Basic disclosure, Offering dominance/order, empty state, same-window behavior, and read-only controls. |
| Progressive disclosure — `User opens the catalog editor`, `User opens a Blueprint`, `User opens a Blueprint Offering`, `User opens a focused management surface` | Existing navigation tests plus headless assertions for region visibility/order, context breadcrumb, and absence of relationship forms on parent screens. |
| Offering overview — `User reviews an Offering overview`, `Offering overview preserves the approved composition`, `User changes a fixed Print Provider`, `User reviews incomplete setup`, `User reviews blocked setup`, `User reviews Provider identity`, `User reviews a Provider-Network Offering`, `User returns from focused management` | View-model tests for status, provider wording, setup summaries, refresh, and provider selection; headless tests for heading status, single primary save owner, consolidated setup routes, blocked guidance, and focus return. |
| Variant management — `User opens Variant management`, `User scans available choices`, `User manages values for one Option`, `User enables provider-catalog choices`, `User scans sellable Variants`, `User starts one Variant draft`, `User starts a bulk Variant draft`, `User creates one sellable Variant` | View-model tests for Option-kind projections, missing-value fallback, draft exclusivity/reset, and command outcomes; headless tests for Available-before-Sellable order, on-demand editor visibility, semantic row content, and compact peer actions. Existing Application tests continue to verify valid/duplicate combinations. |
| Variant lifecycle — `User cancels a Variant draft`, `User closes Option Value management`, `User leaves with unsaved Variant changes`, `User retires a referenced Variant`, `Provider catalog is unavailable` | View-model tests for cancellation/reset and unavailable state; existing navigation/discard and Application archive/dependency tests; headless focus/visibility assertions where framework behavior is material. |
| Design Areas — `User opens Design Area management`, `Design Area management preserves master-detail composition`, `User reviews maximum size and artwork guidance`, `User creates a Design Area for all Variants`, `User limits a Design Area to compatible Variants`, `User reviews a lifecycle action` | View-model tests for card projection and all/subset state; headless tests for peer regions, editor group order, pixels-first presentation, subset disclosure, and secondary archive action. Existing Application tests verify compatibility and dependency behavior. |
| Design Area dimensions — `User reviews maximum design dimensions`, `User reviews recommended artwork guidance`, `Secondary physical dimensions cannot be derived`, `User enters invalid maximum dimensions` | Focused view-model tests for physical summary and unavailable fallback; headless hierarchy assertions; existing Application/domain validation tests for non-positive dimensions. |
| Mockup Templates — `User opens Mockup Template management`, `Mockup Template management preserves master-detail composition`, `Provider image is unavailable`, `User creates a template from a provider-catalog image`, `Target Design Area is incompatible`, `Offering has no Design Areas` | View-model tests for card projection, revision/lifecycle, stable Color/Design Area resolution, and unavailable image state; headless tests for peer collection/editor and preview/configuration regions; existing Application/domain tests for target compatibility and blocked prerequisites. |

Completion also requires `dotnet test .\FusionCanvas.sln` and `openspec validate align-catalog-management-with-description-designs --strict` to pass, with any unrelated baseline defect called out explicitly rather than hidden.

## Risks / Trade-offs

- **[Risk] `StoreManagementViewModel` and `CatalogSetupViewModel` currently split legacy and normalized state** → Build projections from one normalized refresh path and avoid parallel mutable copies; add tests around save/return refresh.
- **[Risk] `StoreEditorWindow.axaml` is already large** → Keep behavior in view models, use focused presentation records, and limit the AXAML change to existing catalog regions; do not refactor unrelated tabs.
- **[Risk] Structured Variant rows become awkward for products without Color or Size** → Generate semantic columns/summaries only for Option kinds actually present and test Color-only and Color/Size fixtures.
- **[Risk] More summary joins could expose stale identities** → Rebuild from authoritative state after mutations and display truthful unresolved fallbacks while preserving stable IDs.
- **[Risk] Visual prominence is difficult to prove with unit tests** → Verify semantic region order, visibility, grouping, automation IDs, and size relationships headlessly; reserve optional desktop review for qualitative confirmation.
- **[Trade-off] One scrollable Store editor remains a dense host** → Focused routes and on-demand drafts control density without introducing additional windows or navigation complexity.

## Migration Plan

No data migration or staged deployment is needed. Implement presentation changes behind the existing Store Management routes, run deterministic regression tests, and verify existing workspaces load unchanged. Reverting the code restores the previous composition without transforming stored data.

## Open Questions

None. The Blueprint placement decision and all high-impact behavior, data, and integration boundaries are resolved for implementation.
