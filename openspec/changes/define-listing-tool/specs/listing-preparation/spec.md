## ADDED Requirements

### Requirement: Listing preparation uses one persistent provider-neutral model
FusionCanvas SHALL represent listing preparation as one logical, persistent listing-data model associated one-to-one with the existing Item. Manual fulfillment, Shopify plus manual fulfillment, and Shopify plus Printify fulfillment SHALL be strategies on that model rather than separate Listing tools, separate records, or duplicated common fields.

#### Scenario: Manual listing is prepared without a connector
- **WHEN** a user prepares an Item under manual fulfillment
- **THEN** FusionCanvas exposes the shared Listing-stage preparation surface without requiring marketplace credentials or a FusionCanvas marketplace connector
- **AND** the listing can be prepared for any marketplace or store, including Shopify before Shopify connector support is enabled

#### Scenario: Strategy changes preserve the listing record
- **WHEN** a user changes an existing listing between manual, Shopify plus manual, and Shopify plus Printify fulfillment
- **THEN** FusionCanvas preserves the listing's stable local identity and all confirmed common listing data
- **AND** it changes only the strategy state, applicable capabilities, bindings, and ownership metadata

### Requirement: Common listing properties are provider-neutral
The shared Listing-stage surface SHALL support title, description, tags, price and currency, media references, variant/product references, readiness or publication state, and marketplace metadata only where the metadata is genuinely provider-neutral. Existing Item title/description, reusable tag links, asset links, and catalog references SHALL remain the canonical sources rather than being copied into strategy-specific records.

#### Scenario: User edits common listing data manually
- **WHEN** an active editable listing is in manual fulfillment
- **THEN** the user can maintain its common title, description, tags, price/currency, media references, variant/product references, readiness state, and shared metadata
- **AND** the values persist with the same Item/listing identity

#### Scenario: Shopify strategy displays common data
- **WHEN** a listing uses Shopify plus manual or Shopify plus Printify fulfillment
- **THEN** the shared surface continues to display the same common listing data
- **AND** Shopify-specific controls extend or map that data without creating duplicate title, description, tag, price, or currency fields

#### Scenario: Invalid common data is rejected recoverably
- **WHEN** a user submits invalid listing data such as an unsupported currency, malformed price, or invalid reference
- **THEN** FusionCanvas rejects the affected change with inline actionable guidance
- **AND** leaves the last confirmed listing data unchanged while preserving recoverable user input where applicable

### Requirement: Listing fields have explicit source and override ownership
FusionCanvas SHALL record or deterministically resolve whether each listing field is sourced from the Item or Store catalog, manually overridden, or managed by a connected provider. A manual override SHALL remain distinguishable from an inherited or provider-managed value and SHALL NOT be silently replaced by a strategy transition or synchronization attempt.

#### Scenario: User overrides an inherited common value
- **WHEN** a user changes a value supplied by Item or catalog context and commits it as a listing value
- **THEN** FusionCanvas retains the explicit value and its manual-override ownership
- **AND** later strategy changes do not silently restore the inherited value

#### Scenario: Provider ownership is unavailable locally
- **WHEN** a listing is in a Shopify strategy but no Shopify connector operation has completed for a provider-managed field
- **THEN** FusionCanvas keeps the local value and reports that provider ownership or synchronization is unavailable
- **AND** does not erase or replace the local value

### Requirement: Strategies control capability visibility and enabling
FusionCanvas SHALL provide one shared Listing-stage UI whose common sections remain stable while strategy-specific sections and actions are progressively disclosed and enabled only when their prerequisites are satisfied.

#### Scenario: Manual strategy shows local preparation
- **WHEN** manual fulfillment is selected
- **THEN** the shared common fields and manual readiness guidance are visible
- **AND** connector-specific publish, sync, and provider-management actions are hidden or disabled with actionable guidance

#### Scenario: Shopify plus manual binds an existing item
- **WHEN** the user selects Shopify plus manual fulfillment and has not yet bound a Shopify item
- **THEN** Shopify management actions remain unavailable and the UI explains that a Shopify item identity is required
- **WHEN** the user selects a valid unbound Shopify item
- **THEN** FusionCanvas persists the provider/channel binding and enables the applicable Shopify management surface

#### Scenario: Shopify plus Printify acquires identity after publication
- **WHEN** a Printify publication operation successfully produces a Shopify item identity
- **THEN** FusionCanvas stores that identity on the same listing model and enables the Shopify management surface
- **AND** the Printify preparation surface becomes locked by default with an explicit warning and unlock action for exceptional post-publish edits

### Requirement: External identities and provider state extend the shared model
FusionCanvas SHALL scope external identities by provider and channel and SHALL retain Shopify identity, publication-channel state, provider metadata, publish or synchronization status, last attempt/result, external-state timestamp, recoverable error details, and conflict information as optional extensions of the shared listing model. The first local phase SHALL not require a live connector to create or edit these records.

#### Scenario: Shopify identity is acquired by either strategy
- **WHEN** a Shopify item identity is selected manually or returned by successful Printify publication
- **THEN** FusionCanvas stores the same provider/channel-scoped identity on the listing
- **AND** the identity is visible and copyable for debugging

#### Scenario: Provider operation fails
- **WHEN** a future provider operation fails or returns a conflict
- **THEN** FusionCanvas retains the local listing data and records an actionable diagnostic with operation status and conflict/error context
- **AND** it does not report the remote change as confirmed

### Requirement: Readiness and publication state are distinct and recoverable
FusionCanvas SHALL distinguish local preparation/readiness from external publication state. A listing SHALL be able to remain locally ready for manual work without being externally published, and unavailable connectors SHALL not invalidate the local listing.

#### Scenario: Locally ready manual listing remains unpublished
- **WHEN** a user completes the required local preparation checks under manual fulfillment
- **THEN** FusionCanvas marks the listing locally ready for manual marketplace work
- **AND** does not fabricate a published state or external identity

#### Scenario: Publication state is unavailable
- **WHEN** no provider identity or confirmed publication result exists
- **THEN** the UI presents the listing as locally prepared or unpublished according to its local state
- **AND** keeps provider actions unavailable rather than implying remote publication

### Requirement: Listing preparation persists atomically and migrates without data loss
FusionCanvas SHALL persist common listing data, strategy state, ownership metadata, and optional provider/channel state atomically. Existing Items SHALL migrate to valid manual listings without requiring connector configuration, losing identity, or duplicating existing common data.

#### Scenario: Existing Item is opened after migration
- **WHEN** an existing workspace is opened after the listing-preparation schema migration
- **THEN** each existing Item remains addressable as a valid manual listing with its title, description, tags, assets, catalog references, and metadata intact
- **AND** optional provider/channel state is empty unless it was already confirmed

#### Scenario: Listing save fails
- **WHEN** persistence fails while saving listing preparation data
- **THEN** FusionCanvas reports a recoverable error
- **AND** leaves the last confirmed workspace snapshot unchanged without partial strategy, ownership, or provider-state updates
