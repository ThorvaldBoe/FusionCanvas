## MODIFIED Requirements

### Requirement: Additional AI parameters are understandable

The AI settings profile editor SHALL present each supported additional parameter with a visible human-readable label and concise explanatory text, while preserving capability-based visibility and the existing bound value.

#### Scenario: Supported parameters are displayed with guidance

- **GIVEN** the selected model supports one or more additional parameters
- **WHEN** the user expands Additional parameters
- **THEN** each supported parameter is shown in a clearly labeled, consistently arranged field
- **AND** each field includes concise guidance describing its effect or expected input
- **AND** unsupported parameters remain hidden

#### Scenario: Parameter editing behavior is unchanged

- **GIVEN** a visible additional parameter field
- **WHEN** the user edits its value
- **THEN** the same existing profile property is updated
- **AND** capability gating, validation, persistence, and provider serialization remain unchanged
