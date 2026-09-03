## MODIFIED Requirements

### Requirement: Design tool presents selected printable-area guidance
FusionCanvas SHALL present an Item's selected Store Offering Placeholder targets in the Design Stage Tool alongside its existing design-file controls and SHALL use Placeholder terminology with explanatory helper text where needed.

#### Scenario: Item opens with selected targets
- **WHEN** an Item with persisted selected Offering Placeholders opens at Design
- **THEN** the Design Stage Tool displays each target's Blueprint, Blueprint Offering, position, decoration method, and dimensions
- **AND** existing design-file import, preview, export, and remove behavior remains available according to editability

#### Scenario: Selected Choice target is displayed
- **WHEN** an Item has a selected Placeholder from a Printify Choice Provider-Network offering
- **THEN** the Design Stage Tool displays the target and its network-consistency warning
- **AND** does not display a fabricated fixed Print Provider name
