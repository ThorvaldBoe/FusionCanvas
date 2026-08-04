## ADDED Requirements

### Requirement: Store editor owns product and fulfillment setup
FusionCanvas SHALL provide a Products & fulfillment tab in the dedicated Store Editor for the selected Store and SHALL keep this administration out of the regular workspace rail and application Settings window.

#### Scenario: User opens product setup for active Store
- **WHEN** the user opens Manage stores and selects an active Store
- **THEN** FusionCanvas provides a Products & fulfillment tab for that Store
- **AND** the regular workspace remains focused on Store selection and creative work

#### Scenario: Store has no configured products
- **WHEN** the user opens Products & fulfillment for a Store with no product blueprints
- **THEN** FusionCanvas shows a useful empty state and a New product action
- **AND** it does not fabricate product or provider data

#### Scenario: User changes editor context with an unsaved catalog draft
- **WHEN** the user has meaningful unsaved product, offering, variant, or area changes and changes Store, tab, selection, or closes the Store Editor
- **THEN** FusionCanvas offers discard and keep-editing actions
- **AND** keep-editing retains the current draft and focus

#### Scenario: User reviews archived Store setup
- **WHEN** the user selects an archived Store in the Store Editor
- **THEN** FusionCanvas shows its configured product data read-only
- **AND** it does not enable create, edit, or target-affecting catalog mutations
