## ADDED Requirements

### Requirement: Stores can use standalone Printify fulfillment

The store configuration SHALL offer a standalone `Printify` fulfillment strategy in addition to the existing strategies. The strategy SHALL be persisted and reloaded using the existing store management flow, while existing strategy values and behavior remain compatible.

#### Scenario: Strategy list includes standalone Printify

- **WHEN** a user opens the store editor
- **THEN** the fulfillment strategy selector includes `Printify`
- **AND** the existing Manual, Shopify + Manual, and Shopify + Printify options remain available

#### Scenario: Standalone Printify strategy persists

- **WHEN** a user saves a store with the standalone `Printify` strategy
- **THEN** loading the store returns the standalone `Printify` strategy
- **AND** the store is not treated as a Shopify-integrated store

### Requirement: Printify configuration is available for both Printify strategies

The store editor SHALL expose the existing Printify credential, verification, and shop-selection controls when the selected strategy is either standalone `Printify` or `Shopify + Printify`. It SHALL NOT expose those controls for Manual or Shopify + Manual.

#### Scenario: Standalone Printify exposes configuration

- **WHEN** a saved active store uses standalone `Printify`
- **THEN** the editor shows the Printify credential controls
- **AND** the user can verify the credential and select a Printify shop
- **AND** selecting a shop uses the existing store context persistence flow

#### Scenario: Shopify-only strategy does not expose Printify configuration

- **WHEN** the selected strategy is `Shopify + Manual`
- **THEN** the Printify credential and shop-selection controls are hidden

### Requirement: Standalone Printify excludes Shopify behavior

Standalone `Printify` SHALL enable Printify configuration only; it SHALL NOT enable Shopify integration or publishing behavior. Strategy guidance SHALL describe the distinction accurately.

#### Scenario: Strategy guidance distinguishes standalone Printify

- **WHEN** the store editor displays fulfillment guidance
- **THEN** it identifies standalone Printify as a Printify-connected strategy without claiming Shopify connectivity

### Requirement: Printify configuration remains protected during strategy changes

Changing an existing Printify-configured store from either Printify strategy to a non-Printify strategy SHALL use the existing confirmation flow before saving the change. The saved credential and catalog data SHALL remain retained as they do today.

#### Scenario: Leaving standalone Printify requires confirmation

- **WHEN** a user changes a saved standalone Printify store to Manual or Shopify + Manual and attempts to save
- **THEN** the editor shows the strategy-change confirmation
- **AND** cancelling leaves standalone Printify selected
- **AND** confirming saves the new non-Printify strategy
