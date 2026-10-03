## MODIFIED Requirements

### Requirement: The SLL section is theme coherent and accessible
FusionCanvas SHALL make the Generate and regenerate actions keyboard reachable in a logical order after the Concept fields and refinement section, SHALL give the actions and the rendered SLL meaningful accessible names, SHALL present one compact explanatory information box with an info icon and the approved SLL explanation whenever the Concept-stage SLL section is visible, and SHALL resolve the information box, busy, disabled, error, and rendering states from shared application theme resources. The information box SHALL be static and SHALL NOT change SLL generation availability, persistence, or action behavior.

#### Scenario: Keyboard operation
- **WHEN** the user navigates the Concept surface without a pointer
- **THEN** the Generate and regenerate actions are reachable in a predictable order after the refinement actions
- **AND** the explanatory information box is available in the same SLL section without adding a new interactive stop

#### Scenario: Theme coherence
- **WHEN** the application appearance changes while the Concept surface is visible
- **THEN** the SLL section and its information box adopt the active theme and busy, disabled, and error states remain distinguishable

#### Scenario: SLL explanation is visible during normal Concept work
- **WHEN** the Concept-stage SLL section is visible for an editable Item
- **THEN** an information box with a recognizable info icon and the text “SLL stands for Symbolic Layout Languate. It's a rough visual sketch of the proposed design.” is visible before the SLL actions

#### Scenario: SLL explanation remains available when generation is blocked
- **WHEN** the SLL section is visible but generation is unavailable because the triangle is incomplete, SLL AI is unavailable, the Item is read-only, or an SLL is stale
- **THEN** the same information box remains visible
- **AND** the existing actionable disabled-state guidance and action enabled states remain unchanged

#### Scenario: Explanation is accessible without a separate interaction
- **WHEN** assistive technology inspects the visible Concept-stage SLL section
- **THEN** the information box exposes one meaningful accessible description for the explanation and info icon
- **AND** the static box and icon do not add a separate focusable or clickable action
