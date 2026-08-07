## ADDED Requirements

### Requirement: Navigation supports familiar multi-selection
FusionCanvas SHALL support separate active-context and multi-selection state in the workspace tree. Plain click SHALL replace the selection and make the clicked entity active; Ctrl-click SHALL toggle the clicked entity; Shift-click SHALL select the visible range from the selection anchor; Ctrl+Shift-click SHALL extend the current selection with that range; and Ctrl+A SHALL select all visible selectable entities.

#### Scenario: User selects one entity
- **WHEN** the user clicks an active Item or group without a selection modifier
- **THEN** FusionCanvas clears other selected entities, selects the clicked entity, makes it the active context, and updates the reusable inspector

#### Scenario: User toggles an entity
- **WHEN** the user Ctrl-clicks an active Item or group
- **THEN** FusionCanvas toggles that entity in the selection without opening a tab
- **AND** the clicked entity becomes the active context when it remains selected

#### Scenario: User selects a visible range
- **WHEN** the user Shift-clicks an active Item or group after establishing a selection anchor
- **THEN** FusionCanvas selects every selectable visible entity between the anchor and clicked row in tree display order
- **AND** the clicked row becomes the active context

#### Scenario: User selects all visible entities
- **WHEN** the tree has keyboard focus and the user presses Ctrl+A outside a text editor
- **THEN** FusionCanvas selects all visible selectable active Items and groups in the current store and filtered projection
- **AND** does not select hidden, filtered-out, archived, or niche-root rows unless they are explicitly selectable under the current projection

#### Scenario: User clears the active selection
- **WHEN** the user Ctrl-clicks the only selected entity
- **THEN** FusionCanvas clears the multi-selection and leaves the last valid canonical active context available for inspection

### Requirement: Selection states are visually distinct
FusionCanvas SHALL render the active entity with a brighter selection treatment and other selected entities with a dimmer selection treatment, while preserving keyboard-focus visibility and inactive/archive treatment.

#### Scenario: Multiple entities are selected
- **WHEN** the tree contains one active selected entity and one or more other selected entities
- **THEN** the active row is visibly brighter than the other selected rows
- **AND** every selected row remains distinguishable from unselected rows

#### Scenario: Selection survives filtering when visible
- **WHEN** filtering changes the tree projection and a selected entity remains visible
- **THEN** FusionCanvas preserves that entity in the multi-selection by stable ID
- **AND** selection of an entity hidden by filtering is not acted on by “select all visible”

### Requirement: Pointer and keyboard focus do not create accidental actions
FusionCanvas SHALL keep tree selection, inline editing, document activation, and structural keyboard commands distinct.

#### Scenario: Text editing has focus
- **WHEN** a TextBox or inline tree editor has keyboard focus
- **THEN** Ctrl+A selects text in that editor and does not select tree entities
- **AND** unrelated global selection or structural shortcuts do not execute

#### Scenario: Drag begins on an unselected row
- **WHEN** the user begins a drag on an entity that is not selected
- **THEN** FusionCanvas replaces the selection with that entity before starting the drag

### Requirement: Tab opening is explicit without Ctrl-click
FusionCanvas SHALL open a persistent tab for a single group or Item through middle-click or an `Open in new tab` context-menu action, and SHALL open one tab for each selected entity through `Open in new tabs`.

#### Scenario: User middle-clicks one entity
- **WHEN** the user middle-clicks an active group or Item row
- **THEN** FusionCanvas opens or activates the corresponding persistent tab
- **AND** does not interpret the middle-click as a Ctrl-assisted selection toggle

#### Scenario: User opens selected entities in tabs
- **WHEN** the user chooses `Open in new tabs` with multiple selected entities
- **THEN** FusionCanvas opens or activates one persistent tab per selected entity without duplicate tabs
- **AND** preserves the current multi-selection
