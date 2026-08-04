# Concept Refinement

## ADDED Requirements

### Requirement: Concept stage surface hosts the SLL generation section
FusionCanvas SHALL present an SLL generation section inside the existing Concept stage surface of the Item document, directly below the refinement section, and SHALL follow the visibility of the Concept stage surface. The section SHALL be hidden for Idea, Design, and Listing stage surfaces and SHALL be disabled whenever the Concept fields are read-only.

#### Scenario: Section appears with the Concept stage surface
- **WHEN** an Item document shows the Concept stage surface
- **THEN** the SLL generation section appears below the refinement section
- **AND** it is not shown for Idea, Design, or Listing stage surfaces

#### Scenario: Earlier-stage review disables the SLL section
- **WHEN** the Item's persisted current stage is beyond Concept and the user reviews the Concept stage read-only
- **THEN** the SLL generation section actions are disabled and no SLL AI request can be started
