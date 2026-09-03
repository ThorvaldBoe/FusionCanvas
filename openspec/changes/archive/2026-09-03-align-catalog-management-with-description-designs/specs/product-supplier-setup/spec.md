## MODIFIED Requirements

### Requirement: Offering details disclose dependent controls in a logical order
The Blueprint Offering overview SHALL prioritize Offering identity, lifecycle or readiness status, and fulfillment context; show one concise Basics region followed by one consolidated setup region; and route complex relationships to focused management surfaces. Advanced external identifiers and provider references SHALL remain secondary to user-facing setup information, and the overview SHALL expose no duplicate primary save action.

#### Scenario: User reviews an Offering overview
- **WHEN** a Blueprint Offering overview is active
- **THEN** the heading identifies the Offering and displays its lifecycle or readiness status without requiring Basics to be expanded
- **AND** Basics identifies the actual fixed Print Provider or Provider-Network context
- **AND** setup summaries report the configured state of Variants, Design Areas, and Mockup Templates
- **AND** each setup summary provides one focused management route

#### Scenario: Offering overview preserves the approved composition
- **WHEN** a Blueprint Offering overview is active
- **THEN** FusionCanvas presents Offering identity and status before Offering Basics and one consolidated setup region
- **AND** the setup region groups Variants, Design Areas, and Mockup Templates with their status or counts and corresponding management actions
- **AND** does not scatter those routes among unrelated forms or present more than one primary save action for the same Basics draft

#### Scenario: User changes a fixed Print Provider
- **WHEN** the user edits Basics for a fixed-provider Offering
- **THEN** FusionCanvas allows selection of an active Print Provider belonging to the same Store
- **AND** provides an explicit adjacent route to create a Store-owned Print Provider when needed
- **AND** saving updates the Offering's stable Print Provider identity and refreshed fulfillment summary

#### Scenario: User reviews incomplete setup
- **WHEN** an Offering has no sellable Variants, no Design Areas, or no Mockup Templates
- **THEN** the overview identifies each incomplete area without presenting its full creation form
- **AND** the corresponding focused route remains the clear next action when prerequisites permit

#### Scenario: User reviews blocked setup
- **WHEN** a focused setup action cannot proceed because a prerequisite is missing
- **THEN** FusionCanvas explains the prerequisite and routes the user to the safe preceding task
- **AND** does not silently create placeholder catalog relationships

#### Scenario: User reviews Provider identity
- **WHEN** an Offering has a fixed Print Provider supplied through Printify catalog data
- **THEN** the overview labels the fulfillment partner as the Provider and Printify as the integration or catalog source where that source is relevant
- **AND** external identifiers remain in an Advanced or otherwise secondary disclosure

#### Scenario: User reviews a Provider-Network Offering
- **WHEN** the selected Offering uses Printify Choice or another Provider Network
- **THEN** the overview keeps the variable-network warning visible near fulfillment context or setup status
- **AND** does not show or fabricate a fixed Provider name

#### Scenario: User returns from focused management
- **WHEN** the user completes or cancels a focused management task
- **THEN** FusionCanvas returns to the same Offering overview with refreshed summaries and a meaningful focus target
- **AND** does not lose the active Store, Blueprint, or Offering context
