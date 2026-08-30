## ADDED Requirements

### Requirement: Active Option Values can be renamed in place
FusionCanvas SHALL expose an accessible **Edit** action for every active Option Value in the focused value-management dialog. The action SHALL open the current display name in an editable draft and SHALL persist a successful rename against the existing Option Value identity. Color and Size SHALL use identical behavior; no replacement value SHALL be created.

#### Scenario: Edit a Color or Size value
- **WHEN** the user activates Edit for an active Color or Size value
- **THEN** the dialog shows that value's current name in an editable form
- **AND** saving a valid new name updates the same stable Option Value record
- **AND** the dialog list and parent Option summary show the new name

#### Scenario: Reject invalid or duplicate rename
- **WHEN** the user saves a blank, invalid, or normalized duplicate name for an active value in the same Option
- **THEN** the existing validation and recoverable error behavior is shown
- **AND** the original value and its display name remain unchanged

#### Scenario: Preserve references during rename
- **WHEN** a value used by Variant memberships, template/value links, or other catalog relationships is renamed successfully
- **THEN** every relationship still references the same Option Value identity
- **AND** dependent views refresh to display the new name

#### Scenario: Cancel an Option Value edit
- **WHEN** the user cancels, closes, or presses Escape while editing a value
- **THEN** no rename is persisted
- **AND** the existing value and all references remain unchanged
