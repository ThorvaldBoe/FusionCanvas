## MODIFIED Requirements

### Requirement: Blueprint detail presents a focused Offering list
FusionCanvas SHALL present Blueprint Offerings in a Blueprint-scoped virtual grid that identifies the current Blueprint, summarizes each Offering, and does not expose Variant, Design Area, or Mockup Template editing controls in the list. The grid SHALL present concise identity, fulfillment context, lifecycle status, setup counts or completeness, and readiness information in aligned columns. Full readiness summary and guidance SHALL remain available for each Offering without requiring expanded row height.

#### Scenario: User opens a Blueprint with Offerings
- **WHEN** the user opens a Blueprint that has one or more active Blueprint Offerings
- **THEN** FusionCanvas shows only Offerings belonging to that Blueprint and Store in aligned grid columns
- **AND** each row provides concise identity, fulfillment-partner or Provider-Network context, lifecycle status, setup counts or completeness, and readiness summary
- **AND** the grid does not expose complex catalog relationships for editing

#### Scenario: User reviews detailed readiness guidance
- **WHEN** a row has a readiness summary and one or more readiness guidance messages
- **THEN** the grid keeps the row compact and makes the full summary and guidance available through the row's accessible help text or tooltip
- **AND** the concise readiness value remains visible in its column

#### Scenario: User opts into archived Offerings
- **WHEN** the user checks **Show archived Blueprint Offerings**
- **THEN** archived Offerings appear in the same Blueprint-scoped grid alongside active Offerings
- **AND** archived rows identify themselves as archived
- **AND** unchecking the control removes archived Offerings from the grid

#### Scenario: User opens a Blueprint without Offerings
- **WHEN** the user opens a Blueprint that has no active Blueprint Offerings
- **THEN** FusionCanvas shows a useful empty state explaining that an Offering connects the Blueprint to fulfillment setup
- **AND** provides one explicit route to add an Offering for that Blueprint

#### Scenario: User reviews an archived Store
- **WHEN** the selected Store is archived
- **THEN** FusionCanvas presents the Blueprint Offering grid read-only
- **AND** does not enable Offering creation or mutation actions

### Requirement: Offering list owns add and open routes
FusionCanvas SHALL provide one explicit Blueprint-scoped route to begin a new Offering draft and a visible, one-click **Open** action for each existing Offering. Activating the Open action by pointer or keyboard SHALL open that Offering's overview without changing the current Blueprint or Store context. The grid SHALL NOT add a separate selection step before opening an Offering.

#### Scenario: User starts a new Offering
- **WHEN** the user invokes the add-Offering route from a Blueprint
- **THEN** FusionCanvas starts a draft already scoped to that Blueprint and Store
- **AND** places keyboard focus in the first required field
- **AND** does not persist the Offering until the draft is valid and explicitly saved

#### Scenario: User opens an Offering
- **WHEN** the user activates an Offering row's Open action by pointer or keyboard
- **THEN** FusionCanvas opens that Offering's overview without changing the current Blueprint or Store context
- **AND** does not require a separate row-selection or second Offering selector

#### Scenario: User leaves a meaningful Offering draft
- **WHEN** the user attempts to change Blueprint, Store, tab, or close the editor with meaningful unsaved Offering input
- **THEN** FusionCanvas offers to discard the draft or keep editing
- **AND** keep-editing preserves the draft, selection, and focus
