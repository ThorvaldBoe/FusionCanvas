## ADDED Requirements

### Requirement: Printify Listing tool availability follows Store and connection readiness
The Listing stage SHALL expose a Printify tool for an Item whose Store uses `Printify` or `Shopify + Printify`. The tool SHALL evaluate connection readiness before enabling any remote operation. Readiness SHALL require an active Store, a usable Printify credential, a reachable selected Printify shop belonging to that credential, and the permissions required for the requested operation. Shopify publication actions SHALL additionally require that the selected Printify shop is connected to Shopify.

#### Scenario: Standalone Printify Store is ready
- **WHEN** the Item belongs to an active Store using `Printify` and the credential and selected shop pass readiness checks
- **THEN** the Printify tool enables create, update, refresh, reconciliation, and unpublished-product management actions
- **AND** it does not expose Shopify publication actions

#### Scenario: Shopify + Printify Store is publication-ready
- **WHEN** the Item belongs to an active Store using `Shopify + Printify` and the credential, selected shop, and Shopify connection pass readiness checks
- **THEN** the Printify tool enables product lifecycle actions and Shopify publish/unpublish actions

#### Scenario: Printify connection is unavailable
- **WHEN** a credential, shop, permission, or connectivity check fails
- **THEN** the tool shows a clear unavailable or needs-reconnection state
- **AND** disables remote mutations
- **AND** preserves local Item, Design, and existing mapping data

### Requirement: The Printify tool consumes authoritative local product inputs
The Printify tool SHALL use the current Item title and optional description, the selected Blueprint, one fixed Print Provider, the Design stage's selected colors and variant rows, and the Design stage's design-area assignments. The tool SHALL NOT edit Design color selections or introduce a second variant-policy editor. Provider-Network Offerings without a resolved fixed provider SHALL be unavailable for product creation and update.

#### Scenario: Design colors are projected into Printify variants
- **WHEN** the Design stage contains selected colors and the selected fixed Provider exposes compatible color and size variants
- **THEN** the Printify tool projects those selected colors and compatible sizes into the desired product variant set
- **AND** it does not add unselected colors

#### Scenario: A selected color is not supported by the Provider
- **WHEN** a Design-selected color has no compatible current Provider variant
- **THEN** the Printify tool explains the missing mapping as a readiness problem
- **AND** directs the user to resolve the color in the Design stage
- **AND** does not silently remove the color or mutate Design data

#### Scenario: Provider-Network Offering is selected
- **WHEN** the Item's Offering is a Provider Network without a concrete fixed Provider
- **THEN** product creation and update remain unavailable
- **AND** the tool explains that a fixed Provider is required for this module

### Requirement: Artwork is projected to Printify print areas with tolerant placement
The Printify tool SHALL map each valid Design-stage design-area assignment to the corresponding Printify print area and placeholder. Each uploaded artwork image SHALL be placed with zero rotation, its full width scaled to the target print-area width, and its top edge aligned with the target print-area top edge. The tool SHALL permit aspect-ratio differences, including square artwork, and SHALL NOT reject a product solely because the artwork dimensions do not exactly match the target placeholder.

#### Scenario: Matching target-resolution artwork is uploaded
- **WHEN** an artwork asset is assigned to a compatible design area
- **THEN** the tool uploads or reuses the remote image reference
- **AND** positions the image at full target width with its top edge aligned to the print area
- **AND** applies no rotation

#### Scenario: Square artwork is used for a non-square print area
- **WHEN** a square artwork asset is assigned to a compatible non-square print area
- **THEN** the tool accepts the asset
- **AND** scales it to the full target width
- **AND** aligns its top edge to the target print-area top

#### Scenario: A design area has no valid assignment
- **WHEN** a required Printify placeholder has no compatible Design-stage assignment
- **THEN** the tool identifies the missing assignment as a readiness problem
- **AND** does not create or update the remote product

### Requirement: Pricing uses one explicit initial policy
The Printify tool SHALL support either a fixed retail price policy or a fixed profit amount policy for one product operation. Fixed retail price SHALL apply the same retail price to every enabled variant. Fixed profit amount SHALL calculate each enabled variant's retail price from its current production cost plus the configured profit amount. The tool SHALL not provide arbitrary per-variant pricing in this module.

#### Scenario: Fixed retail price is selected
- **WHEN** the user selects fixed retail price and confirms a valid price
- **THEN** every enabled Printify variant receives that retail price

#### Scenario: Fixed profit amount is selected
- **WHEN** the user selects fixed profit amount and confirms a valid amount
- **THEN** each enabled variant receives its production cost plus that amount
- **AND** variants with different production costs receive different calculated retail prices when necessary

### Requirement: Product creation and update are synchronized and idempotent
The Printify tool SHALL create a product when no mapping exists and SHALL update the mapped product when a mapping exists. A successful operation SHALL persist the Store identity, Printify shop ID, Printify product ID, external publication identity when present, the resulting lifecycle state, and a normalized synchronization snapshot. An ambiguous create or update result SHALL trigger remote re-read or reconciliation before another mutation is attempted.

#### Scenario: New product is created
- **WHEN** readiness passes and the user confirms creation for an Item without a Printify mapping
- **THEN** the tool uploads required artwork, creates one Printify product, persists its stable identity, and shows the resulting state

#### Scenario: Existing product is updated
- **WHEN** readiness passes and the user confirms an update for an Item with a mapped Printify product
- **THEN** the tool updates that product from the desired Fusion Canvas projection
- **AND** refreshes the persisted synchronization snapshot

#### Scenario: Create response is lost
- **WHEN** the create request may have succeeded but Fusion Canvas does not receive a definitive response
- **THEN** the mapping enters an uncertain state
- **AND** the tool attempts to reconcile the shop before allowing another create
- **AND** it does not blindly create a second product

### Requirement: Remote drift requires an explicit resolution
Before mutating a mapped product, the Printify tool SHALL compare the current remote product with the last synchronized snapshot and the current desired Fusion Canvas projection. When a remote-only change is detected, the tool SHALL identify the changed fields and offer accepting the remote values into Fusion Canvas or updating Printify from Fusion Canvas. The tool SHALL not silently overwrite either side.

#### Scenario: Remote price changed outside Fusion Canvas
- **WHEN** the last synchronized price and local desired price are `$30` but the current Printify price is `$40`
- **THEN** the tool identifies the remote price change
- **AND** offers accepting `$40` into Fusion Canvas or updating Printify back to `$30`

#### Scenario: User accepts a remote listing change
- **WHEN** the user accepts a remote change to an integration-owned field
- **THEN** Fusion Canvas updates that local integration value and synchronization snapshot
- **AND** does not silently change Design-stage colors or local source artwork

#### Scenario: User keeps the Fusion Canvas value
- **WHEN** the user chooses to keep the local value
- **THEN** the tool updates Printify from the current Fusion Canvas projection
- **AND** records a new synchronization snapshot after success

### Requirement: Shopify publication is Printify-mediated and strategy-gated
The Printify tool SHALL use only Printify operations for Shopify publication. It SHALL expose publish and unpublish actions only when the Store uses `Shopify + Printify` and publication readiness passes. A successful publication SHALL persist the Printify product identity, the external sales-channel identity when returned, and a distinct publication state. The publication state SHALL remain separate from the Item lifecycle status.

#### Scenario: Publish is unavailable for standalone Printify
- **WHEN** the Store uses standalone `Printify`
- **THEN** the tool does not expose or invoke a Shopify publication action

#### Scenario: Product is published through Printify
- **WHEN** the Store uses `Shopify + Printify`, readiness passes, and the user confirms publish
- **THEN** the tool invokes the Printify publication operation
- **AND** does not call Shopify directly
- **AND** records the resulting publication or pending state

#### Scenario: Publication result is pending or uncertain
- **WHEN** Printify accepts or may have accepted a publication request but the final external state is not yet verified
- **THEN** the product is shown as publishing or unverified
- **AND** the tool offers refresh/reconciliation rather than issuing an uncontrolled duplicate publication request

#### Scenario: Published product is unpublished
- **WHEN** the Store uses `Shopify + Printify`, the product is published, and the user confirms unpublish
- **THEN** the tool requests unpublication through Printify
- **AND** verifies or reports the resulting external state
- **AND** preserves the Printify product and its design/settings mapping

### Requirement: Destructive product actions are explicitly separated
The Printify tool SHALL distinguish local archiving from remote deletion. Local archive SHALL preserve the mapping and remote product. Remote deletion SHALL require explicit confirmation and SHALL be available only after the product is verified as unpublished, unlocked, and not in an uncertain state. A published, locked, or uncertain product SHALL not be remotely deleted.

#### Scenario: User archives locally
- **WHEN** the user archives the local Printify mapping or Item
- **THEN** Fusion Canvas preserves the remote product and mapping history
- **AND** no remote deletion request is sent

#### Scenario: User deletes an unpublished remote product
- **WHEN** the mapped Printify product is verified as unpublished and unlocked
- **THEN** the tool requests explicit confirmation naming the remote product
- **AND** deletes the remote product only after confirmation
- **AND** records the mapping as deleted or detached after success

#### Scenario: User attempts to delete a published product
- **WHEN** the mapped product is published
- **THEN** remote deletion is unavailable
- **AND** the tool explains that the product must be unpublished first

### Requirement: Lifecycle and connection states are visible and accessible
The Printify tool SHALL show distinct text states for connection readiness, synchronization, product existence, and Shopify publication. Visual color MAY reinforce state but SHALL not be the only indication. The tool SHALL represent at least unavailable, ready, draft/saved, publishing, published, unpublished, remote-changed, missing, uncertain, and failed states with actionable guidance where recovery is possible.

#### Scenario: Product is published
- **WHEN** a mapped product has verified external publication
- **THEN** the tool shows a clearly labeled published state with a positive visual indicator
- **AND** exposes unpublish and update actions according to the current strategy and readiness

#### Scenario: Product is remote-changed
- **WHEN** reconciliation detects remote drift
- **THEN** the tool shows a clearly labeled remote-changed state
- **AND** exposes the conflict review before mutation

#### Scenario: Connection becomes unavailable
- **WHEN** a previously ready connection fails verification
- **THEN** the tool shows connection unavailable and disables remote actions
- **AND** preserves the last known product and publication information with an unverified indication
