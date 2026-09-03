## Purpose

Refines the source-image editor's metadata filtering and table presentation.

## MODIFIED Requirements

### Requirement: Metadata sections are option-kind accurate
The Color section SHALL contain only active values belonging to Color options. Size and all other option values SHALL appear only in the secondary options section.

#### Scenario: Creator configures metadata
- **WHEN** the editor displays source-image metadata choices
- **THEN** Color contains no Size values and the secondary section contains Size and other values

### Requirement: Source images are presented as a selectable table
The image list SHALL show bold column headings, a distinct header background, grid-aligned columns, alternating row backgrounds, and a clearly highlighted selected row.

#### Scenario: Creator selects a source row
- **WHEN** the creator selects a row in the image table
- **THEN** the row is highlighted and the lower editor shows that row's metadata
