## ADDED Requirements

### Requirement: A Niche can start a Printify listing import
FusionCanvas SHALL offer an `Import from Printify…` action from an active Niche's navigation context menu. The action SHALL use that Niche's active Store and its configured Printify credential and selected shop. It SHALL explain when the Store or Printify connection is not ready and SHALL not start a remote request until the user opens the import flow.

#### Scenario: Open import from an active Niche
- **WHEN** the user invokes `Import from Printify…` for an active Niche in a Store with a configured Printify connection
- **THEN** FusionCanvas opens a focused import dialog for that Store's selected Printify shop
- **AND** the main workspace remains on the invoking Niche

#### Scenario: Printify connection is unavailable
- **WHEN** the user invokes the import action for a Store without a usable Printify credential or selected shop
- **THEN** FusionCanvas explains the missing setup and provides a route to Store setup
- **AND** no Printify product request is made

### Requirement: The import dialog previews all shop products and identifies linked products
The import dialog SHALL retrieve every product from the selected Printify shop, following the API's pagination. It SHALL preview each product with enough information to distinguish it, including its title and visibility state. Products already linked to a FusionCanvas Item by the same Store, shop, and Printify product identity SHALL be hidden by default. The dialog SHALL provide a control to show or hide linked products; linked products SHALL remain ineligible for import.

#### Scenario: Preview an unlinked shop product
- **WHEN** shop product retrieval succeeds
- **THEN** the dialog displays all retrieved shop products across pages with their title and visibility state
- **AND** unlinked products can be selected individually

#### Scenario: Show products already imported
- **WHEN** the user enables the control to show already imported products
- **THEN** previously linked products appear with a linked indicator
- **AND** those products cannot be selected for another import

#### Scenario: Product retrieval is empty or fails
- **WHEN** the selected Printify shop has no products or product retrieval fails
- **THEN** the dialog presents a distinct empty or error state
- **AND** an error state offers a retry without changing local workspace data

### Requirement: Users select products and run a separate duplicate check
The import dialog SHALL provide an individual checkbox for every eligible unlinked product. It SHALL allow the user to select and unselect products and SHALL run duplicate checking only when the user explicitly requests it for the selected products. Duplicate checking SHALL compare normalized Printify title and description against active Items in the selected Store, including Items in other Niches. A close match SHALL be presented as a possible duplicate for user review and SHALL NOT silently block import or change local data.

#### Scenario: Check selected products for possible duplicates
- **WHEN** the user selects eligible products and runs the duplicate check
- **THEN** FusionCanvas compares each selected product's title and description with active Items in the selected Store
- **AND** presents close matches as possible duplicates with enough local Item context to identify each candidate
- **AND** leaves unmatched products available for import as new Items

#### Scenario: No products are selected for duplicate checking
- **WHEN** the user invokes duplicate checking with no eligible products selected
- **THEN** FusionCanvas explains that products must be selected first
- **AND** makes no remote detail or artwork download request

### Requirement: A possible duplicate can be imported as new or linked to an existing Item
For a selected product with one or more possible duplicates, FusionCanvas SHALL let the user choose to import it as a new Item or connect it to a selected active Item in the same Store. Connecting SHALL preserve the existing Item's title, description, stage, status, and topic placement while attaching the Printify product identity, imported listing data, and original print-area artwork. A local Item that already has a Printify product mapping SHALL be shown as a non-linkable duplicate candidate. FusionCanvas SHALL preserve the one-Printify-product-per-Item-per-Store mapping invariant.

#### Scenario: Connect a Printify product to an unlinked existing Item
- **WHEN** the user chooses a possible duplicate Item that has no Printify mapping in the Store
- **THEN** FusionCanvas adds the selected Printify product mapping and imported listing data to that Item
- **AND** preserves its local title, description, stage, status, and topic placement
- **AND** attaches the imported original print-area artwork to the existing Item

#### Scenario: A possible duplicate is already linked
- **WHEN** a possible duplicate Item already maps to a Printify product
- **THEN** FusionCanvas identifies that candidate as non-linkable
- **AND** the user may still choose to import the selected product as a new Item

#### Scenario: The remote product is already linked
- **WHEN** a selected product is already mapped to an Item in the selected Store
- **THEN** FusionCanvas prevents a second import regardless of duplicate-check results

### Requirement: New imported products become linked Listing-stage Items
When the user confirms import as a new Item, FusionCanvas SHALL create an Item in the selected Store and Niche, place it in a new group named `Printify yyyy-MM-dd` (using the local date), and make the group name unique if it already exists. The Item SHALL start at the Listing stage, with status Published when the Printify product is visible and Draft when it is hidden. For a new Item, FusionCanvas SHALL initialize the local title and description from the Printify product. It SHALL preserve the remote product identity, listing snapshot (including visibility, Blueprint/Provider identity, variants/prices, and print-area relationships), and imported artwork provenance in the external listing mapping and SHALL save every original print-area artwork image as a managed local Asset linked to the Item. Initial listing import SHALL NOT create or update Store catalog records or Design-slot assignments.

#### Scenario: Import a visible product as a new Item
- **WHEN** the user confirms a visible, unlinked Printify product as a new Item
- **THEN** FusionCanvas creates a Listing-stage, Published Item with the Printify title and description
- **AND** saves its external mapping and original print-area artwork
- **AND** places the Item in the dated Printify group under the invoking Niche

#### Scenario: Import a hidden product as a new Item
- **WHEN** the user confirms a hidden, unlinked Printify product as a new Item
- **THEN** FusionCanvas creates a Listing-stage, Draft Item with the Printify title and description
- **AND** saves its external mapping, original print-area artwork, and dated group placement

#### Scenario: The dated group name already exists
- **WHEN** the chosen `Printify yyyy-MM-dd` group name already exists in the selected Niche
- **THEN** FusionCanvas creates a new unique group name for this import run
- **AND** places all newly created Items from that run in that group

#### Scenario: Initial import defers variant setup
- **WHEN** the user imports a Printify listing whose Blueprint or Design-stage configuration is not present in the local Store
- **THEN** FusionCanvas preserves the remote listing snapshot and artwork as Item-linked data
- **AND** does not create or modify Store catalog records or Design-slot assignments

### Requirement: Variant setup can be downloaded from the Listing stage
FusionCanvas SHALL offer an explicit Listing-stage action for a mapped Printify product to download its variant setup when the user is ready to edit the listing. The action SHALL use the linked Printify product's Blueprint, provider, variants, options, and print areas to reuse or import the corresponding Store catalog configuration and initialize the Item's Listing configuration and Design variant setup. It SHALL preserve the Item's local title, description, stage, status, topic placement, Printify mapping, and original imported artwork. It SHALL be safe to repeat: an existing matching catalog and Design setup SHALL be reused or reconciled without creating duplicate catalog entities or duplicate Item variant rows.

#### Scenario: Download variant setup on demand
- **WHEN** the user invokes the variant-setup download action for a mapped Printify listing
- **THEN** FusionCanvas imports or reuses the corresponding Printify catalog structure for the Store
- **AND** associates the Item with the matching Offering and initializes its available colors, variant rows, and representable artwork assignments
- **AND** preserves the Item's local fields and remote listing mapping

#### Scenario: Variant setup was already downloaded
- **WHEN** the user invokes the action again and the listing's catalog and Design setup are already present
- **THEN** FusionCanvas reuses the existing matching setup
- **AND** does not create duplicate catalog records or duplicate Item rows

#### Scenario: Variant setup download fails
- **WHEN** Printify data cannot be retrieved or local setup cannot be saved
- **THEN** FusionCanvas reports the failure in the Listing stage
- **AND** does not leave a partially configured Item

#### Scenario: Reconstruct a single finished image per print area and variant
- **WHEN** downloaded Printify artwork has one finished image for a print area and variant
- **THEN** FusionCanvas assigns that image to the matching Design slot
- **AND** groups variants that share equivalent artwork where the local Design row model allows it

#### Scenario: Artwork uses unsupported layers or text
- **WHEN** downloaded Printify artwork uses multiple image layers or text elements instead of one finished image
- **THEN** FusionCanvas preserves the original artwork as Item-linked assets
- **AND** reports which Design assignments could not be reconstructed
- **AND** leaves those assignments empty without discarding other successfully reconstructed setup

#### Scenario: Use FusionCanvas placement for imported artwork
- **WHEN** the user downloads variant setup for a Printify listing with a finished image
- **THEN** FusionCanvas uses its standard placement calculation for the imported image
- **AND** ignores any Printify-specific position, scale, or rotation values
- **AND** a later FusionCanvas upload may reset placement adjustments that exist only in Printify

### Requirement: Product imports have per-product outcomes and can be retried safely
FusionCanvas SHALL report success or a user-actionable failure for each confirmed product. Each product import SHALL commit its Item or Item link, mapping, and completed artwork assets as one logical operation; a failed product SHALL not leave a partially imported Item or mapping. Other successful products in the same run SHALL remain imported. A later retry SHALL recognize successful products by stable Printify identity and SHALL allow products that failed previously to be imported.

#### Scenario: Some products fail during import
- **WHEN** the user confirms multiple products and one or more products fail to retrieve or persist
- **THEN** FusionCanvas reports the outcome and available error guidance for each product
- **AND** retains successfully imported products
- **AND** leaves failed products eligible for a later retry

#### Scenario: Retry after a partial import
- **WHEN** the user later opens the Printify import dialog after a partial import
- **THEN** products that succeeded previously appear as linked and cannot be imported again
- **AND** products that failed previously remain available for selection and retry

#### Scenario: Cancel before confirming import
- **WHEN** the user closes the preview or duplicate-review dialog before confirming import
- **THEN** FusionCanvas performs no local Item, mapping, group, or artwork changes

### Requirement: Printify import is read-only toward the remote shop
The import flow SHALL retrieve Printify product data and artwork only. It SHALL NOT create, update, publish, unpublish, or delete a Printify product. Duplicate detection SHALL use text fields only in this module and SHALL NOT perform image-based comparison.

#### Scenario: Import selected products
- **WHEN** the user confirms import or connection for selected Printify products
- **THEN** FusionCanvas makes read requests only to Printify
- **AND** does not change remote product or publication state
