## ADDED Requirements

### Requirement: The selected Mockup Template image has an enlarged placement editor
The Mockup Template editor SHALL expose one clearly recognizable magnifying-glass-with-plus launch control in the lower-right area of the selected image placement preview. When activated for an available selected image, it SHALL open a substantially larger focused placement editor that displays the same selected image and image-space design-area rectangle.

#### Scenario: Creator opens the enlarged editor
- **WHEN** the creator has selected a source image and activates the lower-right zoom control
- **THEN** FusionCanvas opens a focused placement editor with substantially more canvas area than the compact preview
- **AND** the enlarged editor displays the selected image and its current design-area rectangle

#### Scenario: No image is selected
- **WHEN** the placement region has no selected image
- **THEN** the zoom control is unavailable or not shown
- **AND** no enlarged placement editor opens

### Requirement: Enlarged placement editing reuses the active mapping draft
The enlarged editor SHALL bind to the same selected image, image dimensions, image path, mapping coordinates, and draft state as the compact editor. Dragging the design-area rectangle SHALL update X and Y independently, and resizing the lower-right handle SHALL update width and height independently, while keeping the rectangle positive and inside the image bounds.

#### Scenario: Creator repositions and resizes in the enlarged editor
- **WHEN** the creator drags the rectangle and then drags its resize handle in the enlarged editor
- **THEN** the shared mapping draft reflects the new X/Y position and width/height
- **AND** movement and resizing remain clamped to the selected image dimensions

#### Scenario: Opening preserves current placement state
- **WHEN** the creator opens the enlarged editor after changing the compact preview mapping
- **THEN** the enlarged editor starts with the current mapping values, selected image, dimensions, and preview path

### Requirement: Enlarged editor dismissal preserves existing save semantics
The enlarged editor SHALL provide an explicit accessible Close action and Escape dismissal. Closing it SHALL return to the Mockup Template editor without saving independently, discarding the template draft, changing applicability, or creating a revision. The existing template Save and Cancel actions SHALL remain responsible for persistence and draft discard.

#### Scenario: Creator closes after editing
- **WHEN** the creator changes placement in the enlarged editor and activates Close
- **THEN** the enlarged editor closes
- **AND** the compact editor reflects the changed shared draft
- **AND** the change remains subject to the existing template Save or Cancel action

### Requirement: Enlarged placement editing is accessible and responsive
The zoom and Close controls SHALL expose meaningful accessible names/descriptions, support keyboard focus and activation, and maintain a predictable focus path. The enlarged editor SHALL remain usable at supported narrow dimensions without clipping its close path or essential placement surface. Archived or read-only template contexts SHALL not permit launch or placement editing.

#### Scenario: Keyboard user opens and dismisses the editor
- **WHEN** the keyboard user focuses and activates the zoom control
- **THEN** the enlarged editor opens and focus moves to the placement editor or its primary accessible surface
- **AND** pressing Escape or activating Close dismisses it
- **AND** focus returns to the zoom control when practical

#### Scenario: Creator uses a narrow enlarged editor
- **WHEN** the enlarged editor is displayed at its supported minimum size
- **THEN** the placement editor remains visible and usable
- **AND** the accessible Close action remains visible and activatable
