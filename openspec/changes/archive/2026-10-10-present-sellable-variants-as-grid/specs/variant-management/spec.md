## ADDED Requirements

### Requirement: Sellable Variants use an aligned virtual grid
The Sellable Variants region SHALL present active explicit Variant rows in a fixed-height virtual grid with aligned Name, Color, Size, Other, and Action columns. It SHALL preserve the existing active-Variant order and each row's resolved values from stable Option kinds, including the existing truthful unavailable-value label. It SHALL keep archived Variants hidden. Long cell values SHALL remain available in full through an accessible help text or pointer tooltip without making rows variable-height. The grid SHALL preserve the existing direct per-row Archive command, including row identity, stale-target handling, dependency blocking, recoverable errors, and list/count refresh after a successful archive. It SHALL NOT add search, sorting, paging, inline editing, or a new archive confirmation step.

#### Scenario: Creator scans sellable Variants
- **WHEN** the Offering has active confirmed sellable Variants
- **THEN** each Variant appears in the virtual grid with aligned Name, Color, Size, Other, and Action columns
- **AND** each semantic value remains associated with its stable Option kind
- **AND** the grid keeps the existing active-Variant order and does not show archived Variants

#### Scenario: A cell value exceeds its visible width
- **WHEN** a Variant name or semantic value is longer than its fixed-height grid cell can display
- **THEN** the cell truncates the visible text without overlapping adjacent columns
- **AND** the complete value remains available through accessible help text or a pointer tooltip
- **AND** the row height remains fixed

#### Scenario: Creator archives a sellable Variant
- **WHEN** the creator activates Archive for a Variant row
- **THEN** the existing archive command receives that row's Variant identity
- **AND** a stale target or dependent record is reported through the existing recoverable error behavior
- **AND** a successful archive removes the active row and refreshes the count
- **AND** the action remains available by pointer and keyboard
