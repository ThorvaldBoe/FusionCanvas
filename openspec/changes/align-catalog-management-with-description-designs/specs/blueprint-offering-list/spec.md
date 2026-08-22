## MODIFIED Requirements

### Requirement: Blueprint detail presents a focused Offering list
FusionCanvas SHALL present Blueprint Offerings as the dominant content of a Blueprint-scoped page that identifies the current Blueprint, keeps Blueprint editing available through a compact Basic section on the same page, summarizes each Offering from normalized catalog state, and does not expose Variant, Design Area, or Mockup Template editing controls in the list. Blueprint editing SHALL NOT require or open a separate Blueprint window.

#### Scenario: User opens a Blueprint with Offerings
- **WHEN** the user opens a Blueprint that has one or more active Blueprint Offerings
- **THEN** FusionCanvas shows only Offerings belonging to that Blueprint and Store
- **AND** the Offering collection remains the dominant page content rather than being displaced by the Blueprint form
- **AND** each item provides concise identity, fulfillment-partner or Provider-Network context, lifecycle or readiness status, Variant count, Design Area count, and Mockup Template count from authoritative normalized state
- **AND** complex catalog relationships are not editable from the list

#### Scenario: User opens Blueprint Basics
- **WHEN** the user invokes the Blueprint Basic section
- **THEN** FusionCanvas reveals the Blueprint's basic editable fields on the same Blueprint page
- **AND** keeps the Offering collection and current Blueprint context available
- **AND** does not open a separate Blueprint window or duplicate Blueprint editing controls elsewhere on the page

#### Scenario: User opens a Blueprint without Offerings
- **WHEN** the user opens a Blueprint that has no active Blueprint Offerings
- **THEN** FusionCanvas shows a useful empty state explaining that an Offering connects the Blueprint to fulfillment setup
- **AND** provides one explicit route to add an Offering for that Blueprint
- **AND** retains the compact Blueprint Basic section without allowing it to dominate the empty-state guidance

#### Scenario: User reviews an archived Store
- **WHEN** the selected Store is archived
- **THEN** FusionCanvas presents Blueprint Basics and the Blueprint Offering list read-only
- **AND** does not enable Blueprint, Offering creation, or Offering mutation actions

### Requirement: Catalog management uses progressive disclosure
The Products & fulfillment editor SHALL present catalog management as a Blueprint overview, Blueprint detail with a compact same-page Basic section and focused Blueprint Offering list, a concise Blueprint Offering overview, and separate focused management surfaces for Variants, Design Areas, and Mockup Templates. It SHALL keep the current Store, Blueprint, and Offering context visible while showing only the controls needed for the active task.

#### Scenario: User opens the catalog editor
- **WHEN** the user opens Products & fulfillment for an active Store
- **THEN** the editor shows a Blueprint overview with the Blueprint list, an empty state when none exist, and one primary new-Blueprint action
- **AND** it does not show Offering, Variant, Design Area, or Mockup Template forms until the relevant context is opened

#### Scenario: User opens a Blueprint
- **WHEN** the user selects a Blueprint from the overview
- **THEN** the editor shows Blueprint identity, its compact Basic section, and its focused Blueprint Offering list on one page
- **AND** the primary Offering creation route is scoped to that Blueprint
- **AND** it does not open a separate Blueprint editor or expose the full Offering relationship graph on the Blueprint page

#### Scenario: User opens a Blueprint Offering
- **WHEN** the user opens a Blueprint Offering from the Blueprint-scoped list
- **THEN** the editor shows a concise Offering overview with Basics, lifecycle or readiness status, setup summaries, and focused routes to manage Variants, Design Areas, and Mockup Templates
- **AND** it does not expose all Option Values, concrete Variants, Design Area compatibility, and template mappings in one giant form

#### Scenario: User opens a focused management surface
- **WHEN** the user chooses to manage Variants, Design Areas, or Mockup Templates
- **THEN** FusionCanvas opens the corresponding Offering-scoped surface with a path back to the same Offering overview
- **AND** preserves the Store, Blueprint, and Offering identities throughout the transition
