## ADDED Requirements

### Requirement: Idea Inbox triages unprocessed idea-stage listings
FusionCanvas SHALL provide a focused Idea Inbox surface that lists non-archived, non-rejected listings whose current workflow stage is Idea for an active store, niche, group, or topic context, and SHALL allow the creator to triage those listings in place without leaving the inbox.

#### Scenario: User opens the inbox from an active context
- **WHEN** the user opens the Idea Inbox for an active store, niche, group, or topic
- **THEN** FusionCanvas lists the non-archived, non-rejected idea-stage listings scoped to that context
- **AND** preselects the canonically selected listing when it is an idea-stage listing in scope
- **AND** keeps canonical tree and document context intact

#### Scenario: Context has no unprocessed ideas
- **WHEN** the user opens the inbox for a context with no non-archived, non-rejected idea-stage listings
- **THEN** FusionCanvas shows an empty state that explains how to capture a new idea
- **AND** keeps the capture action available

#### Scenario: Archived or rejected ideas are excluded
- **WHEN** a listing is archived or rejected
- **THEN** the active inbox list excludes it through the shared archive-aware projection
- **AND** the listing remains reachable through the archived-listing lifecycle surface

### Requirement: Idea Inbox captures ideas into the selected topic
FusionCanvas SHALL allow the user to capture a new idea from the inbox into the resolved active topic using the existing listing-management topic-resolution and atomic persistence behavior, with focus retention and rollback on failure.

#### Scenario: User captures an idea from the inbox
- **WHEN** the user invokes capture with a resolvable active topic and commits one valid line of idea text
- **THEN** FusionCanvas creates a new draft idea-stage listing beneath that topic through the listing-management service
- **AND** selects and reveals the new listing in the inbox
- **AND** preserves the current filter and expansion context

#### Scenario: Capture is unavailable
- **WHEN** no active store or topic/default niche can be resolved
- **THEN** FusionCanvas blocks capture with a clear store, niche, selection, or default-niche setup path
- **AND** does not show a misleading enabled commit action

#### Scenario: Capture fails after an optimistic update
- **WHEN** the repository cannot save a capture after the inbox has projected a draft
- **THEN** FusionCanvas reports a recoverable error
- **AND** restores the last confirmed inbox projection, selection, and filter state
- **AND** retains the recoverable input needed to retry

### Requirement: Idea Inbox records optional idea-stage context
FusionCanvas SHALL allow the user to record optional audience, phrase fragments, and visual direction for an idea-stage listing as documented idea-stage metadata keys, while preserving the listing's stable identity, topic, archive state, status, tags, prompts, assets, and unknown metadata.

#### Scenario: User adds idea-stage context
- **WHEN** the user edits the audience, phrase fragments, or visual direction field for a selected idea and saves
- **THEN** FusionCanvas persists the values as idea-stage metadata keys and updates the timestamp atomically
- **AND** preserves the listing's stable identity and related context

#### Scenario: User omits optional idea-stage context
- **WHEN** the user captures or edits an idea without supplying optional idea-stage fields
- **THEN** FusionCanvas creates or updates the listing without fabricated idea-stage metadata
- **AND** omitted fields do not block creation or editing

#### Scenario: Unknown metadata is preserved
- **WHEN** an idea-stage listing carries metadata keys outside the documented idea-stage set
- **THEN** idea-inbox edits preserve those unknown keys unchanged

### Requirement: Idea Inbox promotes ideas through the workflow stage navigator
FusionCanvas SHALL allow the user to promote a selected idea to the Concept, Design, or Listing stage by requesting the accepted workflow-stage-navigator behavior, and SHALL NOT bypass stage-transition validation or set the stage field directly.

#### Scenario: User promotes an idea to Concept
- **WHEN** the user invokes Promote to Concept for a selected idea-stage listing
- **THEN** FusionCanvas requests the workflow-stage-navigator to advance the listing to the Concept stage
- **AND** removes the listing from the active inbox list when the transition succeeds
- **AND** selects and reveals the listing in its new stage context

#### Scenario: User skips ahead to Design or Listing
- **WHEN** the user invokes Promote to Design or Promote to Listing for an idea the creator considers ready
- **THEN** FusionCanvas requests the same workflow-stage-navigator advancement
- **AND** the navigator may accept or reject the transition according to accepted stage rules

#### Scenario: Promotion is blocked
- **WHEN** the workflow-stage-navigator rejects the requested transition
- **THEN** FusionCanvas reports the actionable reason
- **AND** leaves the listing's stage, identity, and inbox selection unchanged

### Requirement: Idea Inbox archives and rejects without deletion
FusionCanvas SHALL allow the user to archive or reject a selected idea without deleting it, SHALL mark rejected ideas with an `idea.rejected` metadata marker, and SHALL keep rejected and archived ideas reachable through the existing archived-listing lifecycle surface.

#### Scenario: User archives an idea
- **WHEN** the user confirms archive for a selected idea
- **THEN** FusionCanvas marks the listing archived through the existing listing-management behavior
- **AND** removes it from the active inbox list
- **AND** preserves its topic, idea-stage metadata, tags, prompts, assets, and remaining context

#### Scenario: User rejects an idea
- **WHEN** the user rejects a selected idea with an optional reason
- **THEN** FusionCanvas marks the listing archived and records the `idea.rejected` metadata marker with the optional reason
- **AND** removes it from the active inbox list without deleting the listing
- **AND** keeps the listing reachable through the archived-listing lifecycle surface

#### Scenario: User restores a rejected idea
- **WHEN** the user restores a rejected idea through the archived-listing lifecycle surface
- **THEN** FusionCanvas clears the `idea.rejected` marker through the normal restore flow
- **AND** returns the listing to the active inbox list when its topic path is active

### Requirement: Idea Inbox persists operations atomically
FusionCanvas SHALL persist idea-inbox capture, metadata edits, promotion requests, archive, and reject operations in the active workspace database as atomic snapshot operations, reloading the latest snapshot before each mutation and saving once per operation.

#### Scenario: Workspace reloads after inbox operations
- **WHEN** a successful inbox operation is followed by an application or workspace database reload
- **THEN** the resulting listings, idea-stage metadata, stage assignments, archive flags, and reject markers match the last successful persisted state

#### Scenario: Persistence fails after an optimistic inbox change
- **WHEN** the repository cannot save an inbox operation after the inbox has projected a draft or selection change
- **THEN** FusionCanvas reports a recoverable error
- **AND** restores the last confirmed inbox projection, selection, filter, and draft state
- **AND** retains recoverable user input needed to retry when applicable

### Requirement: Idea Inbox coordinates with canonical selection
FusionCanvas SHALL make normal inbox selection update canonical workspace context and the reusable working tab through the existing normal-selection path, and SHALL keep the inbox focused on triage without accumulating duplicate selection state.

#### Scenario: User selects an idea in the inbox
- **WHEN** the user selects an inbox row
- **THEN** that listing becomes canonical workspace selection
- **AND** the current working tab is created or reused for that listing
- **AND** the inbox selection stays coherent with the tree selection

#### Scenario: Selected idea leaves the active inbox
- **WHEN** the selected idea is promoted, archived, rejected, or permanently deleted
- **THEN** FusionCanvas selects a sensible adjacent idea-stage listing when one exists
- **AND** otherwise returns focus to the invoking context
- **AND** removes the departed listing from the active inbox list

### Requirement: Idea Inbox follows shared desktop control guidance
FusionCanvas SHALL present the inbox with compact action sizing, clear tooltips for icon-only commands, predictable keyboard flow, accessible confirmation and cancellation, busy states that prevent duplicate submission, and error states that preserve selection and window context.

#### Scenario: Triage actions are presented
- **WHEN** capture, save, promote, archive, reject, or delete-confirmation actions are shown
- **THEN** their buttons are sized to their command groups rather than evenly stretched across the surface
- **AND** essential actions are reachable without pointer-only interaction

#### Scenario: Triage operation is in progress or fails
- **WHEN** a capture, metadata save, promotion, archive, or reject operation is running or fails validation or persistence
- **THEN** FusionCanvas prevents duplicate submission
- **AND** keeps the surface available to report success or an actionable error
- **AND** preserves selection and recoverable input after failure
