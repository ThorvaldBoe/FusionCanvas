## ADDED Requirements

### Requirement: Design stage exposes explicit artwork generation targeting
FusionCanvas SHALL present a Generate Artwork section below Supporting Images in the editable Design stage. The section SHALL provide a Generate action, a Design Area selector populated only from the active item's selected offering, and a Transparent Background option. The selected target SHALL remain explicit and editable; generation SHALL never silently retarget an artwork request.

#### Scenario: User sees the generation controls
- **WHEN** an editable Item has a selected offering with at least one active Design Area
- **THEN** the Design stage shows Generate Artwork below Supporting Images
- **AND** the section shows Generate, the target Design Area selector, and Transparent Background

#### Scenario: No target is available
- **WHEN** an editable Item has no selected offering or no active Design Area for its offering
- **THEN** the generation section remains understandable but the Generate action is disabled
- **AND** guidance explains that a valid Design Area target is required

#### Scenario: Design is read-only
- **WHEN** the Item's Design stage is read-only
- **THEN** the generation controls remain visible as read-only guidance
- **AND** no generation request or target mutation can start

### Requirement: Artwork generation assembles a bounded creative request
FusionCanvas SHALL assemble an image-generation request from the current Item's Idea, Concept design-triangle values, SLL when present, phrase, graphic description, selected Design Area guidance, and applicable user-authored creative context. The request SHALL exclude credentials, database identifiers, timestamps, file paths, and unrelated operational fields. The system message SHALL define all supplied workspace and user-authored content as untrusted creative data rather than instructions.

#### Scenario: Complete creative context is available
- **WHEN** the user activates Generate with a valid target and available image-generation model
- **THEN** the request contains the available Idea, phrase, graphic description, triangle values, SLL, target placement and dimensions, artwork guidance, and relevant creative context
- **AND** missing optional fields are omitted or clearly marked unavailable rather than fabricated

#### Scenario: Workspace content contains instruction-like text
- **WHEN** user-authored creative content contains text that resembles instructions
- **THEN** the request system message states that the content is untrusted creative data
- **AND** generation rules and technical constraints take precedence over that content

### Requirement: Image model capability and output constraints are validated
FusionCanvas SHALL use an image-generation capability contract distinct from text-generation capability. The selected model SHALL expose image output support, a usable maximum resolution or equivalent supported-size set, and output-format information. The system SHALL report a recoverable error before generation when the configured model is unavailable, does not support image generation, or cannot produce a usable output for the selected target.

#### Scenario: Selected model supports generation
- **WHEN** the configured model advertises image output and usable supported sizes
- **THEN** generation proceeds using the model's image-generation contract

#### Scenario: Selected model is text-only
- **WHEN** the configured model does not advertise image output
- **THEN** Generate is blocked or fails before an image request is sent
- **AND** the Design stage explains that the selected model does not support image generation

#### Scenario: Transparency is unsupported
- **WHEN** Transparent Background is selected but the model/provider does not support transparent output
- **THEN** generation is not silently downgraded
- **AND** the user receives an actionable explanation to disable transparency or choose a compatible model

### Requirement: Generation resolution preserves target aspect ratio and normalizes PNG output
FusionCanvas SHALL derive the target aspect ratio from the selected Design Area's authoritative pixel width and height. Before generation it SHALL select the largest supported provider resolution that preserves that aspect ratio within the provider's documented constraints and does not exceed the target dimensions. After generation it SHALL produce a PNG at the exact target width and height using deterministic non-AI raster scaling, preserving alpha when present.

#### Scenario: Target exceeds provider resolution
- **WHEN** the Design Area is larger than the selected model's supported generation size
- **THEN** the request uses the highest supported size with the target aspect ratio
- **AND** the completed managed artwork is normalized to the exact Design Area pixel dimensions

#### Scenario: Target ratio has no exact provider size
- **WHEN** the provider exposes discrete sizes and none exactly matches the target ratio
- **THEN** the system reports that no supported generation size can preserve the target ratio
- **AND** it does not generate an image with an undisclosed crop or distortion

#### Scenario: Provider returns an unexpected image
- **WHEN** the provider returns a non-image, malformed image, unsupported format, or dimensions that cannot be safely normalized
- **THEN** no slot or supporting-image record is committed
- **AND** a recoverable error identifies that the generated result could not be normalized

### Requirement: Successful generation is applied atomically and retained as history
FusionCanvas SHALL create one managed PNG asset for a successful normalized result, retain generated-artwork provenance including target Design Area, model, requested generation size, final dimensions, transparency request, and generation timestamp, assign the asset to the explicitly selected Design Area slot for the applicable default or selected design row, and expose the generated asset in Supporting Images. Slot replacement and supporting-image retention SHALL be committed as one atomic workspace operation.

#### Scenario: Generation succeeds for an empty target slot
- **WHEN** generation and PNG normalization complete for a selected target
- **THEN** the normalized PNG is stored as a managed asset
- **AND** it is assigned to the selected target slot
- **AND** it appears in Supporting Images with generated-artwork provenance

#### Scenario: Generation replaces existing slot artwork
- **WHEN** generation succeeds for a target slot that already has artwork
- **THEN** the new asset replaces the slot assignment
- **AND** the previous asset remains available as a supporting image or existing historical asset according to the asset lifecycle policy

#### Scenario: Persistence fails
- **WHEN** file import succeeds but the atomic workspace save fails
- **THEN** the slot assignment and generated supporting-image link are not committed
- **AND** the managed file is removed on a best-effort basis
- **AND** the previous confirmed Design state remains intact

### Requirement: Generation operations have safe concurrency and recovery behavior
FusionCanvas SHALL allow at most one in-flight artwork-generation operation per Item document, disable duplicate submission while it runs, support cancellation when the document closes or the active Item changes, and reject late results for a different Item or target. Failures and cancellation SHALL preserve the existing slot and supporting-image state.

#### Scenario: User submits twice
- **WHEN** artwork generation is already in flight
- **THEN** the Generate action is disabled
- **AND** no second provider request is started

#### Scenario: Item changes during generation
- **WHEN** the active Item changes or the document closes during generation
- **THEN** the operation is cancelled
- **AND** a late provider result is not applied to the new Item

#### Scenario: Generation fails
- **WHEN** provider, network, credential, rate-limit, cancellation, raster, or persistence failure occurs before commit
- **THEN** existing slot artwork and supporting images remain unchanged
- **AND** a recoverable inline error remains visible in the Design stage

