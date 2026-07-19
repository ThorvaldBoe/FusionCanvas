# Listing Lifecycle Status

## Purpose

Defines how FusionCanvas tracks each listing's position in the creative workflow (one persisted workflow stage) and its broad operational state (one persisted lifecycle status) as two independent, user-owned facts, and how those facts drive the workflow stage navigator, the document surface, the navigation tree, and filtering.

## Requirements

### Requirement: Listing carries one workflow stage and one lifecycle status
A listing SHALL persist exactly one workflow stage (`Idea`, `Concept`, `Design`, or `Listing`) and exactly one lifecycle status as two independent, user-owned facts, and the workflow stage SHALL NOT be derived from the lifecycle status.

#### Scenario: Listing persists stage and status independently
- **WHEN** a listing is saved and reloaded
- **THEN** its workflow stage and lifecycle status are reconstructed with their persisted values
- **AND** neither value is computed from the other

#### Scenario: Status change does not move the stage
- **WHEN** the user changes a listing's lifecycle status
- **THEN** its workflow stage remains unchanged

### Requirement: Lifecycle status vocabulary is operational and fixed
The listing lifecycle status SHALL be one of `Draft`, `Published`, `Paused`, or `Rejected`, and SHALL NOT include stage-like values, readiness values, or an archive value (archive state remains the separate archive mechanism).

#### Scenario: Contributor inspects the status vocabulary
- **WHEN** a contributor reviews the listing lifecycle status model
- **THEN** the available statuses are exactly `Draft`, `Published`, `Paused`, and `Rejected`
- **AND** archive state is represented by the existing archive flag rather than a status value

#### Scenario: Status answers the operational question
- **WHEN** a user reviews a listing's status
- **THEN** the status communicates whether the work is draft, published, paused, or rejected
- **AND** it does not duplicate which workflow stage the listing is in

### Requirement: User changes lifecycle status quickly
FusionCanvas SHALL allow the user to change a listing's lifecycle status from the listing's document surface through one compact selector, without requiring edits to unrelated fields, and SHALL apply the change atomically.

#### Scenario: User changes status from the document surface
- **WHEN** the user selects a different lifecycle status while a listing document is active
- **THEN** FusionCanvas persists the new status
- **AND** the document surface, navigation context, and any active filters reflect the new status

#### Scenario: Status change fails to persist
- **WHEN** the status change cannot be saved
- **THEN** the selector reverts to the persisted status
- **AND** an inline error is shown without partially applying the change

### Requirement: User moves the workflow stage with explicit controls
FusionCanvas SHALL provide explicit advance and regress controls in the listing document surface that move a listing to the adjacent workflow stage, and clicking a stage in the workflow stage navigator SHALL remain view-only navigation that does not change the listing's stage.

#### Scenario: User advances the stage
- **WHEN** the listing is at `Concept` and the user activates the advance control
- **THEN** FusionCanvas persists `Design` as the listing's stage
- **AND** the active document view switches to the `Design` stage
- **AND** the workflow stage navigator marks `Design` as current

#### Scenario: User regresses the stage
- **WHEN** the listing is at `Design` and the user activates the regress control
- **THEN** FusionCanvas persists `Concept` as the listing's stage
- **AND** the active document view switches to the `Concept` stage

#### Scenario: Controls at workflow boundaries
- **WHEN** the listing is at `Idea`
- **THEN** the regress control is unavailable
- **WHEN** the listing is at `Listing`
- **THEN** the advance control is unavailable

#### Scenario: Navigator click does not move the stage
- **WHEN** the listing is at `Design` and the user selects the available `Idea` stage in the navigator
- **THEN** FusionCanvas shows the `Idea` stage view
- **AND** the listing's persisted stage remains `Design`

### Requirement: Stage movement pauses for inactive listings while status stays recoverable
FusionCanvas SHALL disable the stage advance and regress controls while a listing is inactive (rejected or archived), and SHALL keep lifecycle status change available for a rejected listing so it can be reactivated.

#### Scenario: Rejected listing cannot move stages
- **WHEN** the active listing's status is `Rejected`
- **THEN** the stage advance and regress controls are unavailable
- **AND** the workflow stage navigator exposes the inactive state separately from the stage row

#### Scenario: User reactivates rejected work
- **WHEN** the user changes a rejected listing's status to `Draft`
- **THEN** the listing is treated as active work again
- **AND** its persisted workflow stage resumes driving stage availability

### Requirement: Workflow stage drives navigation availability and initial view
FusionCanvas SHALL determine stage navigability from the listing's persisted workflow stage: the current stage and all earlier stages are navigable, later stages are disabled, and opening a listing document SHALL present the listing's current workflow stage view.

#### Scenario: Future stages are disabled
- **WHEN** a listing's stage is `Concept`
- **THEN** the `Idea` and `Concept` stages are navigable
- **AND** the `Design` and `Listing` stages are visibly disabled and do not navigate

#### Scenario: Opening a listing opens its current stage
- **WHEN** the user opens a listing whose stage is `Design`
- **THEN** the document opens with the `Design` stage as the active view
- **AND** the navigator marks `Design` as current

#### Scenario: Advancing unlocks the next stage
- **WHEN** a listing advances from `Concept` to `Design`
- **THEN** the `Design` stage becomes navigable for that listing

### Requirement: Rejected listings remain available for reference
FusionCanvas SHALL keep rejected listings visible in the navigation tree with a visibly inactive treatment, SHALL allow opening them for review, and SHALL NOT treat them as active work.

#### Scenario: Rejected listing stays in the tree
- **WHEN** a listing's status is `Rejected`
- **THEN** the listing remains visible in its topic in the navigation tree
- **AND** it is presented with an inactive treatment distinct from active listings

#### Scenario: User reviews rejected work
- **WHEN** the user opens a rejected listing
- **THEN** FusionCanvas shows its workflow stages as reviewable according to its persisted stage
- **AND** the navigator exposes the `Rejected` inactive state beside the stage row

### Requirement: Navigation filters focus by stage and status
FusionCanvas SHALL provide workflow-stage and lifecycle-status filter selectors in the navigation pane filter area that narrow visible listings, combine with the existing text filter using AND semantics, preserve parent topic context for matching listings, and allow clearing back to normal browsing.

#### Scenario: User filters by stage
- **WHEN** the user selects the `Idea` stage filter
- **THEN** only listings at the `Idea` stage remain visible as listing rows
- **AND** their parent topics remain visible to show where the matches live

#### Scenario: User filters by status
- **WHEN** the user selects the `Rejected` status filter
- **THEN** only rejected listings remain visible as listing rows

#### Scenario: Filters combine with the text query
- **WHEN** a text query and a stage or status filter are both active
- **THEN** a listing row is visible only when it satisfies every active filter

#### Scenario: User clears the filters
- **WHEN** the user returns the stage and status selectors to their unfiltered state
- **THEN** the tree returns to normal browsing with expansion and selection state preserved

### Requirement: Lifecycle defaults and preservation across listing operations
New listings SHALL begin with `Draft` status and the `Idea` workflow stage, and rename, property edits, moves, archive, and restore SHALL preserve a listing's workflow stage and lifecycle status.

#### Scenario: New listing starts as draft idea
- **WHEN** the user commits a new listing through inline capture
- **THEN** the listing is created with `Draft` status
- **AND** the `Idea` workflow stage

#### Scenario: Move preserves lifecycle facts
- **WHEN** the user moves a listing to another valid topic
- **THEN** its workflow stage and lifecycle status are preserved

#### Scenario: Archive and restore preserve lifecycle facts
- **WHEN** the user archives and later restores a listing
- **THEN** its workflow stage and lifecycle status are preserved across both operations

### Requirement: Existing databases migrate lifecycle values safely
FusionCanvas SHALL migrate pre-v4 workspace databases to the lifecycle model by translating persisted status values to the new vocabulary, backfilling the workflow stage deterministically, and mapping archive-valued rows onto the archive flag.

#### Scenario: Older database is migrated
- **WHEN** a workspace database predating the lifecycle model is opened
- **THEN** every listing's status is translated to `Draft`, `Published`, `Paused`, or `Rejected` without inventing published state for non-published rows
- **AND** every listing receives a workflow stage consistent with what the pre-migration navigator displayed
- **AND** listings whose old status represented archive are marked archived through the archive flag

#### Scenario: Migrated listings behave like native ones
- **WHEN** migration completes
- **THEN** migrated listings support stage movement, status change, navigator availability, and filtering identically to newly created listings
