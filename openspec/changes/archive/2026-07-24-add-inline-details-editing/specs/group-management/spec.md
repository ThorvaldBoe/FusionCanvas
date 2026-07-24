## MODIFIED Requirements

### Requirement: Group management edits detailed properties safely
FusionCanvas SHALL present the active group's detailed properties inline in the document window details pane: name, description, and notes edit inline and persist automatically when the edited field loses focus, while destination move, archive, and restore remain explicit confirmed actions, and ordinary inline create or rename remains in the tree.

#### Scenario: User reviews group details
- **WHEN** a group becomes the active document context
- **THEN** the details pane displays the group's name, description, notes, topic path, and archive state
- **AND** the main tree selection and document context remain preserved
- **AND** ordinary inline create or rename remains in the tree

#### Scenario: User leaves a field with valid edits
- **WHEN** the user changes the group's name, description, or notes and the edited field loses focus
- **THEN** FusionCanvas validates and persists the normalized values and updated timestamp atomically
- **AND** preserves descendant groups, contained listings, placement, archive state, and unknown metadata

#### Scenario: User leaves a field with an invalid name
- **WHEN** the user empties the group name or enters a duplicate active sibling name and the field loses focus
- **THEN** FusionCanvas reverts the name to its last persisted value
- **AND** reports an inline validation error explaining the revert
- **AND** still persists any other valid edited fields

#### Scenario: User moves, archives, or restores the group
- **WHEN** the user chooses a destination and invokes Move, or confirms archive, or invokes restore
- **THEN** FusionCanvas performs the existing validated operation with confirmation for destructive actions
- **AND** reports an actionable error when the operation is unavailable or fails
- **AND** the details pane and tree refresh to the persisted state

#### Scenario: Commit fails to persist
- **WHEN** the repository cannot complete a details commit
- **THEN** FusionCanvas reports a recoverable inline error
- **AND** keeps the user's editable draft for a later retry
- **AND** leaves the persisted group unchanged
