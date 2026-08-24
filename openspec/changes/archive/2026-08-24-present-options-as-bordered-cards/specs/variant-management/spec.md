## ADDED Requirements

### Requirement: Available options render as bordered choice cards
FusionCanvas SHALL present each available Option in the Available choices region as a distinct compact bordered card, SHALL show the Option name, semantic kind label, current value summary, and the Option's manage and archive actions inside the same boundary, and SHALL use shared semantic theme resources so the boundary remains visible in both Light and Dark appearance.

#### Scenario: User scans available choices as cards
- **WHEN** Variant management has multiple available Options
- **THEN** FusionCanvas encloses each Option in its own bordered card
- **AND** places the Option name, kind label, value summary, and its actions inside the same boundary
- **AND** applies consistent padding, corner radius, and spacing across the cards

#### Scenario: Empty Option uses the same card treatment
- **WHEN** an available Option has no configured values
- **THEN** FusionCanvas renders the empty Option as a choice card with the same boundary
- **AND** shows a truthful summary that no values are configured

#### Scenario: Custom Option kind uses the same card treatment
- **WHEN** an available Option is neither Color nor Size
- **THEN** FusionCanvas renders it as a choice card with the same boundary
- **AND** labels the card by its custom Option kind

### Requirement: Choice cards align and respond to available width without clipping
FusionCanvas SHALL align available-option cards cleanly in the available width, SHALL wrap or stack them gracefully at narrower supported widths, and SHALL wrap long Option names and value summaries so card layout does not clip content.

#### Scenario: Cards align cleanly in the available width
- **WHEN** multiple available Option cards fit within the available width
- **THEN** they sit side by side aligned on the same row with consistent spacing

#### Scenario: Cards stack at narrower supported widths
- **WHEN** the window narrows toward its minimum supported width
- **THEN** the cards wrap onto new rows instead of overflowing or being clipped

#### Scenario: Long content does not clip
- **WHEN** an available Option name or value summary is longer than the card width
- **THEN** the text wraps within the card and remains readable