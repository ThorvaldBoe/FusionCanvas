## ADDED Requirements

### Requirement: AI profiles expose an explicit model and routing policy
FusionCanvas SHALL store an AI model selection and an explicit routing policy independently for each General, Ideation, and Concept profile. The supported routing policies SHALL be Automatic, Specific provider, and Exact endpoint.

#### Scenario: Existing profile is migrated
- **WHEN** FusionCanvas loads an AI profile created before provider routing was available
- **THEN** it preserves the selected model and parameters
- **AND** assigns the profile the Automatic routing policy
- **AND** does not silently choose a provider or endpoint

#### Scenario: User selects a specific provider
- **WHEN** the user selects Specific provider and a provider for a profile
- **THEN** the profile retains the provider identity as a non-secret application preference
- **AND** the profile explains that endpoint variants for that provider may still differ unless one exact endpoint is selected

#### Scenario: User selects an exact endpoint
- **WHEN** the user selects Exact endpoint and one endpoint variant for a profile
- **THEN** the profile retains the exact endpoint identity
- **AND** the profile indicates that alternate providers will not be used for requests from that profile

### Requirement: Endpoint choices come from current or cached endpoint metadata
FusionCanvas SHALL obtain endpoint choices for a selected model from OpenRouter endpoint metadata, cache bounded non-secret metadata, and display provider identity, endpoint variant, context limit, maximum completion limit, supported parameters, privacy compatibility, pricing, and available performance metadata when supplied.

#### Scenario: Endpoint metadata loads
- **WHEN** endpoint metadata is available for the selected model
- **THEN** FusionCanvas lists the available provider and endpoint choices with their reported capabilities
- **AND** does not represent model-level limits as endpoint-specific facts

#### Scenario: Endpoint metadata is stale
- **WHEN** the endpoint list is available only from a previous successful cache load
- **THEN** FusionCanvas keeps the cached choices available
- **AND** visibly marks the metadata as stale with a refresh action
- **AND** does not claim that the stale capabilities are current

#### Scenario: Endpoint metadata cannot be loaded
- **WHEN** no usable endpoint metadata exists for a selected model
- **THEN** FusionCanvas keeps the saved model and routing choice visible when possible
- **AND** marks new endpoint selection unavailable
- **AND** explains that refreshing endpoint information is required before selecting an unrecognized endpoint

### Requirement: Routing policy is enforced without silent provider substitution
FusionCanvas SHALL translate each routing policy into an explicit OpenRouter provider request and SHALL fail the generation when the policy cannot be honored.

#### Scenario: Automatic routing request
- **WHEN** a profile uses Automatic routing
- **THEN** FusionCanvas permits OpenRouter's normal provider routing and fallback behavior
- **AND** the request/result diagnostics identify that the provider may vary

#### Scenario: Specific provider request
- **WHEN** a profile uses Specific provider routing
- **THEN** FusionCanvas restricts the request to the selected provider identity
- **AND** does not allow fallback to a different provider

#### Scenario: Exact endpoint request
- **WHEN** a profile uses Exact endpoint routing
- **THEN** FusionCanvas sends the selected exact endpoint in provider ordering with fallbacks disabled
- **AND** the request fails with a routing-specific error when that endpoint cannot serve the request

#### Scenario: Strict request parameters remain enforced
- **WHEN** a profile sends recognized explicit generation parameters or requires Zero Data Retention
- **THEN** FusionCanvas continues to require provider support for the submitted parameters and enforce the active privacy policy
- **AND** routing selection does not weaken those constraints

### Requirement: Explicit routing selections are preserved across catalog changes
FusionCanvas SHALL preserve an explicitly saved model, provider, or endpoint selection when catalog data changes and SHALL report the selection as unavailable or incompatible rather than silently replacing it.

#### Scenario: Selected endpoint disappears
- **WHEN** a refresh no longer reports a previously selected provider or endpoint
- **THEN** FusionCanvas retains the saved selection for diagnosis or later recovery
- **AND** the affected profile is not treated as ready
- **AND** no alternate model or endpoint is selected automatically

#### Scenario: Selected endpoint becomes incompatible
- **WHEN** a selected endpoint no longer satisfies the active privacy policy, requested output capacity, or submitted parameter requirements
- **THEN** FusionCanvas reports the applicable incompatibility
- **AND** blocks generation until the user changes the policy, parameters, or route

### Requirement: Generation results include a truthful routing receipt
FusionCanvas SHALL return a non-secret routing receipt with every completed or failed text-generation attempt when the provider supplies the relevant metadata.

#### Scenario: Generation succeeds
- **WHEN** a text generation completes successfully
- **THEN** the result includes the requested model, selected routing mode, reported actual model and provider, finish reason, usage, reported cost, generation identifier, and available endpoint metadata
- **AND** missing optional provider metadata is identified as unavailable rather than fabricated

#### Scenario: Strict route fails
- **WHEN** an Exact endpoint request fails before producing a usable completion
- **THEN** the result identifies that the strict route was not fulfilled
- **AND** it does not present a different provider as the actual result
- **AND** it reports that retrying with Automatic or another explicit route is a user choice

#### Scenario: Receipt is displayed or copied
- **WHEN** an existing AI caller exposes diagnostics for a generation result
- **THEN** the routing receipt is available as a concise user-readable summary
- **AND** copying the summary excludes credentials, authorization headers, complete prompts, complete responses, and reasoning content

### Requirement: Routing failures are actionable and cost-safe
FusionCanvas SHALL distinguish an unavailable or ineligible route from authentication, privacy, parameter, and provider-response failures and SHALL not automatically retry a generation POST after dispatch.

#### Scenario: No eligible endpoint remains
- **WHEN** OpenRouter reports that no endpoint satisfies the selected routing, privacy, capacity, or parameter constraints
- **THEN** FusionCanvas returns a no-eligible-provider failure
- **AND** identifies the relevant constraint category without including submitted content or credentials

#### Scenario: Generation fails after dispatch
- **WHEN** a timeout, connection loss, or provider failure may have occurred after a generation request was accepted
- **THEN** FusionCanvas does not automatically resubmit the request
- **AND** explains that retrying could create additional usage
