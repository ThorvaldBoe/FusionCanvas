## Context

FC-0103/FC-0104 shipped a filter-ready foundation: `WorkspaceTreeQuery` (text, entity kinds, listing statuses, tag ids) projected by the pure `WorkspaceTreeProjector`, a persistent search box above the tree wired to text-only filtering, parent-context preservation, auto-expansion during filtering with expansion restore on clear, a disabled "Filters" placeholder button, and a text-only guard that blocks group sibling positioning while filtering. Accepted navigation-tree behavior already guarantees parent context for matches. What is missing is the FC-0107 product behavior: remembered-content search (notes, description, tag names), tag and subtree filters, intentional archived inclusion, a clear-all path, and an empty-results state.

The primary workflow is an individual creator narrowing the workspace to refind or focus work many times per session. Text search is the frequent action and stays persistent; tag, scope, and archived controls are occasional and belong in a progressively disclosed flyout per `docs/ux-guidelines.md`. FC-0105 (proposed in parallel) owns the workflow-stage and lifecycle-status facts and their filter selectors; this change must leave the filter surface ready to host those selectors without implementing them. No persistence or schema work is involved: filtering is an in-memory projection over the loaded `WorkspaceSnapshot`.

## Goals / Non-Goals

**Goals:**

- Extend text matching beyond node names to listing description, listing notes metadata, and attached tag names, keeping the current trimmed case-insensitive substring semantics.
- Expose the existing tag-filter dimension in the UI with AND semantics across selected tags.
- Add a current-topic subtree scope derived from canonical selection (selected niche/group, or the selected listing's containing topic).
- Add an explicit include-archived option that reveals archived and effectively archived nodes with inactive treatment without making them active context or structural destinations.
- Replace the disabled Filters button with a working flyout, add an active-filter indication, a clear-all action with expansion restore, and an empty-results state.
- Generalize the structural-positioning guard from text-only to any active filter dimension.
- Keep the projection pure and deterministic in the Application layer with view-model-only filter state in the App layer.

**Non-Goals:**

- Do not implement workflow-stage or lifecycle-status facts or filter selectors (FC-0105); do not conflict with its parallel proposal.
- Do not search asset names, prompts, or FC-020x creative fields (phrases, graphic direction) that do not exist yet.
- Do not add cross-store or cross-workspace search, ranking, relevance scoring, query languages, saved views (FC-0307), or bulk actions from results (FC-0301/FC-0302).
- Do not add tag creation, rename, or assignment UI (FC-0108); the tag picker is a read-only selector over existing active tags.
- Do not change archive/restore lifecycle behavior or the focused lifecycle surface (FC-0103/FC-0104).
- Do not add persistence for filter state or any schema migration; filter state is session-local.

## Decisions

### 1. Extend `WorkspaceTreeQuery` with scope and archived dimensions

Add `ScopeTopic` (`NavigationTopicReference?`) and `IncludeArchived` (`bool`) to the existing Application-layer query record, and extend `IsActive` to cover all dimensions. The projector remains a pure function of `(snapshot, storeId, query)`, which keeps every matching rule unit-testable without UI.

Alternative considered: keep filter state as separate view-model flags merged into the tree build. Rejected because the query record is already the accepted projection input and splitting dimensions across layers would scatter the combination semantics.

### 2. Keep text matching a single trimmed case-insensitive substring across a defined field set

A node matches the text dimension when the query text is a case-insensitive substring of: the node name (all kinds), or for listings additionally the description, the notes value from listing metadata, or any attached tag name. Listing notes are read through one shared Application-level metadata-key helper so the projector and `ListingManagement` cannot drift on the `"notes"` key. Text matching does not tokenize, rank, or stem.

Alternative considered: tokenized AND matching or SQLite FTS. Rejected because the PRD explicitly avoids complex search in Phase 1, the snapshot is already in memory, and substring matching is what creators expect from a remembered-words filter.

### 3. Source the tag picker from the active store's active tags

The flyout lists the active store's non-archived tags alphabetically as a checkable list with an empty state ("No tags yet") that routes attention to future tag management. Selected tag ids feed the existing `WorkspaceTreeQuery.TagIds` dimension, preserving its AND-across-selected-tags semantics. Topics never match the tag dimension directly; they appear only as ancestor context.

Alternative considered: OR semantics across selected tags. Rejected because the existing query contract already defines AND, and AND matches the "narrow to a campaign" workflow; OR can be revisited with saved views.

### 4. Resolve the subtree scope from canonical selection

The scope choice is "Whole store" (default) or "Current topic and below". The current topic resolves from the canonical tree selection: a selected niche or group directly, or a selected listing through `ListingHierarchy.GetTopic`. When scoped, the projection treats the scoped topic as the single visible root and matches within its subtree, so the user still sees one anchored context rather than a flat result list. The choice is disabled when selection cannot resolve a topic, and the flyout labels the scoped topic by name.

Alternative considered: a free topic picker detached from selection. Rejected because it duplicates navigation, invites disagreement between the tree and the filter, and the PRD asks for "the current topic or subtree".

### 5. Add an archive-inclusive tree input in the Domain for the include-archived option

`WorkspaceNavigation.BuildTree` currently drops archived and effectively inactive nodes, so the projector cannot reveal them. Add an archive-inclusive build option that retains archived niches, groups, and listings (and active entities beneath archived ancestors) and marks each node with an `IsInactive` flag derived from the existing `GroupHierarchy`/`ListingHierarchy` effective-activity rules. The projector includes inactive nodes only when `query.IncludeArchived` is set; the default path is unchanged. Inactive rows render with the established inactive styling, never become canonical selection or structural targets, and the FC-0104 lifecycle surface remains the only review/restore path.

Alternative considered: a separate archived-results pane or reusing the lifecycle dialog for archived search. Rejected because it fragments the one-tree mental model and duplicates an intentional surface; a flag on the existing projection keeps behavior local and reversible.

### 6. Replace the placeholder Filters button with a flyout that applies immediately

The Filters button becomes enabled and opens an Avalonia `Flyout` containing: the scope selector, the tag check-list, the include-archived toggle, and a Clear-all footer action. Filters apply immediately as the user changes them (no draft/commit step), the button shows an active-filter indication (count or highlighted class) while any non-text dimension is active, and dismissal via Escape or light-dismiss keeps the applied filters. Controls follow the shared compact sizing, tooltip, and keyboard-flow guidance.

Alternative considered: a modal filter dialog with explicit Apply. Rejected because filtering is a frequent, reversible navigation aid; immediate application matches the existing search box and avoids unsaved-change ceremony.

### 7. Generalize expansion capture and clear-all to the whole query

`WorkspaceTreeViewModel` currently snapshots `_expandedIdsBeforeFilter` only when the text query transitions. Key that behavior on `WorkspaceTreeQuery.IsActive` instead: entering any filtered state captures expansion once, leaving it restores the captured state. Clear-all resets the view model's text, selected tags, scope, and include-archived to an empty query, restores expansion, preserves selection when still visible, and returns focus to the search box.

Alternative considered: per-dimension expansion memory. Rejected as needless complexity; one captured pre-filter state matches the existing accepted behavior.

### 8. Show an explicit empty-results state in the tree area

When the query is active and the projection has no visible roots, the tree area swaps to a centered message stating that nothing matches the current filters with a Clear-filters action, instead of an empty `TreeView`. Clearing from that state returns the full tree. This is presentation state owned by the view model (`HasActiveFilters`, `HasVisibleResults`), keeping the projector free of UI concepts.

Alternative considered: leave the blank tree and rely on the user to edit filters. Rejected because a blank navigation pane reads as data loss; the UX guidelines require a designed empty state.

### 9. Generalize the structural-positioning guard to any active filter

Replace the view model's `!string.IsNullOrWhiteSpace(QueryText)` checks for group sibling positioning with an `IsFiltering` property backed by the full query, keeping the existing actionable error copy ("Clear filtering before positioning..."). Other structural operations already route through confirmed projections and remain unchanged.

Alternative considered: allow sibling positioning against visible order while filtered. Rejected earlier in FC-0103 and still rejected: visible order under filtering is not the persisted order.

### 10. Leave the surface ready for FC-0105's selectors

The flyout is sectioned so FC-0105 can add workflow-stage and lifecycle-status selectors as a new section feeding its own query dimensions. This change does not reserve code for it beyond keeping the filter sections additive and the query record extensible, since FC-0105 is proposed but not yet merged.

## Risks / Trade-offs

- [Listing notes live behind a private metadata key in `ListingManagement`] → Introduce one shared Application-level metadata-key helper used by both the management service and the projector, with a round-trip test pinning the key name.
- [FC-0105 edits the same filter area in parallel] → Keep flyout sections additive and query dimensions independent; whichever change merges second adds its section without reworking the other's; the boundary is recorded in both proposals.
- [Include-archived could blur lifecycle invariants] → Archived rows are inactive-only: never canonical context, never structural destinations, and restore stays in the FC-0104 lifecycle surface; tests cover selection and structural rejection.
- [Scope selection may surprise users] → The scope topic is resolved and pinned from the canonical selection at the moment the user enables "Current topic and below"; the flyout labels the pinned topic by name, the scope choice disables when the current selection cannot resolve a topic, and the user re-pins by toggling off and on with a new selection.
- [Text matching scans metadata and tag names per node] → Phase 1 workspaces are small and the projection already recomputes per keystroke; confirm responsiveness during the desktop verification pass before considering debounce or indexing.
- [AND tag semantics may hide intended results] → Documented in the flyout copy; OR/saved-view semantics remain a deliberate FC-0307-era decision.

## Migration Plan

None. No schema, persistence, or file-format changes; filtering operates on the already-loaded snapshot. Rollback is reverting the code change.

## Open Questions

- Should asset-name search join the text dimension when asset workflows surface assets in navigation? Deferred to the asset-workflow changes (FC-0109/FC-020x).
- Should rejected listings (arriving with FC-0105) participate in the include-archived treatment or remain always visible as FC-0105 proposes? Owned by FC-0105; this change's include-archived option governs archived entities only.
