## ADDED Requirements

### Requirement: AI settings make model and provider routing transparent
The focused AI Settings section SHALL present model selection separately from routing selection and SHALL explain the consequences of Automatic, Specific provider, and Exact endpoint routing.

#### Scenario: User reviews Automatic routing
- **WHEN** a profile uses Automatic routing
- **THEN** the surface identifies that OpenRouter may select different providers and use fallback endpoints
- **AND** the current model remains visible

#### Scenario: User reviews a specific provider route
- **WHEN** a profile uses Specific provider routing
- **THEN** the surface identifies the selected provider and explains whether endpoint variants remain eligible
- **AND** it does not imply that every endpoint from that provider has identical capabilities

#### Scenario: User reviews an exact endpoint route
- **WHEN** a profile uses Exact endpoint routing
- **THEN** the surface identifies the exact endpoint, its reported capabilities, and that a request will fail rather than silently fall back

### Requirement: AI endpoint selection supports progressive disclosure and safe interaction
The AI Settings surface SHALL keep routine model selection compact while making endpoint details, stale metadata, incompatibilities, refresh, and route changes discoverable and keyboard accessible.

#### Scenario: User opens endpoint routing details
- **WHEN** the user expands routing details for a selected profile
- **THEN** the endpoint selector and capability summary appear in the profile editor
- **AND** focus and tab order remain predictable

#### Scenario: Endpoint data is unavailable
- **WHEN** endpoint metadata cannot be loaded and no cached endpoint data exists
- **THEN** the surface explains why endpoint selection is unavailable
- **AND** keeps model selection and the refresh action available when applicable

#### Scenario: Route change makes a profile unavailable
- **WHEN** the user selects a provider or endpoint that does not satisfy the active privacy or capability constraints
- **THEN** the profile shows the blocking reason
- **AND** generation remains disabled until the user selects a compatible route or changes the relevant explicit setting

#### Scenario: User changes a route and closes Settings
- **WHEN** a route change has not yet been persisted and the user attempts to close Settings
- **THEN** the existing AI-settings persistence or discard behavior applies consistently
- **AND** declining discard keeps the selected route and focus available for review
