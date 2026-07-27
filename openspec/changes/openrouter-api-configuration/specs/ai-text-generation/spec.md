## ADDED Requirements

### Requirement: Application callers use a provider-independent text service
FusionCanvas SHALL provide an application-layer text-generation service whose request and result contracts contain no OpenRouter transport types and whose request identifies a General, Ideation, or Concept purpose.

#### Scenario: Caller submits a text request
- **WHEN** an application caller submits system instructions and text conversation messages for a supported request purpose
- **THEN** the service accepts the request through provider-independent application contracts
- **AND** does not require the caller to supply an API key, provider DTO, model ID, or provider parameter map

#### Scenario: Caller supplies invalid application input
- **WHEN** a request has no usable text messages, an unsupported role, or an unsupported purpose
- **THEN** the service rejects it before provider communication
- **AND** returns an application-level invalid-request failure

### Requirement: Text requests resolve the effective profile
FusionCanvas SHALL resolve every text request through the current General/Ideation/Concept profile rules and SHALL refuse provider communication when the effective configuration is incomplete or incompatible.

#### Scenario: Advanced mode is off
- **WHEN** an Ideation or Concept request is submitted while Advanced mode is off
- **THEN** the service uses the current General profile

#### Scenario: Purpose inherits General
- **WHEN** an Ideation or Concept request is submitted while that profile uses General
- **THEN** the service resolves the current General profile at request time

#### Scenario: Purpose has a custom profile
- **WHEN** an Ideation or Concept request is submitted with an active valid custom profile
- **THEN** the service uses that custom model and parameters

#### Scenario: Effective profile is not ready
- **WHEN** no credential is readable, no model is selected, the selected model is unavailable, or the saved configuration conflicts with known capabilities or privacy policy
- **THEN** the service returns a specific not-configured or invalid-configuration result before sending network content

### Requirement: OpenRouter translation is strict and privacy-preserving
The OpenRouter adapter SHALL submit non-streaming Chat Completions requests with the effective model, messages, recognized explicit overrides, strict parameter routing, and the active Zero Data Retention policy.

#### Scenario: Request uses safe defaults
- **WHEN** the effective profile leaves all optional parameters at `Provider default` and Zero Data Retention is enabled
- **THEN** the adapter sends the selected model and messages with `provider.require_parameters` set to true and `provider.zdr` set to true
- **AND** omits optional generation parameters

#### Scenario: Request contains explicit supported overrides
- **WHEN** the effective profile contains validated recognized overrides
- **THEN** the adapter translates only those overrides to their OpenRouter fields
- **AND** uses strict parameter routing so an endpoint cannot silently ignore them

#### Scenario: Zero Data Retention is disabled
- **WHEN** the effective application privacy preference allows endpoints that may retain content
- **THEN** the adapter does not claim or force Zero Data Retention for the request
- **AND** continues to require support for submitted parameters

#### Scenario: Reasoning effort is configured
- **WHEN** a valid reasoning effort is explicit in the effective profile
- **THEN** the adapter sends it through OpenRouter's normalized reasoning object
- **AND** does not also send a reasoning token budget

#### Scenario: Reasoning token budget is configured
- **WHEN** a valid reasoning token budget is explicit in the effective profile
- **THEN** the adapter sends that budget through OpenRouter's normalized reasoning object
- **AND** does not also send an effort level

### Requirement: Successful responses are normalized without automatic persistence
FusionCanvas SHALL return generated text and available diagnostic and usage metadata through an application result and SHALL NOT automatically write submitted messages, generated text, or reasoning content to prompt history, workspace records, or application settings.

#### Scenario: OpenRouter returns a complete response
- **WHEN** OpenRouter successfully completes a text request
- **THEN** the service returns the generated text, requested model, actual model and provider when reported, finish reason, token usage, reported cost, and generation identifier when available

#### Scenario: Provider omits optional metadata
- **WHEN** generated text succeeds but optional provider, cost, usage, or generation metadata is absent
- **THEN** the service returns the successful text with absent optional fields
- **AND** does not treat missing diagnostic metadata as a failed generation

#### Scenario: Request completes
- **WHEN** a text request succeeds or fails
- **THEN** FusionCanvas does not automatically persist the request messages, response text, or reasoning content

### Requirement: Provider failures are actionable and secret-safe
FusionCanvas SHALL translate OpenRouter and transport failures into stable application-level categories without exposing credentials, authorization headers, or unnecessary submitted content.

#### Scenario: Authentication fails
- **WHEN** OpenRouter rejects the credential
- **THEN** the service returns an authentication failure that directs the user to AI settings
- **AND** does not include the credential or authorization header

#### Scenario: Account has insufficient credit
- **WHEN** OpenRouter reports payment or credit exhaustion
- **THEN** the service returns an insufficient-credit failure distinct from authentication and availability failures

#### Scenario: No endpoint satisfies the request
- **WHEN** OpenRouter reports that no endpoint satisfies the privacy, parameter, or availability requirements
- **THEN** the service returns a no-eligible-provider failure
- **AND** identifies the effective model and configured constraint category without including prompt content

#### Scenario: Request is rate limited
- **WHEN** OpenRouter rate limits the request
- **THEN** the service returns a rate-limited failure with the reported retry-after duration when available

#### Scenario: Request is blocked
- **WHEN** OpenRouter or a downstream provider rejects the request through moderation, a guardrail, or permissions
- **THEN** the service returns a blocked or permission failure with a safe actionable explanation

#### Scenario: Provider returns partial text with an error
- **WHEN** a non-streaming OpenRouter response contains partial text and a terminal provider error
- **THEN** the service reports an incomplete-generation failure
- **AND** preserves partial text only as explicitly identified non-authoritative diagnostic output

#### Scenario: Response cannot be interpreted
- **WHEN** OpenRouter returns malformed, unsupported, or textless success content
- **THEN** the service returns an invalid-provider-response failure
- **AND** does not fabricate generated text

### Requirement: Cancellation and retries avoid duplicate generation cost
FusionCanvas SHALL honor caller cancellation and SHALL NOT automatically retry a generation POST after it may have reached OpenRouter, while permitting bounded retries for safe key-status and catalog reads.

#### Scenario: Caller cancels a pending generation
- **WHEN** the caller's cancellation token is cancelled before completion
- **THEN** the service cancels provider processing as far as the transport permits
- **AND** returns or propagates cancellation distinctly from provider failure

#### Scenario: Generation transport fails ambiguously
- **WHEN** a timeout, connection loss, or service failure occurs after a generation POST may have been accepted
- **THEN** FusionCanvas does not automatically resubmit the generation
- **AND** reports that retrying could create additional usage

#### Scenario: Safe metadata read fails transiently
- **WHEN** a catalog or current-key GET request receives an eligible transient failure
- **THEN** the integration may perform a bounded cancellation-aware retry
- **AND** honors OpenRouter's retry-after value when present

### Requirement: AI content is treated as untrusted external input
FusionCanvas MUST treat model metadata, errors, generated text, and reasoning content as untrusted input and MUST bound parsing, display, and diagnostic handling accordingly.

#### Scenario: Provider content contains markup or instructions
- **WHEN** OpenRouter returns generated text, model descriptions, or errors containing markup or instruction-like content
- **THEN** FusionCanvas handles it as display data rather than executable application instructions
- **AND** does not grant it access to application services, files, or credentials

#### Scenario: Diagnostic logging is enabled
- **WHEN** the application records request lifecycle or provider-error diagnostics
- **THEN** logs use safe identifiers, categories, durations, and generation IDs
- **AND** exclude the API key, authorization headers, complete prompts, complete responses, and reasoning content
