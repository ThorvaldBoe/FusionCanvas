## Why

FusionCanvas now has a niche-rooted editable workspace tree and complete group-management foundation, but creators still cannot capture and maintain listings as the primary product concepts inside that structure. FC-0104 adds the fast item workflow needed to preserve ideas before artwork or marketplace data exists and supplies the Phase 1 foundation for lifecycle, inspector, search, tagging, and asset features.

## What Changes

- Add fast inline listing capture in the editable workspace tree, requiring one line of idea text and placing the listing in the selected niche/group, the selected listing's containing topic, or the store's active default niche.
- Add inline rename, canonical tree selection, drag/drop and cut/paste movement, and copy/paste or explicit duplication for listings while reusing one current working tab for normal selection.
- Add a secondary focused listing-properties/lifecycle surface for optional core details, archived review, restore, and guarded permanent deletion.
- Reuse FC-0103's `GroupHierarchy`, archive-aware tree projection, default-niche resolution model, canonical selection, optimistic rollback, and extend Ctrl-click/open-tab behavior so it preserves the current tab and adds or activates another tab.
- Preserve listing identity and related context when moving; create a distinct draft identity when duplicating and exclude asset and prompt relationships by default.
- Allow listings to exist without assets, mockups, optimized marketplace metadata, or other later-stage records.
- Define complete empty, validation, blocked, loading, rollback, cancellation, focus, and destructive-action behavior.
- Keep listings alphabetically ordered within a topic for FC-0104; persisted manual sibling ordering and before/after listing drops remain out of scope.
- Keep listing-to-group conversion out of scope and require explicit confirmation plus dependency checks for permanent deletion.

## Capabilities

### New Capabilities

- `listing-management`: Defines topic-resolved creation, inline editing, movement, duplication, archival, restoration, deletion, persistence, canonical selection, and secondary property-management behavior for listings.

### Modified Capabilities

None. FC-0103's navigation-tree and persistence contracts already establish the niche-rooted editable tree, default niche, canonical selection, and archive-aware projection. FC-0104 refines the tab contract for listing items by keeping one reusable working tab and reserving Ctrl-click/Open in Tab for additive tabs.

## Impact

- Adds listing-management application contracts and orchestration alongside the completed group-management service.
- Reuses `GroupHierarchy` and extends `WorkspaceTreeViewModel` with listing-specific inline creation, rename, move, copy, paste, archive, and selection operations.
- Extends `AppWorkspaceFactory` and main-window coordination with listing management while preserving the repository boundary.
- Exercises existing listing persistence, tag links, asset links, prompts, and metadata; no schema migration or listing sibling-order field is expected.
- Adds domain, application, integration, view-model, and UI tests for placement resolution, inherited context, archive ancestry, persistence, canonical selection, tree operations, rollback, focused lifecycle actions, and explicit tab opening.
- Requires the completed FC-0103 group-management implementation and its accepted OpenSpec deltas.
