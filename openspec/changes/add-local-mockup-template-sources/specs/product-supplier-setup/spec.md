## MODIFIED Requirements

### Requirement: Provider mockup image selection communicates source and recovery
FusionCanvas SHALL identify Mockup Template local source-image configuration with persistent visible labels and accessible names. The focused Template editor SHALL explain that the current module uses managed local source images which are uploaded independently and may then be associated with the active Offering's option values. It SHALL expose a keyboard-accessible local upload route and SHALL distinguish per-image metadata completeness, Template-level Variant coverage, file-import failures, and ready configuration without fabricating external candidates or implying current Printify synchronization.

#### Scenario: User opens local source-image configuration
- **WHEN** the Mockup Template editor is shown
- **THEN** it identifies the configuration as **Mockup source images**
- **AND** nearby instructions explain that the creator uploads local images and configures each selected row independently
- **AND** it provides a clear accessible action to upload a source image whenever the Store is editable

#### Scenario: Template has no source images
- **WHEN** the configured Template has no active local source-image entries
- **THEN** state text explains that the Template needs source imagery before it is ready
- **AND** the browse/import action remains available when the Store is editable

#### Scenario: Template source configuration is incomplete
- **WHEN** one or more compatible concrete Variants have no exact source-image match or have multiple matches
- **THEN** the editor distinguishes the affected Variant and the missing or ambiguous source condition
- **AND** it directs the creator to add or adjust local source images and applicability values
- **AND** it permits the incomplete Template to be saved while preserving successfully resolved Variants

#### Scenario: Template source configuration is ready
- **WHEN** every compatible concrete Variant resolves to exactly one active local source-image entry
- **THEN** the editor reports that the Template source configuration is ready
- **AND** it presents the selected managed image and its placement configuration without requiring a provider-catalog request

#### Scenario: Local source import fails
- **WHEN** the creator's local source-image import or managed-image preview cannot be completed
- **THEN** the editor identifies the recoverable local failure without creating a fabricated candidate
- **AND** preserves confirmed Template configuration and the editable draft where one exists

### Requirement: Mockup Template management uses a focused guarded master-detail editor
The focused Mockup Template dialog SHALL retain Template identity and one shared target Design Area at Template level, SHALL expose an upper source-image collection with upload, selection, archive, summaries, and complete/incomplete status, and SHALL expose a lower selected-image editor with grouped applicability and per-image mapping. It SHALL preserve catalog validation, revision and persistence behavior, archived-store read-only policy, guarded dismissal, and focus behavior.

#### Scenario: User edits a Mockup Template
- **WHEN** the user selects an existing template's Edit action
- **THEN** the dialog populates its stable identity, shared target Design Area, source-image rows, grouped applicability, per-image mappings, and revision context

#### Scenario: User uploads a source image independently of metadata
- **WHEN** the user uploads a valid local raster file from the source-image collection
- **THEN** one new incomplete row is added without inheriting applicability or mapping from another row
- **AND** the new row is selected for metadata editing

#### Scenario: User configures one selected source image
- **WHEN** the user selects Color values and optionally Size or another Offering Option for the selected row and enters a valid mapping
- **THEN** those metadata values remain attached only to that row
- **AND** values are OR alternatives within an Option and AND conditions across Options

#### Scenario: User saves an incomplete Template
- **WHEN** one or more source-image rows have missing applicability or mapping
- **THEN** FusionCanvas saves the Template draft and visibly reports the incomplete rows
- **AND** the Template is not reported ready while independently resolved Variants remain individually identifiable

#### Scenario: User archives a source-image row
- **WHEN** the user confirms archiving a selected active source-image row
- **THEN** the row leaves current resolution and readiness evaluation while historical revisions retain its identity
