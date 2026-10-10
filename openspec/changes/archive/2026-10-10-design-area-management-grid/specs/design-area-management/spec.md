## ADDED Requirements

### Requirement: Design Area management presents a compact aligned grid
FusionCanvas SHALL present the active Design Area collection for the current Blueprint Offering as aligned rows with Name, Placement, Maximum size, Compatibility, and Actions columns. Each row SHALL retain the compatibility summary and visibly identify whether that Design Area is primary for artwork generation. The collection SHALL retain its empty state and Add Design Area action. Long values SHALL remain available in full through accessible names or help text and pointer tooltips.

#### Scenario: User scans configured Design Areas
- **WHEN** the user opens Manage Design Areas for an Offering with active Design Areas
- **THEN** each active Design Area is presented as one aligned row under the Name, Placement, Maximum size, Compatibility, and Actions headings
- **AND** the row summarizes its placement, maximum pixel dimensions, and Variant compatibility
- **AND** any primary-for-artwork-generation status is visible in the row
- **AND** full values remain accessible when their compact cell presentation is truncated

#### Scenario: User opens the empty Design Area collection
- **WHEN** the user opens Manage Design Areas for an Offering with no active Design Areas
- **THEN** FusionCanvas shows the existing empty-state message
- **AND** Add Design Area remains available when the Store is editable

#### Scenario: User edits or archives a row
- **WHEN** the user invokes Edit or Archive in a Design Area row
- **THEN** the action targets that row's Design Area
- **AND** Edit opens the existing guarded modal editor and returns focus to that row's Edit action when the dialog closes
- **AND** Archive retains the existing confirmation, dependency safeguards, and focus restoration behavior

#### Scenario: User reviews Design Areas at minimum window width
- **WHEN** the Store Editor is at its minimum supported width
- **THEN** the grid allows horizontal scrolling to reach every column and row action without clipping cell content
- **AND** the collection heading and Add Design Area action remain visible
