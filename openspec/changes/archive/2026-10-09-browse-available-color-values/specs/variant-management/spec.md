## ADDED Requirements

### Requirement: Available Color values can be searched, paged, and sorted
FusionCanvas SHALL show the active Color Option Values for the selected Offering in a read-only, virtualized grid inside the existing Color choice card. The grid SHALL retain the Color values' saved `SortOrder` as its default order, SHALL offer case-insensitive search and explicit page-size choices of 10, 25, and 50, and SHALL allow sorting by Color name without changing the saved order. Grid rows SHALL alternate semantic surface fills in both Light and Dark appearance. Existing value editing SHALL remain available through **Manage values**.

#### Scenario: User scans configured Colors
- **WHEN** the user opens Variant management and the Offering has active Color values
- **THEN** each value appears in its own grid row inside the Color choice card
- **AND** the default view follows the saved Color value order
- **AND** alternating rows use theme-aware semantic surface fills
- **AND** browsing or sorting values does not change sellable Variants or persist a new Color order

#### Scenario: User searches Color values across all pages
- **WHEN** the user enters a search term in the Color grid
- **THEN** FusionCanvas filters the complete set of active Color values using a case-insensitive substring match on the displayed name
- **AND** shows only matching rows on the first page
- **AND** clearing the search restores all active Color values in the currently selected sort order

#### Scenario: User changes the Color page size
- **WHEN** the user chooses 10, 25, or 50 records per page
- **THEN** the grid shows at most that many matching values per page
- **AND** reports the visible range and total number of matching values
- **AND** enables Previous and Next only when those pages exist
- **AND** shows 10 records per page initially

#### Scenario: User sorts Color values
- **WHEN** the user chooses Name A–Z or Name Z–A in the Color grid sort control
- **THEN** FusionCanvas sorts all matching Color values case-insensitively before paging
- **AND** uses saved order and stable value identity to break equal-name ties
- **AND** resets to the first page
- **AND** leaves the persisted Color value order unchanged

#### Scenario: User returns to saved Color order
- **WHEN** the user chooses Configured order in the Color grid sort control
- **THEN** the current search results return to their saved `SortOrder` before paging
- **AND** the persisted Color value order remains unchanged

#### Scenario: Color grid has no rows to show
- **WHEN** the Offering has no active Color values or the search matches none
- **THEN** the Color card shows a clear message distinguishing an empty Color list from a search with no matches
- **AND** the page summary reports zero matching Colors
- **AND** Previous and Next are disabled

#### Scenario: User reaches controls and truncated names accessibly
- **WHEN** a keyboard or assistive-technology user interacts with Color grid browsing controls, or a Color name is too long for its row
- **THEN** search, page size, sort, and paging controls expose descriptive accessible names and remain keyboard operable
- **AND** the full Color name is available from the truncated row through its tooltip
- **AND** no grid row offers editing or selection

#### Scenario: User edits Colors through the existing action
- **WHEN** the user activates **Manage values** on the Color card
- **THEN** FusionCanvas opens the existing Option value management dialog for the same Color Option
- **AND** adding, editing, archiving, validation, persistence, and dialog lifecycle follow their existing requirements

## MODIFIED Requirements

### Requirement: Available options render as bordered choice cards
FusionCanvas SHALL present each available Option in the Available choices region as a distinct compact bordered card, SHALL show the Option name, semantic kind label, current value presentation, and the Option's manage and archive actions inside the same boundary, and SHALL use shared semantic theme resources so the boundary remains visible in both Light and Dark appearance. The Color card SHALL present active Color values using the paged Color grid; Size and custom Option cards SHALL retain their compact value summaries.

#### Scenario: User scans available choices as cards
- **WHEN** Variant management has multiple available Options
- **THEN** FusionCanvas encloses each Option in its own bordered card
- **AND** places the Option name, kind label, Color grid or other Option value summary, and its actions inside the same boundary
- **AND** applies consistent padding, corner radius, and spacing across the cards

#### Scenario: Empty Option uses the same card treatment
- **WHEN** an available Option has no configured values
- **THEN** FusionCanvas renders the empty Option as a choice card with the same boundary
- **AND** shows a truthful empty state within that card

#### Scenario: Custom Option kind uses the same card treatment
- **WHEN** an available Option is neither Color nor Size
- **THEN** FusionCanvas renders it as a choice card with the same boundary
- **AND** labels the card by its custom Option kind
- **AND** retains its compact value summary

### Requirement: Choice cards align and respond to available width without clipping
FusionCanvas SHALL align available-option cards cleanly in the available width, SHALL wrap or stack them gracefully at narrower supported widths, SHALL wrap long Option names and compact Size or custom Option value summaries so card layout does not clip content, and SHALL keep long Color values readable in the bounded Color grid through truncation with full-name tooltips.

#### Scenario: Cards align cleanly in the available width
- **WHEN** multiple available Option cards fit within the available width
- **THEN** they sit side by side aligned on the same row with consistent spacing

#### Scenario: Cards stack at narrower supported widths
- **WHEN** the window narrows toward its minimum supported width
- **THEN** the cards wrap onto new rows instead of overflowing or being clipped

#### Scenario: Long content remains readable
- **WHEN** an Option name or a compact Size/custom Option value summary exceeds its card width
- **THEN** the text wraps within the card and remains readable
- **AND** when a Color value exceeds its grid cell width, the visible text is truncated and the full name is available through its tooltip
