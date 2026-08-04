## ADDED Requirements

### Requirement: Design tool presents selected printable-area guidance
FusionCanvas SHALL present an Item's selected Store design-area targets in the Design Stage Tool alongside its existing design-file controls.

#### Scenario: Item opens with selected targets
- **WHEN** an Item with persisted selected printable areas opens at Design
- **THEN** the Design Stage Tool displays each target's product, fulfillment offering, position, decoration method, and dimensions
- **AND** existing design-file import, preview, export, and remove behavior remains available according to editability

#### Scenario: Selected Choice target is displayed
- **WHEN** an Item has a selected target from a Printify Choice network offering
- **THEN** the Design Stage Tool displays the target and its network consistency warning
- **AND** it does not display a fabricated fixed provider name
