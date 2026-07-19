## ADDED Requirements

### Requirement: Navigator distinguishes the current stage from the active view stage
The workflow stage navigator SHALL expose a distinct emphasis for the active view stage that is separate from the current stage emphasis, keeping the item's current stage the most prominent stage indication.

#### Scenario: User reviews an earlier stage
- **WHEN** the active item's current workflow stage is `Design`
- **AND** the user selects the available `Idea` stage
- **THEN** the `Idea` stage is marked as the active view stage
- **AND** the `Design` stage remains marked as the current stage
- **AND** the current stage emphasis remains more prominent than the active view emphasis

#### Scenario: Item stage changes the current emphasis
- **WHEN** the active item's current workflow stage changes from `Concept` to `Design`
- **THEN** the navigator moves the current stage emphasis to `Design`
- **AND** the active view stage becomes `Design`

#### Scenario: Item opens at its current stage
- **WHEN** an item with workflow stage `Concept` becomes the active item
- **THEN** the `Concept` stage is marked as both current and active view
