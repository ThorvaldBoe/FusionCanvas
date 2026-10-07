## ADDED Requirements

### Requirement: Printify lifecycle capabilities are derived from fulfillment strategy
For an active Store using `Printify` or `Shopify + Printify`, Fusion Canvas SHALL make Printify product creation, update, refresh, and reconciliation capabilities available when Printify readiness passes. Shopify publish and unpublish capabilities SHALL be available only for `Shopify + Printify` when Shopify publication readiness passes. The user SHALL not configure the publication capability independently.

#### Scenario: Strategy is standalone Printify
- **WHEN** the Store's fulfillment strategy is `Printify`
- **THEN** Printify product lifecycle actions may be available after readiness
- **AND** Shopify publication actions are hidden or unavailable with an explanation

#### Scenario: Strategy is Shopify + Printify
- **WHEN** the Store's fulfillment strategy is `Shopify + Printify`
- **THEN** Printify product lifecycle actions and Printify-mediated Shopify publication actions may be available after their readiness checks

#### Scenario: Strategy is not Printify-enabled
- **WHEN** the Store's fulfillment strategy is `Manual` or `Shopify + Manual`
- **THEN** the Printify tool does not enable remote Printify operations
- **AND** existing historical mappings remain visible without being mutated automatically

### Requirement: Strategy changes preserve external history and fail closed
Changing a Store's fulfillment strategy SHALL not delete Printify credentials, catalog data, product mappings, or publication history. When the new strategy does not support Printify, dependent Printify remote actions SHALL be disabled. When the Store later returns to a Printify-enabled strategy, the application SHALL require readiness verification and reconciliation before enabling mutations.

#### Scenario: User switches away from Printify
- **WHEN** the user changes a Store from a Printify-enabled strategy to a non-Printify strategy
- **THEN** Fusion Canvas preserves existing Printify mappings and history
- **AND** disables Printify mutations without sending automatic remote delete or unpublish requests

#### Scenario: User switches back to Printify
- **WHEN** the user changes the Store back to `Printify` or `Shopify + Printify`
- **THEN** the application requires Printify readiness verification
- **AND** existing mappings remain read-only until reconciliation completes
