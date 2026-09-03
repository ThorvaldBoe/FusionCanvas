## Purpose

Defines the focused Blueprint Offering list and its navigation responsibilities.

## Requirements

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
