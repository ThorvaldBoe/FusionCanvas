## ADDED Requirements

### Requirement: A Mockup Template owns local source-image entries
FusionCanvas SHALL let an editable Mockup Template own zero or more active local source-image entries. Each entry SHALL reference one managed, Store-owned raster Asset, SHALL retain its own image-space mapping, and SHALL be independent of external provider, network, credential, or synchronization state.

#### Scenario: Creator adds a local source image
- **WHEN** the creator selects a supported local raster file in an editable Mockup Template dialog and confirms adding it
- **THEN** FusionCanvas copies the file into managed workspace storage and creates one Store-owned source Asset and one source-image entry
- **AND** the entry identifies the managed Asset by stable identity rather than the original file path
- **AND** the confirmed Template configuration remains local and usable without network access

#### Scenario: Import cannot complete
- **WHEN** the selected file is unsupported, unavailable, unreadable, not a decodable supported raster image, or its managed copy or persistence operation fails
- **THEN** FusionCanvas reports a recoverable error and preserves the dialog draft and last confirmed Template configuration
- **AND** it creates no partially configured source-image entry
- **AND** it removes a newly copied managed file on a best-effort basis when persistence fails

#### Scenario: Creator replaces a source image
- **WHEN** the creator replaces an existing source-image entry with another valid local raster file and saves
- **THEN** FusionCanvas retains the prior confirmed source in the historical revision that used it
- **AND** creates a new current source-image configuration using the new managed Asset
- **AND** does not make the original external file path authoritative

### Requirement: Source images use offering option-value applicability
FusionCanvas SHALL let a creator assign each active source-image entry to one or more active Option Values belonging to its Template's Blueprint Offering. The UI SHALL make Color values prominent while allowing the creator to add conditions from any other active Option defined by that Offering. FusionCanvas SHALL not link an entry to an Option Value from another Offering or to an archived value.

#### Scenario: Creator configures common color-specific source images
- **WHEN** the creator adds source images for Black, White, Navy, Military Green, and Gray under one Mockup Template
- **THEN** FusionCanvas lets the creator assign each image to its corresponding Color Option Value
- **AND** the five entries remain part of the same named Template and target Placeholder

#### Scenario: Creator adds a non-color condition
- **WHEN** the Offering defines another active Option such as Size and the creator assigns a source image to Navy and XL
- **THEN** FusionCanvas records both stable Option Value identities as that image's applicability conditions
- **AND** it does not encode Color or Size labels as relationship keys

#### Scenario: Creator attempts an invalid applicability assignment
- **WHEN** the creator attempts to save an entry with no selected values, a value from another Offering, or an archived value
- **THEN** FusionCanvas keeps the draft editable and explains the invalid selection
- **AND** confirmed source images, applicability, and revisions remain unchanged

### Requirement: Template image resolution is exact and explainable
For each concrete Variant compatible with a Template's target Placeholder, FusionCanvas SHALL consider a source-image entry applicable when the Variant contains every Option Value assigned to that entry. A Template SHALL be ready only when every compatible Variant resolves to exactly one active source-image entry. FusionCanvas SHALL report missing and overlapping matches by concrete Variant and SHALL never choose among multiple matching entries implicitly.

#### Scenario: Color-only images cover every compatible variant
- **WHEN** a Template has one source image for each offered Color and every compatible Variant has exactly one of those Color values
- **THEN** FusionCanvas reports the Template as ready
- **AND** each compatible Variant resolves to the source image assigned to its Color

#### Scenario: A variant has no matching image
- **WHEN** a compatible Variant does not contain all applicability values for any active source image
- **THEN** FusionCanvas identifies that Variant as missing source imagery
- **AND** prevents the Template from being reported as ready

#### Scenario: A variant matches more than one image
- **WHEN** a compatible Variant satisfies the applicability values of two or more active source-image entries
- **THEN** FusionCanvas identifies the Variant and conflicting entries as ambiguous
- **AND** prevents the Template from being reported as ready or used as though a source had been selected

### Requirement: Source-image dimensions and mappings remain valid
FusionCanvas SHALL determine a local source image's width and height from its managed file, SHALL initialize a positive in-bounds mapping for every newly added entry, and SHALL validate every edited mapping against that entry's image dimensions. Each source-image entry SHALL retain its own mapping rather than sharing mapping coordinates with a different source image.

#### Scenario: Creator adds a source image
- **WHEN** a supported raster source image is imported successfully
- **THEN** FusionCanvas displays the managed image in the Template placement editor
- **AND** initializes a mapping that remains within the image bounds

#### Scenario: Creator enters an invalid mapping
- **WHEN** the creator enters a non-positive size or a mapping that extends outside the selected image
- **THEN** FusionCanvas explains the validation failure and keeps the draft open
- **AND** it does not persist the invalid mapping or create a revision

### Requirement: Source configuration is revisioned and protected
FusionCanvas SHALL create an immutable Template revision whenever a source image, its applicability conditions, or its image-space mapping changes. Each revision SHALL snapshot every active source Asset identity, mapping, and applicability condition used by that configuration. FusionCanvas SHALL block permanent deletion of an Asset referenced by current source configuration or a historical Template revision until the creator resolves the reference through a supported replacement or removal workflow.

#### Scenario: Creator changes source applicability
- **WHEN** the creator saves a Template after changing which Option Values apply to an existing source image
- **THEN** FusionCanvas creates the next Template revision
- **AND** the new revision contains the changed source-image applicability while the earlier revision remains unchanged

#### Scenario: Creator attempts to delete a referenced asset
- **WHEN** the creator requests permanent removal of a managed Asset used by a current or historical Template source
- **THEN** FusionCanvas blocks the removal and identifies the dependent Template configuration or revision
- **AND** the managed file and confirmed records remain unchanged

### Requirement: Local source-image setup has a focused, accessible editor workflow
FusionCanvas SHALL provide local source-image setup inside the existing focused Mockup Template dialog. It SHALL expose one clear keyboard-accessible Browse image action, selected-source collection, option-value applicability controls, preview, readiness feedback, and explicit Save/Cancel behavior. The dialog SHALL preserve meaningful unsaved work on cancellation or close requests and SHALL return focus to the invoking control after successful save or confirmed discard. Archived Stores SHALL remain read-only.

#### Scenario: Creator opens a new Template dialog
- **WHEN** the creator opens an editable Mockup Template dialog
- **THEN** FusionCanvas identifies the local **Mockup source images** configuration and exposes a Browse image action
- **AND** it does not show a provider-catalog loading, unavailable, synchronization, or credential state
- **AND** keyboard focus is placed at the first required Template field or the next incomplete source-setup action

#### Scenario: Creator cancels while configuring sources
- **WHEN** the creator has made meaningful unsaved source-image, applicability, or mapping changes and requests Cancel, Escape, or dialog close
- **THEN** FusionCanvas offers to discard or continue editing
- **AND** continue editing preserves the complete draft and focus
- **AND** confirmed discard creates no Asset, source entry, revision, or persistence change

#### Scenario: Archived Store is reviewed
- **WHEN** the Mockup Template belongs to an archived Store
- **THEN** Browse, source replacement, applicability editing, placement editing, and Save are unavailable
- **AND** existing source identity and readiness feedback remain reviewable
