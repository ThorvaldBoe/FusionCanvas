## MODIFIED Requirements

### Requirement: Catalog management uses progressive disclosure
The Products & fulfillment editor SHALL present catalog management as a Blueprint overview, Blueprint detail with a focused Blueprint Offering list, a concise Blueprint Offering overview, and separate focused management surfaces for Variants, Design Areas, and Mockup Templates. It SHALL keep the current Store, Blueprint, and Offering context visible while showing only the controls needed for the active task.

#### Scenario: User opens the catalog editor
- **WHEN** the user opens Products & fulfillment for an active Store
- **THEN** the editor shows a Blueprint overview with the Blueprint list, an empty state when none exist, and one primary new-Blueprint action
- **AND** it does not show Offering, Variant, Design Area, or Mockup Template forms until the relevant context is opened

#### Scenario: User opens a Blueprint
- **WHEN** the user selects a Blueprint from the overview
- **THEN** the editor shows Blueprint identity and its focused Blueprint Offering list
- **AND** the primary Offering creation route is scoped to that Blueprint
- **AND** it does not expose the full Offering relationship graph on the Blueprint page

#### Scenario: User opens a Blueprint Offering
- **WHEN** the user opens a Blueprint Offering from the Blueprint-scoped list
- **THEN** the editor shows a concise Offering overview with Basics, lifecycle or readiness status, setup summaries, and focused routes to manage Variants, Design Areas, and Mockup Templates
- **AND** it does not expose all Option Values, concrete Variants, Design Area compatibility, and template mappings in one giant form

#### Scenario: User opens a focused management surface
- **WHEN** the user chooses to manage Variants, Design Areas, or Mockup Templates
- **THEN** FusionCanvas opens the corresponding Offering-scoped surface with a path back to the same Offering overview
- **AND** preserves the Store, Blueprint, and Offering identities throughout the transition

### Requirement: Offering details disclose dependent controls in a logical order
The Blueprint Offering overview SHALL prioritize Offering identity and fulfillment context, show concise setup summaries and status, and route complex relationships to focused management surfaces. Advanced external identifiers and provider references SHALL remain secondary to user-facing setup information.

#### Scenario: User reviews an Offering overview
- **WHEN** a Blueprint Offering overview is active
- **THEN** Basics identifies the Offering and its actual fixed Print Provider or Provider-Network context
- **AND** setup summaries report the configured state of Variants, Design Areas, and Mockup Templates
- **AND** each setup summary provides one focused management route

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
