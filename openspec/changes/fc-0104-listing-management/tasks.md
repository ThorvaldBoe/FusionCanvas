## 1. Listing Placement and Effective Activity

- [x] 1.1 Add a listing topic reference that accepts niche/group destinations and resolves store and effective niche through existing workspace entities and `GroupHierarchy`.
- [x] 1.2 Implement deterministic listing creation-destination resolution from canonical selected group, selected listing parent, selected niche, or the store's valid default niche.
- [x] 1.3 Add listing effective-activity validation covering listing archive state plus archived or missing store, niche, group, and group-ancestor paths.
- [x] 1.4 Tighten listing movement validation to active same-store topics and preserve existing topic placement invariants without introducing store-level listings.
- [x] 1.5 Add domain/application tests for nested groups, selected-listing parent resolution, default-niche fallback, ambiguous/missing defaults, archived ancestry, cross-store rejection, and existing navigation regressions.

## 2. Listing Management Application Layer

- [x] 2.1 Define listing context, create/update/move/duplicate/delete requests, summaries, results, active/archived state, creation-resolution, and selection contracts with recoverable error reporting.
- [x] 2.2 Implement loading and canonical listing selection scoped to the active workspace/store, including active versus archived projections and deterministic clearing of effectively inactive selection.
- [x] 2.3 Implement one-line listing creation with normalization, resolved topic validation, draft defaults, optional core details, stable identity, timestamps, and one atomic save.
- [x] 2.4 Integrate applicable resolved tags and metadata into creation while preserving explicit overrides, inherited provenance, unknown metadata, and reusable tag records.
- [x] 2.5 Implement inline-title and optional-property updates that preserve identity, topic, status, archive state, relationships, and unknown metadata while rejecting invalid titles.
- [x] 2.6 Implement same-store moves to active niches/groups without recalculating inherited context or changing existing tag, prompt, and asset relationships.
- [x] 2.7 Implement duplication with a new identity, collision-safe copy title, draft status, copied core metadata/tag links, optional active same-store topic, and excluded prompt/asset relationships.
- [x] 2.8 Implement reversible archive and restore with preserved topic/context and blocked or explicitly retargeted restoration when the preserved parent path is unavailable.
- [x] 2.9 Implement confirmed permanent deletion, block listings with prompt/asset/future dependent records, and atomically remove listing-specific tag links without deleting reusable tags.
- [x] 2.10 Add application tests for all successful operations, invalid or archived ancestry, cross-workspace/store contexts, inherited values and overrides, preservation rules, duplicate exclusions, restore blocking, deletion guards, state, and repository-failure atomicity.

## 3. Persistence Verification

- [x] 3.1 Confirm the existing schema and snapshot validation support all FC-0104 mutations without a database migration or listing `SortOrder` field.
- [x] 3.2 Add SQLite integration tests for create, property edit, niche/group move, duplicate metadata/tag links, archive/restore, guarded delete, and complete reload behavior.
- [x] 3.3 Verify failed or cancelled operations persist no partial listings, tag links, placement changes, archive changes, or relationship removals.

## 4. Niche-Rooted Editable Tree Integration

- [x] 4.1 Wire `IListingManagementService` through `AppWorkspaceFactory`, `MainWindowViewModel`, and the existing `WorkspaceTreeViewModel` without exposing repository details to the UI.
- [x] 4.2 Add New Listing resolution from canonical selection/default niche and insert a non-persisted inline draft beneath the resolved topic with selected text and keyboard focus.
- [x] 4.3 Implement inline commit, Escape cancellation, validation retention, duplicate-submission prevention, and optimistic rollback for listing creation.
- [x] 4.4 Add inline F2/context rename backed by the listing service, retaining edit text and focus on validation or persistence failure.
- [x] 4.5 Generalize typed tree drag/drop and cut/paste routing so listing payloads move only onto active same-store niche/group destinations and group payload behavior remains unchanged.
- [x] 4.6 Generalize typed clipboard copy/paste and Duplicate routing so listing payloads create independent variations while group subtree copy behavior remains unchanged.
- [x] 4.7 Keep listing rows alphabetically ordered and reject or normalize before/after listing drops to topic-only destination semantics without adding persisted listing order.
- [x] 4.8 Select and reveal listings after create, rename, move, duplicate, or restore while preserving filter, expansion, clipboard, and canonical selection state across success and rollback.
- [x] 4.9 Keep normal listing selection independent of tabs and route Ctrl-click/Open in Tab through the existing tab coordinator without duplicate tabs.
- [x] 4.10 Add view-model and app tests for destination resolution, inline drafts, keyboard flow, typed drag/drop/clipboard operations, invalid targets, filtering, rollback, selection, and explicit tab opening.

## 5. Secondary Properties and Lifecycle Surface

- [x] 5.1 Add a secondary listing properties/lifecycle coordinator and Avalonia surface opened from the selected listing while preserving canonical tree and document context.
- [x] 5.2 Implement optional description/notes editing with explicit Save, dirty tracking, and save/discard/cancel handling for selection changes and close.
- [x] 5.3 Implement active/archived review plus state-coherent Archive, Restore, and Delete actions with archived-parent guidance and progressively disclosed destructive controls.
- [x] 5.4 Add explicit deletion confirmation/cancellation, connected-record blocking, and keyboard-accessible recovery paths.
- [x] 5.5 Implement deterministic post-archive/delete replacement selection and focus return to an adjacent listing or active parent topic.
- [x] 5.6 Apply compact action sizing, icon tooltips, predictable tab order, inline errors, busy states, and duplicate-submission prevention.
- [x] 5.7 Add view-model and UI tests for property drafts, lifecycle action enablement, active/archived transitions, unavailable parents, confirmations, errors, replacement selection, focus behavior, and shared control guidance.

## 6. Verification

- [x] 6.1 Run domain, application, integration, app, and UI automation test suites and resolve listing-management and FC-0103 regressions.
- [ ] 6.2 Manually verify keyboard-only inline create/rename, selected-listing/default-niche resolution, drag/drop, cut/copy/paste, canonical selection, explicit tab opening, property editing, archive/restore, guarded delete, filtering, rollback, and application reload.
- [x] 6.3 Run strict OpenSpec validation and confirm every listing-management scenario is covered by implementation or automated tests.
