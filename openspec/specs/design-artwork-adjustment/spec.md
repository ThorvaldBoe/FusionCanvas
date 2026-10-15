# Design Artwork Adjustment

## Purpose

Defines preview, transparency-based bounds detection, reversible positioning and proportional scaling, and destructive application of final Design artwork adjustments while preserving each immediate before-image as a related image.

## Requirements

### Requirement: Final artwork can be inspected on its full image canvas
FusionCanvas SHALL open a focused, resizable preview for a final Design slot's managed PNG when the user activates the artwork thumbnail's magnifier action. The preview image SHALL scale to fit the available image view as the window changes size, preserve the image aspect ratio, and show the full PNG canvas over a checkerboard so transparent pixels are distinguishable from the canvas boundary. The main Design workspace SHALL retain its context when the preview opens and closes.

#### Scenario: Creator opens a final artwork preview
- **WHEN** the creator activates the magnifier action for an available final Design artwork thumbnail
- **THEN** FusionCanvas opens the managed PNG in a focused preview window
- **AND** the image view displays the full canvas and grows or shrinks with the resizable window
- **AND** transparent canvas pixels are visible against a checkerboard

#### Scenario: Creator resizes the preview window
- **WHEN** the creator resizes the final artwork preview window
- **THEN** the image view uses the newly available space while preserving the complete image canvas and aspect ratio

### Requirement: Visible artwork bounds are detected from PNG transparency
FusionCanvas SHALL identify the smallest pixel rectangle containing pixels whose alpha value is greater than zero. If the PNG has no transparent pixels, its visible artwork bounds SHALL cover the full canvas. If every pixel is transparent, FusionCanvas SHALL report that no visible artwork is available and SHALL not enable adjustment or Apply.

#### Scenario: PNG contains transparent margins
- **WHEN** the creator opens adjustment mode for a PNG with transparent margins
- **THEN** the red adjustment rectangle encloses all and only the outer extent of pixels with non-zero alpha
- **AND** the rectangle is distinct from the full image canvas boundary

#### Scenario: PNG has no transparent pixels
- **WHEN** the creator opens adjustment mode for a fully opaque PNG
- **THEN** the red adjustment rectangle initially covers the full canvas

#### Scenario: PNG is fully transparent
- **WHEN** the creator requests adjustment for a fully transparent PNG
- **THEN** FusionCanvas explains that no visible artwork was found
- **AND** adjustment and Apply remain unavailable

### Requirement: Artwork adjustments remain a reversible draft until Apply
Adjustment mode SHALL display a red rectangle around the detected visible artwork. The creator SHALL be able to move the artwork within the full image canvas and resize it using a handle while preserving its aspect ratio. The artwork may extend beyond the canvas during adjustment so it can be moved back before Apply. Adjustment SHALL retain the source canvas dimensions and SHALL NOT modify the managed source file until Apply succeeds.

#### Scenario: Creator moves artwork outside and back inside the canvas
- **WHEN** the creator moves the artwork partly beyond the canvas boundary and then moves it back before applying
- **THEN** the preview reflects both movements
- **AND** no pixels are discarded from the managed source image

#### Scenario: Creator resizes artwork proportionally
- **WHEN** the creator drags the adjustment resize handle
- **THEN** the artwork width and height change by the same scale factor
- **AND** the opposite corner remains the resize anchor

#### Scenario: Creator cancels an adjustment draft
- **WHEN** the creator chooses Cancel in adjustment mode
- **THEN** the pending movement and scale are discarded
- **AND** the current slot image and related images remain unchanged

#### Scenario: Creator closes a preview with unapplied adjustments
- **WHEN** the creator attempts to close the preview while an adjustment draft has changed
- **THEN** FusionCanvas asks whether to discard the unapplied adjustment
- **AND** choosing to keep editing returns focus to the adjustment surface with the draft intact
- **AND** choosing to discard closes the preview without changing workspace data

### Requirement: Applying artwork adjustments preserves the before-image and replaces slot artwork
Apply SHALL destructively render the current adjustment into a PNG with the original canvas dimensions. Pixels transformed outside the canvas SHALL be clipped, while transparent pixels fill any newly uncovered canvas regions. In the same persisted workspace operation, FusionCanvas SHALL add an independent copy of the current pre-apply image as an Item-linked Related Image and replace the Design slot's current artwork asset with the applied PNG. Every successful Apply SHALL preserve its own immediate before-image. Apply SHALL remain explicit and SHALL not require an additional destructive confirmation.

#### Scenario: Creator applies an artwork adjustment
- **WHEN** the creator activates Apply for a valid adjustment draft
- **THEN** FusionCanvas stores the pre-apply image as a Related Image linked to the same Item
- **AND** replaces the current slot artwork with the transformed managed PNG
- **AND** keeps the source canvas width and height
- **AND** clips transformed artwork outside the canvas in the applied image
- **AND** leaves the preview showing the applied result

#### Scenario: Creator applies a later adjustment to the same slot
- **WHEN** the creator adjusts the slot again after a prior successful Apply and applies the new draft
- **THEN** FusionCanvas adds the current pre-apply image as another Related Image
- **AND** replaces the slot artwork with the latest applied result

#### Scenario: Persistence fails during Apply
- **WHEN** the backup and transformed files have been prepared but the workspace update cannot be saved
- **THEN** FusionCanvas removes newly prepared managed files on a best-effort basis
- **AND** preserves the prior slot assignment, prior managed image, and Related Images
- **AND** keeps the adjustment draft available and reports a recoverable error

### Requirement: Artwork adjustment respects Design editability and asset availability
FusionCanvas SHALL allow inspection of an available final artwork in read-only Design contexts but SHALL expose adjustment and Apply only when the Item is editable. Missing or unreadable managed PNG files SHALL not enter adjustment mode and SHALL produce actionable feedback while preserving the existing slot and workspace context.

#### Scenario: Creator reviews artwork in a read-only Design context
- **WHEN** the creator opens an available final artwork preview for a read-only Item
- **THEN** FusionCanvas allows inspection
- **AND** does not offer an enabled adjustment or Apply action

#### Scenario: Managed slot artwork is missing or unreadable
- **WHEN** the creator activates the magnifier for missing or unreadable slot artwork
- **THEN** FusionCanvas does not show a broken image or enter adjustment mode
- **AND** explains that the managed artwork cannot be opened
- **AND** preserves the slot assignment and workspace state

### Requirement: Artwork adjustment controls are accessible and responsive
The magnifier, adjustment entry, adjustment surface, Apply, Cancel, and discard confirmation SHALL have meaningful accessible names and keyboard focus. When the adjustment surface is focused, arrow keys SHALL move the artwork; Space SHALL switch the surface to proportional resize mode, where arrow keys resize around the fixed corner. Focus SHALL move into the preview on open and return to the invoking thumbnail action on close. The preview SHALL keep its image canvas and essential actions available at its supported minimum size.

#### Scenario: Creator adjusts artwork without a pointer
- **WHEN** the creator focuses the adjustment surface and uses the keyboard
- **THEN** the creator can move the artwork, resize it proportionally, Apply, or Cancel using keyboard input
- **AND** Space switches between movement and proportional resize modes

#### Scenario: Creator closes the preview with the keyboard
- **WHEN** the creator presses Escape with no changed draft or confirms discard of a changed draft
- **THEN** the preview closes
- **AND** focus returns to the magnifier action that opened it
