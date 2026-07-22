## ADDED Requirements

### Requirement: Navigator review preserves current-stage edit ownership
The workflow navigator SHALL treat selection of an earlier available stage as review-only and SHALL leave edit ownership with the persisted current stage.

#### Scenario: Earlier stage is selected
- **WHEN** Design is current and the user selects Concept
- **THEN** Concept becomes the active view
- **AND** Design remains the current stage
- **AND** Concept content is read-only

#### Scenario: User needs to edit an earlier stage
- **WHEN** the user activates explicit regression from Design
- **THEN** Concept becomes current, active, and editable
- **AND** downstream data remains preserved

### Requirement: Navigator movement eligibility follows Item lifecycle policy
The navigator SHALL keep view navigation distinct from stage movement and SHALL reflect Published, Rejected, and archived movement restrictions.

#### Scenario: Published Item is active
- **WHEN** the Item is Published at Listing
- **THEN** available stages remain reviewable according to persisted stage history
- **AND** advance and regress controls remain unavailable

