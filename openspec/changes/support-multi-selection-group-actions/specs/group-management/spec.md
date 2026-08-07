## MODIFIED Requirements

### Requirement: Group selection is independent of document tabs
FusionCanvas SHALL maintain canonical active tree selection, multi-selection, and persistent document tabs as separate states.

#### Scenario: User normally selects a node
- **WHEN** the user clicks a niche, group, or future Item without a tab-opening gesture, or reaches it by keyboard
- **THEN** FusionCanvas updates canonical active selection and the reusable right-side inspector
- **AND** does not create a new tab

#### Scenario: User toggles or ranges nodes
- **WHEN** the user Ctrl-clicks or Shift-clicks active group or Item rows
- **THEN** FusionCanvas updates the multi-selection according to the `multi-selection` capability
- **AND** does not create or activate a document tab solely because of the selection gesture

#### Scenario: User explicitly opens a node in a tab
- **WHEN** the user middle-clicks a group or future Item, or invokes `Open in new tab`
- **THEN** FusionCanvas opens or activates a persistent tab for that entity
- **AND** avoids opening duplicate tabs

### Requirement: Group management moves complete subtrees within one store
FusionCanvas SHALL move a group beneath an active niche or group in the same store while preserving the moved group, descendant groups, contained listings, and connected context. When a drag begins on a selected group or Item, the operation SHALL use the normalized effective multi-selection and preserve the same subtree semantics.

#### Scenario: User drops a group onto a valid container
- **WHEN** the user drops an active group onto an active same-store niche or non-descendant group
- **THEN** FusionCanvas changes the moved root's direct parent and appends it to the destination's children
- **AND** preserves descendant parent relationships and contained listing identities

#### Scenario: User drops a selected set onto a valid container
- **WHEN** the user drags a selected set of Items or groups onto an active same-store niche or group and every effective source is valid
- **THEN** FusionCanvas moves all effective sources in one validated operation
- **AND** does not move descendants of an already selected group as separate sources

#### Scenario: User cuts and pastes a group
- **WHEN** the user presses Ctrl+X on a group and Ctrl+V on a valid destination
- **THEN** FusionCanvas performs the same validated subtree move as drag-and-drop
- **AND** clears cut state only after a successful save or clipboard replacement

### Requirement: Group movement rejects invalid destinations atomically
FusionCanvas SHALL reject movement to self, a descendant, a selected source, a descendant of a selected group, a missing or archived topic, or a topic in another store before saving any part of the operation.

#### Scenario: User attempts an invalid single-source drop or paste
- **WHEN** the requested destination would create a cycle or violates store or active-path rules
- **THEN** FusionCanvas shows blocked-target feedback and leaves the canonical hierarchy unchanged

#### Scenario: User attempts an invalid multi-source drop
- **WHEN** any effective selected source would be moved into itself, its descendant, another selected source, or a descendant of another selected group
- **THEN** FusionCanvas disables the drop and explains that the destination must be outside the selected hierarchy
- **AND** performs no partial move

#### Scenario: Persistence fails during a valid move
- **WHEN** a validated single- or multi-source move cannot be saved
- **THEN** FusionCanvas restores the last confirmed parent, position, active context, multi-selection, and expansion projection
- **AND** reports a recoverable error without a partial subtree move

### Requirement: Group rows expose contextual management actions
FusionCanvas SHALL provide a context menu when the user right-clicks an active group or Item row, with the menu determined by whether one or multiple entities are selected.

#### Scenario: User opens a single group context menu
- **WHEN** the user right-clicks an active group row outside a multi-selection
- **THEN** FusionCanvas selects that group and offers New group, Rename, Copy, Cut, Paste, Delete, and Export to CSV... actions
- **AND** New group creates a direct child of the clicked group
- **AND** Paste reflects whether the application clipboard currently contains a group operation

#### Scenario: User opens a multi-selection context menu
- **WHEN** the user right-clicks an active row already included in a multi-selection
- **THEN** FusionCanvas preserves the multi-selection and offers the group actions defined by the `group-actions` capability
- **AND** does not replace the selection merely to open the menu

#### Scenario: User right-clicks an unselected row
- **WHEN** the user right-clicks an active row not included in the current multi-selection
- **THEN** FusionCanvas makes that row the sole selection before showing its single-entity context menu
