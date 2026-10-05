## ADDED Requirements

### Requirement: Settings exposes terms acknowledgement state
FusionCanvas SHALL expose app-wide terms acknowledgement status from Settings without duplicating the mandatory startup acceptance workflow.

#### Scenario: Settings shows accepted acknowledgement state
- **WHEN** the user opens the terms or legal section in Settings with a current acknowledgement record
- **THEN** the section displays the accepted FusionCanvas terms version, acknowledgement-policy version, and UTC timestamp
- **AND** it provides links or commands to review the FusionCanvas, Printify, and Shopify policy documents

#### Scenario: Settings shows missing or stale acknowledgement state
- **WHEN** the user opens the terms or legal section in Settings with no current acknowledgement record
- **THEN** the section explains that acknowledgement is required before normal workspace use
- **AND** it provides a command to open the mandatory consent surface

#### Scenario: Settings does not create a second acceptance path
- **WHEN** the user reviews terms from Settings
- **THEN** Settings does not independently persist a different acknowledgement record
- **AND** the consent capability remains the single owner of acceptance validation and persistence
