## ADDED Requirements

### Requirement: The Idea-stage host exposes Ideation as an auxiliary action
The Stage Tool Host SHALL expose `Ideation…` as a stage-specific auxiliary action rather than as a replacement hosted editor when the active view is Idea and the context resolves to an active niche.

#### Scenario: Supported Idea context is active
- **WHEN** the active view is Idea for a niche, group, or Item with an active parent topic
- **THEN** the tool area shows one `Ideation…` action in a consistent stage-action location
- **AND** the existing hosted Idea editor or topic surface remains selected

#### Scenario: Another stage is active
- **WHEN** Concept, Design, or Listing is the active view
- **THEN** the Ideation action is not presented as an action for that stage

#### Scenario: Placeholder access is unavailable
- **WHEN** the Idea-stage context is supported but placeholder AI access is unavailable
- **THEN** the Ideation action remains visible but disabled
- **AND** unavailable guidance identifies the missing placeholder access

#### Scenario: Dialog closes
- **WHEN** an owned Ideation dialog closes
- **THEN** the host preserves the prior active tool selection and document context
- **AND** returns focus to the Ideation action when it remains available
