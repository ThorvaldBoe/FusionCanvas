## REMOVED Requirements

### Requirement: Ideation availability uses placeholder API access

## ADDED Requirements

### Requirement: Ideation availability uses configured secure AI access
FusionCanvas SHALL derive Ideation availability from the securely stored OpenRouter inference credential and the effective Ideation AI profile, SHALL keep the action visible but disabled when generation is not ready, and SHALL identify the blocking prerequisite without reading or exposing secret material in presentation code.

#### Scenario: OpenRouter-backed Ideation is ready
- **WHEN** a readable OpenRouter inference key is saved and the effective Ideation profile resolves to an available compatible model
- **THEN** the Ideation action is enabled for a supported Idea-stage context
- **AND** generation uses the provider-independent AI text service with the Ideation request purpose

#### Scenario: OpenRouter key is absent
- **WHEN** no OpenRouter credential is saved
- **THEN** the Ideation action remains visible but disabled
- **AND** its unavailable guidance directs the creator to add a key in AI Settings

#### Scenario: Credential store is unavailable
- **WHEN** the saved credential cannot be read because native secure storage is locked, denied, or unavailable
- **THEN** the Ideation action remains disabled
- **AND** the guidance distinguishes credential unavailability from a missing key without exposing credential content

#### Scenario: Ideation profile is incomplete
- **WHEN** the credential is readable but the effective Ideation profile has no usable model or conflicts with the privacy policy or advertised capabilities
- **THEN** the Ideation action remains disabled
- **AND** the guidance directs the creator to complete the Ideation AI profile

#### Scenario: Environment placeholder is present
- **WHEN** `FUSIONCANVAS_AI_API_KEY` contains a value but no readable OpenRouter credential and profile are available
- **THEN** the environment value does not enable Ideation
- **AND** the value is neither persisted nor transmitted
