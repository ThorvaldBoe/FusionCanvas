## MODIFIED Requirements

### Requirement: Available options render as bordered choice cards
FusionCanvas SHALL present each available Option in the Available choices region as a distinct compact bordered card, SHALL show the Option name, semantic kind label, current value presentation, and the Option's manage and archive actions inside the same boundary, and SHALL use shared semantic theme resources so the boundary remains visible in both Light and Dark appearance. The Color card SHALL present active Color values using the paged Color grid. Size and custom Option cards SHALL present active values in a fixed-height, vertically scrollable, read-only virtual grid in each Option's saved `SortOrder`, and SHALL show a truthful empty message when there are no active values. The grids SHALL retain the existing **Manage values** and archive actions without exposing editing or selection in the summary.

#### Scenario: User scans available choices as cards
- **WHEN** Variant management has multiple available Options
- **THEN** FusionCanvas encloses each Option in its own bordered card
- **AND** places the Option name, kind label, Color grid or other Option value grid, and its actions inside the same boundary
- **AND** applies consistent padding, corner radius, and spacing across the cards

#### Scenario: Empty Option uses the same card treatment
- **WHEN** an available Option has no active configured values
- **THEN** FusionCanvas renders the empty Option as a choice card with the same boundary
- **AND** shows a truthful empty-state message within that card
- **AND** does not show an empty virtual grid

#### Scenario: Custom Option kind uses the same card treatment
- **WHEN** an available Option is neither Color nor Size
- **THEN** FusionCanvas renders it as a choice card with the same boundary
- **AND** labels the card by its custom Option kind
- **AND** presents its active values in the bounded read-only virtual grid

#### Scenario: User scans Size values in configured order
- **WHEN** an available Size Option has active values
- **THEN** its card shows the values in their saved `SortOrder`
- **AND** the grid offers no selection, editing, search, sorting, or paging controls
- **AND** the existing **Manage values** and archive actions remain available

#### Scenario: User scrolls through a long custom Option value list
- **WHEN** a custom Option has more active values than fit in its summary grid
- **THEN** the grid scrolls vertically within the bounded card
- **AND** only rows needed for the visible area are realized
- **AND** scrolling does not change the saved value order

### Requirement: Choice cards align and respond to available width without clipping
FusionCanvas SHALL align available-option cards cleanly in the available width, SHALL wrap or stack them gracefully at narrower supported widths, and SHALL keep card content within each boundary. Option names SHALL wrap. Size and custom Option cards SHALL use a compact fixed-height virtual grid so long value lists do not make cards unbounded; each row SHALL truncate a value that exceeds its cell and SHALL expose the full value through a tooltip and accessible automation text. The bounded Color grid SHALL continue to truncate long Color values and provide their full names through tooltips.

#### Scenario: Cards align cleanly in the available width
- **WHEN** multiple available Option cards fit within the available width
- **THEN** they sit side by side aligned on the same row with consistent spacing

#### Scenario: Cards stack at narrower supported widths
- **WHEN** the window narrows toward its minimum supported width
- **THEN** the cards wrap onto new rows instead of overflowing or being clipped

#### Scenario: Long content remains readable
- **WHEN** an Option name or a value exceeds the width available in its card or grid cell
- **THEN** the Option name wraps within the card and the grid value is visually truncated without clipping the card
- **AND** the full grid value is available through its tooltip and accessible automation text
- **AND** when a Color value exceeds its grid cell width, the visible text is truncated and the full name remains available through its tooltip
