## ADDED Requirements

### Requirement: AI workflow availability is observable without exposing credentials
FusionCanvas SHALL provide application callers with an asynchronous, provider-neutral availability result for a request purpose that distinguishes a missing credential, inaccessible secure storage, incomplete effective profile, unavailable model, privacy incompatibility, and ready state without returning credential content.

#### Scenario: Ideation queries availability
- **WHEN** the Idea-stage presentation evaluates whether configured generation can run
- **THEN** it obtains the current Ideation-purpose availability through an application boundary
- **AND** it does not read the native credential or model cache directly

#### Scenario: Credential or profile changes
- **WHEN** the creator saves or removes the OpenRouter credential, changes the effective Ideation profile, refreshes models, or changes Zero Data Retention policy
- **THEN** the application publishes an availability change
- **AND** an open main window refreshes the Ideation action and unavailable guidance without restart

#### Scenario: Availability is reported
- **WHEN** a caller receives a blocked availability result
- **THEN** the result contains a stable category and secret-safe user guidance
- **AND** contains no credential, authorization header, or submitted creative content

