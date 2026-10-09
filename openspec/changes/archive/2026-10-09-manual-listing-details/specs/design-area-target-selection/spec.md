## ADDED Requirements

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
