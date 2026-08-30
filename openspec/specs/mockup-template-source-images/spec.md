# Mockup Template Source Images

## Purpose

Defines local source-image collection, independent metadata configuration, grouped offering applicability, per-image placement, revision provenance, and explainable per-Variant readiness for Mockup Templates.
## Requirements
### Requirement: A Mockup Template owns independently configurable local source-image entries
FusionCanvas SHALL let an editable Mockup Template own zero or more active local source-image entries. Each entry SHALL reference one managed, Store-owned raster Asset and SHALL be independent of external provider, network, credential, or synchronization state. Uploading an image SHALL NOT automatically assign applicability values, a Design Area, or an image-space mapping to that entry. An entry with missing configuration SHALL remain persistable and visibly incomplete.

#### Scenario: Creator adds a local source image
- **WHEN** the creator selects a supported local raster file in an editable Mockup Template dialog and confirms adding it
- **THEN** FusionCanvas copies the file into managed workspace storage and creates one Store-owned source Asset and one source-image entry
- **AND** the entry identifies the managed Asset by stable identity rather than the original file path
- **AND** the entry remains unconfigured until the creator explicitly assigns its metadata and placement

#### Scenario: Creator saves uploaded images before configuring them
- **WHEN** the creator uploads valid source images and saves the Template without completing one or more image entries
- **THEN** FusionCanvas persists the valid managed Assets and incomplete source-image entries
- **AND** reports each missing applicability or mapping requirement without treating the save as a failure
- **AND** does not report the Template as ready

#### Scenario: Creator archives a source image
- **WHEN** the creator confirms archiving a selected active source-image entry and saves the Template
- **THEN** FusionCanvas excludes that entry from current applicability and readiness evaluation
- **AND** preserves the prior entry and Asset identity in historical Template revisions

### Requirement: Source images use grouped offering option-value applicability
FusionCanvas SHALL let a creator assign each active source-image entry to zero or more applicability groups, where each group identifies one active Option and one or more of its active Option Values belonging to the Template's Blueprint Offering. A Variant satisfies a configured group when it contains any selected value in that group, and satisfies the entry when it satisfies every configured group. The UI SHALL make Color prominent, optimize the initial state for one selected Color with no Size restriction, and progressively disclose Size or any other active Offering Option.

#### Scenario: Creator configures common color-specific source images
- **WHEN** the creator adds source images for Black, White, Navy, Military Green, and Gray under one Mockup Template
- **THEN** each image can be assigned to its corresponding Color Option Value
- **AND** leaving Size unconfigured means that image applies to all otherwise-compatible Sizes
- **AND** all entries remain part of the same named Template and target Design Area

#### Scenario: Creator selects alternatives within and conditions across Options
- **WHEN** the creator assigns a source image to Black or Navy in Color and M or L in Size
- **THEN** the entry applies to Black-M, Black-L, Navy-M, and Navy-L variants but not to another Color or Size

#### Scenario: Creator leaves applicability unconfigured
- **WHEN** an active source-image entry has no applicability groups
- **THEN** FusionCanvas reports that entry as incomplete rather than invalid
- **AND** the entry matches no Variant until the creator explicitly assigns applicability

### Requirement: Template image resolution is exact, per-Variant, and explainable
For each concrete Variant compatible with a Template's shared target Design Area, FusionCanvas SHALL evaluate only active source-image entries with complete applicability and a valid image-space mapping. A Template SHALL be ready only when every compatible Variant resolves to exactly one active complete source-image entry. FusionCanvas SHALL retain the individual resolved, missing, and ambiguous outcome for every Variant, SHALL never choose among multiple matching entries implicitly, and SHALL NOT discard successful outcomes merely because another Variant is unresolved.

#### Scenario: A variant has no matching image
- **WHEN** a compatible Variant does not match any complete active source-image entry
- **THEN** FusionCanvas identifies that Variant as missing source imagery
- **AND** preserves exact-one resolutions for other compatible Variants

#### Scenario: A variant matches more than one image
- **WHEN** a compatible Variant matches two or more active source-image entries
- **THEN** FusionCanvas identifies the Variant and conflicting entries as ambiguous
- **AND** preserves exact-one resolutions for other compatible Variants

### Requirement: Source-image dimensions and mappings remain valid
FusionCanvas SHALL determine a local source image's width and height from its managed file. Each source-image entry SHALL retain its own optional image-space mapping rather than sharing mapping coordinates with a different source image. An absent mapping SHALL mean incomplete setup; an assigned mapping SHALL be positive and within that entry's image dimensions. The Template SHALL retain one shared target Design Area.

#### Scenario: Creator configures placement for one image
- **WHEN** the creator explicitly assigns an in-bounds placement rectangle to the selected source image
- **THEN** FusionCanvas retains that mapping only for the selected entry

### Requirement: Source configuration is revisioned and protected
FusionCanvas SHALL create an immutable Template revision whenever a source image, its applicability conditions, its archive state, or its image-space mapping changes. Each revision SHALL snapshot every active source Asset identity, mapping, and applicability condition used by that configuration. FusionCanvas SHALL block permanent deletion of an Asset referenced by current source configuration or a historical Template revision.

#### Scenario: Creator changes source metadata
- **WHEN** the creator saves changed applicability, mapping, or archive state for a source image
- **THEN** FusionCanvas creates a new immutable revision while preserving the prior source configuration and Asset identity

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
