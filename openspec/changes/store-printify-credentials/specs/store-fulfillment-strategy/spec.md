## RENAMED Requirements

- FROM: `### Requirement: Initial strategy availability is manual-only`
- TO: `### Requirement: All planned fulfillment strategies are selectable`

## MODIFIED Requirements

### Requirement: All planned fulfillment strategies are selectable
The Store Editor SHALL enable exactly `Manual`, `Shopify + Manual`, and `Shopify + Printify`, mapped to the existing stable strategy values. Only `Shopify + Printify` SHALL expose Printify credential configuration. Selecting or saving a strategy SHALL NOT itself perform marketplace requests, and missing credentials SHALL NOT prevent local Store saving.

#### Scenario: User configures an existing Store
- **WHEN** the user opens fulfillment configuration for an active Store
- **THEN** all three strategies are selectable and the saved strategy is selected
- **AND** saving any selection preserves its corresponding stable value across reload

#### Scenario: User operates the editor by keyboard
- **WHEN** the user reaches the strategy selector through keyboard navigation
- **THEN** all three alternatives have meaningful accessible names and can be selected and saved by keyboard

#### Scenario: User selects a strategy without Printify
- **WHEN** Manual or Shopify + Manual is selected
- **THEN** Printify credential controls are hidden and no Printify credential lookup or marketplace request is performed for that selection
- **AND** Shopify + Manual does not require a Printify key

#### Scenario: User saves incomplete Printify configuration
- **WHEN** Shopify + Printify is selected and no key has been saved or verified
- **THEN** Store saving remains available subject to ordinary Store validation
- **AND** local catalog work remains available without automatic external communication

### Requirement: Strategy transitions preserve Store identity and require explicit confirmation
FusionCanvas SHALL preserve Store identity and strategy-neutral catalog data when changing strategies. Before committing a saved Store's transition away from Shopify + Printify, it SHALL warn that Printify configuration becomes unavailable and the saved key is retained. Declining SHALL leave the persisted strategy and other Store data unchanged.

#### Scenario: Enabled strategy is changed
- **WHEN** the user saves a different strategy and confirms a warning when leaving Shopify + Printify
- **THEN** the strategy changes on the same Store identity
- **AND** Blueprints, Blueprint Offerings, Options, Variants, Placeholders, Mockup Templates, and any saved key are retained

#### Scenario: User cancels a strategy warning
- **WHEN** the user declines the transition warning
- **THEN** no Store update is committed and the saved strategy remains unchanged
- **AND** the editor retains other unsaved fields and restores the previous strategy selection

#### Scenario: User returns to Printify configuration
- **WHEN** the user selects Shopify + Printify again for a Store with a retained key
- **THEN** the same Store's key becomes available without re-entry
- **AND** no verification runs automatically
