## MODIFIED Requirements

### Requirement: Listing management creates product concepts in active topics
FusionCanvas SHALL provide Item management that creates a stage-agnostic Item beneath an active niche or group without requiring a title, assets, mockups, marketplace metadata, or stage content.

#### Scenario: User creates an empty Item from a selected topic
- **WHEN** the user invokes New Item while an active niche or group is canonically selected
- **THEN** FusionCanvas creates a new Item with stable identity beneath that topic
- **AND** the Item belongs to the topic's store
- **AND** starts at Idea with Draft status
- **AND** may have an empty working title and no attached assets

#### Scenario: Selected Item supplies its containing topic
- **WHEN** an active Item is selected and the user starts another Item
- **THEN** FusionCanvas uses the selected Item's containing group, or its niche when ungrouped, as the destination

#### Scenario: Store context uses the default niche
- **WHEN** an active store has no applicable topic or Item selection and has a valid active default niche
- **THEN** FusionCanvas creates the Item beneath that default niche
- **AND** does not create a store-level Item

#### Scenario: Store has no resolvable topic
- **WHEN** the active store has no active niche, or has multiple active niches with no applicable selection and no valid default niche
- **THEN** FusionCanvas blocks Item creation with an actionable setup path
- **AND** creates no Item

#### Scenario: User supplies optional creation details
- **WHEN** the user creates an Item with an optional working title, description-compatible legacy input, Notes, Tags, or inherited context
- **THEN** FusionCanvas persists the supplied optional values
- **AND** omitted values do not block creation

#### Scenario: Generated identity is invalid
- **WHEN** Item creation cannot obtain a non-empty unique ID
- **THEN** FusionCanvas rejects creation
- **AND** leaves persisted workspace state unchanged

## ADDED Requirements

### Requirement: Item management preserves fallback labels without persisting them
Item management SHALL expose one stable fallback display label for an empty working title and SHALL keep that label derived from identity.

#### Scenario: Empty Item appears across surfaces
- **WHEN** an empty-title Item is selected or opened
- **THEN** its tree row, tab, and Overview use the same `Untitled item · <short ID>` label
- **AND** saving unrelated data does not write the fallback into the Item name

