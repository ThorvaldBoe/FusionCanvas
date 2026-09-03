## ADDED Requirements

### Requirement: Final artwork slots clearly teach drag and browse interactions
The Design stage SHALL present each applicable final-artwork slot as a clearly identified drop target when it is empty, and SHALL provide a visible Browse/Upload action for every editable slot.

#### Scenario: Empty editable slot explains the primary interaction
- **WHEN** an editable Design-stage slot has no assigned final artwork
- **THEN** the slot identifies itself as a final design artwork target
- **AND** the empty state visibly explains that the user can drag and drop PNG artwork there
- **AND** the slot provides a visible Browse/Upload action

#### Scenario: Populated editable slot keeps replacement available
- **WHEN** an editable Design-stage slot already has final artwork
- **THEN** the slot keeps a visible Browse/Upload or Replace artwork action
- **AND** activating that action targets only the selected slot

#### Scenario: Protected slot remains informative but not editable
- **WHEN** the Design stage is read-only or an earlier stage is being reviewed
- **THEN** persisted final artwork and its preview state remain visible
- **AND** browse, drop, and remove actions are disabled

### Requirement: Final artwork assignment gives immediate visual feedback
The Design stage SHALL assign a valid PNG dropped onto or browsed for a slot through the existing managed-file boundary and SHALL show the resulting thumbnail or preview in that same slot after success.

#### Scenario: User drops valid artwork
- **WHEN** the user drops a readable PNG onto an editable final-artwork slot
- **THEN** the PNG is assigned to that row and design-area slot
- **AND** the slot immediately displays the managed artwork thumbnail

#### Scenario: User browses for valid artwork
- **WHEN** the user activates Browse/Upload for an editable final-artwork slot and selects a readable PNG
- **THEN** the PNG is assigned to that row and design-area slot
- **AND** the slot immediately displays the managed artwork thumbnail

#### Scenario: Invalid artwork is rejected recoverably
- **WHEN** a dropped or browsed file is not a PNG, cannot be read, or cannot be imported
- **THEN** the assignment is rejected before successful persistence
- **AND** the slot's prior artwork and persisted state remain unchanged
- **AND** the Design stage shows an actionable error message without closing or losing the current context

### Requirement: Final artwork supports independent preview and file actions
The Design stage SHALL provide discoverable enlarge, download, and remove actions for every assigned final artwork, while preserving replacement behavior.

#### Scenario: User enlarges assigned artwork
- **WHEN** the user activates the slot's enlarge or magnifier action for an available assigned artwork
- **THEN** the Design preview opens in-app and shows the selected artwork at a substantially larger size

#### Scenario: User downloads assigned artwork
- **WHEN** the user activates download for an available assigned artwork and chooses a valid destination
- **THEN** an independent copy of that artwork is written to the destination
- **AND** the managed assignment remains unchanged

#### Scenario: User removes assigned artwork
- **WHEN** the user confirms removal of an assigned artwork
- **THEN** only that row and design-area assignment is cleared
- **AND** other final artworks remain assigned
- **AND** the slot returns to its empty drop-and-browse state

### Requirement: Multiple final artworks remain independent and durable
The Design stage SHALL support final artwork assigned across multiple applicable rows and design-area slots without overwriting unrelated assignments, and SHALL restore those assignments after reload or revisiting the Design stage.

#### Scenario: User assigns multiple slot artworks
- **WHEN** the user assigns valid artwork to two or more applicable slots
- **THEN** each selected slot displays its own artwork
- **AND** assigning or replacing one slot does not change unrelated slot assignments

#### Scenario: User revisits Design after persistence
- **WHEN** successful final-artwork assignments are followed by reload or navigation away and back
- **THEN** the same slot assignments, thumbnails, preview availability, download availability, and removal availability are restored

### Requirement: Final artwork is distinct from other image workflows
The Design stage SHALL identify final artwork controls as belonging to Design-stage slots and SHALL not mix them with Mockup Template source-image controls or Supporting Images.

#### Scenario: User distinguishes image categories
- **WHEN** the Design stage displays final artwork slots and Supporting Images
- **THEN** final artwork is labelled in the slot grid as Design artwork
- **AND** Supporting Images retain their separate heading and import action
- **AND** no Mockup Template source-image control is presented as a final artwork control
