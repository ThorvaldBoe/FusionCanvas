## MODIFIED Requirements

### Requirement: Local source-image setup has a focused, accessible master-detail editor workflow
FusionCanvas SHALL provide local source-image setup inside the focused Mockup Template dialog. An upper image table SHALL expose upload, selection, archive, applicability and mapping summaries, and complete/incomplete indicators. A lower selected-image editor SHALL expose applicability controls, preview, and that entry's mapping independently of upload. The placement editor SHALL also expose the accessible **Keep aspect ratio** option defined by the `mockup-placement-aspect-ratio` capability. The dialog SHALL preserve meaningful unsaved work on cancellation or close requests and SHALL keep Archived Stores read-only.

#### Scenario: Creator uploads images independently of metadata
- **WHEN** the creator uploads a valid file from the image-table action
- **THEN** FusionCanvas adds a row without copying applicability or mapping from another row
- **AND** selects the new row for metadata editing

#### Scenario: Creator selects an image row
- **WHEN** the creator selects a different active image in the upper table
- **THEN** the lower editor displays that image's own applicability and mapping draft
- **AND** preserves unsaved edits made to previously selected image rows

#### Scenario: Creator configures ratio-aware placement
- **WHEN** the creator edits the selected image placement with a valid Design Area selected
- **THEN** the editor exposes **Keep aspect ratio** checked by default
- **AND** the creator can uncheck it to permit intentional independent dimensions

#### Scenario: Archived Store is reviewed
- **WHEN** the Mockup Template belongs to an archived Store
- **THEN** upload, archive, applicability editing, placement editing, ratio-option editing, and Save are unavailable
