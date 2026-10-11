# Printify Existing Product Linking

## Purpose

Define how FusionCanvas verifies Printify listing readiness and safely links an existing Printify product to an Item.

## Requirements

### Requirement: Printify listing readiness verifies the selected shop
The Printify Listing tool SHALL verify the configured shop through the authenticated account's shop list and SHALL enable remote product operations only when that exact shop is present. An unavailable, malformed, or mismatched response SHALL fail closed.

#### Scenario: The configured shop is present
- **WHEN** Printify returns a valid account shop list containing the exact selected shop ID
- **THEN** the Listing tool reports the connection as ready
- **AND** it enables remote product operations according to the Store strategy

#### Scenario: The configured shop is not present
- **WHEN** the authenticated account shop list does not contain the selected shop ID
- **THEN** the Listing tool reports the connection as unavailable
- **AND** it disables remote product operations
- **AND** it preserves the current local mapping and product identity

#### Scenario: The account shop response is invalid
- **WHEN** the account shop endpoint returns an unsuccessful, malformed, oversized, or invalid response
- **THEN** the Listing tool reports a safe unavailable state
- **AND** it does not infer that the selected shop is ready

### Requirement: Existing Printify products are verified before linking
For an active editable Item in a Printify-enabled Store, FusionCanvas SHALL allow the user to enter an existing Printify product ID. Before presenting confirmation, it SHALL read the product through the Store's selected shop-scoped product endpoint and SHALL require the response's product ID and shop ID to match the requested values exactly. Linking SHALL NOT mutate the remote product.

#### Scenario: Product belongs to the selected shop
- **WHEN** the entered product ID resolves through the selected shop endpoint and the returned product and shop IDs match
- **THEN** FusionCanvas shows the verified product ID and title
- **AND** offers a confirmation that names the Item, shop, product ID, and title
- **AND** does not change the local mapping before confirmation

#### Scenario: Product does not exist in the selected shop
- **WHEN** the product endpoint reports that the entered ID is missing from the selected shop
- **THEN** FusionCanvas explains that the product could not be verified
- **AND** leaves the existing mapping unchanged

#### Scenario: Returned product or shop identity does not match
- **WHEN** the product response omits or mismatches the requested product ID or selected shop ID
- **THEN** FusionCanvas rejects the candidate
- **AND** leaves the existing mapping unchanged

#### Scenario: Printify cannot verify the product
- **WHEN** product verification fails because of connection, permission, response, or service errors
- **THEN** FusionCanvas shows a safe actionable error
- **AND** leaves the existing mapping unchanged

### Requirement: Linking requires confirmation and preserves mapping integrity
FusionCanvas SHALL update the local Item-to-Printify mapping only after explicit user confirmation and a fresh successful product verification. It SHALL reject a product already mapped to another Item in the same Store. It SHALL retain the prior mapping when verification fails or the user cancels. It SHALL prevent replacing a mapping with a pending or uncertain operation. The link action SHALL NOT create, edit, publish, unpublish, or delete the remote product.

#### Scenario: User confirms an unlinked product
- **WHEN** the candidate remains valid during confirmation and is not mapped to another Item in the Store
- **THEN** FusionCanvas persists the Store, Item, selected shop, product identity, remote snapshot, integration-owned values, and publication state
- **AND** shows the linked product ID in the Listing tool
- **AND** does not mutate the remote product

#### Scenario: User replaces a verified product mapping
- **WHEN** the Item has a stable mapping and the user confirms a different verified product that is not mapped elsewhere
- **THEN** FusionCanvas replaces the product identity and initializes synchronization data from the newly verified remote product
- **AND** preserves the existing mapping if the fresh verification fails

#### Scenario: User cancels linking
- **WHEN** the user cancels the confirmation
- **THEN** FusionCanvas discards the pending candidate
- **AND** retains the current mapping and makes no repository or remote product change

#### Scenario: Product is already mapped to another Item
- **WHEN** the verified product is already mapped to a different Item in the same Store
- **THEN** FusionCanvas rejects the link with an actionable explanation
- **AND** retains the current mapping

#### Scenario: Existing mapping has an uncertain operation
- **WHEN** the Item's mapping is pending or uncertain
- **THEN** FusionCanvas disables replacement
- **AND** directs the user to refresh or reconcile the existing product first

#### Scenario: Item is read-only
- **WHEN** the Listing tool is opened for a read-only Item
- **THEN** FusionCanvas shows the current product ID when available
- **AND** disables candidate verification, confirmation, and replacement

### Requirement: The linked Printify product identity is visible and copyable
The Listing tool SHALL show the currently linked Printify product ID in a selectable or equivalent keyboard-copyable control. The verified product ID SHALL remain distinct from the editable candidate field, and the current mapping SHALL remain visible until a replacement is confirmed.

#### Scenario: Item has a linked product
- **WHEN** a persisted Printify mapping has a product ID
- **THEN** the Listing tool shows that ID in a keyboard-selectable control
- **AND** keeps it visible while another candidate is being verified or confirmation is pending

#### Scenario: Item has no linked product
- **WHEN** the Item has no Printify product mapping
- **THEN** the Listing tool shows a clear unlinked state
- **AND** allows an editable user to enter a candidate product ID
