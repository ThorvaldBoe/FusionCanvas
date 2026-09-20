## MODIFIED Requirements

### Requirement: Blueprint detail presents a focused Offering list
FusionCanvas SHALL present Blueprint Offerings as a Blueprint-scoped list that identifies the current Blueprint, summarizes each Offering, and does not expose Variant, Design Area, or Mockup Template editing controls in the list. The list SHALL show only active Offerings by default and SHALL provide an explicit, opt-in **Show archived Blueprint Offerings** checkbox scoped to the selected Blueprint and Store. Archived Offering rows SHALL be visually and textually distinguishable from active rows.

#### Scenario: User opens a Blueprint with Offerings
- **WHEN** the user opens a Blueprint that has one or more active Blueprint Offerings
- **THEN** FusionCanvas shows only active Offerings belonging to that Blueprint and Store
- **AND** the **Show archived Blueprint Offerings** checkbox is unchecked by default
- **AND** each row provides a concise identity, fulfillment-partner or Provider-Network context, lifecycle status, and relevant setup counts or completeness summary
- **AND** complex catalog relationships are not editable from the list

#### Scenario: User opts into archived Blueprint Offerings
- **WHEN** the user checks **Show archived Blueprint Offerings**
- **THEN** archived Offerings belonging to the current Blueprint and Store appear alongside active Offerings
- **AND** active Offerings remain visible
- **AND** archived rows identify themselves as archived

#### Scenario: User hides archived Blueprint Offerings again
- **WHEN** the user unchecks **Show archived Blueprint Offerings**
- **THEN** archived Offerings leave the list
- **AND** an active Offering selection remains selected when one exists
- **AND** an archived selection is cleared or replaced with a valid active selection

#### Scenario: User opens a Blueprint without active Offerings
- **WHEN** the user opens a Blueprint that has no active Blueprint Offerings
- **THEN** FusionCanvas shows a useful empty state explaining that an Offering connects the Blueprint to fulfillment setup
- **AND** provides one explicit route to add an Offering for that Blueprint
- **AND** the archived filter remains available when archived Offerings exist

#### Scenario: User reviews an archived Store
- **WHEN** the selected Store is archived
- **THEN** FusionCanvas presents the Blueprint Offering list read-only
- **AND** does not enable Offering creation, restore, archive, or permanent-delete actions

## ADDED Requirements

### Requirement: Offering lifecycle actions follow the record state
The Store Editor SHALL expose archive, restore, and permanent-delete actions according to the selected Blueprint Offering state. Active Offerings SHALL expose the existing reversible archive workflow but no permanent-delete action. Archived Offerings SHALL expose **Restore Blueprint Offering** and **Delete permanently** only while the selected Store and Blueprint are editable. Permanent deletion SHALL require explicit confirmation.

#### Scenario: User reviews an active Offering
- **WHEN** an active Offering is selected
- **THEN** the Offering detail presents **Archive Blueprint Offering**
- **AND** it does not present **Restore Blueprint Offering** or **Delete permanently**

#### Scenario: User reviews an archived Offering
- **WHEN** an archived Offering is selected while archived Offerings are shown
- **THEN** the detail identifies the Offering as archived and read-only
- **AND** presents **Restore Blueprint Offering** and **Delete permanently**
- **AND** does not present **Archive Blueprint Offering**

#### Scenario: User cancels Offering permanent deletion
- **WHEN** the user opens the archived Offering's permanent-delete confirmation and cancels
- **THEN** the Offering, its descendants, and all external relationships remain unchanged

#### Scenario: User receives a protected-reference deletion block
- **WHEN** the user requests permanent deletion for an archived Offering that has protected external references
- **THEN** the confirmation or recoverable error identifies each blocking record type and name where available
- **AND** explains that the references must be cleared or remapped before deletion
- **AND** the Offering remains selected and unchanged
