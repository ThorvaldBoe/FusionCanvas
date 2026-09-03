## ADDED Requirements

### Requirement: Every Store has one fulfillment strategy
FusionCanvas SHALL persist exactly one fulfillment strategy for every Store using the stable values `Manual`, `ShopifyManual`, or `ShopifyPrintify`, and SHALL use that Store-scoped value to decide which external-system configuration and communication capabilities are available.

#### Scenario: Existing Store is migrated
- **WHEN** a Store created before fulfillment strategies is loaded after migration
- **THEN** the Store has the `Manual` strategy
- **AND** its identity, context, catalog, Items, and other Store-scoped data remain associated with the same Store

#### Scenario: Strategy survives reload
- **WHEN** a valid fulfillment strategy is persisted for a Store and the workspace is reopened
- **THEN** FusionCanvas reconstructs the same strategy for that Store
- **AND** another Store's strategy is not inherited or changed

### Requirement: Initial strategy availability is manual-only
The Store Editor SHALL present `Manual`, `Shopify + Manual`, and `Shopify + Printify` as the complete planned strategy set, SHALL enable only `Manual` in this module, and SHALL explain that the disabled strategies require future integrations.

#### Scenario: User configures an existing Store
- **WHEN** the user opens fulfillment strategy configuration for an active Store
- **THEN** `Manual` is selected and available
- **AND** `Shopify + Manual` and `Shopify + Printify` are visible but disabled with explanatory guidance

#### Scenario: User operates the editor by keyboard
- **WHEN** the user reaches fulfillment strategy controls through keyboard navigation
- **THEN** the current strategy and unavailable alternatives expose meaningful accessible names and availability state
- **AND** unavailable strategies cannot be committed

### Requirement: Strategy transitions preserve Store identity and require explicit confirmation
FusionCanvas SHALL model fulfillment strategy as changeable without replacing the Store or its strategy-neutral catalog, and SHALL require an explicit warning before a future enabled transition can disable integrations or make strategy-specific configuration unavailable.

#### Scenario: Future enabled strategy is changed
- **WHEN** a later capability enables a different strategy and the user confirms changing to it
- **THEN** FusionCanvas updates the strategy on the same Store identity
- **AND** retains the Store's Blueprints, Blueprint Offerings, Options, Variants, Placeholders, and Mockup Templates unless that later change explicitly defines a warned migration

#### Scenario: User cancels a strategy warning
- **WHEN** a strategy transition presents a warning and the user cancels it
- **THEN** the prior strategy and all Store configuration remain unchanged

### Requirement: Manual strategy performs no marketplace communication
Under `Manual`, FusionCanvas SHALL treat catalog and mockup-template values as user-entered local data and SHALL NOT communicate with Shopify, Printify, or fulfillment-provider backends.

#### Scenario: User edits a Manual Store catalog
- **WHEN** the user creates or edits Blueprint, offering, Variant, Placeholder, or Mockup Template data for a Manual Store
- **THEN** FusionCanvas persists the confirmed values locally
- **AND** performs no network request or credential lookup for Shopify, Printify, or a Print Provider

#### Scenario: Manual Store records external terminology or identifiers
- **WHEN** a user records a Printify-aligned name or optional external identifier manually
- **THEN** FusionCanvas treats it as local configuration
- **AND** does not imply that the value was validated or synchronized by an external service
