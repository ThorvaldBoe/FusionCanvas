## ADDED Requirements

### Requirement: Listing inspector captures optional idea-stage audience
FusionCanvas SHALL allow the user to record an optional audience for a listing as the documented `idea.audience` metadata key, SHALL present it in the inspector's Idea section with the same automatic-save behavior as the other creative fields, and SHALL preserve the listing's stable identity, topic, archive state, status, tags, prompts, assets, and unknown metadata.

#### Scenario: User records an audience
- **WHEN** the user enters an audience for the active listing and the field loses focus
- **THEN** FusionCanvas persists the value as the `idea.audience` metadata key and updates the timestamp atomically
- **AND** preserves the listing's identity and related context

#### Scenario: User omits the audience
- **WHEN** the user captures or edits a listing without supplying an audience
- **THEN** FusionCanvas creates or updates the listing without fabricating the `idea.audience` key
- **AND** the omission does not block creation or editing

#### Scenario: Audience survives unrelated edits
- **WHEN** a listing carries an `idea.audience` value and the user edits other inspector fields
- **THEN** the audience value is preserved unchanged

### Requirement: Idea-stage triage uses the existing document controls
FusionCanvas SHALL present idea-stage triage for the active idea-stage listing through the existing document-surface controls — explicit stage advance and regress, the lifecycle status selector, and the inspector lifecycle actions — and SHALL NOT introduce a competing triage surface, a second status control, or a reject marker duplicating the `Rejected` lifecycle status.

#### Scenario: User promotes an idea
- **WHEN** the user activates the advance control for an active idea-stage listing
- **THEN** FusionCanvas persists `Concept` as the listing's stage through the accepted stage-movement behavior
- **AND** repeated activation reaches `Design` and `Listing` when the creator has enough direction

#### Scenario: User rejects an idea without deletion
- **WHEN** the user sets an idea-stage listing's lifecycle status to `Rejected`
- **THEN** the idea remains visible in the navigation tree with its inactive treatment
- **AND** remains openable, reviewable, and reactivatable by returning the status to `Draft`

#### Scenario: User archives an idea
- **WHEN** the user confirms archive for an idea-stage listing from the inspector
- **THEN** FusionCanvas preserves the idea with its topic and idea-stage metadata through the reversible archive behavior

#### Scenario: User reviews unprocessed ideas
- **WHEN** the user activates the `Idea` workflow-stage filter together with the `Draft` lifecycle-status filter
- **THEN** the navigation tree lists exactly the unprocessed ideas with their parent topic context preserved
