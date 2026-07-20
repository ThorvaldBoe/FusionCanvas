## Why

The workspace tree now holds stores, niches, nested groups, and listings, but the only working filter is a name-only text query; the "Filters" button above the tree is a disabled placeholder. As workspaces grow, creators cannot refind a listing by remembered idea text, notes, or tag, cannot narrow the tree to a subtree or a tag, and cannot intentionally include archived work in a filtered view. PRD FC-0107 calls for practical Phase 1 search and filtering: narrow the visible workspace by remembered text, tag, or location while preserving the hierarchy that explains where each result lives.

## What Changes

- Expand the existing navigation text query beyond node names: niche and group names keep matching, and listing matches now include the title, description (idea detail), notes, and attached tag names. Store scoping remains the existing store selector; no cross-store search is added.
- Replace the disabled "Filters" placeholder with a compact filter flyout above the tree that hosts occasional-use controls: a tag multi-select over the active store's active tags, a topic-scope selector (whole store vs. current topic and below), and an explicit include-archived option. The always-visible search box remains the primary entry point.
- Wire the existing `WorkspaceTreeQuery.TagIds` dimension into the UI and add a subtree-scope dimension to the query and projection.
- Define combination semantics: filter dimensions combine with AND; the text query matches when any searchable field matches; multiple selected tags require all of them (existing query semantics).
- Keep archived topics and listings excluded from filtered results by default; the include-archived option intentionally reveals them with inactive visual treatment without changing FC-0103/FC-0104 archive and restore behavior.
- Add a clear-all action that resets text, tags, scope, and archived inclusion and returns to normal browsing with the pre-filter expansion state restored (generalizing the existing text-only expansion restore).
- Add an empty-results state that explains no nodes match and offers the clear action instead of rendering a blank tree.
- Generalize the existing "clear filtering before positioning" guard so structural placement commands stay unavailable while any filter dimension is active.
- UX placement: the primary workflow is an individual creator refinding or focusing work many times per day, so controls live in the primary workspace navigation pane; the search box is persistent while tag, scope, and archived controls are progressively disclosed in the flyout. No focused dialog, drafts, or destructive actions are introduced.
- Out of scope and owned elsewhere: workflow-stage and lifecycle-status filter selectors and their persisted facts (FC-0105), the listing inspector (FC-0106), tag creation and management (FC-0108), asset-name search (deferred until asset workflows surface assets in navigation), phrase and graphic-direction text (arrives with FC-020x creative fields), saved views (FC-0307), bulk actions from results (FC-0301/FC-0302), ranking, query languages, semantic or AI search, and cross-workspace search.

## Capabilities

### New Capabilities

- `search-filtering`: Text search across topic names and listing title, description, notes, and tag names; tag filtering; current-topic subtree scoping; default archived exclusion with intentional inclusion; AND combination across dimensions; clear-all and empty-results behavior; filter-aware structural-command guarding; and the navigation-pane filter surface presentation.

### Modified Capabilities

None. The accepted navigation-tree requirements already establish the filter-ready projection that preserves parent store/topic context for matches, and listing-management already covers filter-state restoration after rollback; FC-0107 builds on both without changing their requirements.

## Impact

- **Application**: `WorkspaceTreeQuery` gains subtree-scope and include-archived dimensions; `WorkspaceTreeProjector` matches text against listing description, notes metadata, and attached tag names in addition to node names; projection exposes empty-results state.
- **App**: filter flyout view and view-model state replacing the disabled Filters button; tag multi-select, scope selector, include-archived toggle, and clear-all wired to the tree; empty-results presentation; inactive styling for intentionally included archived rows; generalized structural-command guard while filtering.
- **Domain**: none; reuses existing workspace entities, tag relationships, and archive flags.
- **Integration**: none; filtering remains an in-memory projection over the loaded snapshot with no schema migration.
- **Tests**: application projector tests for searchable fields, tag AND semantics, subtree scope, archived inclusion, and combined filters; view-model tests for flyout state, clear-all, expansion restore, selection preservation, and guarded structural commands; a real desktop UI verification pass on a disposable workspace per the testing baseline.
- **Boundaries respected**: FC-0105 (stage/status facts and selectors), FC-0106 (inspector), FC-0108 (tag management), FC-020x (creative text fields), FC-0301/FC-0302 (bulk operations), FC-0307 (saved views), and later asset workflows (asset-name search).
- PRD intent source: `docs/LifeOS/PRD/FC-0107 - Basic Search and Filtering.md`.
