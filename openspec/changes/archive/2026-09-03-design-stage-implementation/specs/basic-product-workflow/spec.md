# Basic Product Workflow

## Purpose

Updates the accepted Design tool behavior to reflect that the Design stage is anchored to a mandatory listing configuration and presents a color working set and a row × design-area slot grid, replacing the previous printed design-area-target guidance.

## RENAMED Requirements

- FROM: `### Requirement: Design tool presents selected printable-area guidance`
  TO: `### Requirement: Design tool presents listing configuration guidance`

## MODIFIED Requirements

### Requirement: Design tool presents listing configuration guidance
FusionCanvas SHALL present an Item's selected listing configuration in the Design Stage Tool, showing its design areas as the columns of the final-design slot grid, alongside the color working set and Supporting images area, and it SHALL continue to honor Design-file import, preview, export, and remove behavior according to editability.

#### Scenario: Item opens with selected configuration
- **WHEN** an Item with a persisted listing configuration opens at Design
- **THEN** the Design Stage Tool displays the configuration, the design areas as slot-grid columns, the color working set, and any filled slots and Supporting images
- **AND** existing Design-file import, preview, export, and remove behavior remains available according to editability

#### Scenario: No configuration is selected
- **WHEN** an Item opens at Design with no listing configuration
- **THEN** FusionCanvas shows the Supporting images area and a configuration-selection prompt
- **AND** it does not show the final-design slot grid

#### Scenario: Selected Choice configuration is displayed
- **WHEN** an Item has a listing configuration from a Printify Choice network offering
- **THEN** the Design Stage Tool displays the configuration and its network consistency status
- **AND** it does not display a fabricated fixed provider name
