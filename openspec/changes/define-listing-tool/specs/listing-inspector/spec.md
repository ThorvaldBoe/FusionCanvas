## MODIFIED Requirements

### Requirement: Listing inspector presents stage-relevant creative fields
The Item document surface SHALL show exactly one primary built-in Stage Tool for the active view stage, SHALL make current-stage content editable when lifecycle policy allows, SHALL present earlier-stage content read-only while retaining shared Item metadata outside the tool, and SHALL use the shared Listing-stage tool for listing preparation without introducing a competing Shopify or Printify Listing tool.

#### Scenario: Listing is at the Listing stage
- **WHEN** the active Item's current stage and active view stage are both `Listing`
- **THEN** the shared Listing-stage tool shows the provider-neutral listing preparation fields and the selected fulfillment strategy state
- **AND** it keeps the existing shared Item overview, lifecycle, tags, and related-asset context coordinated with the same document

#### Scenario: Listing strategy changes
- **WHEN** the user changes the active listing's fulfillment strategy
- **THEN** the same Listing-stage tool updates strategy-specific visibility and enabled state
- **AND** it does not open a second tool or duplicate common fields

#### Scenario: User reviews an earlier stage
- **WHEN** the active Item is at Listing and the user activates Idea, Concept, or Design
- **THEN** the selected earlier Stage Tool is visible read-only according to existing stage policy
- **AND** the listing preparation data remains preserved and available when Listing is reactivated

#### Scenario: Inactive listing opens
- **WHEN** the listing is archived or effectively inactive through its parent path
- **THEN** the shared Listing-stage tool presents its confirmed listing data read-only
- **AND** disables strategy and provider mutations while retaining lifecycle guidance
