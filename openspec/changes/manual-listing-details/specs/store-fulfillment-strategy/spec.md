## ADDED Requirements

### Requirement: Manual Listing Details visibility follows Store fulfillment strategy
FusionCanvas SHALL expose local manual Listing Details for the `Manual` strategy and for `ShopifyManual` when that strategy is enabled. It SHALL expose the Printify listing workflow, rather than local manual Listing Details, for `Printify` and `ShopifyPrintify` strategies. Tool visibility SHALL derive from the active Store's persisted strategy and SHALL not change or migrate the strategy itself.

#### Scenario: Manual strategy is active
- **WHEN** an Item at Listing belongs to a Store whose strategy is `Manual`
- **THEN** local manual Listing Details are available
- **AND** no external system is contacted

#### Scenario: Shopify Manual strategy is enabled and active
- **WHEN** an Item at Listing belongs to a Store whose strategy is `ShopifyManual`
- **THEN** local manual Listing Details are available
- **AND** this capability does not call Shopify

#### Scenario: Printify strategy is active
- **WHEN** an Item at Listing belongs to a Store whose strategy is `Printify` or `ShopifyPrintify`
- **THEN** local manual Listing Details are unavailable
- **AND** Printify tool availability follows the Printify lifecycle and connection readiness rules

#### Scenario: Active Store changes
- **WHEN** the selected Store changes to one with a different fulfillment strategy
- **THEN** the Listing-stage tools refresh from the newly selected Store's strategy
- **AND** values from the prior Store are not exposed or edited
