## ADDED Requirements

### Requirement: Multiple selected entities expose group actions
FusionCanvas SHALL replace the single-entity context menu with a selection-aware menu when multiple active Items or groups are selected.

#### Scenario: User opens a multi-selection context menu
- **WHEN** the user right-clicks an entity that is already part of a multi-selection
- **THEN** FusionCanvas preserves the selection and shows the selected count with the available group actions

#### Scenario: User right-clicks outside the selection
- **WHEN** the user right-clicks an unselected active Item or group while another selection exists
- **THEN** FusionCanvas replaces the selection with the clicked entity before showing its single-entity context menu

#### Scenario: Group actions are shown
- **WHEN** multiple eligible entities are selected
- **THEN** the context menu offers Open in new tabs, Duplicate, Delete, Archive, Export, and Group
- **AND** an unavailable action is disabled or omitted with an actionable reason

### Requirement: Group actions operate on stable effective sources
FusionCanvas SHALL normalize a mixed selection before executing a group action. When a selected group contains another selected group or Item, the effective source set SHALL retain only the outermost selected group for operations that act on hierarchy.

#### Scenario: Selection contains a group and its descendant Item
- **WHEN** the user invokes a hierarchy-affecting group action
- **THEN** FusionCanvas does not duplicate, archive, delete, export, or move the descendant twice
- **AND** the result preserves the group subtree semantics of the selected outer group

#### Scenario: Selection changes during an action
- **WHEN** a group action is running
- **THEN** FusionCanvas uses the captured stable IDs and initial validation result
- **AND** prevents conflicting selection-dependent submissions until the operation completes or fails

### Requirement: Group actions provide safe destructive and partial-result feedback
FusionCanvas SHALL require confirmation for multi-entity Delete and Archive actions and SHALL report per-entity outcomes when a selection contains ineligible or failed entities.

#### Scenario: User confirms multi-entity deletion
- **WHEN** the user chooses Delete for multiple selected entities and confirms the warning
- **THEN** FusionCanvas deletes only the validated effective sources atomically where the existing entity service supports atomic deletion
- **AND** closes tabs for deleted entities and selects a surviving valid context

#### Scenario: User cancels a destructive action
- **WHEN** the user cancels a multi-entity Delete or Archive confirmation
- **THEN** FusionCanvas leaves data, tabs, active context, and multi-selection unchanged

#### Scenario: Some selected entities are ineligible
- **WHEN** a requested action cannot apply to one or more selected entities
- **THEN** FusionCanvas identifies skipped entities and reasons in the confirmation or completion summary
- **AND** does not silently claim that the entire selection was changed

### Requirement: Group action operations preserve selection coherently
FusionCanvas SHALL refresh the authoritative tree and retain selected surviving IDs after a successful group action.

#### Scenario: A group action succeeds
- **WHEN** Duplicate, Archive, Export, or Group completes successfully
- **THEN** FusionCanvas refreshes the tree from persisted state
- **AND** keeps surviving selected entities selected by stable ID
- **AND** keeps the active context meaningful or selects the nearest surviving context when it was removed

### Requirement: Group action Group creates a containing group
FusionCanvas SHALL provide a Group action that creates a new group and moves the selected effective Items or groups beneath it after the user supplies a valid name and destination.

#### Scenario: Selected Items share a parent
- **WHEN** the user chooses Group for Items with one common active parent
- **THEN** FusionCanvas proposes that parent as the destination
- **AND** after a valid name is confirmed, creates one group and moves the selected Items into it

#### Scenario: Selected entities have different parents
- **WHEN** the user chooses Group for entities without one common direct parent
- **THEN** FusionCanvas requires an explicit valid niche or group destination
- **AND** rejects a name conflict or invalid destination without partial changes

### Requirement: Dragging a selected set moves the complete effective selection
FusionCanvas SHALL treat a drag that begins on a selected entity as a drag of the complete effective selection, including mixed Item/group selections.

#### Scenario: User drags selected Items into a group
- **WHEN** multiple selected Items are dragged onto an active same-store niche or group
- **THEN** FusionCanvas moves all selected Items to that destination in one validated operation
- **AND** preserves Item identity and unrelated selection state

#### Scenario: User drags selected groups and Items
- **WHEN** both groups and Items are selected and the user drags from one selected row
- **THEN** FusionCanvas moves the normalized effective sources together only when every source can move to the destination
- **AND** does not separately move Items already contained by a selected group

### Requirement: Multi-entity drag rejects hierarchy-invalid destinations
FusionCanvas SHALL reject a drop onto any selected source, any descendant of a selected group, any destination that creates a cycle, any missing or archived topic, or any topic in another store before persistence.

#### Scenario: User drags a group onto itself or its descendant
- **WHEN** the effective selection contains a group and the pointer is over that group or a descendant
- **THEN** FusionCanvas shows blocked-target feedback and performs no persistence

#### Scenario: User drags a mixed selection into its own selected hierarchy
- **WHEN** the effective selection contains Items and groups and the pointer is over a selected group or one of its descendants
- **THEN** FusionCanvas disables the drop and explains that the destination must be outside the selected hierarchy
- **AND** leaves the canonical tree unchanged

#### Scenario: Multi-entity move persistence fails
- **WHEN** a validated multi-entity move cannot be saved
- **THEN** FusionCanvas restores the last confirmed tree projection and selection
- **AND** reports a recoverable error without partial movement
