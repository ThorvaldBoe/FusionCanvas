# Asset Management

## Purpose

Reconciles the accepted Design-file behavior with the Design Stage Implementation: Design files are now slot-bound final images (managed PNG in a row × design-area cell) or Supporting images (broader supported set, independent of design areas), rather than a single flat item-linked list.

## RENAMED Requirements

- FROM: `### Requirement: Item-linked exported PNGs act as basic Design files`
  TO: `### Requirement: Design slot images and supporting images act as Design assets`
- FROM: `### Requirement: Design files support preview, export, missing state, and confirmed removal`
  TO: `### Requirement: Design slot and supporting images support preview, export, missing state, and confirmed removal`

## MODIFIED Requirements

### Requirement: Design slot images and supporting images act as Design assets
FusionCanvas SHALL present Item-linked managed images in the Design Stage Tool as either final design slot images (PNG, one per row × design-area cell) or Supporting images (the supported creative image set, not bound to a design area), without adding a Design entity, ordering, roles, or version history.

#### Scenario: User fills a final design slot
- **WHEN** Design is current, the Item is editable, and the user fills a slot with a readable PNG file
- **THEN** the source creates an independent managed copy, Asset record, Item link, and slot binding
- **AND** the slot shows the PNG thumbnail and the cell holds that final image

#### Scenario: User selects a non-PNG file for a final design slot
- **WHEN** the final-design slot import surface receives a file whose extension is not `.png`
- **THEN** FusionCanvas rejects it before copy or persistence
- **AND** reports that final design slot images must be PNG

#### Scenario: User imports a supporting image
- **WHEN** Design is current, the Item is editable, and the user imports a supported image as supporting
- **THEN** the source creates an independent managed copy and Asset record linked to the Item
- **AND** it appears in the Supporting images area independent of any design area

#### Scenario: Same slot source is imported twice
- **WHEN** the user fills the same slot with the same source twice
- **THEN** the second fill replaces the first slot binding
- **AND** the replaced managed file and record are handled according to asset removal rules

### Requirement: Design slot and supporting images support preview, export, missing state, and confirmed removal
The Design Stage Tool SHALL allow in-app preview, Export copy, missing-state review, and confirmed permanent removal for both slot-bound final images and Supporting images through managed file and asset boundaries.

#### Scenario: User previews a slot or supporting image
- **WHEN** the managed image exists and the user invokes View
- **THEN** FusionCanvas displays an in-app preview of the authoritative managed copy
- **AND** does not require or launch an external application

#### Scenario: User exports a slot or supporting image
- **WHEN** the user chooses Export copy and a valid destination
- **THEN** FusionCanvas copies identical bytes from the managed source to that destination
- **AND** does not change the managed source, Asset, Item link, or slot binding

#### Scenario: Managed file is missing
- **WHEN** the asset record exists but its managed file is absent
- **THEN** the affected slot or Supporting image shows a missing state
- **AND** disables preview and export with actionable explanation
- **AND** keeps confirmed record removal available

#### Scenario: User removes a slot or supporting image
- **WHEN** the user confirms removal and persistence succeeds
- **THEN** FusionCanvas atomically removes the Asset and Item link (and slot binding where applicable)
- **AND** deletes the managed file on a best-effort basis after the save

#### Scenario: Removal persistence fails
- **WHEN** confirmed removal cannot be persisted
- **THEN** the Asset, Item link, slot binding, and managed file remain
- **AND** a recoverable error preserves selection and retry context
