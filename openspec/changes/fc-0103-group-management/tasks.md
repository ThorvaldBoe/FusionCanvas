## Revision Status

FC-0103 was revised after user validation of the first UI implementation. Completed domain, application, archive, and focused-editor foundations remain checked. Superseded flat-navigation and dialog-first UI work is replaced by the pending tree-workspace tasks below.

## 1. Existing Group Hierarchy Foundation

- [x] 1.1 Add tested hierarchy helpers that resolve a group's direct parent, effective niche, ancestors, descendants, and effective active/archived visibility with cycle detection.
- [x] 1.2 Tighten topic validation and movement so destinations must exist in the same store, have an active ancestor path, and cannot be the moved group or its descendant.
- [x] 1.3 Update active navigation projection to omit archived entities and complete subtrees hidden by archived ancestors while preserving expand, select, and reveal behavior for visible nodes.

## 2. Revised Domain and Application Behavior

- [x] 2.1 Define group parent references, context, create/update/move requests, summaries, result types, active/archived projections, and `IGroupManagementService` state contracts.
- [x] 2.2 Implement loading and selection scoped to the active workspace/store, including effective niche/path resolution, archived review data, valid move destinations, and empty-context state.
- [x] 2.3 Implement create and update operations with trimmed required names, case-insensitive sibling uniqueness, optional notes/context metadata, stable identity, and single-save persistence.
- [x] 2.4 Implement same-store moves to active niches or groups, including cross-niche moves, current-parent no-ops, cycle/cross-store/archive validation, subtree preservation, and post-move selection.
- [x] 2.5 Implement archive and restore without cascading lifecycle flags, including inherited subtree visibility, sibling-name and ancestor checks, and selection fallback after archive.
- [x] 2.6 Add application tests for minimal and nested creation, sibling-name rules, context edits, active selection, valid moves, rejected destinations, archive/restore combinations, and repository-failure atomicity.
- [x] 2.7 Add parent-scoped sibling ordering to group entities and hierarchy operations, including append, before/after insertion, normalization, archive preservation, and deterministic restoration.
- [x] 2.8 Add a store-scoped default niche and deterministic destination resolver for selected group, future selected item, selected niche, and fallback creation.
- [x] 2.9 Introduce canonical tree selection independent of document tabs and expose selection changes to inspector, creation, and context-aware workflows.
- [x] 2.10 Implement group-subtree copy with new identities and preserved relative hierarchy/metadata, plus application-owned Copy/Cut clipboard intent and paste validation.
- [x] 2.11 Add an extensible `TreeQuery` and canonical hierarchy projection that includes match ancestors, highlight metadata, stable-ID selection, and filtered-reorder safety.
- [x] 2.12 Return confirmed projections and sufficient structural-operation state for UI rollback after persistence failures.
- [x] 2.13 Add domain/application tests for ordering, default-niche lifecycle, destination precedence, canonical selection, copy identities, paste validation, filter ancestry, and rollback results.

## 3. Persistence Revision

- [x] 3.1 Add SQLite integration coverage that round-trips group notes/context, direct parent relationships, archive state, nested descendants, and contained listing relationships after create, edit, move, archive, and restore snapshots.
- [x] 3.2 Add a versioned SQLite migration for group sibling order and store default niche, with deterministic alphabetical order backfill for existing groups.
- [x] 3.3 Round-trip ordered create, reparent, before/after reorder, copy, archive/restore position, and default-niche changes atomically.
- [x] 3.4 Add compatibility tests for pre-migration workspaces, current schema round trips, invalid default-niche references, and safe refusal of newer unsupported schemas.

## 4. Niche-Rooted Editable Tree

- [x] 4.1 Replace the flat navigation `ItemsControl` with an Avalonia `TreeView` bound to observable niche/group hierarchy nodes and stable expansion/selection state.
- [x] 4.2 Build a compact node template with hierarchy guides, expanders, semantic entity icons, optional color/status/tag/count/warning slots, hover actions, selected state, inline editor, cut state, and drop adorners.
- [x] 4.3 Implement tree keyboard navigation and shortcut routing for `Ctrl+Shift+N`, F2, Enter, Escape, Shift+Enter, Ctrl+C, Ctrl+X, and Ctrl+V without leaking commands from focused text editors.
- [x] 4.4 Implement non-persisted inline create and rename drafts with unique selected default names, validation retention, retry/cancel behavior, focus restoration, and commit-and-add-another sibling flow.
- [x] 4.5 Add drag/drop initiation, valid/invalid target feedback, onto/before/after placement, auto-expand on hover, application-command routing, busy-state protection, and confirmed-state rollback.
- [x] 4.6 Add free-text search and extensible structured-filter controls above the tree, ancestor-preserving results, match highlighting, filtered-out selection guidance, and restoration of pre-filter expansion.
- [x] 4.7 Add view-model tests for tree projection, inline edit states, shortcut routing, rapid sibling creation, drag/drop placement, clipboard state, filtering, and rollback.

## 5. Inspector, Tabs, and Secondary Properties

- [x] 5.1 Add a reusable right-side inspector driven by canonical tree selection without automatically creating a document tab.
- [x] 5.2 Implement Ctrl-click to open or activate one persistent tab per group or future item while normal click and keyboard selection remain inspector-only.
- [x] 5.3 Reframe the existing focused group editor as Edit Properties for notes/context, appearance metadata when available, accessible move fallback, archive, restore, and protected unsaved changes.
- [x] 5.4 Update creation commands to use selected group/item/niche/default-niche resolution rather than the active document tab.
- [x] 5.5 Preserve reveal, selection, inspector, expansion, and existing tab state coherently after create, rename, move, reorder, copy, archive, restore, filter, and save failure.
- [x] 5.6 Add app tests for normal selection versus Ctrl-click tabs, default-niche fallback, secondary property editing, unchanged tab context, and post-operation coordination.

## 6. Verification

- [x] 6.1 Run the complete solution test suite and fix regressions across domain, application, integration, and Avalonia app tests.
- [ ] 6.2 Manually verify keyboard-only rapid create, rename, cut/copy/paste, selection, inspector, Ctrl-click tabs, archive/restore, filtering, validation retention, and recoverable save failures.
- [ ] 6.3 Manually verify drag/drop onto/before/after targets, invalid-target feedback, persistent ordering after reload, auto-expand, filtered-tree safety, and rollback after simulated persistence failure.
- [x] 6.4 Verify representative deep/wide trees remain responsive and preserve expansion/selection state across refresh and filtering.
- [x] 6.5 Confirm FC-0103 adds no item implementation, item-inclusive copy, permanent deletion, cross-store move, batch editing, saved views, analytics, templates, automation, or production-queue behavior.
