# Design Stage Implementation

## Purpose

Defines accepted behavior for the Design stage working surface: a mandatory listing configuration that yields the printable areas, a per-item color working set, a Printify-style row model where each row serves one set of colors, a row × design-area slot grid that binds one final image to each cell, drag-and-drop filling, and an independent Supporting images area.

## ADDED Requirements

### Requirement: Design stage is anchored to one mandatory listing configuration
FusionCanvas SHALL require an editable Item at Design to select exactly one listing configuration (a Store catalog offering) before presenting final-design slots. The configuration SHALL be validated to belong to the Item's Store and to be active, and its design areas SHALL define the columns of the final-design slot grid. When no complete configuration is selected, FusionCanvas SHALL NOT show the final-design slot grid; it SHALL show the Supporting images area and an actionable prompt to select a configuration.

#### Scenario: No configuration selected
- **WHEN** an editable Item at Design has no listing configuration selected
- **THEN** FusionCanvas does not show the final-design slot grid
- **AND** it shows the Supporting images area
- **AND** it presents a prompt to select a listing configuration

#### Scenario: User selects a valid configuration
- **WHEN** the user selects an active catalog offering that belongs to the Item's Store
- **THEN** FusionCanvas persists it as the Item's single listing configuration
- **AND** the final-design slot grid appears with one column per design area of that offering, regardless of each area's variant applicability
- **AND** every row displays the same set of design-area columns from the selected offering

#### Scenario: User selects a configuration from another Store
- **WHEN** a configuration request refers to an offering from a different Store
- **THEN** FusionCanvas rejects the request
- **AND** preserves the Item's prior configuration

#### Scenario: User tries to select a second configuration
- **WHEN** the Item already has a listing configuration and the user selects another
- **THEN** FusionCanvas replaces the previous configuration
- **AND** backs up or clears the prior configuration's slot assignments so no slot refers to a design area outside the new configuration

### Requirement: Configuration selection and the row grid respect workflow editability
FusionCanvas SHALL present the listing configuration selector and the final-design slot grid read-only whenever the Item's active Design content is read-only, and SHALL NOT commit a configuration, color, row, or slot mutation in a read-only context.

#### Scenario: User reviews Design from a protected context
- **WHEN** Design-stage editing is unavailable because the Item is protected or an earlier stage is being reviewed
- **THEN** FusionCanvas shows the persisted configuration, color working set, rows, and slot images as read-only
- **AND** it does not commit any design-stage mutation

### Requirement: The design working set narrows the offering's color universe
FusionCanvas SHALL let the user select a subset of the configuration's available colors to work with for the Item's design. Size SHALL NOT participate in the working set or the slot grid. The default row SHALL initially serve all selected colors, and colors SHALL be deduplicated by color value across the configuration's variants.

#### Scenario: User selects a subset of colors
- **WHEN** the configuration exposes many colors and the user selects a smaller set, for example Black, Navy, Military Green, and Sand
- **THEN** FusionCanvas records exactly those selected colors
- **AND** a single default row is created that serves all selected colors
- **AND** the slot grid shows one row (the default) with each design area as a column

#### Scenario: Size does not create rows or slots
- **WHEN** the configuration has multiple sizes for a selected color
- **THEN** the working set, rows, and slot grid are unaffected by size
- **AND** the selected color appears once regardless of how many sizes it has

#### Scenario: Duplicate color across variants is collapsed
- **WHEN** the same color value appears on multiple variants of the offering
- **THEN** it is recorded once in the working set and appears in exactly one row

#### Scenario: Color value is derived from the Color option
- **WHEN** the configuration's variants define option values and FusionCanvas builds the available-color list
- **THEN** a color value is the `Value` of a variant option whose `Name` is `Color`, compared case-insensitively against the literal `Color` label
- **AND** deduplication and row membership use that derived color value

### Requirement: One row serves one set of colors and colors form a partition
FusionCanvas SHALL keep the selected colors partitioned across rows so that every selected color belongs to exactly one row. A single default row serves the colors not claimed by a specific row. Rows may serve multiple colors.

#### Scenario: Default row serves the unclaimed colors
- **WHEN** no specific color row exists
- **THEN** the default row serves every selected color
- **AND** its cells show thumbnails or Add-image states as configured

#### Scenario: A specific row serves only its colors
- **WHEN** the user creates a specific design row for Sand
- **THEN** Sand is removed from the default row's set and belongs only to the new row
- **AND** every selected non-Sand color remains in the default row

### Requirement: The creator can make a specific design for a color
FusionCanvas SHALL provide a "make specific design for a color" action that moves a selected color out of its current row into a new specific row, duplicating the row's design-area slot structure so that a different set of final images can be provided for that color.

#### Scenario: Make a specific design for a color
- **WHEN** the user selects Sand and invokes "make specific design for Sand"
- **THEN** FusionCanvas atomically moves Sand from its current row into a new specific row
- **AND** the new row contains one slot per design area of the configuration
- **AND** the new row's slots start empty

#### Scenario: Color is the only color in its row
- **WHEN** the user makes specific a color that is the only color in its row
- **THEN** the new specific row replaces the old row's function for that color
- **AND** the old row is removed when it no longer serves any color

#### Scenario: Remove a specific row
- **WHEN** the user confirms removal of a specific color row
- **THEN** FusionCanvas atomically moves all of that row's colors back to the default row
- **AND** removes the specific row and its slot assignments
- **AND** the partition invariant still holds with every color in exactly one row

### Requirement: The slot grid binds one final image to each row × design-area cell
FusionCanvas SHALL render one slot per (row, design area) combination. Each empty slot SHALL present an Add-image action and each filled slot SHALL show a thumbnail with view-large, download, and remove commands. Each cell SHALL hold at most one final image. Slot final images SHALL be managed PNG copies, and a source whose extension is not `.png` SHALL be rejected before copy or persistence.

#### Scenario: User fills a slot
- **WHEN** an item at Design is editable, a row and design area are selected, and the user chooses an image for that cell
- **THEN** FusionCanvas persists the image as that slot's final image
- **AND** the cell shows a thumbnail of that image

#### Scenario: User replaces a slot image
- **WHEN** a filled slot receives a new image
- **THEN** FusionCanvas replaces the cell's image with the new image
- **AND** manages the replaced managed file and record according to asset removal rules

#### Scenario: User views a slot image large
- **WHEN** the user invokes view-large on a filled slot
- **THEN** FusionCanvas displays an in-app larger preview of the authoritative managed copy

#### Scenario: User downloads a slot image
- **WHEN** the user invokes download/export on a filled slot
- **THEN** FusionCanvas copies identical bytes of the managed copy to the chosen destination
- **AND** does not change the managed source or the slot binding

#### Scenario: User removes a slot image
- **WHEN** the user confirms removal of a filled slot image
- **THEN** FusionCanvas removes the slot binding
- **AND** deletes the managed file on a best-effort basis after persistence succeeds
- **AND** the cell returns to the Add-image state

### Requirement: Drag and drop fills slot cells
FusionCanvas SHALL let the user drag and drop supported image files onto a slot cell to fill it, subject to the same validation as explicit import.

#### Scenario: User drops an image on a slot
- **WHEN** the Design stage is editable and the user drops a supported image file onto an empty or filled slot cell
- **THEN** FusionCanvas fills or replaces that cell's image with a managed copy of the dropped file

#### Scenario: User drops an unsupported file type
- **WHEN** the dropped file is not a supported image
- **THEN** FusionCanvas rejects it before copy or persistence
- **AND** reports that the file type is not supported
- **AND** leaves the slot unchanged

### Requirement: Supporting images are independent of configuration and design areas
FusionCanvas SHALL provide a Supporting images area that lists images such as sketches, references, and existing artwork used as a basis for the design. Supporting images SHALL be importable in any quantity, SHALL accept the supported creative image set (not restricted to PNG or a specific resolution or design area), SHALL NOT be bound to a design area, and SHALL remain available whether or not a listing configuration is selected.

#### Scenario: User imports a supporting image
- **WHEN** the Design stage is editable and the user imports a supported image as supporting
- **THEN** FusionCanvas creates an independent managed copy and assets record linked to the Item
- **AND** it appears in the Supporting images area

#### Scenario: Supporting images show without a configuration
- **WHEN** an Item at Design has no listing configuration but has supporting images
- **THEN** FusionCanvas shows the Supporting images area with those images
- **AND** it does not show the final-design slot grid

#### Scenario: User views, downloads, or removes a supporting image
- **WHEN** the user invokes view-large, download, or remove on a supporting image
- **THEN** FusionCanvas behaves as for other managed images, with removal confirmed and the managed file handled on a best-effort basis
