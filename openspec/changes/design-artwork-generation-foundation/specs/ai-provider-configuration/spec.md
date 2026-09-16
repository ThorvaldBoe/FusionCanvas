## ADDED Requirements

### Requirement: Artwork uses an independent image-model catalog and profile
FusionCanvas SHALL provide one dedicated Artwork profile backed by an image-generation model catalog. Artwork SHALL NOT inherit the text-only General profile, and text profiles SHALL continue using their existing text-capable catalog and parameters unchanged. The Artwork profile SHALL persist an explicit model selection or no selection and SHALL expose only model choice and readiness in this module.

#### Scenario: User configures Artwork
- **WHEN** the image-model catalog is available
- **THEN** the Artwork selector lists image-output models allowed by the active privacy policy
- **AND** does not list text-only models

#### Scenario: General text model changes
- **WHEN** the user changes the General text profile
- **THEN** the Artwork model selection and readiness remain unchanged

#### Scenario: Artwork model is absent
- **WHEN** no Artwork model is selected or the saved model disappears from the current image catalog
- **THEN** Artwork is unavailable
- **AND** FusionCanvas preserves the explicit saved model identity without silently selecting a replacement

### Requirement: Image catalog capabilities are endpoint-specific and privacy aware
FusionCanvas SHALL discover image models separately from text models and SHALL obtain definitive per-endpoint capabilities for the selected image model. Capability data SHALL include provider identity, ZDR eligibility, output raster formats, supported sizes or aspect ratios/resolution tiers, and transparency support. Model-level union metadata SHALL NOT be treated as proof that one eligible endpoint supports every requested capability.

#### Scenario: ZDR is required
- **WHEN** the global Zero Data Retention setting is enabled
- **THEN** Artwork readiness and generation consider only ZDR-compatible endpoints

#### Scenario: One model has heterogeneous endpoints
- **WHEN** model-level metadata combines capabilities that no single endpoint supports together
- **THEN** FusionCanvas evaluates each endpoint independently
- **AND** reports ready only when one endpoint satisfies the complete request

#### Scenario: Image catalog refresh fails
- **WHEN** live image-catalog refresh fails and a policy-compatible cached image catalog exists
- **THEN** the saved Artwork selection may remain usable with stale-catalog guidance
- **AND** request-time endpoint enforcement remains mandatory

#### Scenario: No image catalog is available
- **WHEN** refresh fails and no policy-compatible image catalog is cached
- **THEN** Artwork is unavailable without affecting configured text generation

### Requirement: Image generation is submitted once through a provider-neutral boundary
FusionCanvas SHALL expose provider-neutral image request/result contracts in Application and SHALL translate OpenRouter image catalog, endpoint, request, response, usage, and failure data only in Integration. A dispatched image-generation POST SHALL receive no automatic retry. Request routing SHALL require the selected endpoint's supported parameters and active ZDR policy, and SHALL return bounded base64 raster data plus normalized non-secret provenance.

#### Scenario: Compatible request is dispatched
- **WHEN** Application submits one validated Artwork request
- **THEN** Integration sends one provider request using the selected model and compatible endpoint constraints
- **AND** returns at most the single requested image result to the application boundary

#### Scenario: Transient failure follows dispatch
- **WHEN** a timeout, network interruption, rate limit, or provider failure occurs after dispatch
- **THEN** Integration does not retry the image request automatically
- **AND** returns a normalized failure that explains an explicit retry may create another charge

#### Scenario: Provider returns excessive data
- **WHEN** encoded or decoded image data exceeds configured byte or pixel bounds
- **THEN** Integration rejects the response as invalid without persisting it
