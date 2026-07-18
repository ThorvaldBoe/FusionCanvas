## Context

FC-0103 established the architecture FC-0104 must extend: a niche-rooted editable `WorkspaceTreeViewModel`, canonical tree selection independent of document tabs, an explicit store default niche, `GroupHierarchy` ancestry and effective-visibility helpers, inline create/rename, drag/drop and clipboard structural operations, optimistic rollback, archive-aware projection, and explicit Ctrl-click/open-tab behavior. SQLite schema version 3 persists the default niche and group sibling order. Listings already persist through `WorkspaceSnapshot` with optional niche/group references, draft status, archive state, timestamps, description, metadata, tag links, prompt links, and asset links.

What remains missing is an application-owned listing workflow and listing-specific tree commands. Individual creators capture ideas and select or rename listings frequently; movement and duplication are common organizational operations; optional detail editing is less frequent; archive, restore, and permanent deletion are rare. FC-0105 and FC-0106 retain ownership of workflow/lifecycle status editing and the full document-window Listing Inspector.

## Goals / Non-Goals

**Goals:**

- Provide application services for listing creation, core-detail editing, movement, duplication, archive, restore, deletion, loading, and selection.
- Resolve every listing to an active niche or group using canonical tree context and store default-niche fallback.
- Add fast inline listing capture and rename to the existing editable tree without requiring assets or later-stage data.
- Reuse tree drag/drop, clipboard, selection, focus, filtering, and rollback patterns for listing movement and duplication.
- Preserve listing identity and every related record during movement and reversible lifecycle actions.
- Materialize applicable inherited tags and metadata at creation through the shared context-resolution boundary.
- Keep optional property editing and destructive lifecycle actions in a secondary focused surface.
- Reuse the existing listing model and SQLite schema without adding persisted listing order.

**Non-Goals:**

- Do not create listings directly under a store; store-level creation resolves to a selected topic or valid default niche.
- Do not implement workflow-stage or operational-status editing from FC-0105.
- Do not implement the document-window Listing Inspector, stage-specific creative fields, or asset management from FC-0106 and later changes.
- Do not implement marketplace metadata, publishing, version history, performance data, bulk operations, or listing-to-group conversion.
- Do not permit cross-store movement or duplication because tags, assets, prompts, and inherited context are store-scoped.
- Do not copy asset or prompt relationships when duplicating a listing.
- Do not add manual listing sibling order or before/after listing placement; listings remain alphabetically ordered within their topic.

## Decisions

### 1. Add an application-owned `IListingManagementService`

The service will expose requests, results, summaries, state, creation-destination resolution, and mutation operations over `WorkspaceSnapshot`, following the completed group-management service. Every mutation reloads the latest snapshot, validates the full operation, creates one replacement snapshot, and calls `IWorkspaceRepository.SaveAsync` once. Tree view models remain responsible for drafts and optimistic presentation, not business rules or SQLite access.

Alternative considered: extend `WorkspaceTreeViewModel` to mutate listing records directly. Rejected because it would duplicate validation, bypass atomic persistence, and make rollback depend on UI-specific logic.

### 2. Resolve listing creation to one active niche or group

Listing creation resolves its destination in this order:

1. A selected active group.
2. A selected active listing's containing group, or its niche when ungrouped.
3. A selected active niche.
4. The selected store's valid active default niche.
5. The store's only active niche when that niche can be established as the default by the existing FC-0103 behavior.

If none resolves, creation is blocked with guidance to select a topic, configure a default niche, or create a niche. A listing destination reference accepts only niche or group kinds. Group destinations use `GroupHierarchy.GetEffectiveNiche` and `GroupHierarchy.IsEffectivelyActive` to validate store ownership, ancestry, and visibility. Listings remain topic children; FC-0104 does not broaden the navigation model to store-only items.

Alternative considered: allow a nullable topic and display listings directly under the store. Rejected because FC-0103 deliberately established a niche-rooted tree and default niche for fallback future-item creation, and the current domain/navigation invariants require a niche or group parent.

### 3. Treat the one-line value as the listing's working title or idea line

Inline creation starts a non-persisted listing draft beneath the resolved topic, focuses and selects the required one-line field, and persists only on commit. The value is trimmed, nonblank, and single-line. New listings receive stable identity, new timestamps, and Draft status; optional description/notes and inherited context may be added without requiring assets, prompts, mockups, marketplace fields, or artwork.

Alternative considered: add separate title and idea columns now. FC-0106 and Phase 2 refine those concepts; adding schema prematurely would risk freezing the wrong representation.

### 4. Apply inherited context as explicit creation data

The service requests resolved context for the destination topic from the accepted context-aware boundary. Only tags and metadata marked applicable to new work are copied into listing tag links and metadata, with provenance that distinguishes inherited values from explicit user input. Explicit values win before save. Movement does not recalculate inherited context because restructuring an existing concept must not rewrite it.

Alternative considered: derive inherited values dynamically on every read. Rejected because parent changes and moves would silently alter established listing context.

### 5. Extend the editable tree as the primary listing workspace

`WorkspaceTreeViewModel` remains the canonical selection and structural interaction owner. FC-0104 adds:

- New Listing from the selected topic, selected listing's parent, or store default niche.
- Inline commit/cancel creation with focus retention and optimistic rollback.
- F2/context rename through the listing service.
- Drag/drop onto an active niche or group and cut/paste to an active topic for movement.
- Copy/paste or Duplicate for independent listing variations.
- Normal selection that updates reusable detail context without creating a tab.
- Explicit Ctrl-click or Open in Tab that opens/activates a persistent document tab.

Listings remain alphabetically ordered after creation, rename, move, or duplication. Drops onto topic rows are valid; before/after ordering for listing rows is unavailable because FC-0104 does not add a `SortOrder` field.

Alternative considered: put move and duplication only in a modal manager. Rejected because FC-0103 user feedback established file-explorer-level tree operations as the appropriate pattern for frequent organization.

### 6. Duplicate as an independent draft variation

Duplication creates a new identity and timestamps, copies the working title with a collision-safe `Copy` suffix, description/notes, listing metadata, and tag links, and resets status to Draft. Asset links, prompts, and future version/dependent records remain attached only to the source. Clipboard paste defaults to the selected active destination topic; an explicit Duplicate command defaults to the source topic. Both must remain within the source store.

Alternative considered: copy all related records. Rejected because shared/final asset semantics and version lineage require later feature rules.

### 7. Make archive reversible and permanent deletion guarded

Archiving sets the listing's archive flag and relies on the existing archive-aware tree projection to remove it from normal navigation while preserving placement and relationships. A listing is effectively available only when it is active and its store, niche, group, and complete group ancestry are active. Restore returns it to the preserved topic only when that path is active; otherwise the focused lifecycle surface explains which parent must be restored or allows selection of another active same-store topic.

Permanent deletion remains secondary, requires explicit confirmation, and is blocked while prompts, asset links, or future dependent creative records reference the listing. Successful deletion removes the listing and listing-tag links atomically without deleting reusable tags.

Alternative considered: cascade every connected record. Rejected because a compact management command must not destroy creative history or files.

### 8. Use a secondary focused surface for properties and lifecycle actions

Inline creation, rename, selection, move, and duplication belong to the tree. A selected listing exposes Edit Properties for optional description/notes plus archived review, restore, and permanent deletion. The surface preselects the invoking listing, uses explicit Save for property drafts, detects meaningful unsaved changes before switching or closing, uses content-sized actions, and returns focus to the invoking or replacement tree row.

Alternative considered: permanently show the complete management form beside the tree. Rejected because FC-0106 will own the durable listing inspector and FC-0104 should not consume document workspace with a competing form.

### 9. Keep tree selection and document tabs independent

Successful create, rename, move, duplicate, or restore selects and reveals the listing in the tree but does not automatically open a document tab. Ctrl-click or an explicit Open in Tab command uses the existing tab coordinator. Archiving or deleting a selected listing selects a sensible adjacent active listing when possible, otherwise its active parent topic. A filtered tree retains canonical identity and ancestor context, and structural commands must not target hidden invalid destinations.

Alternative considered: automatically open a tab after creation. Rejected because rapid capture would create tab clutter and contradict FC-0103's accepted navigation/tab contract.

### 10. Reuse optimistic interaction and error-state patterns

The tree may update drafts or structural placement optimistically, but it retains a confirmed projection and restores hierarchy, filter, expansion, selection, and clipboard state after validation or persistence failure. Busy operations prevent duplicate submission. Inline errors preserve text and focus. Escape cancels an uncommitted draft; destructive confirmations are keyboard accessible; cancellation leaves persisted and visible state unchanged.

Alternative considered: reload the entire workspace after every failure. Rejected because it would lose user orientation and provide weaker recovery than FC-0103's established rollback model.

## Risks / Trade-offs

- [Listing placement depends on group ancestry correctness] -> Reuse `GroupHierarchy` rather than implementing a second ancestry walker and add nested/archived ancestor tests.
- [Tree clipboard behavior now supports both groups and listings] -> Keep typed payloads and route each entity kind to its own application service with cross-kind rejection tests.
- [Alphabetical listing order limits before/after drag/drop] -> Accept only topic destinations for listing movement and defer persisted listing order to an explicit future change.
- [Snapshot replacement can overwrite concurrent mutations] -> Reload immediately before mutation and keep operations serialized through application services; optimistic concurrency remains future work.
- [Metadata JSON can become an untyped catch-all] -> Limit FC-0104 keys to documented context/provenance and preserve unknown keys during edits and duplication.
- [Duplicate semantics may evolve with asset/version features] -> Keep asset and prompt copying explicitly excluded and make duplication extensible through request contracts.
- [Archived parent topics can make restoration ambiguous] -> Preserve the original location and block or explicitly retarget restore rather than silently relocating.
- [FC-0103 remains an active completed change until archived] -> Treat its implementation and delta specs as prerequisites and revalidate FC-0104 after FC-0103 specifications are archived into main specs.

## Migration Plan

1. Add listing placement/effective-activity helpers and application contracts that reuse `GroupHierarchy` and default-niche resolution.
2. Implement and test the listing service over existing snapshots and SQLite schema.
3. Extend `WorkspaceTreeViewModel`, `AppWorkspaceFactory`, and main-window coordination with inline listing and typed clipboard operations.
4. Add the secondary property/lifecycle surface and archive/delete workflows.
5. Run domain, application, integration, app, and UI automation suites, then validate the OpenSpec change strictly.

No database migration is expected. Rollback removes listing service and UI wiring; existing listing rows remain readable through the pre-FC-0104 model because topic placement and identity formats do not change.

## Open Questions

None for FC-0104. Store-level creation uses FC-0103's default-niche model, listing ordering remains alphabetical, duplication excludes assets/prompts, permanent deletion is guarded, and listing/group conversion remains out of scope.
