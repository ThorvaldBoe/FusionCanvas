## Purpose

Defines the focused Blueprint Offering list and its navigation responsibilities.

## Requirements

### Requirement: Blueprint overview is archive-first and active by default
The Store Editor SHALL show only active Blueprints in the Blueprint overview by default. The overview SHALL provide an explicit, opt-in **Show archived Blueprints** checkbox that includes archived Blueprints in the same Store-scoped list when checked. Archived rows SHALL be visually and textually distinguishable from active rows.

#### Scenario: User opens the Blueprint overview
- **WHEN** the user opens the catalog overview for an active Store
- **THEN** only active Blueprints are listed
- **AND** the **Show archived Blueprints** checkbox is unchecked by default
- **AND** no permanent-delete action is exposed for an active Blueprint

#### Scenario: User opts into archived Blueprints
- **WHEN** the user checks **Show archived Blueprints**
- **THEN** archived Blueprints from the selected Store appear in the list
- **AND** active Blueprints remain visible
- **AND** archived rows identify themselves as archived

#### Scenario: User hides archived Blueprints again
- **WHEN** the user unchecks **Show archived Blueprints**
- **THEN** archived Blueprints leave the overview
- **AND** the current active Blueprint selection remains valid when one exists

### Requirement: Blueprint lifecycle actions follow the record state
The Store Editor SHALL expose **Archive Blueprint** as the only destructive lifecycle action for an active Blueprint. A permanently destructive **Delete permanently** action SHALL be available only after the user has selected an archived Blueprint through the archived-visibility control, and SHALL require explicit confirmation.

#### Scenario: User reviews an active Blueprint
- **WHEN** an active Blueprint is selected
- **THEN** the Blueprint detail presents Archive Blueprint
- **AND** it does not present Delete permanently

#### Scenario: User reviews an archived Blueprint
- **WHEN** an archived Blueprint is selected while archived Blueprints are shown
- **THEN** the detail identifies the Blueprint as archived
- **AND** presents Delete permanently as an archived-only action
- **AND** does not present Archive Blueprint as an available action

#### Scenario: User cancels permanent deletion
- **WHEN** the user opens the archived Blueprint's permanent-delete confirmation and cancels
- **THEN** the Blueprint and every related record remain unchanged

### Requirement: Blueprint detail presents a focused Offering list
FusionCanvas SHALL present Blueprint Offerings as a Blueprint-scoped list that identifies the current Blueprint, summarizes each Offering, and does not expose Variant, Design Area, or Mockup Template editing controls in the list.

#### Scenario: User opens a Blueprint with Offerings
- **WHEN** the user opens a Blueprint that has one or more active Blueprint Offerings
- **THEN** FusionCanvas shows only Offerings belonging to that Blueprint and Store
- **AND** each row or item provides a concise identity, fulfillment-partner or Provider-Network context, lifecycle status, and relevant setup counts or completeness summary
- **AND** complex catalog relationships are not editable from the list

#### Scenario: User opens a Blueprint without Offerings
- **WHEN** the user opens a Blueprint that has no active Blueprint Offerings
- **THEN** FusionCanvas shows a useful empty state explaining that an Offering connects the Blueprint to fulfillment setup
- **AND** provides one explicit route to add an Offering for that Blueprint

#### Scenario: User reviews an archived Store
- **WHEN** the selected Store is archived
- **THEN** FusionCanvas presents the Blueprint Offering list read-only
- **AND** does not enable Offering creation or mutation actions

### Requirement: Offering list owns add and open routes
FusionCanvas SHALL provide one explicit Blueprint-scoped route to begin a new Offering draft and one clear interaction for opening an existing Offering overview.

#### Scenario: User starts a new Offering
- **WHEN** the user invokes the add-Offering route from a Blueprint
- **THEN** FusionCanvas starts a draft already scoped to that Blueprint and Store
- **AND** places keyboard focus in the first required field
- **AND** does not persist the Offering until the draft is valid and explicitly saved

#### Scenario: User opens an Offering
- **WHEN** the user activates an Offering item by pointer or keyboard
- **THEN** FusionCanvas opens that Offering's overview without changing the current Blueprint or Store context
- **AND** does not require a second Offering selector

#### Scenario: User leaves a meaningful Offering draft
- **WHEN** the user attempts to change Blueprint, Store, tab, or close the editor with meaningful unsaved Offering input
- **THEN** FusionCanvas offers to discard the draft or keep editing
- **AND** keep-editing preserves the draft, selection, and focus

### Requirement: Provider wording distinguishes fulfillment from integration source
FusionCanvas SHALL use Provider for the actual fulfillment partner and SHALL distinguish that partner from Printify as an integration or catalog source.

#### Scenario: Fixed-provider Offering appears in the list
- **WHEN** a Blueprint Offering is fulfilled by SwiftPOD, Monster Digital, or another fixed Print Provider
- **THEN** FusionCanvas identifies that actual Print Provider as the Provider
- **AND** does not label Printify as the Provider merely because Printify supplied catalog data

#### Scenario: Provider-Network Offering appears in the list
- **WHEN** a Blueprint Offering uses Printify Choice or another Provider Network without one fixed Print Provider
- **THEN** FusionCanvas identifies the Provider-Network context and its variability
- **AND** does not fabricate a fixed Provider identity
