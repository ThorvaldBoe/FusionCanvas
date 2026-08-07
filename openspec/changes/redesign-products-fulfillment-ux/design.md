## Context

The current `StoreEditorWindow.axaml` presents the Products & fulfillment tab as a 180/180/star three-column layout. Products and offerings are listed in separate columns, while the right column simultaneously exposes Product editor, Offering editor, Variants, and Printable areas. `StoreManagementViewModel` already owns selection, draft, save, discard, delete, variant, and design-area commands; application services and domain records already enforce store scoping and relationships.

The user is a creator configuring reusable products and fulfillment targets occasionally, while the primary workspace is for daily creative work. The focused Store Editor may therefore provide a denser management surface, but it must reveal only the current level's controls and keep the hierarchy understandable.

## Goals / Non-Goals

**Goals:**

- Make the hierarchy and next action obvious through Products overview → Product detail → Fulfillment offering detail.
- Keep one clear primary creation action at each level.
- Preserve existing service contracts, persistence, store isolation, validation, confirmations, and unsaved-change safeguards.
- Make Choice-network behavior and dependent variant applicability understandable.
- Provide deterministic keyboard/focus and empty/error states.

**Non-Goals:**

- No domain, SQLite schema, application-service, marketplace, or fulfillment-integration changes.
- No new catalog relationships or additional product option types beyond current Color and Size inputs.
- No redesign of the main workspace or Item Design-stage targeting surface.

## Decisions

### Focused navigation state over simultaneous panes

Use a small view state in `StoreManagementViewModel` (overview, product detail, offering detail) and explicit navigation commands. This keeps existing commands as the mutation boundary and avoids duplicating catalog state in nested view models. A replacement nested window or separate dialog per level was rejected because it would make the existing unsaved-change guard and selection synchronization harder to preserve.

### Summary-first cards and disclosed sections

Product and offering rows/cards show identity plus derived counts. Product detail shows compact Product fields and offerings. Offering detail shows Basics first, then collapsed/expandable Variants, Printable areas, and Advanced sections. Add forms are inline or contained within their section and are hidden until invoked. This reduces visual load while keeping collections reachable.

### Explicit record terminology

The UI will use Product, fulfillment offering, variant, and printable area consistently. “Combination” is not introduced because it obscures whether the user is creating an offering or an option combination. Generic Add/Remove labels are replaced with record-specific actions; backend enum names remain unchanged.

### Preserve mutation and guard ownership

Existing `IProductSupplierSetupService` calls remain responsible for validation and atomic persistence. Navigation methods call the existing discard prompt before changing selection or level. No mutation is performed merely by expanding a section or navigating. After a successful mutation, the existing authoritative refresh path remains responsible for selecting the resulting record.

### Accessible, keyboard-reachable disclosure

Disclosure controls will be named buttons or expander headers with visible state. Tab order follows breadcrumb/back, summary, section headers, fields, and actions. Starting a new Product continues to focus the Product name field; opening an add form focuses its first labeled field.

## Risks / Trade-offs

- [Risk] A level-based surface can hide useful context → Mitigation: persistent breadcrumb, selected-record summary, and count badges.
- [Risk] New navigation state may accidentally bypass existing discard prompts → Mitigation: centralize transitions through guarded navigation methods and add headless tests for every transition type.
- [Risk] Derived counts may become stale after mutations → Mitigation: derive counts from the refreshed `ProductSupplierSetupState`; do not maintain independent counters.
- [Risk] Accordion sections may make keyboard discovery slower → Mitigation: keep section headers in tab order, expose expanded state, and focus the first field after expansion.
- [Risk] Choice warnings may be detached from the area they explain → Mitigation: show the warning in the offering summary and adjacent to Printable areas when applicable.

## Migration Plan

No data migration is required. Replace the Products & fulfillment view composition while retaining existing bindings and commands where possible. Existing stored catalogs render in the overview and can be opened at each detail level. Rollback is a presentation-only revert if needed.

## Open Questions

None blocking. Product basic fields should remain visible in a compact Product details card; external identifiers belong in Advanced. The exact visual control (Avalonia `Expander` versus equivalent visibility state) may follow existing project conventions without changing behavior.

## Implementation Plan

1. Add catalog-editor navigation state and derived summary projections to `StoreManagementViewModel`, including guarded Back/open-product/open-offering transitions and section expansion state. Reuse `EditorProducts`, `SelectedProduct`, `SelectedOffering`, `OfferingVariants`, and `OfferingDesignAreas` as authoritative sources.
2. Refactor `StoreEditorWindow.axaml` Products & fulfillment content into overview, Product detail, and offering detail layouts with breadcrumb/back controls, explicit labels, empty states, summary counts, and one primary action per level.
3. Move Product fields into a compact Product details section and move offering fields into Basics/Advanced. Keep Choice conditional provider visibility and warning behavior unchanged.
4. Replace always-visible variant/area forms with disclosed Add variant and Add printable area forms. Use labeled fields and explicit removal labels while preserving existing commands and applicable-variant selection semantics.
5. Add or update view-model tests for navigation, derived summaries, guarded transitions, draft cancellation, and post-mutation selection. Add Avalonia headless tests for initial/empty/populated states, disclosure visibility, Choice conditions, focus, keyboard-reachable actions, and destructive confirmation surfaces.
6. Run focused tests, `openspec validate`, and the full `dotnet test .\\FusionCanvas.sln` baseline; review the changed scope for accidental domain/persistence/API changes.

## Acceptance-to-Verification Mapping

| Acceptance area | Planned evidence |
|---|---|
| Overview and one primary action | Headless view tests for initial, empty, and populated overview |
| Product and offering navigation | View-model tests plus headless breadcrumb/back/selection tests |
| Terminology and action ownership | Headless visual-tree/control-name assertions |
| Disclosed sections and conditional fields | Headless tests for section visibility, fixed provider, and Choice network |
| Variant/area relationships | Existing application/domain tests plus focused VM tests for add/applicability |
| Unsaved changes and draft cancellation | View-model tests covering Save/Discard/Cancel on every navigation path |
| Destructive actions and empty aftermath | Headless confirmation tests plus existing service/persistence tests |
| Regression safety | `openspec validate` and full solution test baseline |
