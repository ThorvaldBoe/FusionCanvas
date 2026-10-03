## ADDED Requirements

### Requirement: Mockup source-image upload supports multi-select through the existing action
FusionCanvas SHALL allow the focused Mockup Template source-image upload action to select one or more supported local raster files in a single picker interaction. The action SHALL use an image-specific filter appropriate to the raster metadata reader and SHALL remain distinct from the Store Assets single-file import route.

#### Scenario: Creator selects multiple source images
- **WHEN** the creator chooses several supported raster files from the Mockup Template image-table upload action
- **THEN** FusionCanvas stages one new source-image draft row for each selected file
- **AND** the rows remain independently configurable
- **AND** the first newly staged row is selected for metadata editing

#### Scenario: Creator selects one source image
- **WHEN** the creator chooses one supported raster file from the same upload action
- **THEN** FusionCanvas preserves the existing single-image workflow and stages one new source-image draft row
- **AND** the row has no copied applicability or image-space mapping

#### Scenario: Bulk selection preserves per-image independence
- **WHEN** multiple source-image drafts are staged together
- **THEN** each draft retains its own file path, dimensions, preview diagnostic, applicability draft, and mapping draft
- **AND** configuring or selecting one draft does not copy metadata or mapping to another draft

#### Scenario: One selected file has unreadable metadata
- **WHEN** metadata reading fails for one file in a multi-file selection
- **THEN** FusionCanvas retains the other selected files as source-image drafts
- **AND** retains the affected file as an actionable incomplete draft with a file-specific preview warning
- **AND** does not silently discard the failed selection

#### Scenario: Creator saves a mixed bulk selection
- **WHEN** the creator saves a template containing multiple newly staged source-image drafts and one draft later fails source-image validation or import
- **THEN** FusionCanvas preserves the existing sequential-save and partial-completion diagnostics
- **AND** reports how many source-image changes were saved and identifies the failure
- **AND** does not copy or merge metadata between the selected files

#### Scenario: Archived Store is reviewed
- **WHEN** the Mockup Template belongs to an archived Store
- **THEN** the multi-file upload action is unavailable together with the existing source-image mutations
