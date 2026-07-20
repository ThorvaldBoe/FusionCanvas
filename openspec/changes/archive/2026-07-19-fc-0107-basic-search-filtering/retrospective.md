# fc-0107-basic-search-filtering Retrospective

## Outcome

Basic search and filtering is implemented and accepted. The navigation text query now matches listing title, description, notes, and attached tag names in addition to topic/group names. A Filters flyout above the tree exposes tag multi-select (AND), current-topic subtree scoping, and an optional include-archived toggle. Filters combine with AND, preserve parent context, restore pre-filter expansion on clear-all, and show an empty-results state. The structural-positioning guard is generalized to any active filter. Archived rows revealed by include-archived are inactive-only and never become canonical navigation context. The full deterministic baseline passes (216/216); the real desktop UI pass is not-applicable under OpenCode and is retained for a desktop-enabled run. Stage/status filters remain owned by FC-0105; the flyout is sectioned to accept them later.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| Subtree scope live-follows the canonical selection (the tree narrows as the user clicks different topics) | Live-following would surprise users: clicking a listing to inspect it would silently re-narrow the tree, and selection-driven scope changes interact poorly with the filter itself (the scope is both a filter input and a side effect of selection) | Resolve and pin the scope topic at the moment the user enables "Current topic and below"; label the pinned topic by name; re-pin by toggling off and on with a new selection. Disable when selection cannot resolve a topic. | UX | Change-specific | None — recorded in design.md risk note; not a generalizable rule beyond this feature |

## Learning Review

- Result: no reusable lessons identified.
- Evidence reviewed: proposal, design, spec delta, tasks, verification record, and the two implementation commits (`b19a677` spec, `a0b5705` implementation). The one in-flight adjustment (scope pinning) is change-specific UX rationale already captured in `design.md` and does not generalize to project-wide guidance.
- Promotions completed: none.
- Deferred promotions: none. The archive-inclusive tree input (`NavigationNode.IsInactive` + `BuildTree(snapshot, includeArchived)`) is consistent with the existing archive-aware projection pattern from fc-0103 and does not warrant a new architecture-guidelines rule. The shared `ListingMetadata.NotesKey` helper is ordinary focused-responsibility practice already covered by the coding principles. The FC-0105 boundary coordination is change-specific and recorded in the proposal/design.
