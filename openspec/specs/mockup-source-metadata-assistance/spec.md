# Mockup Source Metadata Assistance

## Purpose

Provide optional, batch AI assistance for local Mockup Template source-image metadata while keeping the workflow draft-first, reviewable, provider-independent, and compatible with the existing Template Save workflow.

## Requirements

### Requirement: The Mockup Template editor provides optional batch metadata assistance
The focused Mockup Template source-image editor SHALL expose a **Set up metadata with AI** action when one or more local source-image rows are selected. The action SHALL remain unavailable when no row is selected, while the editor is read-only, or while another metadata-assistance operation is running. The action SHALL use the configured General AI path and SHALL NOT be required for manual source-image editing.

#### Scenario: No source image is selected
- **WHEN** the source-image table has no selected row
- **THEN** the metadata-assistance action is disabled or unavailable
- **AND** no AI request can be started

#### Scenario: Selected source images are processed together
- **WHEN** the creator invokes metadata assistance with one or more selected new or existing local source-image rows
- **THEN** one user operation produces an individual result for every selected row
- **AND** the operation uses the General AI configuration
- **AND** unselected rows remain unchanged

#### Scenario: AI is unavailable
- **WHEN** General AI is not configured, its credential is unavailable, or its selected model cannot satisfy a required request
- **THEN** the editor presents actionable unavailable guidance
- **AND** existing source-image metadata remains unchanged
- **AND** manual metadata editing remains available when the editor itself is editable

### Requirement: Metadata assistance uses project context and conditional image input
The metadata-assistance operation SHALL provide the AI with the selected source-image filenames, dimensions, current metadata, active Offering options and values, existing source-image metadata and mappings, and the selected Template's target Design Area context. The operation SHALL use recognizable filename and existing metadata signals before requesting image content. It SHALL include image content only for selected rows whose unresolved result requires visual inspection, and SHALL honor the AI Settings Zero Data Retention preference for every provider request.

#### Scenario: Filename resolves the product color
- **WHEN** a selected source-image filename contains an unambiguous active Color value
- **THEN** the operation may resolve that Color from the filename and project context
- **AND** it does not include that image's content solely to determine the already-resolved Color

#### Scenario: Visual inspection is needed
- **WHEN** a selected source image has unresolved metadata after filename and existing-context analysis
- **AND** the configured General model supports image input
- **THEN** the operation includes that image's raster content in the provider-independent AI request
- **AND** the request preserves the configured Zero Data Retention preference

#### Scenario: Visual inspection is needed but unsupported
- **WHEN** a selected source image requires visual inspection
- **AND** the configured General model does not support image input
- **THEN** that image receives an actionable uncertain or failed result
- **AND** other selected images that can be processed from metadata continue to receive results

### Requirement: Suggestions populate editable drafts without hidden persistence
The operation SHALL apply usable results to the selected source-image drafts immediately so the resulting per-image Color, Size, and placement values remain visible and editable. It SHALL use the existing Mockup Template Save workflow for persistence. It SHALL preserve existing values when the AI result is uncertain or cannot be validated, and MAY replace existing values only when the result is sufficiently confident and contextually supported. No AI operation SHALL create a database revision or persist source-image changes before the creator saves the Template.

#### Scenario: Creator reviews generated draft metadata
- **WHEN** metadata assistance returns a usable result for a selected source image
- **THEN** the draft row and selected-image editor reflect the resulting values
- **AND** the creator can edit those values before saving
- **AND** closing or cancelling the unsaved Template preserves the existing discard workflow

#### Scenario: Existing metadata is uncertain or unsupported
- **WHEN** a result is missing, malformed, ambiguous, or below the confidence required for replacement
- **THEN** the existing draft metadata is retained
- **AND** the row or operation status identifies that review is needed

#### Scenario: Creator saves assisted metadata
- **WHEN** the creator reviews assisted values and saves the Mockup Template
- **THEN** the existing source-image persistence and revision workflow stores the final edited values
- **AND** no separate AI-result persistence is required

### Requirement: Size and placement assistance remain bounded by known project data
The operation SHALL map AI suggestions only to active Offering Option Values known to the selected Template's Offering. When Size cannot be inferred reliably, it SHALL select all active values of the Offering's Size Option. Placement assistance SHALL reuse a suitable existing mapped source-image placement from the current draft context, preferring compatible metadata and unambiguous established mappings. If no suitable reference exists or references conflict, placement SHALL remain unchanged and the result SHALL be marked for review.

#### Scenario: Size cannot be inferred
- **WHEN** a selected image has no reliable Size signal
- **THEN** all active Size Option Values for the Offering are selected
- **AND** the result does not invent a value outside the Offering

#### Scenario: Existing placement convention is unambiguous
- **WHEN** an existing source image has a compatible mapping and provides the best available placement reference
- **THEN** the selected image receives that mapping with dimensions validated against the selected image
- **AND** the mapping remains editable in the placement editor

#### Scenario: Placement references conflict or are absent
- **WHEN** no compatible mapped source image exists or candidate mappings conflict materially
- **THEN** the selected image's existing mapping is preserved
- **AND** the result clearly identifies placement as requiring review

### Requirement: Partial results and failures are visible per image
The operation SHALL produce a status for every selected source image, including successful, uncertain, and failed outcomes. A failure or uncertain result for one image SHALL NOT prevent valid results for other selected images. Provider failures, malformed responses, unreadable image content, unsupported image input, and cancellation SHALL surface recoverable guidance without discarding existing draft metadata.

#### Scenario: One selected image fails
- **WHEN** the provider cannot produce a valid result for one selected image
- **THEN** that image is marked failed or requiring review with recoverable guidance
- **AND** successful results for other selected images remain applied

#### Scenario: Operation is cancelled
- **WHEN** the creator cancels the metadata-assistance operation or closes/discards the draft while it is running
- **THEN** no late result is applied to the current editor state
- **AND** existing metadata remains available through the normal draft/discard behavior
