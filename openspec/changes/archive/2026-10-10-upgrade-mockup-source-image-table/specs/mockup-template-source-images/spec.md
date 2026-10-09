## ADDED Requirements

### Requirement: Source-image table sorting and preview warnings remain accessible
The focused Mockup Template editor SHALL let creators sort active source-image rows by File, Applicability, and Status, and SHALL expose preview-read failures in the compact table without hiding their explanation from assistive technology or pointer users. Sorting SHALL preserve the selected rows and active row used by the selected-image editor. The Action column SHALL remain an explicit per-row archive action and SHALL NOT be treated as a sort key.

#### Scenario: Creator sorts source-image rows
- **WHEN** the creator activates the File, Applicability, or Status column heading
- **THEN** the source-image rows are ordered by that visible value
- **AND** activating the same heading again reverses its sort direction
- **AND** the heading exposes the current sort column and direction accessibly
- **AND** the selected rows and active detail-editor row remain selected

#### Scenario: Source image preview cannot be read
- **WHEN** a source-image draft has a preview-read warning
- **THEN** the compact source-image row visibly indicates the warning
- **AND** the complete warning text remains available through the warning's accessible name or help text and pointer tooltip
- **AND** the warning does not require a variable-height row
