## MODIFIED Requirements

### Requirement: The Printify tool consumes authoritative local product inputs
The Printify tool SHALL use the current Item title and optional description, the Item's selected Blueprint Offering and fixed Print Provider, the Item's Listing Color and compatible sellable Variant selections, and the Design stage's artwork-variant Design Area assignments. It SHALL NOT read or mutate Design-stage product Color selections. Provider-Network Offerings without a resolved fixed Provider SHALL be unavailable for product creation and update.

#### Scenario: Listing Colors are projected into Printify variants
- **WHEN** the Item's Listing contains selected Colors and the selected fixed Provider exposes compatible configured sellable Variants
- **THEN** the Printify tool projects the selected Variant identities into the desired product variant set
- **AND** it does not add unselected Colors or omit selected configured Sizes solely due to current out-of-stock status

#### Scenario: A selected Color is not supported by the Provider
- **WHEN** a Listing-selected Color has no compatible current Provider Variant
- **THEN** the Printify tool explains the missing mapping as a readiness problem
- **AND** directs the user to resolve the Offering or Listing selection
- **AND** does not silently remove the Color or mutate Design data

#### Scenario: Provider-Network Offering is selected
- **WHEN** the Item's Offering is a Provider Network without a resolved fixed Provider
- **THEN** product creation and update remain unavailable
- **AND** the tool explains that a fixed Provider is required for this module

### Requirement: Printify publication reminds the creator to verify out-of-stock availability behavior
Before each Printify Publish or republish operation, FusionCanvas SHALL present a manual reminder to check the product's Printify availability setting for showing variants as available when they are out of stock. The reminder SHALL require explicit acknowledgement to continue, SHALL make clear that FusionCanvas cannot verify or set the remote option through its supported API, and SHALL NOT imply that acknowledgement is proof of the Printify setting's current state. The reminder SHALL NOT refer to Printify's separate automatic Order Routing feature.

#### Scenario: Creator acknowledges the availability reminder
- **WHEN** the creator starts a Printify Publish or republish operation and acknowledges the reminder
- **THEN** the operation may continue through the existing publication readiness and confirmation flow
- **AND** FusionCanvas records no verified remote availability state from the acknowledgement

#### Scenario: Creator cancels at the availability reminder
- **WHEN** the creator dismisses or declines the reminder
- **THEN** FusionCanvas sends no publish request
- **AND** preserves the local product mapping and current publication state

#### Scenario: Creator publishes when the API cannot verify the setting
- **WHEN** the creator acknowledges the reminder but the supported Printify API does not expose the availability setting
- **THEN** FusionCanvas continues to label the setting as manually checked/unverified
- **AND** does not claim it is enabled or automatically manage out-of-stock orders
