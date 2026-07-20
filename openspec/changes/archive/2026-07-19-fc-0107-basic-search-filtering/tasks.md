## 1. Query and Projection (Application)

- [x] 1.1 Extend `WorkspaceTreeQuery` with `ScopeTopic` (`NavigationTopicReference?`) and `IncludeArchived` (`bool`), and cover every dimension in `IsActive`.
- [x] 1.2 Introduce one shared Application-level listing metadata-key helper for the `"notes"` key and switch `ListingManagement` to it without changing persisted data.
- [x] 1.3 Extend projector text matching so listings match on title, description, notes metadata, and attached tag names while topics keep matching on name; keep trimmed case-insensitive substring semantics and blank-text pass-through.
- [x] 1.4 Implement subtree-scope projection: when `ScopeTopic` is set, the scoped topic becomes the single visible root and matching applies within its subtree.
- [x] 1.5 Confirm AND combination semantics across text, tag, scope, and archived dimensions and preserve parent-context (non-direct match) projection behavior.
- [x] 1.6 Add application tests for searchable fields, tag-name matching, tag AND semantics, subtree scoping, archived inclusion flag handling at the query level, combined dimensions, blank text, and empty-result projections.

## 2. Archive-Inclusive Tree Input (Domain)

- [x] 2.1 Add an archive-inclusive tree build option to `WorkspaceNavigation` that retains archived niches, groups, and listings plus active entities beneath archived ancestors.
- [x] 2.2 Mark nodes with an `IsInactive` flag derived from existing `GroupHierarchy`/`ListingHierarchy` effective-activity rules; default `BuildTree` behavior stays unchanged.
- [x] 2.3 Project inactive nodes only when `WorkspaceTreeQuery.IncludeArchived` is set, keeping them out of the default projection.
- [x] 2.4 Add domain/application tests for archived listings, archived groups with active descendants, effectively inactive chains, and unchanged default projection behavior.

## 3. Tree View-Model Filter State (App)

- [x] 3.1 Add filter state to `WorkspaceTreeViewModel` (selected tag ids, scope mode with resolved topic, include-archived) and build the full `WorkspaceTreeQuery` for projection.
- [x] 3.2 Resolve the current-topic scope from canonical selection (selected niche/group, or selected listing's containing topic via `ListingHierarchy.GetTopic`) and expose scope availability.
- [x] 3.3 Generalize pre-filter expansion capture and restore from text-only to any active filter dimension.
- [x] 3.4 Implement clear-all: reset every dimension, restore expansion, preserve still-visible selection, and expose clear availability.
- [x] 3.5 Expose empty-results presentation state (`HasActiveFilters`, `HasVisibleResults`) without changing projector purity.
- [x] 3.6 Generalize the group sibling-positioning guard from text-only to any active filter while keeping the actionable error copy.
- [x] 3.7 Add view-model tests for query construction, scope resolution and availability, expansion capture/restore, clear-all, selection preservation, empty-results state, and the generalized positioning guard.

## 4. Navigation Filter Surface (App UI)

- [x] 4.1 Enable the Filters button and open a flyout with the scope selector (labeled with the resolved topic name), tag check-list over the active store's active tags, include-archived toggle, and a Clear-all footer action; apply changes immediately.
- [x] 4.2 Show an active-filter indication on the Filters button when any non-text dimension is active and keep the search box placeholder copy accurate for the widened field set.
- [x] 4.3 Add the tag-list empty state and keep all flyout controls keyboard reachable with tooltips, compact sizing, Escape/light-dismiss retention, and predictable focus return.
- [x] 4.4 Render the empty-results state in the tree area with an explanatory message and a Clear-filters action.
- [x] 4.5 Style included archived rows with the established inactive treatment and keep them out of canonical selection and structural destinations.
- [x] 4.6 Keep the flyout sectioned so FC-0105's stage/status selectors can be added later without reworking this surface.
- [x] 4.7 Add app/view-model tests for flyout state, active-filter indication, immediate application, dismissal retention, empty-results swap, and archived-row interaction limits.

## 5. Verification

- [x] 5.1 Run `dotnet test .\FusionCanvas.sln` and resolve regressions across domain, application, integration, and app suites.
- [x] 5.2 Perform the real desktop UI verification pass on a disposable workspace: text search by title/description/notes/tag name, tag AND filtering, subtree scoping from topic and listing selections, include-archived behavior and inactive treatment, combined filters, clear-all with expansion restore, empty-results recovery, positioning guard while filtering, keyboard-only flow, and application restart with unfiltered persistence intact; record build, environment, scenarios, results, isolation method, limitations, and evidence. (Not applicable under OpenCode — no interactive desktop; recorded as N/A in `verification.md`. Plan retained for a desktop-enabled run.)
- [x] 5.3 Run strict OpenSpec validation and confirm every `search-filtering` scenario is covered by implementation or automated tests.
