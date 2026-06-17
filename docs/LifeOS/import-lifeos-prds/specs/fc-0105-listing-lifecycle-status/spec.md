## ADDED Requirements

### Requirement: Listing Can Have One Workflow Stage And One Operational Lifecycle Status
FusionCanvas SHALL ensure that a listing can have one workflow stage and one operational lifecycle status.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** A listing can have one workflow stage and one operational lifecycle status

### Requirement: User Can Change A Listing's Status
FusionCanvas SHALL allow users to change a listing's status.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** the user can change a listing's status

### Requirement: Status Should Be Visible When Inspecting A Listing
FusionCanvas SHALL ensure that status should be visible when inspecting a listing.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Status should be visible when inspecting a listing

### Requirement: Current Core Workflow Stage Should Be Visible In The Document Window
FusionCanvas SHALL ensure that the current core workflow stage should be visible in the document window.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The current core workflow stage should be visible in the document window

### Requirement: Idea, Concept, Design, And Listing Should Be Treated As Core Workflow Stages
FusionCanvas SHALL ensure that idea, Concept, Design, and Listing should be treated as core workflow stages.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Idea, Concept, Design, and Listing should be treated as core workflow stages

### Requirement: Future Stages The Item Has Not Reached Should Be Disabled In Workflow Navigation
FusionCanvas SHALL ensure that future stages the item has not reached should be disabled in workflow navigation.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Future stages the item has not reached should be disabled in workflow navigation

### Requirement: Completed Or Available Stages Should Remain Reviewable
FusionCanvas SHALL ensure that completed or available stages should remain reviewable.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Completed or available stages should remain reviewable

### Requirement: Status Should Be Usable As A Filter
FusionCanvas SHALL ensure that status should be usable as a filter.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Status should be usable as a filter

### Requirement: Status Should Support Early Creative Work From Capture Through Archive
FusionCanvas SHALL ensure that status should support early creative work from capture through archive.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Status should support early creative work from capture through archive

### Requirement: Rejected Listings Should Remain Available For Reference
FusionCanvas SHALL ensure that rejected listings should remain available for reference.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Rejected listings should remain available for reference

### Requirement: Status Should Communicate The Broad Operational State Of A Listing
FusionCanvas SHALL ensure that status should communicate the broad operational state of a listing.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Status should communicate the broad operational state of a listing

### Requirement: Acceptance Criteria Remain Satisfied
FusionCanvas SHALL satisfy the acceptance criteria captured in the source PRD.

#### Scenario: User Can Tell Which Listings Are Only Ideas
- **WHEN** the corresponding capability is delivered
- **THEN** A user can tell which listings are only ideas.

#### Scenario: User Can Tell Which Listings Are Draft, Published, Paused, Or Rejected
- **WHEN** the corresponding capability is delivered
- **THEN** A user can tell which listings are draft, published, paused, or rejected.

#### Scenario: User Can Filter Listings By Workflow Stage And Lifecycle Status
- **WHEN** the corresponding capability is delivered
- **THEN** A user can filter listings by workflow stage and lifecycle status.

#### Scenario: User Can See The Current Core Workflow Stage For An Open Item
- **WHEN** the corresponding capability is delivered
- **THEN** A user can see the current core workflow stage for an open item.

#### Scenario: User Can Navigate To Completed Stages From The Workflow Stage Navigator
- **WHEN** the corresponding capability is delivered
- **THEN** A user can navigate to completed stages from the workflow stage navigator.

#### Scenario: Future Stages Are Visibly Disabled Until They Exist Or Become Available
- **WHEN** the corresponding capability is delivered
- **THEN** Future stages are visibly disabled until they exist or become available.

#### Scenario: User Can Preserve Rejected Work Without Treating It As Active
- **WHEN** the corresponding capability is delivered
- **THEN** A user can preserve rejected work without treating it as active.

#### Scenario: Status Supports Action Without Enforcing Strict Transition Rules
- **WHEN** the corresponding capability is delivered
- **THEN** Status supports action without enforcing strict transition rules.

## Source PRD

# FC-0105 - Listing Lifecycle Status

## Summary

Listing Lifecycle Status lets creators track where each listing stands in the creative workflow.

The core workflow is Idea -> Concept -> Design -> Listing. Status should help the creator decide what needs action next without forcing a rigid production process.

## User Need

As a Print on Demand creator, I need to know which creative stage each listing is in and whether it is still draft work, published, paused, or rejected.

## Goals

- Give each listing a clear workflow stage.
- Support the visible core workflow stage navigator.
- Support filtering and focus by status.
- Help creators distinguish draft work from published, paused, and rejected work.
- Preserve rejected work for reference.
- Avoid overcomplicating the workflow in the first phase.

## Requirements

- A listing can have one workflow stage and one operational lifecycle status.
- The user can change a listing's status.
- Status should be visible when inspecting a listing.
- The current core workflow stage should be visible in the document window.
- Idea, Concept, Design, and Listing should be treated as core workflow stages.
- Future stages the item has not reached should be disabled in workflow navigation.
- Completed or available stages should remain reviewable.
- Status should be usable as a filter.
- Status should support early creative work from capture through archive.
- Rejected listings should remain available for reference.
- Status should communicate the broad operational state of a listing.

## Stage and Status Relationship

The visible stage navigator should be driven by `WorkflowStage`, not by a loose status label.

`WorkflowStage` should answer, "Which creative stage is this item in?"

Recommended workflow stages:

- Idea
- Concept
- Design
- Listing
- Archive

`Status` should answer, "What action or operational state does this item need?"

Recommended early statuses:

- Draft
- Published
- Paused
- Rejected

Implementation should avoid treating stage and status as two independent authorities for the same fact. Stage-like labels belong to `WorkflowStage`, not `Status`. Readiness details such as needing a mockup or metadata should be derived from workflow data, validation/readiness checks, queues, or filters rather than becoming core status values.

## User Workflows

### Set Initial Status

The user creates a listing with a status that reflects its current maturity.

New work begins as `Draft` and usually starts in the `Idea` workflow stage unless the user intentionally chooses another stage.

### Move Through Workflow

The user updates the workflow stage as the listing develops from idea to concept, design, listing preparation, and later publishing work. The user updates status only when the broad operational state changes.

Status changes should be quick and should not require completing unrelated fields.

### Navigate Workflow Stages

The user clicks an enabled previous stage in the workflow navigator to review information from that stage. Disabled future stages do not navigate.

### Skip Ahead

The user can begin at Concept or Design when they already have a phrase, graphic direction, or visual idea, with only minimal required information.

### Focus by Status

The user filters by stage and status to focus on a specific kind of work, such as draft ideas to refine, draft designs to create, published listings, paused work, or rejected concepts.

## Acceptance Criteria

- A user can tell which listings are only ideas.
- A user can tell which listings are draft, published, paused, or rejected.
- A user can filter listings by workflow stage and lifecycle status.
- A user can see the current core workflow stage for an open item.
- A user can navigate to completed stages from the workflow stage navigator.
- Future stages are visibly disabled until they exist or become available.
- A user can preserve rejected work without treating it as active.
- Status supports action without enforcing strict transition rules.

## Out of Scope

- Custom workflow builder
- Required status transition rules
- Automation based on status
- Marketplace publishing status sync
- Status analytics
- Bulk status changes

## Open Questions

- Should statuses be fixed in Phase 1?
- Should users be able to rename or hide statuses?
- Should Published be available before marketplace integrations exist?

## Related Notes

- [[Phase 1 - MVP Creative Workspace]]
- [[Roadmap]]
- [[Product]]
- [[Data Model]]
- [[FC-0008 - Workflow Stage Navigator]]
