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
