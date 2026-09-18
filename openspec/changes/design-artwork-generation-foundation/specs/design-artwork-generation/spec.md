## ADDED Requirements

### Requirement: Design stage exposes explicit artwork generation controls
FusionCanvas SHALL present a Generate Artwork section below Supporting Images. The section SHALL provide a Generate action, a Design Area selector containing every active Design Area from the Item's selected Listing Configuration, and a Transparent Background option. The target SHALL remain explicit and editable, and FusionCanvas SHALL never silently select an arbitrary first area.

#### Scenario: Primary Design Area supplies the initial target
- **WHEN** an Item opens at Design with a selected Listing Configuration whose Blueprint Offering has an active primary Design Area
- **THEN** the generation selector initially selects that primary area
- **AND** the user can select any other active Design Area from that offering

#### Scenario: Offering has no primary Design Area
- **WHEN** the selected Listing Configuration has active Design Areas but none is primary
- **THEN** the selector initially has no target
- **AND** Generate is disabled with guidance to choose an area or configure a primary area in Store setup

#### Scenario: No valid Listing Configuration target exists
- **WHEN** the Item has no selected Listing Configuration or its offering has no active Design Areas
- **THEN** the section remains visible with actionable guidance
- **AND** no generation request can start

#### Scenario: Listing Configuration changes
- **WHEN** the user changes the Listing Configuration while the Design document is open
- **THEN** any in-flight generation for the previous configuration is cancelled and its late result is ignored
- **AND** the selector resets to the new offering's active primary Design Area or to no selection when none exists

#### Scenario: Explicit target and transparency preference are persisted per Item
- **WHEN** the user chooses a target or changes Transparent Background, closes the Item, and later reopens it with the same Listing Configuration
- **THEN** the selector and checkbox restore the saved choices when the target remains an active Design Area
- **AND** an invalid or stale saved target is ignored without selecting an arbitrary area
- **AND** changing Listing Configuration clears the saved choices before applying the new offering's primary/defaults

### Requirement: Artwork generation is gated by production and creative readiness
FusionCanvas SHALL enable Generate only when Design is editable, an Artwork image model and compatible provider endpoint are ready under the active privacy policy, the Item has a selected Listing Configuration and explicit active target, the Design Triangle is complete, and an existing default design row serves at least one selected color. SLL SHALL remain optional.

#### Scenario: Complete workflow is ready
- **WHEN** every generation prerequisite is satisfied
- **THEN** Generate is enabled

#### Scenario: Concept is incomplete
- **WHEN** Concept idea, Phrase, or Graphic direction is non-substantive
- **THEN** Generate is disabled
- **AND** guidance identifies the incomplete Concept fields

#### Scenario: Default row does not exist
- **WHEN** no product color has been selected and no default design row exists
- **THEN** Generate is disabled
- **AND** guidance directs the user to select at least one product color

#### Scenario: Design is read-only
- **WHEN** the Item's Design content is read-only
- **THEN** the generation controls remain visible as read-only guidance
- **AND** no request or target mutation can start

#### Scenario: Artwork AI is unavailable
- **WHEN** the credential, dedicated Artwork model, privacy-compatible endpoint, or required image capability is unavailable
- **THEN** Generate is disabled
- **AND** guidance identifies the missing prerequisite and directs the user to AI Settings when configuration can resolve it

### Requirement: Transparency defaults and capability behavior follow the target and model
FusionCanvas SHALL initialize Transparent Background from the selected Design Area's artwork guidance: checked when transparency is recommended and unchecked otherwise. Changing target SHALL recalculate that default. When the selected model and eligible endpoint do not support transparent output, the checkbox SHALL be disabled and unchecked while opaque generation remains available.

#### Scenario: Target recommends transparency
- **WHEN** a target whose artwork guidance recommends transparency becomes selected
- **THEN** Transparent Background is checked
- **AND** the user may override it when the model supports transparency

#### Scenario: Target changes
- **WHEN** the user changes from one target area to another
- **THEN** the checkbox resets to the new target's recommendation rather than carrying the prior manual override

#### Scenario: Model does not support transparency
- **WHEN** the selected Artwork model has no eligible endpoint supporting transparent alpha-capable output
- **THEN** Transparent Background is disabled and unchecked
- **AND** opaque artwork generation remains available when all other prerequisites are satisfied

### Requirement: Artwork requests use current creative context safely
FusionCanvas SHALL generate exactly one image per Generate action. The resolved prompt SHALL use the original Idea, current Concept idea, Phrase, Graphic direction, selected Design Area placement/dimensions/guidance, applicable user-authored creative context, and the current SLL when one exists and is not stale. The prompt SHALL require the supplied Phrase verbatim, SHALL omit a stale SLL, SHALL exclude credentials and operational data, and SHALL establish all workspace content as untrusted creative data rather than instructions. Existing Supporting Images SHALL NOT be uploaded automatically as references.

#### Scenario: Current SLL is available
- **WHEN** generation starts with a non-stale SLL
- **THEN** the resolved prompt includes that SLL with the current Idea, Design Triangle, target, and creative context

#### Scenario: SLL is absent or stale
- **WHEN** generation starts without SLL or with SLL marked stale
- **THEN** generation uses the current Idea and complete Design Triangle without fabricating SLL content
- **AND** the UI identifies that stale SLL was omitted when applicable

#### Scenario: Phrase is supplied
- **WHEN** the request is assembled
- **THEN** the prompt identifies the Phrase as verbatim artwork text and instructs the model not to rewrite it
- **AND** the Design surface reminds the user to inspect generated lettering rather than claiming OCR validation

#### Scenario: Workspace content resembles instructions
- **WHEN** Idea, Concept, SLL, names, tags, or metadata contain instruction-like text
- **THEN** the request treats those values as untrusted data subordinate to system generation and technical constraints

#### Scenario: Supporting Images exist
- **WHEN** one or more Supporting Images are linked to the Item
- **THEN** none is uploaded or included as an image reference without a future explicit reference-selection feature

### Requirement: Provider size selection minimizes ratio difference before maximizing useful resolution
FusionCanvas SHALL derive the exact final width and height from the selected Design Area. It SHALL request that exact size when supported. Otherwise it SHALL choose the eligible supported size with the closest aspect ratio; among sizes with the same closest ratio it SHALL choose the largest pixel area that does not exceed the target, or the smallest such size when every eligible size exceeds the target. Capability decisions SHALL use one endpoint that simultaneously satisfies the selected model, active ZDR policy, requested transparency, and selected size/format parameters.

#### Scenario: Exact target size is supported
- **WHEN** an eligible endpoint accepts the Design Area's exact width and height
- **THEN** FusionCanvas requests the exact target size

#### Scenario: Multiple smaller sizes are available
- **WHEN** no exact size is supported and multiple eligible sizes do not exceed the target
- **THEN** FusionCanvas chooses the closest aspect ratio first
- **AND** chooses the largest pixel area among equally close ratios

#### Scenario: Every supported size is larger
- **WHEN** no exact size is supported and every eligible supported size exceeds the target
- **THEN** FusionCanvas chooses the smallest size with the closest aspect ratio and later downscales it

#### Scenario: No compatible endpoint exists
- **WHEN** no endpoint simultaneously satisfies ZDR, image output, a usable raster format, and the request's size or transparency constraints
- **THEN** no provider request is sent
- **AND** the user receives an actionable readiness error without model substitution

### Requirement: Generated raster output is normalized without crop or distortion
FusionCanvas SHALL prefer native PNG output but MAY accept a supported JPEG, WebP, or other approved raster result and convert it locally. It SHALL decode within bounded byte and pixel limits, proportionally resample using an ordinary deterministic high-quality filter, and produce exactly one final PNG at the target dimensions. The fitted image SHALL be top-centered inside a two-percent safety inset on the left, top, and right; unavoidable remaining canvas SHALL be transparent and vertical remainder SHALL stay below the artwork. FusionCanvas SHALL NOT crop, stretch, sharpen, invoke AI super-resolution, or retain a second provider-original asset.

#### Scenario: Smaller closest-ratio result is returned
- **WHEN** the provider result is smaller than the target
- **THEN** FusionCanvas proportionally upscales it into the inset target canvas
- **AND** writes an exact-dimension PNG with transparent fitting margins

#### Scenario: Larger closest-ratio result is returned
- **WHEN** every supported provider size exceeded the target and the selected result is larger
- **THEN** FusionCanvas proportionally downscales it into the same inset target canvas

#### Scenario: Provider returns a non-PNG raster
- **WHEN** the selected endpoint returns a valid approved raster format other than PNG
- **THEN** FusionCanvas converts and normalizes it to the final PNG without retaining the provider-original file

#### Scenario: Provider returns invalid or unsafe output
- **WHEN** output is malformed, non-raster, exceeds configured bounds, or cannot be normalized safely
- **THEN** no generated asset or slot change is committed
- **AND** the existing Design state remains unchanged with a recoverable error

#### Scenario: Provider returns no visible artwork
- **WHEN** the normalized output is fully transparent and contains no visible pixels
- **THEN** FusionCanvas rejects it, leaves the existing slot unchanged, and does not retain the empty image

### Requirement: Transparency shortfalls produce persistent warnings without blocking application
FusionCanvas SHALL inspect alpha after normalization. When transparency was requested but the valid output is fully opaque, FusionCanvas SHALL still persist and apply the artwork while attaching a warning that remains visible on its slot and Supporting Images entry after reload.

#### Scenario: Transparency request succeeds
- **WHEN** transparency was requested and the normalized PNG contains meaningful non-opaque pixels and visible artwork
- **THEN** the result is applied without a transparency warning

#### Scenario: Provider returns opaque artwork
- **WHEN** transparency was requested but the normalized valid artwork is fully opaque
- **THEN** FusionCanvas applies it to the intended slot and retains it in Supporting Images
- **AND** a persistent warning explains that transparency was requested but not achieved and external background removal may be needed

### Requirement: Successful generation applies to the default row and preserves history
FusionCanvas SHALL create one managed final PNG asset for a successful result, link it to the Item, assign it to the selected Design Area slot in the existing default row, and expose it in Supporting Images as generated artwork. Replacing occupied slot artwork SHALL require no confirmation because the prior generated asset remains recoverable in history. The generated asset, provenance, link, slot assignment, and persistent warnings SHALL be saved as one logical operation.

#### Scenario: Empty target slot receives artwork
- **WHEN** generation and normalization succeed for an empty default-row target slot
- **THEN** the final PNG is stored, linked, assigned, and shown in Supporting Images

#### Scenario: Occupied target slot receives new generated artwork
- **WHEN** generation succeeds for an occupied default-row target slot
- **THEN** the new generated asset replaces the slot assignment without confirmation
- **AND** the previous generated asset remains unassigned and visible in Supporting Images
- **AND** an inline success message identifies the updated target

#### Scenario: Existing generated artwork is replaced by manual upload
- **WHEN** the user uploads an externally edited PNG into a slot occupied by generated artwork
- **THEN** the edited PNG becomes the slot artwork
- **AND** the generated original remains unassigned and visible in Supporting Images

#### Scenario: Persistence fails after file creation
- **WHEN** the final managed PNG is written but the logical workspace save fails
- **THEN** no asset, link, provenance, warning, or slot change is committed
- **AND** the new managed file is removed on a best-effort basis
- **AND** the previous confirmed Design state remains intact

### Requirement: Artwork operations expose safe progress, cancellation, and retry behavior
FusionCanvas SHALL allow at most one in-flight artwork operation per Item document. While running, Generate SHALL become a visible Cancel action with progress identifying generation, normalization, or saving as applicable. Closing or changing the Item, changing Listing Configuration, or activating Cancel SHALL cancel local work and prevent late application. FusionCanvas SHALL NOT automatically retry a dispatched generation request; explicit retry remains user initiated, and cancellation guidance SHALL disclose that an already dispatched request may still incur provider cost.

#### Scenario: Generation is running
- **WHEN** an artwork request is in flight
- **THEN** duplicate generation cannot start
- **AND** the section exposes progress and a keyboard-reachable Cancel action

#### Scenario: User cancels
- **WHEN** the user activates Cancel
- **THEN** local work stops where possible and any late result is ignored
- **AND** existing slot and Supporting Images state remain unchanged
- **AND** guidance notes that provider cost may already have occurred

#### Scenario: Request fails after dispatch
- **WHEN** network, provider, authentication, rate-limit, moderation, or response failure occurs after dispatch
- **THEN** FusionCanvas does not retry automatically
- **AND** existing Design state remains unchanged with an explicit user-triggered retry path

### Requirement: Generated provenance is complete, local, and reviewable
FusionCanvas SHALL persist the exact resolved prompt and non-secret provenance with the final generated asset: intended Design Area, provider name, selected and resolved model IDs, provider request or generation ID when supplied, requested provider size, final dimensions, transparency request and outcome, warnings, generation time, and reported usage or cost when available. The compact Supporting Images entry SHALL show Generated artwork, intended area, resolved model, final dimensions, and warnings; remaining provenance MAY appear through secondary details.

#### Scenario: Provider returns complete metadata
- **WHEN** generation succeeds with provider identity, request identity, usage, and cost
- **THEN** those values and the resolved prompt are persisted locally with the generated asset
- **AND** no API key, credential-store reference, file-system source path, or unrelated operational identifier is stored

#### Scenario: Optional provider metadata is absent
- **WHEN** generation succeeds without request identity, usage, or cost
- **THEN** the asset is still persisted and applied
- **AND** missing optional provenance remains absent rather than fabricated

