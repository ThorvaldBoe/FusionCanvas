# Design Area Target Selection

## Purpose

Defines how an editable Item at Design selects zero or more configured printable areas from its own Store and how those targets respect workflow editability.
## Requirements
### Requirement: Design stage supports optional Store design targets
FusionCanvas SHALL let an editable Item at Design select zero or more configured Offering Placeholders from its own Store without requiring a target to continue design work.

#### Scenario: User designs without configured targets
- **WHEN** an editable Item reaches Design with no selected Offering Placeholders
- **THEN** FusionCanvas presents the existing design-file workflow
- **AND** does not block design-file work or progression because no target is selected

#### Scenario: User selects multiple compatible Placeholders
- **WHEN** the user selects two or more Offering Placeholders configured for the Item's Store
- **THEN** FusionCanvas persists the complete selected set atomically
- **AND** each selected Placeholder is displayed as design guidance after reload

#### Scenario: User attempts cross-Store target selection
- **WHEN** a target request refers to an Offering Placeholder from another Store
- **THEN** FusionCanvas rejects the request
- **AND** preserves the Item's prior selected targets

### Requirement: Target selection respects workflow editability
FusionCanvas SHALL expose selected Offering Placeholders read-only whenever the Item's active Design content is read-only.

#### Scenario: User reviews Design from a protected context
- **WHEN** Design-stage editing is unavailable because the Item is protected or an earlier stage is being reviewed
- **THEN** FusionCanvas shows any persisted Offering Placeholder targets as read-only guidance
- **AND** does not commit a target mutation


### Requirement: Changing an Item's selected Offering migrates manual Listing Details
When an Item has an existing selected Blueprint Offering and active manual Listing Details, FusionCanvas SHALL route a requested change to a different Blueprint Offering through the explicit migration review and atomic migration defined by the `manual-listing-details` capability. The existing direct replacement behavior SHALL remain available when the Item has no manual Listing Details, and first-time Offering selection SHALL attach the Offering without a migration review.

#### Scenario: Item has manual Listing Details
- **WHEN** the user selects a different active same-Store Offering for an Item with active manual Listing Details
- **THEN** Design opens the migration review before applying the replacement
- **AND** cancellation preserves the selected Offering and all Offering-bound Design data

#### Scenario: Item has no manual Listing Details
- **WHEN** the user selects a different active same-Store Offering for an Item with no manual Listing Details
- **THEN** the existing Design configuration replacement behavior remains available

#### Scenario: User confirms migration from Design
- **WHEN** the user confirms the migration from the Design surface
- **THEN** the selected Offering and manual Listing Details transition together atomically
- **AND** the Design surface refreshes from the confirmed destination configuration

#### Scenario: User selects the first Offering from Design
- **WHEN** an Item with saved manual Listing Details has no selected Offering and the user selects its first active same-Store Offering
- **THEN** Design attaches the Offering without a migration review
- **AND** retains the Item's saved listing copy and leaves new setup-specific prices and shipping terms blank
