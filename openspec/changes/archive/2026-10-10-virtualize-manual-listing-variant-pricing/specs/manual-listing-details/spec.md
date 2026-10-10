## ADDED Requirements

### Requirement: Variant pricing uses an aligned virtual grid
The Manual Listing Details pricing area SHALL present active Variant names, selling prices, and fulfillment costs in aligned columns using a virtualized grid. Each price and cost SHALL remain an inline-editable field bound to the corresponding Variant identity, and the grid SHALL follow the existing `CanEditFulfillmentTerms` policy. The grid SHALL preserve the existing amount validation, inline error feedback, dirty tracking, and Save details behavior.

#### Scenario: User edits Variant pricing
- **WHEN** the user enters a selling price or fulfillment cost in a Variant row
- **THEN** the value updates the matching Variant row model
- **AND** validation, gross profit summary, dirty tracking, and Save details continue to use the existing Listing Details behavior

#### Scenario: User moves between realized price editors with the keyboard
- **WHEN** the user focuses a price editor and presses Tab
- **THEN** focus advances through the fulfillment cost and subsequent realized Variant price editors in row order

#### Scenario: Listing details cannot edit fulfillment terms
- **WHEN** the existing Listing Details policy sets `CanEditFulfillmentTerms` to false
- **THEN** all price and cost editors in the grid are disabled
- **AND** existing listing guidance, save status, and other Listing Details controls remain available according to their existing policies
