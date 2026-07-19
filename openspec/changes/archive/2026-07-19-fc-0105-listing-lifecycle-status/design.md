## Context

FC-0104 delivered full listing management (creation, rename, move, duplicate, archive/restore, guarded delete) and deliberately left workflow-stage and operational-status editing to FC-0105. Today's simulation lives in the App layer: `MainWindowViewModel.WorkflowStageForListing` derives a stage from `ListingStatus` at load (`Ready`→Design, `Active`→Listing, else Idea), passes `WorkflowStages.Ordered` as availability so every stage is always clickable, and nothing persists. The domain carries a five-value `ListingStatus` (`Active`, `Draft`, `Ready`, `Published`, `Archived`) plus an `IsArchived` flag, so stage-like labels and a duplicate archive authority are baked into the persisted model. The FC-0008 navigator service already separates `CurrentStage` (item fact) from `ActiveViewStage` (view) and supports an inactive overlay (`IsInactive`/`InactiveLabel`); the view-mutation path (`ChangeActiveStage`) exists but is only exercised by tests. The navigation tree already has a text query filter with expansion save/restore, and FC-0104's `IListingManagementService` owns atomic listing mutations (reload → validate → one save).

PRD FC-0105 asks for one workflow stage and one operational lifecycle status per listing, user change of both, stage-driven navigation, status filtering, and preserved rejected work — without transition rules, automation, or marketplace sync.

## Goals / Non-Goals

**Goals:**

- Persist `WorkflowStage` on listings as a user-owned fact; navigator availability derives from it (current and earlier stages navigable, future stages disabled).
- Redefine `ListingStatus` as the operational states `Draft`, `Published`, `Paused`, `Rejected`; archive remains solely the `IsArchived` mechanism.
- Explicit stage-move controls (advance/regress) in the document workspace; clicking the navigator remains view-only.
- One compact, always-available status selector as the single quick-change surface; status changes never require unrelated fields.
- Rejected listings remain visible for reference with inactive treatment; rejected and archived items show the navigator inactive overlay.
- Stage and status filter selectors in the navigation pane riding the existing filter pipeline.
- Schema v4 migration translating persisted values deterministically; duplication resets copies to `Idea` + `Draft`.

**Non-Goals:**

- No required transition rules, stage gates, or per-stage required fields.
- No skip-ahead stage choice at creation (capture stays one-line at `Idea`; advancing is explicit afterward).
- No high-water "furthest reached" tracking beyond the single stage pointer (see Decision 1).
- No bulk status/stage changes (FC-0302), no status automation, no marketplace publishing sync (FC-0607), no status analytics.
- No FC-0106 listing inspector and no FC-0107 general text/tag/location search; filters here cover stage and status only.
- No user-defined, renamed, or hidden statuses in Phase 1 (fixed vocabulary).
- No `Archive` workflow-stage member; archive stays a flag-driven state per the accepted navigator spec.

## Decisions

### 1. Persist one stage pointer; defer "furthest reached" tracking

`Listing` gains a persisted `WorkflowStage`. Availability = the current stage and all earlier stages. Regressing the stage makes later stages unavailable again.

Alternative considered: persist both current stage and a monotonic furthest-reached stage so previously reached stages stay reviewable after a regress. Rejected for Phase 1: stage views are not yet backed by stage-scoped records (Concept/Design entities arrive in Phase 2), so a regressed stage view hides nothing the listing fields don't already carry, and the second column adds ratchet logic plus a concept users must learn. Revisit trigger: when stage-scoped records exist (FC-0202/FC-0203 era), add the reached pointer with a trivial backfill (`reached := stage`).

Alternative considered: keep deriving stage from status. Rejected by the PRD outright — the navigator must be driven by `WorkflowStage`, not a loose status label.

### 2. Four operational statuses; archive stays a flag

`ListingStatus` becomes `Draft`, `Published`, `Paused`, `Rejected`. `Active` and `Ready` are removed because they are stage/readiness talk (readiness belongs to derived checks and queues, not core status). The `Archived` enum value is removed because `IsArchived` is the single archive authority FC-0103/FC-0104 already built on. `Published` is available before marketplace integrations exist; it simply records the creator's own operational claim.

Alternative considered: keep `Ready` as a convenience. Rejected — the PRD explicitly calls readiness-as-status an anti-pattern; work queues and validation derive it later.

### 3. Navigator click views; footer buttons move

Navigator stage clicks remain view-only navigation (accepted FC-0008 behavior; `Navigate` never mutates `CurrentStage`). Stage changes happen through two explicit controls in a footer row at the bottom of the document card: a left-aligned regress button and a right-aligned advance button, each labeled with its adjacent target (`◂ Move to Idea` / `Move to Design ▸`). A successful move persists the new stage and switches the active view to it, so view and current reunite after every move.

Alternative considered: placing move controls in or beside the navigator strip. Rejected — the navigator is view territory; mixing move actions there blurs the view-versus-move distinction this change formalizes. The document card footer is item territory, survives future stage tools inside the detail panel, and matches the left/right workflow direction.

Moves target adjacent stages only; boundaries disable (`Idea`: no regress, `Listing`: no advance). Jumping multiple stages on an existing item is not offered; skip-ahead remains a creation-time concept that FC-0105 deliberately does not implement (Non-Goals).

Alternative considered: a stage dropdown allowing arbitrary jumps. Rejected for now — adjacent moves keep each transition a deliberate checkpoint (notably desired for the duplicate-review flow) and keep the control simple; a jump control can be added later without invalidating this design.

### 4. Status selector lives once, in the document card header

A compact status selector sits at the trailing end of the document card title row, showing the current status and applying a new status immediately on selection. It is the single visible status-change owner (per the one-clear-owner guideline); FC-0104's listing editor window stays focused on properties and archive/delete lifecycle and does not duplicate status editing ahead of the FC-0106 inspector. No save button, no required companion fields.

Failure handling: a failed persistence reverts the selector to the persisted status and shows an inline error in the document card; nothing is left half-applied (mutations are snapshot-atomic via the listing service).

Alternative considered: putting status change only in the FC-0104 properties window. Rejected — changing status from the working document would require opening a separate window, violating the PRD's "status changes should be quick."

### 5. Stage/status mutations extend the existing listing service

`IListingManagementService` gains set-status and move-stage operations following its established pattern (reload latest snapshot, validate, produce one replacement snapshot, one `SaveAsync`). The service validates target values and listing existence but imposes no transition rules; adjacency and inactive disabling are UI concerns (Decision 3, 7). Status changes remain possible on rejected listings so rejection is reversible.

Alternative considered: a separate lifecycle service. Rejected — it would split the single mutation gateway for the same aggregate and duplicate the reload/validate/save discipline for no boundary benefit.

### 6. Navigator shows current stage prominently and view stage secondarily

`WorkflowStageNavigatorEntry` gains an active-view marker alongside `IsCurrent`. The current stage keeps the strongest emphasis; the viewed stage (when different) gets a lighter, still-visible treatment. Opening a listing initializes the view at the item's current stage (existing `Build` behavior when no view stage is requested). The inactive overlay (`Rejected` / `Archived`) continues to render beside the stage row.

### 7. Rejected stays visible and reviewable; inactive items cannot move stages

Rejected listings remain in the navigation tree with a subdued inactive treatment (not hidden like archived ones), open normally for reference, and drive the navigator overlay via `IsInactive` + `InactiveLabel`. Stage-move controls disable while an item is inactive (rejected or archived); the status selector stays enabled on rejected items so the creator can reactivate (`Rejected` → `Draft`/`Paused`). Archived items keep their FC-0104 restore path.

### 8. Stage and status filters join the existing navigation-pane filter area

Two compact selectors (`Stage`, `Status`, each defaulting to *All*) sit with the existing filter input above the tree. They combine with the text query using AND semantics, act on listing rows only, and preserve the existing behaviors: parent topics remain to give matches context, expansion state is saved/restored across filtering, selection/canonical identity survives, and structural commands that require unfiltered positioning keep their current guards. Clearing the selectors returns to normal browsing.

### 9. Defaults and preservation rules

- Creation (FC-0104 inline capture): `Draft` status, `Idea` stage.
- Duplication: `Draft` status and `Idea` stage on the copy, with FC-0104's copied fields preserved — the creator re-reviews each stage and advances explicitly, which is precisely the desired "shortest route to a similar item, then review" flow.
- Rename, property edit, move, archive, and restore preserve stage and status.
- Fixed vocabulary in Phase 1: no rename/hide of statuses (closes the PRD open questions).

### 10. Schema v4 migration translates values explicitly

`listings` gains `workflow_stage INTEGER NOT NULL`; `user_version` advances to 4. Every row's status integer is rewritten from the old enum to the new one, and the stage is backfilled from the previous derivation so the navigator shows the same stages users saw before the migration:

| Old status (int) | New status | Backfilled stage | Archive flag |
|---|---|---|---|
| `Active` (0) | `Draft` | `Listing` | unchanged |
| `Draft` (1) | `Draft` | `Idea` | unchanged |
| `Ready` (2) | `Draft` | `Design` | unchanged |
| `Published` (3) | `Published` | `Listing` | unchanged |
| `Archived` (4) | `Draft` | `Idea` | set `true` |

Rationale: never overstate (nothing silently becomes `Published`), stage backfill mirrors the pre-migration navigator, and old archived rows join the flag-based archive model. New enum values: `Draft`=0, `Published`=1, `Paused`=2, `Rejected`=3.

Alternative considered: mapping `Active` → `Published`. Rejected — old `Active` marked listing-stage work-in-progress (the sample data's "Espresso listing draft" used it), so promoting it would falsely claim publishership.

## Risks / Trade-offs

- [Enum redefinition invalidates persisted integers and all construction sites] → Explicit value-translation migration with integration tests against v3-format fixture databases; mechanical compile-guided update of `Listing` construction sites.
- [Users of pre-v4 databases lose `Ready`/`Active` distinctions] → Acceptable: those values were stage proxies; the backfill preserves the visible stage, and status honesty ("never overstate") is the safer error direction.
- [Two navigator markers could read as noise] → Current keeps strong emphasis, view marker stays subtle; verified in the desktop UI pass against ui-guidelines.
- [Filters combined with text query may surprise] → Single AND predicate, *All* defaults, and the existing expansion/selection preservation; scenarios cover clearing and empty results.
- [Rollback of a migrated database is impossible for older app versions] → By design: the existing version guard makes older builds refuse v4 databases instead of corrupting them; rollback means reverting code before first save or restoring a backup.
- [Optimistic tree projection versus immediate mutations] → Stage/status changes are not projected optimistically; they round-trip through the service and rebuild contexts, matching FC-0104's confirmed-projection discipline.

## Migration Plan

1. Domain: redefine `ListingStatus`, add `WorkflowStage` to `Listing`, add status display-name helper; update construction sites and tests.
2. Integration: schema v4 (`workflow_stage` column, value translation, backfill, archive-flag mapping) with fixture-database migration tests and round-trip tests.
3. Application: set-status and move-stage operations on the listing service; navigator entry active-view marker; context building from persisted stage plus inactive derivation.
4. App: remove the derivation/all-available wiring; document card footer controls and status selector; navigator marker visuals; tree filter selectors and rejected styling.
5. Run all test suites, strict OpenSpec validation, and the desktop UI verification pass on a disposable workspace.

Rollback: revert the change before any v4 database is created; once migrated, older builds intentionally refuse the database (see Risks).

## Open Questions

None blocking. Recorded for later: the furthest-reached pointer revisit trigger (Decision 1), and whether skip-ahead at creation becomes desirable once real stage tools land (FC-0210–0212).
