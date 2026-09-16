## ADDED Requirements

### Requirement: Committed upstream creative changes mark the current SLL stale and offer explicit handling
FusionCanvas SHALL mark an existing SLL stale whenever a committed change alters the Item's original Idea, Concept idea, Phrase, or Graphic direction after that SLL was generated. The Concept surface SHALL show a non-modal warning with `Reset SLL` and `Keep for reference` actions. Keeping SHALL retain the stale SLL and warning; resetting SHALL require confirmation and remove the persisted SLL. Regenerating SLL SHALL replace it and clear stale state.

#### Scenario: Concept content changes after SLL generation
- **WHEN** a committed Concept idea, Phrase, or Graphic direction change differs from the values used by the current SLL
- **THEN** the SLL is marked stale
- **AND** an inline warning explains that the Concept changed after generation

#### Scenario: Original Idea changes after SLL generation
- **WHEN** a committed original Idea change differs from the value used by the current SLL
- **THEN** the SLL is marked stale under the same warning and handling rules

#### Scenario: User keeps stale SLL for reference
- **WHEN** the user activates Keep for reference
- **THEN** the SLL remains persisted and visible with its stale warning
- **AND** artwork generation does not include it

#### Scenario: User requests SLL reset
- **WHEN** the user activates Reset SLL
- **THEN** FusionCanvas asks for confirmation before deleting the persisted SLL
- **AND** cancellation preserves the SLL and warning

#### Scenario: User confirms SLL reset
- **WHEN** the user confirms Reset SLL and persistence succeeds
- **THEN** the persisted SLL and stale warning are removed

#### Scenario: User regenerates SLL
- **WHEN** SLL regeneration succeeds using the current complete Design Triangle
- **THEN** the new SLL replaces the stale SLL
- **AND** stale state and warning are cleared
