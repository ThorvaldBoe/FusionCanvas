# Terms Consent

## Purpose

Defines the app-wide first-run acknowledgement, responsible-use messaging, versioned application-settings record, and policy-review behavior for FusionCanvas.

## Requirements

### Requirement: App-wide consent gates normal workspace access
FusionCanvas SHALL require a current app-wide acknowledgement record before exposing the normal main workspace and navigation to the user.

#### Scenario: First launch has no acknowledgement record
- **WHEN** the application starts with no stored terms acknowledgement
- **THEN** FusionCanvas presents the focused Terms & Responsible Use surface before showing the normal main workspace
- **AND** workspace navigation and workspace actions remain unavailable until the required acknowledgements are completed

#### Scenario: Current acknowledgement exists
- **WHEN** the application starts with an acknowledgement record matching the current FusionCanvas terms version and acknowledgement-policy version
- **THEN** FusionCanvas continues to the normal startup flow without showing the mandatory consent surface

#### Scenario: User declines or quits the consent surface
- **WHEN** the user chooses `Quit FusionCanvas` or closes the mandatory consent surface without accepting
- **THEN** FusionCanvas does not show the normal workspace
- **AND** FusionCanvas does not mutate workspace data

### Requirement: Consent surface separates responsibilities
FusionCanvas SHALL present four separately selectable mandatory acknowledgements: FusionCanvas terms, Printify obligations, Shopify obligations, and user responsibility for content and intellectual property.

#### Scenario: User reviews the required acknowledgements
- **WHEN** the mandatory consent surface is displayed
- **THEN** it presents a link or locally readable content for the versioned FusionCanvas Terms of Use and Responsible Use Policy
- **AND** it presents clearly labeled links to the current official Printify and Shopify terms or policy pages
- **AND** it presents a separate statement that the user is responsible for having rights, licences, permissions, and authorizations for content, brands, trademarks, and other intellectual property

#### Scenario: Provider acknowledgement is presented honestly
- **WHEN** the user reads the Printify or Shopify acknowledgement text
- **THEN** the text explains that the provider's current terms and policies apply when the user uses provider-connected features
- **AND** the text does not claim that FusionCanvas accepts a provider agreement on the user's behalf
- **AND** the text states that FusionCanvas does not provide legal advice, verify ownership, or guarantee non-infringement

#### Scenario: User has not selected every acknowledgement
- **WHEN** one or more of the four acknowledgement controls is not selected
- **THEN** `Agree and continue` is disabled
- **AND** the user can continue reviewing the local policy, provider links, or quit the application

#### Scenario: User selects every acknowledgement
- **WHEN** the user selects all four acknowledgement controls
- **THEN** `Agree and continue` becomes enabled
- **AND** activating it attempts to persist the current acknowledgement record before opening the normal workspace

### Requirement: Consent is versioned and persisted as application state
FusionCanvas SHALL persist only a minimal versioned acknowledgement record in application settings, separate from workspace data and workspace packages.

#### Scenario: User accepts current policy versions
- **WHEN** the user selects all required acknowledgements and the application-settings save succeeds
- **THEN** FusionCanvas stores the FusionCanvas terms version, acknowledgement-policy version, and UTC acknowledgement timestamp
- **AND** the normal workspace startup continues

#### Scenario: Existing settings have no consent field
- **WHEN** an existing application-settings file is loaded without a consent record
- **THEN** the settings load remains compatible
- **AND** FusionCanvas treats consent as required
- **AND** existing workspace data remains unchanged

#### Scenario: FusionCanvas policy version changes
- **WHEN** the stored FusionCanvas terms version or acknowledgement-policy version differs from the current application version
- **THEN** FusionCanvas requires the user to complete the consent surface again before normal workspace access

#### Scenario: Consent save fails
- **WHEN** the user activates `Agree and continue` and the application-settings save fails
- **THEN** FusionCanvas keeps the consent surface open
- **AND** it presents an actionable save error
- **AND** it does not claim that consent was stored or open the normal workspace

### Requirement: Consent content remains available offline and provider content remains external
FusionCanvas SHALL bundle the FusionCanvas-owned policy content locally and SHALL treat Printify and Shopify policy content as externally maintained references.

#### Scenario: User is offline during first launch
- **WHEN** the user opens the consent surface without network access
- **THEN** the FusionCanvas-owned terms remain readable in the application
- **AND** the provider links are identified as external references that may require connectivity
- **AND** the consent surface does not claim to have verified the current provider policy revisions

### Requirement: User can review acknowledgement state from Settings
FusionCanvas SHALL provide a Settings surface that shows the currently accepted FusionCanvas terms and acknowledgement-policy versions and provides access to the relevant policy documents.

#### Scenario: User reviews current acknowledgement state
- **WHEN** the user opens the applicable Settings section after accepting consent
- **THEN** FusionCanvas displays the accepted versions and acknowledgement timestamp
- **AND** it provides access to the FusionCanvas policy and official provider policy links

#### Scenario: Settings has no current acknowledgement
- **WHEN** the user opens the applicable Settings section with no current acknowledgement record
- **THEN** FusionCanvas clearly indicates that acknowledgement is required
- **AND** it provides a route to open the consent surface
