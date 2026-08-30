## ADDED Requirements

### Requirement: A Mockup Template owns independently configurable local source-image entries
FusionCanvas SHALL let an editable Mockup Template own zero or more active local source-image entries. Each entry SHALL reference one managed, Store-owned raster Asset and SHALL be independent of external provider, network, credential, or synchronization state. Uploading an image SHALL NOT automatically assign applicability values, a Design Area, or an image-space mapping to that entry. An entry with missing configuration SHALL remain persistable and visibly incomplete.

#### Scenario: Creator adds a local source image
- **WHEN** the creator selects a supported local raster file in an editable Mockup Template dialog and confirms adding it
- **THEN** FusionCanvas copies the file into managed workspace storage and creates one Store-owned source Asset and one source-image entry
- **AND** the entry identifies the managed Asset by stable identity rather than the original file path
- **AND** the entry remains unconfigured until the creator explicitly assigns its metadata and placement
- **AND** the confirmed Template configuration remains local and usable without network access

#### Scenario: Creator saves uploaded images before configuring them
- **WHEN** the creator uploads valid source images and saves the Template without completing one or more image entries
- **THEN** FusionCanvas persists the valid managed Assets and incomplete source-image entries
- **AND** reports each missing applicability or mapping requirement without treating the save as a failure
- **AND** does not report the Template as ready

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

#### Scenario: Creator archives a source image
- **WHEN** the creator confirms archiving a selected active source-image entry and saves the Template
- **THEN** FusionCanvas excludes that entry from current applicability and readiness evaluation
- **AND** preserves the prior entry and Asset identity in historical Template revisions
- **AND** selects a sensible remaining active row or shows the image-collection empty state

### Requirement: Source images use grouped offering option-value applicability
FusionCanvas SHALL let a creator assign each active source-image entry to zero or more applicability groups, where each group identifies one active Option and one or more of its active Option Values belonging to the Template's Blueprint Offering. A Variant satisfies a configured group when it contains any selected value in that group. It satisfies the entry when it satisfies every configured group. The UI SHALL make Color prominent, optimize the initial state for one selected Color with no Size restriction, and progressively disclose Size or any other active Offering Option. FusionCanvas SHALL not link an entry to an Option or Option Value from another Offering or to an archived value.

#### Scenario: Creator configures common color-specific source images
- **WHEN** the creator adds source images for Black, White, Navy, Military Green, and Gray under one Mockup Template
- **THEN** FusionCanvas lets the creator assign each image to its corresponding Color Option Value
- **AND** leaving Size unconfigured means that the image applies to all otherwise-compatible Sizes
- **AND** the five entries remain part of the same named Template and target Placeholder

#### Scenario: Creator selects alternatives within and conditions across Options
- **WHEN** the creator assigns a source image to Black or Navy in the Color group and M or L in the Size group
- **THEN** FusionCanvas records the stable Option and Option Value identities as grouped applicability
- **AND** the entry applies to Black-M, Black-L, Navy-M, and Navy-L variants but not to another Color or Size
- **AND** it does not encode Color or Size labels as relationship keys

#### Scenario: Creator attempts an invalid applicability assignment
- **WHEN** the creator assigns a group with no selected value, repeats the same Option group or Option Value, or selects an Option or value from another Offering or an archived record
- **THEN** FusionCanvas keeps the draft editable and explains the invalid selection
- **AND** confirmed source images, applicability, and revisions remain unchanged

#### Scenario: Creator leaves applicability unconfigured
- **WHEN** an active source-image entry has no applicability groups
- **THEN** FusionCanvas reports that entry as incomplete rather than invalid
- **AND** the entry matches no Variant until the creator explicitly assigns applicability

### Requirement: Template image resolution is exact, per-Variant, and explainable
For each concrete Variant compatible with a Template's shared target Design Area, FusionCanvas SHALL evaluate only active source-image entries with complete applicability and a valid image-space mapping. A Variant matches an entry when it contains at least one selected Option Value from every configured applicability group. A Template SHALL be ready only when every compatible Variant resolves to exactly one active complete source-image entry. FusionCanvas SHALL retain the individual resolved, missing, and ambiguous outcome for every Variant, SHALL never choose among multiple matching entries implicitly, and SHALL NOT discard successful outcomes merely because another Variant is unresolved.

#### Scenario: Color-only images cover every compatible variant
- **WHEN** a Template has one source image for each offered Color and every compatible Variant has exactly one of those Color values
- **THEN** FusionCanvas reports the Template as ready
- **AND** each compatible Variant resolves to the source image assigned to its Color

#### Scenario: A variant has no matching image
- **WHEN** a compatible Variant does not contain all applicability values for any active source image
- **THEN** FusionCanvas identifies that Variant as missing source imagery
- **AND** prevents the Template from being reported as ready
- **AND** preserves exact-one resolutions for other compatible Variants

#### Scenario: A variant matches more than one image
- **WHEN** a compatible Variant satisfies the applicability values of two or more active source-image entries
- **THEN** FusionCanvas identifies the Variant and conflicting entries as ambiguous
- **AND** prevents the Template from being reported as ready or used as though a source had been selected
- **AND** preserves exact-one resolutions for other compatible Variants

#### Scenario: A future consumer requests an unresolved variant
- **WHEN** a consumer requests source resolution for a Variant whose outcome is missing or ambiguous
- **THEN** FusionCanvas returns that recoverable Variant-specific outcome without fabricating or selecting an image
- **AND** the unresolved outcome does not make independently resolved Variants fail resolution

### Requirement: Source-image dimensions and mappings remain valid
FusionCanvas SHALL determine a local source image's width and height from its managed file. Each source-image entry SHALL retain its own optional image-space mapping rather than sharing mapping coordinates with a different source image. An absent mapping SHALL mean incomplete setup; an assigned mapping SHALL be positive and within that entry's image dimensions. The Template SHALL retain one shared target Design Area, and configuring an image SHALL place that shared Design Area within the selected image rather than choosing a different Design Area for each image.

#### Scenario: Creator adds a source image
- **WHEN** a supported raster source image is imported successfully
- **THEN** FusionCanvas displays the managed image in the Template placement editor
- **AND** reports its mapping as not set until the creator explicitly configures placement

#### Scenario: Creator configures placement for one image
- **WHEN** the creator explicitly assigns an in-bounds placement rectangle to the selected source image
- **THEN** FusionCanvas retains that mapping only for the selected entry
- **AND** switching images preserves every entry's independent mapping draft

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

### Requirement: Local source-image setup has a focused, accessible master-detail workflow
FusionCanvas SHALL provide local source-image setup inside the existing focused Mockup Template dialog. The Template name and shared Design Area SHALL remain Template-level fields. An upper image table SHALL expose one clear keyboard-accessible upload action, selection, archive, applicability and mapping summaries, and a complete or incomplete indicator for every active image. A lower selected-image editor SHALL expose applicability controls, preview, and that entry's image-space mapping independently of upload. The dialog SHALL provide Template readiness feedback and explicit Save/Cancel behavior, preserve meaningful unsaved work on cancellation or close requests, and return focus to the invoking or sensible replacement control after successful save, archive, or confirmed discard. Archived Stores SHALL remain read-only.

#### Scenario: Creator opens a new Template dialog
- **WHEN** the creator opens an editable Mockup Template dialog
- **THEN** FusionCanvas identifies the local **Mockup source images** configuration and exposes an Upload image action
- **AND** it does not show a provider-catalog loading, unavailable, synchronization, or credential state
- **AND** keyboard focus is placed at the first required Template field or the next incomplete source-setup action

#### Scenario: Creator uploads images independently of metadata
- **WHEN** the creator uploads a valid file from the image-table action
- **THEN** FusionCanvas adds a row without copying applicability from the currently selected image or assigning other metadata automatically
- **AND** selects the newly added row and exposes its incomplete metadata editor

#### Scenario: Creator selects an image row
- **WHEN** the creator selects a different active image in the upper table
- **THEN** the lower editor displays that image's own applicability and mapping draft
- **AND** preserves unsaved edits made to previously selected image rows

#### Scenario: Editor reports image completeness
- **WHEN** the image table contains configured and unconfigured entries
- **THEN** every row visibly distinguishes complete setup from missing applicability, missing mapping, invalid mapping, or another actionable metadata gap
- **AND** the Template-level readiness summary separately reports missing or ambiguous Variant coverage

#### Scenario: Creator cancels while configuring sources
- **WHEN** the creator has made meaningful unsaved source-image, applicability, or mapping changes and requests Cancel, Escape, or dialog close
- **THEN** FusionCanvas offers to discard or continue editing
- **AND** continue editing preserves the complete draft and focus
- **AND** confirmed discard creates no Asset, source entry, revision, or persistence change

#### Scenario: Archived Store is reviewed
- **WHEN** the Mockup Template belongs to an archived Store
- **THEN** Upload, source replacement, archive, applicability editing, placement editing, and Save are unavailable
- **AND** existing source identity and readiness feedback remain reviewable
