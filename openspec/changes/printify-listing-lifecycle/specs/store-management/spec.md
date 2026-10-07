## ADDED Requirements

### Requirement: Store shop changes are blocked when Printify mappings exist
The Store Editor SHALL prevent changing the selected Printify shop when the Store has one or more Item-to-Printify product mappings. The blocked guidance SHALL explain that changing the shop would invalidate existing mappings and SHALL identify the safe future path as disconnecting or migrating mappings before changing the shop.

#### Scenario: Store has no mapped Printify products
- **WHEN** the user changes the selected Printify shop for a Store with no Printify product mappings
- **THEN** the Store Editor permits the change after normal validation

#### Scenario: Store has mapped Printify products
- **WHEN** the user attempts to change the selected Printify shop for a Store with mapped Printify products
- **THEN** the Store Editor blocks the change
- **AND** preserves the existing shop selection
- **AND** explains how many mappings prevent the change

### Requirement: Store Printify readiness is explicit and repairable
The Store Editor SHALL expose a Printify connection state that distinguishes ready, checking, unavailable, and needs-reconnection conditions. It SHALL provide guidance to verify or replace the credential and reselect a valid shop without exposing the credential itself. Existing catalog data and product mappings SHALL remain preserved when readiness fails.

#### Scenario: Credential becomes invalid
- **WHEN** Printify rejects a credential used by a Store
- **THEN** the Store Editor shows an unavailable or needs-reconnection state
- **AND** preserves the selected shop, imported catalog, and product mappings
- **AND** disables dependent remote mutations until readiness is restored

#### Scenario: Readiness is restored
- **WHEN** the user verifies a replacement credential and selects a valid shop
- **THEN** the Store Editor shows the Store as ready
- **AND** dependent Printify tools can revalidate and reconcile existing mappings before mutation
