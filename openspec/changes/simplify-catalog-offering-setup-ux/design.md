## Context

Issue #185 established a normalized Store-scoped catalog: `Blueprint` → `BlueprintOffering` → typed Options/Values and explicit concrete Variants, plus Offering Placeholders (presented to users as Design Areas) and color-level Mockup Templates with revisions. The current editor exposes much of that graph together. The approved low-fidelity concepts instead organize occasional setup around a Blueprint Offering list, a concise Offering overview, and three focused management journeys.

The workflow belongs in the dedicated Store Editor, not the primary creative workspace. A creator may revisit it when adding a fulfillment partner, enabling new colors or sizes, updating printable regions, or configuring provider mockup views, but it is not a per-listing or per-design workflow.

### Authority hierarchy

1. Existing domain model and invariants are authoritative: Store ownership, Blueprint and Blueprint Offering identity, actual Print Provider versus Provider Network, typed Options/Values, explicit Variants, Design Area/Placeholder compatibility, required template target, color-level template bindings, revision attribution, and lifecycle safeguards.
2. The behavioral requirements in this OpenSpec change govern the redesigned user journeys and any narrowly required model extensions.
3. The approved wireframes are behavioral references for broad composition. Their major regions, order, grouping, relative prominence, and list-versus-editor relationships are authoritative where reflected in the capability specifications.
4. Detailed labels, button text, exact control placement, dimensions, geometry, colors, spacing, and styling are implementation decisions unless a specification makes them essential to behavior. Implementations may adapt columns to available window width, but SHALL preserve the recognizable hierarchy rather than flattening every screen into one undifferentiated vertical form.

The design does not copy wireframe geometry or add the wireframes as repository assets. It supersedes the earlier “future placement extension only” boundary only for revisioned image-space mapping; it does not reopen artwork composition, rendering, upload, or per-concrete-Variant override decisions.

### Implementation baseline audit

- The current persistence baseline is SQLite schema version 11. Catalog tables and compatibility repair are implemented in `SqliteWorkspaceRepository`, `CatalogSetupService`, and `CatalogCompatibilitySynchronizer`.
- Authoritative catalog types are `Blueprint`, `BlueprintOffering`, `PrintProvider`, `OfferingOption`, `OfferingOptionValue`, `OfferingVariant`, and `OfferingPlaceholder`. Mockup types are `MockupTemplate`, `MockupTemplateColorVariant`, `MockupTemplateRevision`, and `MockupTemplateRevisionColor`.
- `MockupTemplateRevision` currently snapshots target Placeholder and colors but has no image reference or placement mapping. `OfferingPlaceholder` currently has no structured provider reference or artwork-guidance projection.
- Store catalog navigation currently has `Overview`, `ProductDetail`, and `OfferingDetail` levels in `StoreManagementViewModel`; focused Variant, Design Area, and Mockup Template levels must be added without label-based fallback.
- Existing Store/Product/Offering draft transitions use `HasAnyCatalogUnsavedChanges`, `RequestDiscardBefore`, `PendingEditorAction`, and the shared discard prompt. Nested normalized forms in `CatalogSetupViewModel` currently cancel locally but are not all represented by the shared transition guard.
- Issue #185's visible “Placeholder” terminology is intentionally refined here: the domain identity remains `OfferingPlaceholder`, while the focused creator-facing surface uses “Design Area” with supplementary text that it corresponds to the provider/Printify Placeholder concept.
- Issue #185's empty future placement state is intentionally advanced only to revision-owned provider-image mapping. Source upload, artwork placement/composition, rendering, and per-concrete-Variant overrides remain excluded.
- Existing template color-level applicability remains authoritative; “applicable Variants” in this change is a derived summary and compatibility check, not a new per-size binding model.

No high-impact ambiguity was found in the baseline. These reconciliations are binding implementation constraints.

## Goals / Non-Goals

**Goals:**

- Make one Blueprint Offering understandable as a sequence: identify the Offering, define provider-supported choices and sellable Variants, define actual printable Design Areas, then configure mockup templates.
- Keep Blueprint detail and Offering overview concise while moving relationship-heavy work into focused Offering-scoped surfaces.
- Preserve stable identities, Store isolation, archive/read-only behavior, dependency safeguards, and draft guards.
- Distinguish actual fulfillment Provider identity from Printify as catalog/integration source.
- Add an efficient, validity-aware color-plus-all-sizes Variant workflow.
- Add pixel-first Design Area production guidance and a revisioned, bidirectional visual/numeric template mapping.
- Make empty, loading, success, blocked, validation, cancellation, unsaved, and destructive states deterministic and testable.

**Non-Goals:**

- Replacing or flattening the normalized catalog model.
- Selecting artwork by color, Design Area, Variant, or listing.
- Persisting per-size or per-concrete-Variant mockup overrides.
- Uploading/importing mockup source images, rendering artwork, compositing, or generating listing mockups.
- Fetching Printify data over the network, adding credentials, or implementing a Printify adapter in this module.
- Shopify or marketplace publication, listing-stage behavior, or cross-system option mapping.
- Pixel-perfect reproduction of the conceptual wireframes.

### Feedback-driven composition refinement

Manual review of the first implementation showed that preserving only navigation and data relationships was insufficient. Generic stacked panels technically separated the concepts but lost the wireframes' clarity. The following broad compositions are therefore binding while exact measurements remain flexible:

- Blueprint detail presents the Offering list as the primary working region; complex Offering editing remains elsewhere.
- Offering overview presents Basics first and one consolidated setup summary containing the three focused routes.
- Variant management presents available choices first as a distinct upper region grouped by semantic Option, then explicit sellable Variants as a distinct lower region with individual and bulk actions.
- Design Area management uses a recognizable master-detail relationship between the Offering's Design Area list and one focused editor.
- Mockup Template management uses a recognizable master-detail relationship between the template list and one focused editor, with the provider image mapping visually prominent.

Controls that only label a region are headings, not buttons or toggles. Any control styled as interactive SHALL perform a meaningful action and expose that state accessibly.

## Decisions

### 1. Use an Offering hub with focused child surfaces

Blueprint detail owns an Offering list. Opening an Offering shows only Basics, fulfillment context, lifecycle/readiness state, setup summaries, and routes to Variant, Design Area, and Mockup Template management. Each route opens one focused surface while retaining Store, Blueprint, and Offering IDs in presentation state.

This follows the shared Store-management pattern and prevents low-frequency setup controls from consuming permanent workspace area. The alternative—collapsible sections in one long form—was rejected because it still makes unrelated relationship editors compete for attention and leaves selection/draft ownership ambiguous.

### 2. Preserve context by identity, not mutable labels

Every transition carries stable Store, Blueprint, Offering, and selected child-record IDs. Back/cancel returns to the same Offering overview and restores focus to the invoking summary action or the nearest surviving record. No focused surface contains a second Offering selector.

The alternative—re-resolving context from display labels or defaulting to the first Offering—was rejected because labels are mutable and silent fallback can edit the wrong catalog graph.

### 3. Treat Provider candidates as input, not a second authoritative catalog

The domain remains authoritative for enabled Option Values and explicit sellable Variants. A read-only provider-catalog candidate set may be supplied to the application layer by an existing or future integration boundary; this module does not implement network access. Candidate combinations identify what the fulfillment partner permits. Confirmed user actions create or activate the existing Offering Options/Values and Variants.

Bulk color-plus-sizes computes:

1. candidate combinations for the selected Offering and Color;
2. intersection with enabled Size Values;
3. removal of invalid and already-existing equivalent Variant combinations;
4. a deterministic preview/result set;
5. one atomic confirmation that creates all remaining concrete Variants or none.

If no trustworthy candidate combinations are available, FusionCanvas does not infer a Cartesian product from Color and Size labels. Manual users can continue individual explicit Variant setup; validity-aware bulk creation remains unavailable until candidate combinations are present.

The alternative—persisting a second global provider-product graph in this UX module—was rejected as scope expansion and a competing source of truth.

### 4. Present Placeholder records as Design Areas without changing identity

User-facing surfaces use “Design Area” because it describes the task. The existing `OfferingPlaceholder` identity and relationship rules remain authoritative. Non-intuitive Printify terminology may appear in helper text where useful, but no duplicate Design Area entity is introduced.

The all-Variants choice is explicit and selected by default for a new Design Area. It persists the Offering's complete currently compatible Variant set under the existing explicit compatibility model. A subset remains available when the printable region truly differs by Variant.

### 5. Extend Design Area metadata narrowly

Maximum pixel width/height remain authoritative structured fields. Secondary inches and millimetres are derived from reliable physical-size or DPI metadata and never replace pixel dimensions. Recommended minimum artwork pixels, format, DPI, and background guidance are provider-specific advisory metadata. A stable provider Design Area reference is added as a nullable structured external identifier because identity is durable and queryable; evolving artwork recommendations belong in metadata or a focused value object serialized through the established catalog record.

Existing records migrate with a null provider reference and absent recommendations. The UI shows “unavailable” for missing secondary guidance rather than inventing values.

The alternative—making physical dimensions authoritative—was rejected because rounding and DPI assumptions can conflict with provider pixel limits.

### 6. Add image-space mapping to Mockup Template revisions

The mapping belongs to `MockupTemplateRevision`, not the mutable template root, because changes affect future use while historic outputs must remain attributable to the exact revision. The revision stores or references:

- the stable provider mockup image reference;
- known source image pixel width and height;
- placement X and Y in image pixels;
- placement width and height in image pixels.

A focused domain value object validates positive dimensions and image-bound containment. The visual rectangle and numeric controls edit one draft value; neither is a secondary source of truth. Saving changed source image, target Design Area, color applicability, or mapping creates a new revision through the existing revision lifecycle.

Applicable concrete Variants remain derived from active color-level template bindings and compatible Offering Variants. The management surface may summarize those Variants, but it does not persist per-size overrides.

The alternative—storing coordinates on `MockupTemplateColorVariant` or one record per concrete Variant—was rejected because it breaks color-level durability and revision attribution.

### 7. Provider images are catalog descriptors, not uploaded assets

The focused editor consumes available provider mockup image descriptors from an application query boundary or persisted provider reference. This change may define the application-facing descriptor/port needed by the UI, but ships no network adapter, credentials, cache downloader, or upload flow. Without descriptors, the editor shows a clear unavailable/empty state and preserves existing templates.

Remote descriptions, labels, and preview locations are untrusted input: presentation treats them as data, validates references, and does not interpret markup or arbitrary file paths.

### 8. Drafts, focus, and lifecycle follow one common interaction policy

New and edited records remain drafts until explicit confirmation. Meaningful drafts guard selection changes, Back, Store/tab changes, and window close with discard/keep-editing choices. Keep-editing preserves selection and focus; discard restores confirmed state. Creation focuses the first required field. Successful creation selects the new record. Successful removal selects a sensible sibling or the empty state and returns focus to a nearby action.

Archive/read-only Store context disables all mutations while preserving navigation and inspection. Destructive actions use existing dependency checks and confirmation policy; focused views do not duplicate domain rules.

The alternative—independent ad hoc draft behavior in each view—was rejected because it causes inconsistent loss of work and difficult headless verification.

### 9. Existing fixed Provider assignment is editable from Basics

For a fixed-provider Offering, Basics exposes the active Store-owned Print Providers as selectable identities and provides an adjacent, explicit route for creating a new Print Provider when the required identity does not yet exist. Saving changes updates the authoritative `BlueprintOffering.PrintProviderId`; it does not store a free-text Provider label on the Offering. Provider-Network Offerings continue to show their stable network identity instead of a fabricated fixed Provider selector.

Rendering the Provider as read-only text after creation was rejected because occasional catalog maintenance includes correcting or changing the fulfillment partner. Editing only a free-text label was rejected because the normalized Provider identity is authoritative and may be shared.

## Risks / Trade-offs

- **[Risk] The active Issue #185 change is not yet archived and may alter the accepted baseline.** → Implement only after reconciling this change against Issue #185's final accepted specs and model; preserve the authority hierarchy and update deltas rather than duplicating records.
- **[Risk] Provider candidate data is absent in Manual mode.** → Keep individual explicit Variant setup available and show bulk creation as unavailable without trustworthy candidates; never infer validity from labels alone.
- **[Risk] Adding revision mapping fields requires a SQLite migration.** → Add nullable/default-compatible fields or a focused revision-mapping table with ordered migration, transactional validation, package compatibility, and rollback tests.
- **[Risk] Visual dragging and numeric editing can diverge.** → Bind both to one validated draft mapping and cover bidirectional updates with ViewModel and Avalonia headless interaction tests.
- **[Risk] “All Variants” becomes ambiguous when Variants change later.** → In this module it is a creation/edit convenience that expands to the explicit current compatible set, matching the authoritative model; the UI reports the resulting count. Dynamic future inclusion is not implied.
- **[Risk] Large Variant sets can make bulk previews and compatibility selectors expensive.** → Use identity sets, deterministic ordering, summary/select-all controls, and virtualized or bounded item presentation where existing Avalonia patterns support it.
- **[Risk] Provider terminology regresses to calling Printify the Provider.** → Centralize fulfillment-context display projection and add wording-focused tests for fixed Provider and Provider Network cases.

## Migration Plan

1. Reconcile with and, if required by workflow ordering, archive/sync the completed Issue #185 change before implementation.
2. Add the narrow Design Area provider-reference and template-revision mapping persistence extension with a new ordered SQLite schema migration.
3. Migrate existing Design Areas with no provider reference or recommendation metadata and existing template revisions with an explicit “mapping not configured” state; do not fabricate provider images or coordinates.
4. Keep existing catalog and template identities unchanged. No record is duplicated solely for the UX redesign.
5. Deploy focused surfaces behind the same Store Editor entry point and replace the dense Offering detail composition only after navigation/draft tests pass.
6. Rollback consists of reverting the application version while preserving additive nullable fields/table data. Migration tests must prove older records remain readable and migration failure leaves the prior schema version and data unchanged.

## Implementation Plan

### Domain

- Extend `FusionCanvas.Domain.Catalog.OfferingPlaceholder` with a nullable stable provider Design Area reference if the final Issue #185 model does not already expose one; preserve ownership, positive-dimension, and compatibility invariants.
- Add a focused mockup image-space mapping value object under `FusionCanvas.Domain.Mockups` and extend `MockupTemplateRevision` (or a one-to-one revision-owned configuration record) with provider image identity, image dimensions, and mapping. Validate positive sizes, non-negative origin, and containment.
- Keep `MockupTemplateColorVariant` color-level; do not add per-size or per-concrete-Variant override structures.
- Add domain tests for mapping bounds, equality/snapshot behavior, stable references, compatibility, and unchanged lifecycle invariants.

### Application

- Split Offering-oriented orchestration into focused query/command contracts rather than enlarging one presentation service: Offering summary, Variant candidates and bulk-create command, Design Area editor state, and Mockup Template editor state.
- Keep `CatalogSetupService` and `MockupTemplateSetupService` as authoritative mutation boundaries or extract focused use cases only where responsibility is already too broad. Domain validation remains below the UI.
- Introduce a read-only provider-catalog descriptor port only if required to supply valid combinations and mockup image descriptors. Provide an unavailable implementation for Manual/no-integration contexts; do not add external SDK or network behavior.
- Implement the bulk algorithm as a deterministic application use case with Store/Offering ownership checks, OptionKind identity checks, candidate validity, duplicate elimination, and atomic persistence.
- Add application tests for fixed Provider wording projection, candidate absence, bulk success/partial exclusion/no-op/cross-Offering rejection, all-Variant expansion, provider metadata, template compatibility, and revision creation.

### Integration and persistence

- Add an ordered SQLite migration for any new nullable provider-reference and revision-mapping storage. Update repository readers/writers and snapshot validation without changing unrelated tables.
- Preserve workspace-package compatibility and use one transaction for migration and each atomic bulk/template save.
- Add integration tests for new database creation, prior-schema migration, populated and empty mapping round-trip, invalid bounds/reference rejection, Store isolation, and rollback on malformed legacy or migration data.

### App and navigation

- Refactor `StoreManagementViewModel`/catalog navigation state so Blueprint detail owns the Offering list and Offering overview owns focused route commands. Do not use mutable labels or fallback selection to resolve context. Fixed Provider changes use Store-owned Provider IDs and the existing catalog mutation boundary.
- Replace the dense Offering composition in `StoreEditorWindow.axaml` with a concise overview and focused child views or view components for Variants, Design Areas, and Mockup Templates. Preserve the wireframes' broad region order, grouping, relative prominence, and master-detail relationships while allowing responsive sizing and repository-native styling. Reuse the dedicated Store Editor window; do not add permanent main-workspace UI.
- Split `CatalogSetupViewModel` into focused Offering/Variant/DesignArea/MockupTemplate presentation responsibilities where practical. Share a small draft-transition coordinator instead of duplicating unsaved-change logic.
- Implement progressive forms, prerequisite guidance, read-only states, selection aftermath, keyboard invocation, initial focus, predictable tab order, and focus restoration.
- Implement visual mapping with an Avalonia control that manipulates one ViewModel draft rectangle; keep numeric controls synchronized and accessible. The control edits configuration only and performs no rendering/composition.
- Add framework-free ViewModel tests and Avalonia headless tests for construction/bindings, route ownership, section absence, selection, keyboard activation, focus, draft guards, bulk choice interaction, visual/numeric mapping synchronization, validation, and archived/read-only behavior.

### Sequencing and decisions not to reopen

1. Finalize Issue #185 baseline and persistence extension.
2. Implement/test domain and application contracts.
3. Implement/test SQLite migration and round-trip.
4. Implement navigation and Offering list/overview.
5. Implement focused Variant and Design Area management.
6. Implement focused Mockup Template management and visual mapping.
7. Run criterion-level verification, strict OpenSpec validation, complete solution build/tests, and scoped QA.

Do not reopen: actual Provider terminology; normalized model ownership; explicit sellable Variants; color-level mockup applicability; one authoritative Design Area target per template; revision ownership; no per-listing artwork selection; no rendering, upload, Shopify, or network integration; and no pixel-perfect layout mandate from the wireframes. Broad screen composition is authoritative after the feedback-driven refinement above.

## Planned Verification

| Capability / exact scenarios | Planned evidence |
| --- | --- |
| `blueprint-offering-list`: `User opens a Blueprint with Offerings`; `User opens a Blueprint without Offerings`; `User reviews an archived Store` | Offering-list ViewModel tests plus Avalonia headless populated, empty, and read-only view tests. |
| `blueprint-offering-list`: `User starts a new Offering`; `User opens an Offering`; `User leaves a meaningful Offering draft` | ViewModel command/context tests and headless keyboard focus/draft-guard tests. |
| `blueprint-offering-list`: `Fixed-provider Offering appears in the list`; `Provider-Network Offering appears in the list` | Application projection tests and headless wording/absence tests. |
| `product-supplier-setup`: `User opens the catalog editor`; `User opens a Blueprint`; `User opens a Blueprint Offering`; `User opens a focused management surface` | Store-management ViewModel tests and Avalonia headless navigation/context/absence tests. |
| `product-supplier-setup`: `User reviews an Offering overview`; `User reviews incomplete setup`; `User reviews blocked setup`; `User reviews Provider identity`; `User reviews a Provider-Network Offering`; `User returns from focused management` | Offering-overview ViewModel tests plus headless summaries, prerequisite routing, wording, warning, and focus-restoration tests. |
| `variant-management`: `User opens Variant management`; `User enables provider-catalog choices`; `User creates one sellable Variant` | Application ownership/validity tests, ViewModel selection tests, and headless focused-surface tests. |
| `variant-management`: `User bulk-adds all valid sizes for a Color`; `Some enabled Sizes are invalid for the Color`; `No new valid combinations remain` | Deterministic bulk-use-case tests for preview, atomic creation, exclusion reporting, duplicate elimination, and no-op behavior. |
| `variant-management`: `User cancels a Variant draft`; `User leaves with unsaved Variant changes`; `User retires a referenced Variant`; `Provider catalog is unavailable` | ViewModel/headless draft and unavailable-state tests plus application lifecycle/dependency tests. |
| `design-area-management`: `User opens Design Area management`; `User creates a Design Area for all Variants`; `User limits a Design Area to compatible Variants` | Application compatibility tests, ViewModel selection tests, and headless list/editor/all-selection tests. |
| `design-area-management`: `User reviews maximum design dimensions`; `User reviews recommended artwork guidance`; `Secondary physical dimensions cannot be derived`; `User enters invalid maximum dimensions` | Domain conversion/validation tests, projection tests, and headless primary/secondary guidance tests. |
| `design-area-management`: `Imported Design Area has a provider reference`; `Manual Design Area has no provider reference` | Domain/application persistence tests and headless Advanced-disclosure tests. |
| `design-area-management`: `User changes selection with unsaved Design Area edits`; `User removes a Design Area targeted by a Mockup Template` | Headless guard/focus tests and application dependency-policy tests. |
| `mockup-template-management`: `User opens Mockup Template management`; `User creates a template from a provider-catalog image`; `Target Design Area is incompatible`; `Offering has no Design Areas` | Application compatibility tests, ViewModel state tests, and headless focused/empty/blocked tests. |
| `mockup-template-management`: `User positions a Design Area visually`; `User edits numeric mapping values`; `Mapping exceeds image bounds`; `User changes confirmed template mapping` | Domain mapping tests, ViewModel bidirectional synchronization tests, Avalonia headless drag/resize/numeric tests, and revision-service tests. |
| `mockup-template-management`: `Imported mockup image has a provider reference`; `Provider reference changes display context` | Persistence round-trip and stable-identity projection tests. |
| `mockup-template-management`: `User cancels a template draft`; `User leaves with unsaved template changes`; `User reviews an archived Store` | ViewModel and Avalonia headless draft, focus, and read-only tests. |

Full completion also requires `dotnet build .\FusionCanvas.sln`, `dotnet test .\FusionCanvas.sln`, strict OpenSpec validation, migration rollback evidence, changed-scope drift review, and scoped architecture/security/persistence/UI QA. Optional live desktop review may assess visual density and drag feel, but cannot replace deterministic gates.

## Open Questions

None. Detailed visual design and exact wording remain implementation choices within the specifications and are not blocking product decisions.
