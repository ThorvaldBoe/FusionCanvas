## ADDED Requirements

### Requirement: Printify catalog selection uses accessible virtualized rows
The Store Editor SHALL present all loaded Printify shop products in a virtualized grid with fixed-height rows and aligned columns for selection, Blueprint name, shop-product title, and product/provider identity. Selection SHALL be made through a keyboard-accessible checkbox in each row and SHALL remain attached to the stable shop-product identity when rows are recycled. Each selection checkbox's accessible name SHALL include the full Blueprint name and shop-product title. Long visible text MAY be trimmed only when the complete value remains available through a tooltip and the selection's accessible name. The grid SHALL not add search, sorting, or paging, and SHALL not change full-shop retrieval, explicit confirmation, Store scoping, draft preservation, or import idempotency.

#### Scenario: Many products are displayed in a virtualized grid
- **WHEN** Printify shop-product retrieval completes with a large result set
- **THEN** all products are available in the selection grid with aligned Blueprint, shop-product title, and product/provider identity columns
- **AND** only the rows needed for the current viewport are realized
- **AND** the grid does not provide search, sorting, or paging

#### Scenario: Selection survives row recycling
- **WHEN** the user selects multiple products and scrolls until their rows are recycled out of view
- **THEN** the selections remain associated with the same stable shop-product identities
- **AND** confirming import submits exactly the selected product IDs

#### Scenario: Long product text remains available
- **WHEN** a Blueprint name or shop-product title is too long to fit in a fixed-height row
- **THEN** the visible text may be trimmed with an ellipsis
- **AND** the complete value is available from its tooltip and the checkbox accessible name
