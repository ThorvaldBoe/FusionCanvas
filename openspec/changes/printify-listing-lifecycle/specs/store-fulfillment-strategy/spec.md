## ADDED Requirements

### Requirement: Printify lifecycle capabilities are derived from fulfillment strategy
For an active Store using `Printify` or `Shopify + Printify`, Fusion Canvas SHALL make Printify product creation, update, refresh, and reconciliation capabilities available when Printify readiness passes. Publish and unpublish capabilities SHALL be available when the selected Printify shop reports a connected supported sales channel and publication readiness passes. For `Shopify + Printify`, the connected channel SHALL be Shopify. The user SHALL not configure the publication capability independently.

#### Scenario: Strategy is standalone Printify with a connected sales channel
- **WHEN** the Store's fulfillment strategy is `Printify` and its selected Printify shop has a connected supported sales channel
- **THEN** Printify product lifecycle and publish/unpublish actions may be available after readiness

#### Scenario: Strategy is standalone Printify without a connected sales channel
- **WHEN** the Store's fulfillment strategy is `Printify` and its selected Printify shop reports `disconnected`
- **THEN** Printify product lifecycle actions may be available after product readiness
- **AND** publish/unpublish actions are unavailable with an explanation

#### Scenario: Strategy is Shopify + Printify
- **WHEN** the Store's fulfillment strategy is `Shopify + Printify` and its selected Printify shop reports Shopify as the connected sales channel
- **THEN** Printify product lifecycle actions and Printify-mediated Shopify publication actions may be available after their readiness checks
- **AND** a non-Shopify or disconnected channel disables publication with an explanation

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
