## ADDED Requirements

### Requirement: Navigator displays the core workflow stages
The workflow stage navigator SHALL display `Idea`, `Concept`, `Design`, and `Listing` as the ordered primary workflow stages for the active item.

#### Scenario: Active item has workflow context
- **WHEN** a document item with workflow context is active
- **THEN** the navigator displays the stages in the order `Idea`, `Concept`, `Design`, `Listing`

#### Scenario: No item is active
- **WHEN** no document item is active
- **THEN** the navigator does not display misleading workflow progress for a fake or unavailable item

### Requirement: Navigator distinguishes current, available, and unavailable stages
The workflow stage navigator SHALL expose a distinct visual state for the current stage, available non-current stages, and unavailable stages.

#### Scenario: Current stage is concept
- **WHEN** the active item's current workflow stage is `Concept`
- **THEN** the `Concept` stage is marked as current
- **AND** the navigator distinguishes it from the other stages

#### Scenario: Future stage is unavailable
- **WHEN** the active item has not reached the `Design` stage
- **THEN** the `Design` stage is marked unavailable
- **AND** the `Listing` stage is marked unavailable

### Requirement: Navigator enables only available stage navigation
The workflow stage navigator SHALL allow navigation only for stages that are available for the active item.

#### Scenario: User selects an available previous stage
- **WHEN** the active item is currently at `Concept`
- **AND** the `Idea` stage is available
- **AND** the user selects `Idea`
- **THEN** FusionCanvas opens the `Idea` stage view for the same active item

#### Scenario: User selects an unavailable future stage
- **WHEN** the active item is currently at `Concept`
- **AND** the `Design` stage is unavailable
- **AND** the user selects `Design`
- **THEN** FusionCanvas does not navigate away from the current stage view

### Requirement: Navigator updates from active document context
The workflow stage navigator SHALL update when the active item or active document tab changes.

#### Scenario: User switches to a tab for another item
- **WHEN** the user switches from a tab whose active item is at `Idea` to a tab whose active item is at `Design`
- **THEN** the navigator updates to mark `Design` as the current stage
- **AND** the navigator uses the stage availability for the newly active item

#### Scenario: Active item stage changes
- **WHEN** the active item's workflow stage changes from `Idea` to `Concept`
- **THEN** the navigator updates to mark `Concept` as current
- **AND** the navigator no longer marks `Idea` as current

### Requirement: Navigator preserves archive as a related state
The workflow stage navigator SHALL preserve archive or inactive work as a related state without replacing the primary four-stage workflow.

#### Scenario: Active item is archived
- **WHEN** the active item is archived, rejected, retracted, or otherwise inactive
- **THEN** the navigator still presents the primary stages as `Idea`, `Concept`, `Design`, and `Listing`
- **AND** FusionCanvas exposes the inactive or archive state separately from the primary stage row

#### Scenario: User reviews archived work
- **WHEN** the active item is archived and has available stage history
- **THEN** the user can review available stages for that item without the archive state being treated as the next primary workflow stage
