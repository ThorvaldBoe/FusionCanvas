## ADDED Requirements

### Requirement: Settings provides a focused AI section
FusionCanvas SHALL add an `AI` entry to the persistent Settings section rail and SHALL contain provider connection, privacy, and model-profile configuration within that focused pane rather than the primary creative workspace.

#### Scenario: User selects AI settings
- **WHEN** the user selects `AI` in the Settings section rail
- **THEN** the AI pane replaces the prior pane in the content region
- **AND** the selected section remains visibly and programmatically identified

#### Scenario: User opens Settings frequently for other preferences
- **WHEN** the user opens a new Settings session
- **THEN** General remains the initially selected section
- **AND** AI configuration occupies no persistent main-workspace area

#### Scenario: User operates AI settings with a keyboard
- **WHEN** keyboard focus enters the AI pane
- **THEN** credential actions, privacy controls, model selection, applicable parameter controls, profile controls, and dismissal are reachable in a predictable tab order
- **AND** status changes do not move focus unexpectedly

### Requirement: AI settings uses progressive disclosure
FusionCanvas SHALL keep provider connection, privacy, and General model configuration visible while progressively disclosing purpose-specific profiles and model-applicable optional parameters.

#### Scenario: Advanced mode is off
- **WHEN** the AI pane is shown with Advanced mode disabled
- **THEN** General configuration is visible
- **AND** Ideation and Concept custom-profile editors are hidden without deleting their state

#### Scenario: Advanced mode is on
- **WHEN** the user enables Advanced mode
- **THEN** Ideation and Concept profile summaries and inheritance controls become available
- **AND** a custom profile's detailed controls are shown only when that profile is selected for editing

#### Scenario: Selected model has additional parameters
- **WHEN** the selected model supports recognized optional parameters beyond the primary model and reasoning controls
- **THEN** those controls are available through a clearly labelled expandable area
- **AND** the current effective values remain understandable while the area is collapsed

### Requirement: AI settings presents complete interaction states
FusionCanvas SHALL distinguish initial, loading, saved, connected, incomplete, stale, blocked, and recoverable error states and SHALL keep actions coherent with the current credential, catalog, privacy, and profile state.

#### Scenario: AI has not been configured
- **WHEN** no credential or model selection is available
- **THEN** the AI pane explains what must be configured before requests are ready
- **AND** offers only actions valid for the current state

#### Scenario: Validation or catalog loading is in progress
- **WHEN** a key validation or model-catalog refresh is running
- **THEN** the initiating action indicates progress and cannot be started redundantly
- **AND** cancellation or Settings dismissal remains safe

#### Scenario: Configuration is ready
- **WHEN** the credential is readable and the effective General profile is complete and privacy-compatible
- **THEN** the AI pane reports that General text requests are ready
- **AND** reports Ideation and Concept readiness according to their effective profiles

#### Scenario: Operation fails recoverably
- **WHEN** credential access, validation, catalog loading, or non-secret preference persistence fails
- **THEN** the relevant part of the AI pane retains the user's unaffected state
- **AND** presents an inline actionable message and retry or correction action where applicable

### Requirement: AI credential drafts and destructive actions preserve user intent
FusionCanvas SHALL keep API-key drafts temporary until explicit save, protect them from accidental dismissal, and confirm credential removal and privacy opt-out.

#### Scenario: User starts entering a credential
- **WHEN** the API-key entry mode opens
- **THEN** focus moves to the masked key field
- **AND** Save and Cancel have clear ownership of the draft

#### Scenario: User saves a credential draft
- **WHEN** the user invokes Save with a non-empty key
- **THEN** the native credential write is attempted
- **AND** successful storage clears the draft and moves focus to a meaningful credential-status or validation action

#### Scenario: Credential draft is invalid locally
- **WHEN** the user invokes Save with an empty or whitespace-only value
- **THEN** the native credential store is not called
- **AND** focus remains on the field with an inline validation explanation

#### Scenario: Confirmed destructive action completes
- **WHEN** credential removal or Zero Data Retention opt-out is confirmed
- **THEN** the resulting state and next safe action are visible
- **AND** focus returns to the relevant replacement or policy control

### Requirement: Non-secret AI preference edits persist without a pane-wide draft
FusionCanvas SHALL persist valid privacy, model, parameter, Advanced-mode, and inheritance changes as application-wide preferences without requiring a pane-wide Save or Apply action, while retaining current-session state and reporting a failed write.

#### Scenario: User changes a valid non-secret AI preference
- **WHEN** the user changes a model, parameter, Advanced-mode, or inheritance selection
- **THEN** FusionCanvas applies the latest valid value to effective configuration
- **AND** queues it for application-settings persistence without a pane-wide Save action

#### Scenario: User changes preferences repeatedly
- **WHEN** valid non-secret AI preferences change repeatedly before an earlier write completes
- **THEN** the effective configuration follows the most recent values
- **AND** the most recent complete preference state is retained for the next application start

#### Scenario: Non-secret preference cannot be saved
- **WHEN** application-settings persistence fails
- **THEN** FusionCanvas keeps the selected values for the current session
- **AND** reports that they may not survive restart without exposing credential material
