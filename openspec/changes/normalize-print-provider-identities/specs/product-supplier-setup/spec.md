## MODIFIED Requirements

### Requirement: Store catalog maintains product and fulfillment structure
FusionCanvas SHALL let a user maintain Blueprints and one or more Blueprint Offerings within an active Store, SHALL associate each offering with either one fixed Print Provider or one named Provider Network, SHALL maintain at most one active Store-owned Print Provider for each trimmed, case-insensitive provider name, and SHALL preserve that catalog across workspace reload.

#### Scenario: User adds a Blueprint and fixed-Print-Provider offering
- **WHEN** the user saves a valid Blueprint, Print Provider, and fixed-Print-Provider Blueprint Offering in the Store Editor
- **THEN** FusionCanvas persists all records with stable identities scoped to the selected Store
- **AND** the offering remains associated with the Blueprint and Print Provider after reload

#### Scenario: User attempts to create a duplicate provider name
- **WHEN** the user attempts to create an active Print Provider whose trimmed, case-insensitive name is already used by an active Print Provider in the same Store
- **THEN** FusionCanvas rejects the creation with recoverable guidance to use the existing provider
- **AND** the existing provider and all catalog relationships remain unchanged

#### Scenario: Catalog setup repairs persisted same-name providers
- **WHEN** Catalog setup loads a Store containing multiple Print Provider records with the same trimmed, case-insensitive name
- **THEN** FusionCanvas selects one deterministic surviving provider identity
- **AND** reassigns every Blueprint Offering in that Store that references another same-name provider to the survivor
- **AND** archives the superseded provider records without deleting them
- **AND** the active provider picker exposes only the surviving provider name once

#### Scenario: User adds a Provider-Network offering
- **WHEN** the user saves a valid Blueprint Offering whose kind is Provider Network
- **THEN** FusionCanvas requires a stable provider-network code and a display name
- **AND** does not require or fabricate an ordinary Print Provider identity

#### Scenario: Catalog is isolated by Store
- **WHEN** the user opens catalog setup for another Store
- **THEN** FusionCanvas shows only that Store's Blueprints, Print Providers, Blueprint Offerings, Options, Values, Variants, and Placeholders
- **AND** it does not expose or permit editing catalog records from another Store

### Requirement: Imported catalog records retain provider identity and local terminology
Imported catalog records SHALL use the existing Blueprint, Blueprint Offering, Option, Option Value, Variant, and Placeholder/design-area concepts, preserve Store ownership, and retain Printify product and provider source identities separately from editable display labels. Within a Store, a provider's normalized title SHALL be sufficient to identify the canonical local Print Provider; the local Print Provider ID SHALL remain the relationship key for Blueprint Offerings.

#### Scenario: Import maps one provider offering
- **WHEN** a selected Printify Blueprint has one Print Provider offering
- **THEN** FusionCanvas creates or updates one Store-owned Blueprint and offering
- **AND** the offering retains the Printify Blueprint and provider source identities
- **AND** the offering references the canonical local Print Provider identity

#### Scenario: Imported variant compatibility is preserved
- **WHEN** Printify assigns a design area to a subset of sellable variants
- **THEN** the corresponding local Placeholder/design area is compatible with exactly those imported variants
- **AND** the relationship remains stable after a repeated import
