## ADDED Requirements

### Requirement: Item tabs use universal terminology and stable fallback titles
The tabbed document window SHALL treat Item as the universal item context and SHALL display the same derived fallback used by the tree when working title is empty.

#### Scenario: Empty-title Item opens in a tab
- **WHEN** the Item opens or replaces the reusable working tab
- **THEN** the tab title is `Untitled item · <short ID>`
- **AND** the fallback is not persisted as Item content

#### Scenario: Item title is saved
- **WHEN** the working title changes successfully
- **THEN** every open tab for that Item refreshes to the authoritative title

