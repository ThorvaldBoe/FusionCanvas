## MODIFIED Requirements

### Requirement: Design files support preview, export, missing state, and confirmed removal
The Design Stage Tool SHALL allow in-app preview, proportional artwork adjustment and Apply for editable final artwork, Export copy, missing-state review, and confirmed permanent removal through managed file and asset boundaries. Opening a preview SHALL remain read-only until the creator explicitly applies an adjustment. Applying an adjustment SHALL preserve a copy of the pre-apply image as an Item-linked Related Image and replace the slot artwork as defined by the `design-artwork-adjustment` capability.

#### Scenario: User previews a Design file
- **WHEN** the managed PNG exists and the user activates the magnifier action on its final artwork thumbnail
- **THEN** FusionCanvas displays the authoritative managed copy in a focused, resizable in-app preview whose image view scales with available space
- **AND** shows the full image canvas over a checkerboard
- **AND** does not require or launch an external application
- **AND** existing View actions for supporting images continue to open an in-app preview of the managed copy

#### Scenario: User adjusts a Design file
- **WHEN** the Item is editable and the user enters adjustment mode for an available final artwork PNG
- **THEN** FusionCanvas detects visible bounds from non-zero alpha pixels and provides proportional resizing and positioning within the image canvas
- **AND** keeps the adjustment reversible until Apply

#### Scenario: Managed file is missing
- **WHEN** the Design Asset record exists but its managed file is absent
- **THEN** the Design tool shows a missing state
- **AND** disables preview, adjustment, and export with actionable explanation
- **AND** keeps confirmed record removal available

#### Scenario: User exports a Design file
- **WHEN** the user chooses Export copy and a valid destination
- **THEN** FusionCanvas copies identical bytes from the managed source to that destination
- **AND** does not change the managed source, Asset, or Item link

#### Scenario: User applies an adjustment to a Design file
- **WHEN** the user applies a valid artwork adjustment and persistence succeeds
- **THEN** FusionCanvas saves a copy of the pre-apply image in Related Images
- **AND** replaces the slot's managed PNG with the destructively transformed result
- **AND** preserves the full-canvas dimensions and clips artwork outside the canvas

#### Scenario: User removes a Design file
- **WHEN** the user confirms removal and persistence succeeds
- **THEN** FusionCanvas atomically removes the Asset and Item link
- **AND** deletes the managed file on a best-effort basis after the save

#### Scenario: Removal persistence fails
- **WHEN** confirmed Design-file removal cannot be persisted
- **THEN** the Asset, Item link, and managed file remain
- **AND** a recoverable error preserves selection and retry context
