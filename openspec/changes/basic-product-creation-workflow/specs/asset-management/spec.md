## ADDED Requirements

### Requirement: Item-linked exported PNGs act as basic Design files
FusionCanvas SHALL present Item-linked `ExportedImage` assets with `.png` managed-file extensions as Design files without adding a Design entity, ordering, roles, or version history.

#### Scenario: User imports Design files
- **WHEN** Design is current, the Item is editable, and the user imports one or more readable PNG files
- **THEN** each source creates an independent managed copy, Asset record, and Item link
- **AND** every Design file appears in the Design Stage Tool after reload

#### Scenario: User selects a non-PNG file
- **WHEN** the Design import surface receives a file whose extension is not `.png`
- **THEN** FusionCanvas rejects it before copy or persistence
- **AND** reports that basic Design files must be PNG

#### Scenario: Same source is imported twice
- **WHEN** the user imports the same PNG twice
- **THEN** FusionCanvas creates two independent Design files

### Requirement: Design files support preview, export, missing state, and confirmed removal
The Design Stage Tool SHALL allow in-app preview, Export copy, missing-state review, and confirmed permanent removal through managed file and asset boundaries.

#### Scenario: User previews a Design file
- **WHEN** the managed PNG exists and the user invokes View
- **THEN** FusionCanvas displays an in-app preview of the authoritative managed copy
- **AND** does not require or launch an external application

#### Scenario: User exports a Design file
- **WHEN** the user chooses Export copy and a valid destination
- **THEN** FusionCanvas copies identical bytes from the managed source to that destination
- **AND** does not change the managed source, Asset, or Item link

#### Scenario: Managed file is missing
- **WHEN** the Design Asset record exists but its managed file is absent
- **THEN** the Design tool shows a missing state
- **AND** disables preview and export with actionable explanation
- **AND** keeps confirmed record removal available

#### Scenario: User removes a Design file
- **WHEN** the user confirms removal and persistence succeeds
- **THEN** FusionCanvas atomically removes the Asset and Item link
- **AND** deletes the managed file on a best-effort basis after the save

#### Scenario: Removal persistence fails
- **WHEN** confirmed Design-file removal cannot be persisted
- **THEN** the Asset, Item link, and managed file remain
- **AND** a recoverable error preserves selection and retry context

### Requirement: Design files are not duplicated as generic related assets
FusionCanvas SHALL exclude Design files from the shared Related assets section while keeping other Item-linked assets visible there.

#### Scenario: Item has Design and reference assets
- **WHEN** the Item surface loads
- **THEN** Design PNGs appear only through the Design Stage Tool
- **AND** non-Design related assets remain in the shared Related assets section

