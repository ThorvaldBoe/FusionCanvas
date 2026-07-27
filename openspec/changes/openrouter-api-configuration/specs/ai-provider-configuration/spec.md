## ADDED Requirements

### Requirement: OpenRouter credentials use native secure storage
FusionCanvas SHALL store the OpenRouter API key for the current operating-system user in Windows Credential Manager, macOS Keychain, or a Linux Secret Service implementation as applicable, and MUST NOT persist the key in application settings, workspace data, exported data, logs, or source-controlled files.

#### Scenario: User saves an API key
- **WHEN** the user explicitly saves a non-empty OpenRouter API key
- **THEN** FusionCanvas writes it to the current platform's native credential store under a stable FusionCanvas-owned identifier
- **AND** persisted non-secret settings contain no plaintext or reversibly encoded copy of the key

#### Scenario: Native credential storage is unavailable
- **WHEN** the operating system has no usable credential service, the store is locked, or access is denied
- **THEN** FusionCanvas does not persist the submitted key through a plaintext, application-settings, workspace, or silent in-memory fallback
- **AND** the AI settings surface explains that secure credential storage is unavailable and the key was not saved

#### Scenario: Existing credential cannot be read
- **WHEN** a previously stored credential exists but cannot currently be retrieved
- **THEN** FusionCanvas reports the credential as unavailable rather than absent or invalid
- **AND** does not delete or replace it automatically

#### Scenario: Credential replacement fails
- **WHEN** the user attempts to replace a saved API key and the native credential write fails
- **THEN** the previously stored credential remains authoritative
- **AND** FusionCanvas reports that the replacement was not saved

### Requirement: Credential editing is explicit and secret-preserving
FusionCanvas SHALL present the stored OpenRouter credential as a masked configured state without redisplaying its plaintext and SHALL require explicit actions to save, replace, or remove it.

#### Scenario: User views a configured credential
- **WHEN** AI settings opens with a readable saved credential
- **THEN** the surface indicates that a key is saved
- **AND** it does not place the saved plaintext into an editable or copyable control

#### Scenario: User cancels credential entry
- **WHEN** the user begins entering a new or replacement key and cancels the edit
- **THEN** the draft is cleared from the surface
- **AND** the stored credential is unchanged

#### Scenario: User closes Settings with an unsaved credential draft
- **WHEN** the user attempts to close Settings while a non-empty API-key draft has not been saved
- **THEN** FusionCanvas asks whether to discard the draft
- **AND** keeps Settings open with the draft intact when discard is declined

#### Scenario: User removes a saved credential
- **WHEN** the user confirms removal of the saved OpenRouter key
- **THEN** FusionCanvas removes the native credential
- **AND** reports AI text requests as not configured without deleting non-secret model and profile preferences

#### Scenario: User cancels credential removal
- **WHEN** the user cancels the removal confirmation
- **THEN** the native credential and configured state remain unchanged

### Requirement: OpenRouter credentials can be validated independently
FusionCanvas SHALL allow the user to validate the saved key through OpenRouter's current-key information endpoint without submitting a model generation request or incurring generation usage.

#### Scenario: User saves a key while offline
- **WHEN** secure credential storage succeeds but OpenRouter cannot be reached
- **THEN** FusionCanvas retains the key with a saved-but-unverified state
- **AND** allows validation to be retried later

#### Scenario: Inference key validates
- **WHEN** OpenRouter accepts the saved key as an inference-capable key
- **THEN** FusionCanvas reports a connected state
- **AND** may display non-secret account limit information returned for that key without persisting it as credential material

#### Scenario: Key is invalid or revoked
- **WHEN** OpenRouter reports that the saved key is invalid, disabled, or revoked
- **THEN** FusionCanvas reports an invalid-key state with replace and remove actions
- **AND** does not reveal the key in the error

#### Scenario: Management-only key is supplied
- **WHEN** OpenRouter identifies the saved key as management-only and therefore unable to submit inference requests
- **THEN** FusionCanvas reports the wrong key type
- **AND** explains that an inference-capable OpenRouter key is required

#### Scenario: Validation is interrupted
- **WHEN** key validation is cancelled, times out, is rate limited, or fails because of a transient network or service error
- **THEN** FusionCanvas keeps the stored key
- **AND** reports a retryable validation state distinct from invalid credentials

### Requirement: Zero Data Retention is the default privacy policy
FusionCanvas SHALL enable OpenRouter Zero Data Retention for new AI configurations by default and SHALL require deliberate user action to allow endpoints that may retain submitted content.

#### Scenario: User opens AI settings for the first time
- **WHEN** no AI privacy preference has been saved
- **THEN** `Require zero data retention` is on
- **AND** the surface explains that model availability is restricted to compatible endpoints

#### Scenario: User opts out of Zero Data Retention
- **WHEN** the user turns `Require zero data retention` off
- **THEN** FusionCanvas explains that prompts and generated content leave the device and may be retained under downstream-provider policies
- **AND** applies the broader policy only after the user confirms the change

#### Scenario: User cancels the privacy opt-out
- **WHEN** the user declines the Zero Data Retention opt-out confirmation
- **THEN** Zero Data Retention remains enabled
- **AND** model availability remains filtered by that policy

#### Scenario: Privacy change makes the selected model incompatible
- **WHEN** a saved model has no endpoint compatible with the newly active privacy policy
- **THEN** FusionCanvas retains the explicit model selection but marks the profile unavailable
- **AND** does not silently substitute another model

### Requirement: Model selection uses a dynamic text-capable catalog
FusionCanvas SHALL load OpenRouter's current catalog of models that accept text and produce text, apply the active privacy filter, and provide searchable selection without hardcoding a default model.

#### Scenario: Catalog loads successfully
- **WHEN** OpenRouter returns the current model catalog
- **THEN** FusionCanvas displays matching models with stable model ID, display name, relevant modalities, context and output limits, pricing, and capability information when supplied
- **AND** excludes models that cannot accept text or produce text

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

### Requirement: Parameter controls follow advertised model capabilities
FusionCanvas SHALL expose only recognized text-generation controls advertised for the selected model, preserve provider-default omission, and tolerate unknown future capability names without preventing model use.

#### Scenario: Model supports a recognized parameter
- **WHEN** the selected model advertises a parameter recognized by FusionCanvas
- **THEN** the applicable profile exposes a typed control with the FusionCanvas-defined valid range or choices
- **AND** identifies the provider-reported default when available

#### Scenario: User leaves a parameter at Provider default
- **WHEN** a recognized parameter remains set to `Provider default`
- **THEN** FusionCanvas records no explicit override for that parameter
- **AND** the eventual provider request omits it

#### Scenario: Model does not support a recognized parameter
- **WHEN** the selected model does not advertise a recognized optional parameter
- **THEN** FusionCanvas does not offer an active override for it
- **AND** excludes any stale value for that parameter from the effective request configuration

#### Scenario: Model advertises an unknown parameter
- **WHEN** the catalog includes a capability name that the installed FusionCanvas version does not recognize
- **THEN** the model remains selectable
- **AND** FusionCanvas does not create an unvalidated arbitrary-value control or submit that parameter

#### Scenario: Selected model changes
- **WHEN** the user selects a different model for a profile
- **THEN** FusionCanvas recalculates available controls and configuration validity from the new model's capabilities
- **AND** does not submit incompatible values retained from the prior model

### Requirement: Reasoning controls use normalized model metadata
FusionCanvas SHALL derive reasoning availability, supported effort levels, defaults, mandatory state, and token-budget availability from OpenRouter model metadata instead of presenting one fixed reasoning configuration for every model.

#### Scenario: Model does not expose reasoning selection
- **WHEN** reasoning metadata is absent for the selected model
- **THEN** FusionCanvas hides or disables reasoning overrides
- **AND** leaves provider reasoning behavior at its default

#### Scenario: Model supports optional effort selection
- **WHEN** the selected model advertises optional reasoning with supported effort levels
- **THEN** FusionCanvas offers `Provider default`, `Off`, and only the advertised effort levels

#### Scenario: Model requires reasoning
- **WHEN** the selected model reports reasoning as mandatory
- **THEN** FusionCanvas does not offer `Off`
- **AND** treats an obsolete disabled value as incompatible rather than submitting it

#### Scenario: Model supports a reasoning token budget
- **WHEN** the selected model advertises reasoning-token-budget support
- **THEN** FusionCanvas allows the user to configure either an effort level or a token budget
- **AND** does not submit both controls as competing settings

### Requirement: Advanced profiles resolve predictably
FusionCanvas SHALL provide a General AI profile and an Advanced mode containing Ideation and Concept profiles that independently either inherit General or use a retained custom configuration.

#### Scenario: Advanced mode is off
- **WHEN** Advanced mode is disabled
- **THEN** General is the effective configuration for General, Ideation, and Concept request purposes
- **AND** saved custom Ideation and Concept values remain retained but inactive

#### Scenario: Advanced mode is enabled initially
- **WHEN** the user enables Advanced mode and no custom purpose profiles have been created
- **THEN** Ideation and Concept both use General through live whole-profile inheritance

#### Scenario: User creates a custom purpose profile
- **WHEN** the user changes Ideation or Concept from `Use General` to `Custom`
- **THEN** FusionCanvas initializes that custom profile from General's current effective values
- **AND** subsequent General changes do not alter the custom profile

#### Scenario: Purpose profile returns to General
- **WHEN** the user changes a custom purpose profile back to `Use General`
- **THEN** requests for that purpose immediately resolve the current General profile
- **AND** the prior custom values remain retained for later restoration

#### Scenario: Advanced mode is disabled and re-enabled
- **WHEN** the user disables and later re-enables Advanced mode
- **THEN** each purpose profile restores its prior inheritance or custom selection and retained values

### Requirement: Non-secret AI configuration remains application-wide
FusionCanvas SHALL persist AI privacy, catalog-cache, Advanced-mode, inheritance, model, and parameter preferences as versioned application-wide settings independent of workspace data while preserving existing appearance preferences through upgrade and recovery.

#### Scenario: User switches workspace
- **WHEN** the active workspace changes
- **THEN** AI provider configuration and profile preferences remain unchanged
- **AND** no workspace record is used to store them

#### Scenario: Existing settings are upgraded
- **WHEN** FusionCanvas reads a settings document created before AI configuration existed
- **THEN** it preserves the existing appearance preference
- **AND** initializes AI preferences with Zero Data Retention on, Advanced mode off, and no selected model

#### Scenario: AI settings content is invalid
- **WHEN** saved AI preferences or cached catalog content is malformed or unsupported
- **THEN** FusionCanvas recovers the affected AI portion to safe defaults or ignores the invalid cache
- **AND** preserves readable unrelated application preferences and the separately stored native credential
