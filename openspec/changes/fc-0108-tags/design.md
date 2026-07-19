## Context

FusionCanvas already persists store-scoped `Tag` records and `ListingTag` links (`src/FusionCanvas.Domain/Workspace/WorkspaceEntities.cs:147`, `WorkspaceRelationships.cs:40`), round-trips them through SQLite schema version 3 (`SqliteWorkspaceRepository.cs:9`), applies inherited tags on listing creation (`ListingManagement.cs:218`, `ToolContextResolver.cs:361`), and exposes a tag filter on the editable tree query (`WorkspaceTree.cs:9`). What is missing is any user-facing tag workflow: there is no service to create, rename, recolor, archive, restore, or delete tags, no surface to apply or remove tags on a listing, no active tag filter UI, and no color on the `Tag` record.

FC-0108 turns that latent data model into a real workflow. The PRD asks for flexible cross-cutting labels on listings (and topics "where useful"), filter-by-tag, vocabulary refinement over time, and safe removal. Two high-impact scope decisions were resolved with the user before drafting:

- **Topic tagging is deferred.** FC-0108 ships listings-only application. Generalizing `ListingTag` into a generic `EntityTag` and migrating `listing_tags` belongs to a follow-up change. The PRD's "apply tags to topics" requirement is intentionally not satisfied by this change.
- **Color ships in Phase 1.** Tags carry a dedicated color with a compact color picker and colored chips across the tag surfaces.

The implementation remains local-first and Clean-Architecture-aligned. Avalonia owns presentation and gestures; the new `ITagManagementService` owns lifecycle, application, lookup, and filter support; the domain owns `Tag` invariants and the new `Color` field; SQLite owns durable representation and the versioned migration.

## Goals / Non-Goals

**Goals:**

- Provide an application-owned `ITagManagementService` covering tag create, rename, recolor, archive, restore, delete, apply-to-listing, remove-from-listing, lookup, and tag-filter query support, all atomic and reload-safe.
- Add a dedicated **Tags** tab in the existing store management window for occasional tag administration, mirroring the Niches tab pattern.
- Add an inline tag editor on the listing secondary properties surface for frequent apply/remove with autocomplete, create-on-the-fly, colored chips, and keyboard-first add/remove that persists immediately and atomically.
- Activate the existing `TreeQuery.TagIds` filter through a compact tag filter control above the workspace tree that preserves ancestor paths, stable selection, and filtered-out indication.
- Render colored tag chips on listing rows, the editor, the filter control, and the Tags tab.
- Add a dedicated `Color` field on `Tag` and a versioned SQLite migration (3 → 4) with safe backfill and existing compatibility guarantees.
- Reuse optimistic UI, rollback, busy-state protection, unsaved-change handling, compact action sizing, icon tooltips, and keyboard-accessible confirmation from FC-0103/FC-0104.

**Non-Goals:**

- Do not generalize `ListingTag` into a generic `EntityTag`, and do not apply tags to niches, groups, assets, prompts, or future entities. Topic/asset tagging is explicitly deferred to a follow-up change.
- Do not implement tag hierarchy, automation, analytics, marketplace keyword optimization, AI tag suggestions, required tag schemas, saved tag views, or bulk tag operations.
- Do not introduce global (cross-store) tags; tags remain store-scoped.
- Do not change inherited-tag behavior on listing creation or `ToolContextResolver` semantics; FC-0108 only adds the missing management and application surfaces.
- Do not replace the listing properties explicit Save for description/notes; tag apply/remove autosaves separately.

## Decisions

### 1. Resolve the UX preflight: frequency drives placement

Tag actions split by frequency:

- **Frequent** — apply/remove tags on a listing; filter the tree by tag. These belong in the primary workspace: the listing secondary properties surface and a compact filter row above the tree. Tag apply/remove and filter changes persist/project immediately.
- **Occasional** — create, rename, recolor, archive, restore, delete tags. These belong in a focused surface: a new **Tags** tab in the existing store management window, mirroring the Niches tab (active list, archived area, side editor, lifecycle actions, compact button widths). The regular workspace only exposes the tag filter and the per-listing chip editor.

Workspace footprint: the filter consumes one collapsible chip row above the tree; the per-listing editor lives inside the existing secondary surface; the Tags tab adds no persistent rail. Progressive disclosure: the filter starts as a single "Filter by tag" affordance and expands to chips; the Tags tab uses a list-plus-editor side panel. Empty, loading, validation, blocked, cancellation, unsaved, and destructive states are handled per `docs/ux-guidelines.md` and resolved per-surface below.

Alternative considered: put tag CRUD inline in the tree. Rejected because tag vocabulary management is occasional and would consume primary workspace area against the "Protect the Primary Workspace" guideline.

### 2. Add `ITagManagementService` as the single application boundary

The service exposes request/result/summary types, state, and operations over `WorkspaceSnapshot`: create, rename, recolor, update description, archive, restore, delete, apply-to-listing, remove-from-listing, list active/archived, lookup by name, and a tag-filter view for the tree. Every mutation reloads the latest snapshot, validates the full operation, builds one replacement snapshot, and calls `IWorkspaceRepository.SaveAsync` once. View models own only drafts, chips, and optimistic presentation; they never touch SQLite or enforce business rules.

Alternative considered: extend `WorkspaceTreeViewModel` to mutate tags directly. Rejected because it would duplicate validation, bypass atomic persistence, and make rollback depend on UI-specific logic — the same reasoning that drove `IListingManagementService` in FC-0104.

### 3. Tags are store-scoped with case-insensitive active-name uniqueness

A tag belongs to exactly one store. Active tag names are unique within a store using normalized (trimmed, case-insensitive) comparison, matching the niche/store/group precedent. Archived tags do not conflict; restoring an archived tag whose normalized name now matches an active tag is blocked until the conflict is resolved. A tag name may be reused in another store. Names are trimmed, nonblank, single-line. The optional description is freeform single-line. Color is optional.

Alternative considered: global tags shared across stores. Rejected by the PRD open question resolution and the existing domain model; cross-store vocabulary reuse is deferred.

### 4. Add a dedicated nullable `Color` field on `Tag`

`Tag` gains a nullable `string Color` (hex form `#RRGGBB` or `#RGB`, normalized to `#RRGGBB`; `null` means "default accent"). Color is a structured field rather than JSON metadata so queries, sorting, and chip rendering stay simple and the value is validated at the boundary. SQLite stores it in a new nullable `tags.color` column. The existing `MetadataJson` remains for future per-tag extensions (e.g., category, sort hints) and is preserved on rename/recolor.

Alternative considered: store color only inside `MetadataJson`. Rejected because the user chose a real color UI, and a dedicated field keeps round-tripping, validation, and chip binding clean without overloading metadata.

### 5. Tag apply/remove autosaves immediately and atomically

Applying or removing a tag on a listing persists through one `ITagManagementService` operation and one repository save before the chip is committed visually (or optimistically with rollback on failure). This keeps the tag filter, listing chip counts, and inherited-context views consistent with what the user sees. The listing properties surface's existing explicit Save continues to own description/notes only. The two persistence paths are visually separated: chips reflect persisted state with a brief busy indicator; the description/notes Save button is independent.

Alternative considered: queue tag edits behind the description/notes Save. Rejected because the "Find Work by Tag" workflow needs the filter to reflect current tags immediately, and tag edits are structural metadata, not draft text.

### 6. Create-on-the-fly from the listing tag editor

Typing a name that does not match an active tag in the store and pressing Enter creates a new active tag (default color, empty description) AND applies it to the listing in one atomic operation. If the typed normalized name matches an existing active tag, the existing tag is applied instead of duplicating. If it matches an archived tag, the editor offers to restore-and-apply (progressive disclosure) or offers a rename. Escape clears the input without creating anything. This keeps tag vocabulary growth frictionless while preventing duplicate tags.

Alternative considered: require tags to be created only in the Tags tab before they can be applied. Rejected because it would force a context switch during routine listing organization.

### 7. Permanent tag deletion removes all `ListingTag` links atomically after confirmation

Deleting a tag is the removal of a reusable label, so it cascades link cleanup: the operation removes the tag and every `ListingTag` link referencing it in one repository save. The confirmation dialog names the tag, its color, and the count of listings it is applied to, and warns that the tag will be removed from all listings. Listing records themselves are untouched. This resolves the PRD open question ("Should deleting a tag remove it everywhere after confirmation?") to "yes, after confirmation." Reusable tag records are otherwise preserved by listing deletion as already specified by `listing-management`.

Alternative considered: block deletion while any link exists and require manual unlinking. Rejected because vocabulary cleanup would be impractical and the PRD explicitly asks for safe everywhere-removal.

### 8. Archive hides a tag from active surfaces but preserves links

Archiving a tag sets its `IsArchived` flag and removes it from autocomplete, the filter control, and the listing tag editor's available tags, but preserves all existing `ListingTag` links. Listings keep the archived tag applied and render its chip in a muted archived style so historical classification is not silently lost. The Tags tab shows archived tags in a separate archived area with a Restore action. Restoring re-exposes the tag in active surfaces, subject to active-name uniqueness. This mirrors niche/group archive semantics and lets vocabulary evolve without destroying history.

Alternative considered: cascade-remove links on archive. Rejected because archived tags still carry historical meaning and the user may restore them.

### 9. Activate the tree tag filter with AND semantics and ancestor preservation

The compact filter control above the tree lists the active tags of the active store as toggleable chips. Selecting one or more chips builds a `TreeQuery` with `TagIds` set; the existing projection already keeps ancestor paths so matching listings remain locatable. Semantics are AND: a listing must have all selected tags to remain visible. The filter control is keyboard reachable, clears in one action, and restores pre-filter expansion when cleared. Stable entity IDs preserve selection; if the canonical selection is filtered out, the inspector retains it with a clear filtered-out indicator and a reveal/clear-filter action, matching FC-0103's filter model. Only active tags are offered as filters; archived tags are not selectable.

Alternative considered: OR semantics. Rejected because the primary "narrow down related work" workflow benefits from intersection, and OR can be added later if needed.

### 10. Render colored chips in every tag surface

Tags render as colored chips with the tag's color (or the default accent when null) in: listing rows (compact dots or small chips via the existing rich row indicator slots reserved by FC-0103), the listing tag editor, the tree filter control, and the Tags tab. Chips are semantic descriptors (color token + name) mapped by the Avalonia layer, not business rules in row templates. Listing rows cap the visible chip count with a "+N" overflow to protect density.

Alternative considered: text-only tags. Rejected by the user's color decision; color materially improves visual scanning across the tree and filter.

### 11. Reuse optimistic UI, rollback, and shared control guidance

Inline tag create/apply, filter changes, and Tags tab mutations may project optimistically but retain a confirmed snapshot. On save failure they restore the confirmed hierarchy, filter, expansion, selection, and chip state, retain recoverable input where applicable, and show an actionable non-modal error. Busy operations prevent duplicate submission. Icon-only commands expose tooltips. Confirmation and cancellation are keyboard accessible. Action buttons in the Tags tab use compact fixed/content-based widths, not stretched widths. The Tags tab editor detects meaningful unsaved name/color/description changes on switch/close and asks to save, discard, or cancel, matching the niche editor.

Alternative considered: reload the whole workspace after every failure. Rejected because it loses user orientation and provides weaker recovery than the established rollback model.

## Risks / Trade-offs

- [Tag deletion cascades link cleanup across many listings] → Single atomic repository save, confirmation dialog names the affected listing count, and rollback restores the confirmed snapshot on failure.
- [Autosave tag edits beside an explicit Save on the same surface could confuse users] → Visually separate chips (persisted, with busy indicator) from description/notes (draft, Save button); document the split in the properties surface.
- [Create-on-the-fly could spawn accidental duplicate tags] → Normalize names, match against existing active tags before creating, and prefer existing on exact match; Escape clears without creating.
- [Archived tags still applied to listings could confuse filtering] → Archived tags are excluded from the filter control and autocomplete but their chips render in a muted archived style; the Tags tab makes archived state explicit.
- [Color field adds a schema migration] → Versioned 3 → 4 migration with nullable column and empty backfill; safe refusal of newer unsupported schemas remains; compatibility tests cover pre-migration, post-migration, and invalid color round-trips.
- [Many tags per listing could clutter rows] → Cap visible chips per row with a "+N" overflow and keep chip rendering compact.
- [Topic/asset tagging is deferred] → State the deferral explicitly in `proposal.md` and the retrospective so a follow-up change owns the `EntityTag` generalization.

## Migration Plan

1. Add `Color` to the `Tag` domain record, update `WorkspaceSnapshot` normalization, repository mapping, and add the versioned SQLite 3 → 4 migration (`tags.color`, nullable, empty backfill).
2. Implement and test `ITagManagementService` over existing snapshots and the new schema.
3. Add the Tags tab to the store management window with active/archived list, side editor, lifecycle actions, and confirmed deletion.
4. Add the inline tag editor to the listing secondary properties surface with autocomplete, create-on-the-fly, chips, keyboard add/remove, and autosave.
5. Add the tree tag filter control, AND semantics, ancestor preservation, filtered-out indication, and clear-restore behavior.
6. Render colored chips on listing rows, the editor, the filter, and the Tags tab.
7. Run domain, application, integration, app, and UI automation suites; perform the real desktop UI pass with disposable data; run strict OpenSpec validation.

Rollback removes the tag service and UI wiring; the migration is additive (nullable column), so downgraded builds can still read the database. Existing `Tag` and `ListingTag` rows, listing context inheritance, archive behavior, and stable IDs remain intact.

## Open Questions

None blocking FC-0108. Topic and asset tagging, global tags, OR-filter semantics, and tag categories/sorting remain explicitly deferred for follow-up changes.
