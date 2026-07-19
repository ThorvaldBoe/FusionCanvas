## Why

FC-0104 made listings creatable and manageable, but where a listing stands in the creative workflow is still simulated: the workflow stage shown in the navigator is derived from a loose status label at load time and is never persisted, every stage is always clickable, and the `ListingStatus` enum mixes stage-like values (`Active`, `Ready`) with a duplicate archive authority (`Archived`) while lacking the operational states creators need (`Paused`, `Rejected`). PRD FC-0105 requires stage and status to be two distinct, user-owned facts so creators can tell what needs action next without a rigid production process.

## What Changes

- **BREAKING**: Persist `WorkflowStage` on listings as a user-owned fact, replacing the load-time derivation from status (`WorkflowStageForListing` in `MainWindowViewModel`).
- **BREAKING**: Redefine `ListingStatus` as `Draft`, `Published`, `Paused`, `Rejected`; remove the stage-like `Active`/`Ready` values and the duplicate `Archived` value (archive remains the existing `IsArchived` flag and its FC-0103/FC-0104 flows).
- Add SQLite schema v4 migration: new `workflow_stage` column, translation of persisted status integers, mapping of old `Archived` rows onto the archive flag, and deterministic stage backfill from the previous derivation.
- Drive the workflow stage navigator from the persisted stage: reached stages (current and earlier) stay navigable, future stages are disabled, and the navigator distinguishes the prominent current stage from a secondary active-view marker. Opening a listing opens at its current stage.
- Add explicit stage-move controls in the document card footer — regress on the left, advance on the right, each naming the adjacent target stage. A move persists the new stage and switches the active view to it; controls disable at the workflow boundaries and while an item is inactive.
- Add a compact lifecycle-status selector to the document card header as the single quick-change surface for status; changing status never requires completing unrelated fields.
- Keep rejected listings visible in the navigation tree with inactive treatment for reference, show the navigator inactive overlay for rejected and archived items, and allow reactivation through status change.
- Add workflow-stage and lifecycle-status filter selectors to the existing navigation-pane filter area, combining with the current text query.
- Duplication additionally resets the copy to the `Idea` stage alongside its existing `Draft` status reset, so variations deliberately re-enter the workflow from the start.

## Capabilities

### New Capabilities

- `listing-lifecycle-status`: Persisted workflow-stage and lifecycle-status facts, the four-value operational status vocabulary, quick status change, explicit adjacent stage moves with boundary and inactive disabling, navigator availability from the persisted stage, rejected-work reference behavior, stage/status tree filtering, creation and duplication defaults, preservation across management operations, and schema v3-to-v4 value migration.

### Modified Capabilities

- `listing-management`: Duplication resets the copy to the `Idea` workflow stage in addition to its existing draft status reset.
- `workflow-stage-navigator`: The navigator distinguishes the item's current stage from the active view stage with separate visual emphasis.

## Impact

- **Domain**: `Listing` gains a `WorkflowStage` fact; `ListingStatus` is redefined to four operational values; display-name helpers for statuses.
- **Application**: listing workflow mutations (set status, move stage) with snapshot-atomic persistence; navigator state gains an active-view marker; item contexts are built from the persisted stage plus an inactive derivation (rejected or archived).
- **Integration**: SQLite schema version 4 with `workflow_stage`, status value translation, archive-flag mapping, and stage backfill; listing serialization round-trips stage.
- **App**: document card stage-move footer and status selector; navigator current-versus-view marker visuals; navigation-pane stage/status filter selectors; rejected styling in the tree; removal of the status-to-stage derivation and the always-available stage wiring.
- **Tests**: domain, application, integration, and view-model tests per testing-baseline, plus a real desktop UI verification pass on a disposable workspace.
- PRD intent source: `docs/LifeOS/PRD/FC-0105 - Listing Lifecycle Status.md`; boundaries respected toward FC-0106 (inspector), FC-0107 (general search/filter), FC-0302 (bulk changes), and FC-0607 (publishing sync).
