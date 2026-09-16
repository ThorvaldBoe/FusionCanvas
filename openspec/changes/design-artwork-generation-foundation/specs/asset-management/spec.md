## MODIFIED Requirements

### Requirement: Design files support preview, export, missing state, and confirmed removal
The Design Stage Tool SHALL allow in-app preview, Export copy, missing-state review, and confirmed permanent removal through managed file and asset boundaries. For generated artwork, removing the image from a Design Area slot SHALL unassign it without deleting its Item-linked Asset or managed PNG; permanent deletion SHALL be unavailable while any slot references that generated asset.

#### Scenario: User previews a Design file
- **WHEN** the managed PNG exists and the user invokes View
- **THEN** FusionCanvas displays an in-app preview of the authoritative managed copy
- **AND** does not require or launch an external application

#### Scenario: User exports a Design file
- **WHEN** the user chooses Export copy and a valid destination
- **THEN** FusionCanvas copies identical bytes from the managed source to that destination
- **AND** does not change the managed source, Asset, Item link, slot assignment, or provenance

#### Scenario: Managed file is missing
- **WHEN** the Design Asset record exists but its managed file is absent
- **THEN** the Design tool shows a missing state
- **AND** disables preview and export with actionable explanation
- **AND** keeps applicable unassignment or confirmed record-removal actions available

#### Scenario: User removes a manual Design file
- **WHEN** the user confirms removal of a non-generated Design file and persistence succeeds
- **THEN** FusionCanvas atomically removes the Asset, Item link, and any applicable slot assignment
- **AND** deletes the managed file on a best-effort basis after the save

#### Scenario: User removes generated artwork from a slot
- **WHEN** the user activates Remove from slot for generated artwork
- **THEN** FusionCanvas clears only that slot assignment
- **AND** keeps the generated Asset, Item link, managed PNG, provenance, warnings, and Supporting Images entry

#### Scenario: User tries to delete assigned generated artwork
- **WHEN** a generated asset remains assigned to one or more Design Area slots
- **THEN** permanent deletion is unavailable
- **AND** guidance directs the user to remove it from every slot first

#### Scenario: User deletes unassigned generated artwork
- **WHEN** the generated asset is unassigned from every slot and the user confirms Delete generated image
- **THEN** FusionCanvas atomically removes the Asset and Item link
- **AND** deletes the managed PNG on a best-effort basis after the save

#### Scenario: Removal persistence fails
- **WHEN** confirmed Design-file removal, generated unassignment, or generated deletion cannot be persisted
- **THEN** the Asset, Item link, slot assignments, provenance, and managed file remain in their previous confirmed state
- **AND** a recoverable error preserves selection and retry context

## ADDED Requirements

### Requirement: Generated artwork remains visible as reviewable history
FusionCanvas SHALL distinguish generated artwork from manually imported Design files through versioned provenance and SHALL show every Item-linked generated asset in Supporting Images, including the asset currently occupying a slot and generated assets later replaced or unassigned. Generated-history ordering SHALL be deterministic with newest generation first.

#### Scenario: User reviews generated history
- **WHEN** one or more generated artwork assets are linked to the Item
- **THEN** Supporting Images lists every generated result newest first
- **AND** identifies its intended Design Area, model, final dimensions, and persistent warnings

#### Scenario: Generated result remains assigned
- **WHEN** a generated asset is both Item-linked and assigned to a slot
- **THEN** it appears in the slot and in Supporting Images as one shared managed asset
- **AND** the UI does not create a duplicate managed file or Asset record

#### Scenario: Generated result is replaced
- **WHEN** a later generated result or manual PNG replaces generated slot artwork
- **THEN** the earlier generated asset remains in Supporting Images with unchanged provenance and managed file
