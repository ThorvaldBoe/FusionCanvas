# Manual Listing Details

## Purpose

Defines local customer-facing listing copy, per-Variant pricing, shipping terms, and confirmed Offering migration for manual fulfillment strategies.

## Requirements

### Requirement: Manual Listing Details operate only on local values
The manual Listing Details tool SHALL load, edit, and migrate only local workspace values. It SHALL NOT perform marketplace or fulfillment-provider communication. The existing mockup tool remains available according to its own readiness rules.

#### Scenario: User saves manual Listing Details
- **WHEN** the user loads, edits, or migrates manual Listing Details
- **THEN** FusionCanvas persists the values in the local workspace
- **AND** performs no marketplace, credential, or fulfillment-provider request

### Requirement: Listing copy is separate from Item working copy
FusionCanvas SHALL store an optional customer-facing listing title and description separately from the Item's working title and description. When manual Listing Details are first created, the Item values SHALL seed editable listing drafts without becoming linked values. Existing Item tags SHALL remain the tag source; Listing Details SHALL NOT introduce a second tag collection.

#### Scenario: User starts Listing Details from an Item with working copy
- **WHEN** the user first opens Listing Details for an Item with a working title and description
- **THEN** the Listing title and description are initialized from those Item values
- **AND** later edits to either listing field do not change the Item working title or description
- **AND** existing Item tags remain available without duplicate listing-tag records

#### Scenario: User starts Listing Details from an Item without working copy
- **WHEN** the user opens Listing Details for an Item with an empty working title or description
- **THEN** the corresponding listing field starts empty
- **AND** FusionCanvas does not fabricate text

#### Scenario: User edits listing copy
- **WHEN** the user edits the listing title or description and commits the field
- **THEN** FusionCanvas persists the listing value independently
- **AND** the value is reconstructed after reopening the workspace

### Requirement: Manual listing prices and costs belong to configured Variants
FusionCanvas SHALL allow a user to record a non-negative selling price and expected non-shipping fulfillment cost for each active Variant in the Item's selected Blueprint Offering, using one selected ISO currency code for the active setup. Missing values SHALL remain visibly incomplete rather than being treated as zero. FusionCanvas SHALL derive gross profit from selling price, customer shipping charge, Variant non-shipping fulfillment cost, and expected seller shipping cost; it SHALL NOT persist the derived amount as an independently editable value.

#### Scenario: User records Variant pricing
- **WHEN** an editable Item has an active selected Offering with concrete Variants and the user records valid selling price and non-shipping fulfillment cost values
- **THEN** values are associated with the corresponding Variant identities and the active Item fulfillment setup
- **AND** the currency is shared by those amounts
- **AND** the derived gross profit reflects the active shipping charges and costs

#### Scenario: Variant price or cost is missing
- **WHEN** a Variant has no selling price or expected non-shipping fulfillment cost
- **THEN** Listing Details identifies the missing value as incomplete
- **AND** does not substitute zero or display a fabricated margin

#### Scenario: User enters an invalid amount or currency
- **WHEN** the user enters a negative, malformed, or out-of-range amount, or an unsupported currency code
- **THEN** FusionCanvas rejects that field with recoverable inline guidance
- **AND** preserves the last confirmed value

#### Scenario: Offering has no concrete Variants
- **WHEN** the selected Offering has no active concrete Variants
- **THEN** the pricing area explains that Variant setup is required
- **AND** does not show a misleading empty price editor

### Requirement: Manual Listing Details record one local fulfillment and shipping profile
FusionCanvas SHALL show the Item's one selected active Blueprint Offering as its fulfillment setup and SHALL allow local recording of one shipping profile for that setup. The profile SHALL include a user-entered shipping option name, customer-facing shipping charge, expected shipping cost to the creator, and delivery estimate. These values SHALL be manual records and SHALL not imply live rates, validated service terms, or external synchronization.

#### Scenario: User records shipping terms
- **WHEN** the user enters shipping profile values for the active setup
- **THEN** the option name, customer charge, seller cost, and delivery estimate persist with the Item's active manual Listing Details
- **AND** changes remain local to the workspace

#### Scenario: Shipping terms are incomplete
- **WHEN** one or more shipping values are not provided
- **THEN** Listing Details identifies the missing values without fabricating a rate or delivery promise
- **AND** the user can continue editing other listing values

#### Scenario: Item has no active Offering
- **WHEN** the Item has no active selected Blueprint Offering
- **THEN** Listing Details explains that an Offering must be selected in Design before fulfillment and shipping details can be recorded
- **AND** the user can still edit listing title and description

### Requirement: Changing an Item fulfillment setup uses an explicit migration
When an editable Item has an existing selected Offering and saved manual Listing Details, and the user selects a different active Blueprint Offering in Design, FusionCanvas SHALL present a migration review before changing the selected Offering. The migration SHALL keep at most one active setup for the Item, preserve the prior setup as read-only history, copy Item-level listing title, description, and currency, and copy Variant prices and costs only for exact semantic matches of the complete Option Kind/name/value set. The new setup SHALL begin with blank shipping terms and blank pricing for unmatched Variants. The migration SHALL disclose the Offering-bound Design data that will be reset by the existing replacement behavior and SHALL commit the Item configuration, new Listing Details, and history atomically.

#### Scenario: User reviews an Offering migration
- **WHEN** the user selects a different active same-Store Offering for an Item with an existing Offering and saved manual Listing Details
- **THEN** FusionCanvas shows the source and destination Offering names
- **AND** reports which Variant prices and costs will carry forward and which destination Variants need new values
- **AND** explains that prior setup-specific shipping terms remain in history and the new setup starts without shipping terms
- **AND** explains that existing selected Colors, Design Variant rows, row Color selections, artwork slot assignments, and Offering-specific artwork-target preferences will be cleared
- **AND** the migration is not committed before explicit confirmation

#### Scenario: User confirms an Offering migration
- **WHEN** the user confirms the reviewed migration
- **THEN** FusionCanvas atomically selects the destination Offering, retains the prior Listing Details as read-only history, copies listing copy and currency, transfers exact-match Variant prices and costs, and creates blank terms for unmatched destination Variants
- **AND** clears the Offering-bound Design selections identified in the review
- **AND** preserves Item identity, Item tags, working copy, Design assets, and unrelated workspace data
- **AND** leaves only the destination setup active

#### Scenario: User cancels an Offering migration
- **WHEN** the user cancels the migration review
- **THEN** the source Offering, all Design selections, active Listing Details, and history remain unchanged

#### Scenario: Migration persistence fails
- **WHEN** any part of a confirmed migration fails validation or persistence
- **THEN** FusionCanvas retains the source Offering, Design selections, Listing Details, and history as the last confirmed state
- **AND** reports a recoverable error

#### Scenario: User reviews a prior setup
- **WHEN** an Item has prior fulfillment setup history
- **THEN** Listing Details identifies each prior setup as read-only and shows its saved copy, shipping terms, and Variant price/cost values
- **AND** prior values are not presented as belonging to the active Offering

#### Scenario: User selects the first Offering for an Item with Listing Details
- **WHEN** the user selects an active Offering for an Item with saved manual Listing Details but no prior selected Offering
- **THEN** FusionCanvas attaches the Offering without creating migration history or showing a migration confirmation
- **AND** preserves listing copy and currency
- **AND** creates blank Variant pricing and shipping terms for the new setup

### Requirement: Manual Listing Details follow Item editability and save safety
The Listing Details tool SHALL use the Item's existing Listing-stage editability policy. It SHALL save each confirmed edit atomically, keep pending text drafts safe during Item or tool navigation, and show read-only, loading, empty, success, and recoverable error states as applicable.

#### Scenario: Item becomes protected
- **WHEN** Listing Details is opened for a Published, Rejected, archived, or otherwise protected Item
- **THEN** listing and fulfillment values are shown read-only with the existing policy's explanation
- **AND** no mutation is committed

#### Scenario: User leaves a pending listing text edit
- **WHEN** the user changes Item, tab, or stage while listing text is dirty
- **THEN** the existing Item edit-safety behavior commits or preserves the draft before navigation
- **AND** no pending text is silently lost

#### Scenario: A local save fails
- **WHEN** persistence rejects a Listing Details edit
- **THEN** the last confirmed values remain authoritative
- **AND** the user receives recoverable guidance while retaining the current input where safe

### Requirement: Variant pricing uses an aligned virtual grid
The Manual Listing Details pricing area SHALL present active Variant names, selling prices, and fulfillment costs in aligned columns using a virtualized grid. Each price and cost SHALL remain an inline-editable field bound to the corresponding Variant identity, and the grid SHALL follow the existing `CanEditFulfillmentTerms` policy. The grid SHALL preserve the existing amount validation, inline error feedback, dirty tracking, and Save details behavior.

#### Scenario: User edits Variant pricing
- **WHEN** the user enters a selling price or fulfillment cost in a Variant row
- **THEN** the value updates the matching Variant row model
- **AND** validation, gross profit summary, dirty tracking, and Save details continue to use the existing Listing Details behavior

#### Scenario: User moves between realized price editors with the keyboard
- **WHEN** the user focuses a price editor and presses Tab
- **THEN** focus advances through the fulfillment cost and subsequent realized Variant price editors in row order

#### Scenario: Listing details cannot edit fulfillment terms
- **WHEN** the existing Listing Details policy sets `CanEditFulfillmentTerms` to false
- **THEN** all price and cost editors in the grid are disabled
- **AND** existing listing guidance, save status, and other Listing Details controls remain available according to their existing policies
