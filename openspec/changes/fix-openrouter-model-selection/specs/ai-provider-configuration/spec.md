# Delta: ai-provider-configuration

This delta modifies a capability defined by the active base change `openrouter-api-configuration`. It corrects catalog behavior that was specified and implemented against an unverified OpenRouter contract (`GET /api/v1/models/user?zdr=true`, which does not exist) and closes the first-run gap in which no catalog is ever loaded until a manual refresh.

## MODIFIED Requirements

### Requirement: Model selection uses a dynamic text-capable catalog
FusionCanvas SHALL load OpenRouter's current catalog of models that accept text and produce text, apply the active privacy filter derived from OpenRouter's published Zero Data Retention endpoint data, and provide searchable selection without hardcoding a default model. Catalog requests SHALL use only documented OpenRouter request parameters.

#### Scenario: Catalog loads successfully
- **WHEN** OpenRouter returns the current model catalog
- **THEN** FusionCanvas displays matching models with stable model ID, display name, relevant modalities, context and output limits, pricing, and capability information when supplied
- **AND** excludes models that cannot accept text or produce text
- **AND** marks each model Zero Data Retention compatible only when OpenRouter's published ZDR endpoint data lists at least one endpoint for that model

#### Scenario: No model has been selected
- **WHEN** an otherwise configured profile has no selected model
- **THEN** the profile is reported as incomplete
- **AND** FusionCanvas does not silently select a concrete model or dynamic router

#### Scenario: User searches the model catalog
- **WHEN** the user searches by model name, model ID, or author
- **THEN** the selector narrows the currently privacy-compatible model choices
- **AND** retains the current selection when it still matches the active configuration

#### Scenario: Catalog refresh fails with a cached catalog
- **WHEN** the current catalog cannot be loaded and a previously successful catalog cache exists
- **THEN** FusionCanvas presents the cached entries with a last-updated or stale indication
- **AND** permits an explicitly saved model configuration to remain usable subject to request-time validation

#### Scenario: Catalog refresh fails without a cache
- **WHEN** the current catalog cannot be loaded and no cache exists
- **THEN** FusionCanvas reports model discovery as unavailable with a retry action
- **AND** does not invent a model list

#### Scenario: Saved model is absent from the current catalog
- **WHEN** a profile references a model ID that is no longer returned
- **THEN** FusionCanvas retains and displays the saved ID as unavailable
- **AND** requires the user to select a replacement before treating that profile as ready

## ADDED Requirements

### Requirement: Model catalog loads automatically when a credential is usable
FusionCanvas SHALL request the model catalog without requiring the manual refresh action whenever a readable credential exists and no usable catalog is available for the active privacy policy, and SHALL always request it after a credential validates as inference-capable.

#### Scenario: Successful validation loads the catalog
- **WHEN** validation reports the saved key as inference-capable
- **THEN** FusionCanvas automatically requests the model catalog under the active privacy policy
- **AND** populates the model selector on success without a manual refresh

#### Scenario: Settings opens with a credential but no catalog
- **WHEN** AI settings loads with a readable credential and no cached catalog for the active privacy policy
- **THEN** FusionCanvas automatically requests the catalog
- **AND** a failed automatic load leaves the manual refresh action available with an actionable message

#### Scenario: Privacy policy changes without a matching cache
- **WHEN** the active privacy policy changes and no cached catalog exists for the newly active policy
- **THEN** FusionCanvas automatically requests the catalog when a readable credential exists

#### Scenario: No credential exists
- **WHEN** no readable credential exists
- **THEN** FusionCanvas does not contact OpenRouter for the catalog
- **AND** the pane explains that an API key is required before models can be loaded

#### Scenario: Automatic loads never duplicate
- **WHEN** an automatic or manual catalog load is already running
- **THEN** FusionCanvas does not start a concurrent second catalog request

#### Scenario: Empty selector explains itself
- **WHEN** the model selector has no models to offer
- **THEN** the pane states whether the key is missing, the catalog has not been loaded, or no compatible models were returned
- **AND** identifies the matching next action

### Requirement: Zero Data Retention compatibility uses published endpoint data
FusionCanvas SHALL derive per-model Zero Data Retention compatibility from OpenRouter's published ZDR endpoint list rather than from the requested privacy policy, and MUST NOT mark a model compatible by assumption.

#### Scenario: Catalog marks real compatibility
- **WHEN** the model catalog is loaded
- **THEN** FusionCanvas reads OpenRouter's public ZDR endpoint list
- **AND** marks a model compatible only when at least one listed endpoint belongs to that model

#### Scenario: ZDR required narrows the selector
- **WHEN** Zero Data Retention is required
- **THEN** the model selector offers only models marked compatible
- **AND** a saved selection with no compatible endpoint is reported as privacy-incompatible rather than silently substituted

#### Scenario: ZDR endpoint data is unavailable while required
- **WHEN** the ZDR endpoint list cannot be loaded while Zero Data Retention is required
- **THEN** the catalog refresh reports an actionable failure
- **AND** does not present models as ZDR-compatible by assumption

#### Scenario: ZDR not required retains compatibility data
- **WHEN** Zero Data Retention is not required
- **THEN** all text-capable models remain selectable
- **AND** per-model compatibility data remains available so a later policy change does not require a catalog refresh to evaluate compatibility

### Requirement: Catalog refresh applies fetched models and categorizes failures
FusionCanvas SHALL keep a successfully fetched catalog usable even when the cache write fails, and SHALL report refresh failures through secret-safe, actionable categories.

#### Scenario: Cache write fails after a successful fetch
- **WHEN** the catalog fetch succeeds but writing the cache fails
- **THEN** FusionCanvas still applies the fetched models to the selector
- **AND** warns that the catalog may not survive restart without blocking model selection

#### Scenario: Credential is rejected during refresh
- **WHEN** OpenRouter rejects the credential during a catalog refresh
- **THEN** FusionCanvas reports that the saved key was rejected with guidance to re-validate or replace it
- **AND** does not include the credential or authorization header in the message

#### Scenario: Refresh is rate limited
- **WHEN** OpenRouter rate limits a catalog refresh
- **THEN** FusionCanvas reports a rate-limited state with the reported retry-after duration when available

#### Scenario: Refresh fails transiently
- **WHEN** a catalog refresh times out or fails because of a network or unparseable-response error
- **THEN** FusionCanvas reports a retryable unavailable state distinct from a rejected credential
