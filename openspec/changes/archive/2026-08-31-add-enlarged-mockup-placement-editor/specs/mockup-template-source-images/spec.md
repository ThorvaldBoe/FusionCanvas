## MODIFIED Requirements

### Requirement: Local source-image setup has a focused, accessible master-detail editor workflow
FusionCanvas SHALL provide local source-image setup inside the focused Mockup Template dialog. An upper image table SHALL expose upload, selection, archive, applicability and mapping summaries, and complete/incomplete indicators. A lower selected-image editor SHALL expose applicability controls, preview, and that entry's mapping independently of upload. When a selected image is available, the lower editor SHALL provide a clearly recognizable magnifying-glass-with-plus control in the lower-right area of its placement preview that opens the enlarged placement editor defined by the `enlarged-mockup-placement-editor` capability. The dialog SHALL preserve meaningful unsaved work on cancellation or close requests and SHALL keep Archived Stores read-only.

#### Scenario: Creator uploads images independently of metadata
- **WHEN** the creator uploads a valid file from the image-table action
- **THEN** FusionCanvas adds a row without copying applicability or mapping from another row
- **AND** selects the new row for metadata editing

#### Scenario: Creator selects an image row
- **WHEN** the creator selects a different active image in the upper table
- **THEN** the lower editor displays that image's own applicability and mapping draft
- **AND** preserves unsaved edits made to previously selected image rows

#### Scenario: Creator expands the selected image placement preview
- **WHEN** the creator activates the lower-right magnifying-glass-with-plus control in the selected image editor
- **THEN** FusionCanvas opens the enlarged placement editor with the selected image and current mapping draft
- **AND** placement edits remain part of the same Mockup Template draft

#### Scenario: Archived Store is reviewed
- **WHEN** the Mockup Template belongs to an archived Store
- **THEN** upload, archive, applicability editing, placement editing, zoom activation, and Save are unavailable
