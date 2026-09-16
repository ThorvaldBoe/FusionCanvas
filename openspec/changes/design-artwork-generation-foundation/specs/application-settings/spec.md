## ADDED Requirements

### Requirement: AI Settings exposes focused Artwork model configuration
FusionCanvas SHALL add an Artwork profile to the existing AI Settings pane with a searchable image-model selector and concise readiness guidance. Artwork configuration SHALL remain visible independently of Advanced text-purpose profiles, SHALL not offer Use General inheritance, and SHALL leave provider-specific quality or sampling controls at provider defaults in this module.

#### Scenario: User opens AI Settings with image models available
- **WHEN** a readable credential and image catalog are available
- **THEN** AI Settings shows the Artwork model selector and current readiness
- **AND** selecting a compatible image model persists the Artwork preference

#### Scenario: Artwork is not configured
- **WHEN** no Artwork model has been selected
- **THEN** the pane reports Artwork as incomplete
- **AND** existing General and text-purpose readiness remain independently usable

#### Scenario: Privacy policy changes
- **WHEN** the user changes the global ZDR setting
- **THEN** the Artwork selector and readiness re-evaluate against the matching image catalog and endpoint eligibility
- **AND** an incompatible saved selection is retained but marked unavailable

#### Scenario: User operates Artwork settings by keyboard
- **WHEN** keyboard focus reaches the Artwork configuration
- **THEN** search, selection, refresh, status, and dismissal remain reachable in a predictable order
