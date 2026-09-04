## Purpose

Defines visual placement and mapping reuse in the local Mockup Template editor.

## Requirements

### Requirement: Selected source images provide a visual placement preview
The editor SHALL display the selected source image and overlay its current design-area mapping when available. Coordinate edits SHALL update the overlay without changing other source-image drafts.

#### Scenario: Creator positions a selected image
- **WHEN** a source image is selected and the creator changes placement coordinates
- **THEN** the image preview remains visible and the overlay reflects the edited rectangle

### Requirement: Creators can reuse an existing mapping
The editor SHALL offer a Re-use mapping from control listing other source images with explicit non-default mappings. Choosing one SHALL copy only the mapping to the selected image.

#### Scenario: Creator reuses a mapping
- **WHEN** the creator chooses another mapped source image in the reuse control
- **THEN** the selected image receives the copied mapping and its applicability remains unchanged
