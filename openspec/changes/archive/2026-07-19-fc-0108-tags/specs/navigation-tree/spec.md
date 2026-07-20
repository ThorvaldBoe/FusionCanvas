## ADDED Requirements

### Requirement: Navigation tree filters listings by tag
FusionCanvas SHALL provide a tag filter control above the workspace tree that filters listings to those that have all selected active tags, preserves ancestor topic paths for matching listings, and keeps stable entity selection across filter changes.

#### Scenario: User filters by one tag
- **WHEN** the user selects one active tag chip in the tree tag filter control
- **THEN** the visible tree shows only listings that have that tag
- **AND** preserves the store and topic ancestor path needed to locate each matching listing
- **AND** hides listings without that tag

#### Scenario: User filters by multiple tags with AND semantics
- **WHEN** the user selects multiple active tag chips in the tree tag filter control
- **THEN** the visible tree shows only listings that have every selected tag
- **AND** listings missing any selected tag are hidden

#### Scenario: User clears the tag filter
- **WHEN** the user clears all selected tag chips or closes the filter control
- **THEN** the tree restores its pre-filter expansion state
- **AND** canonical selection is preserved

#### Scenario: Canonical selection is filtered out
- **WHEN** the canonical selection is a listing that does not match the active tag filter
- **THEN** the inspector retains the canonical selection with a clear filtered-out indicator
- **AND** offers a reveal or clear-filter action
- **AND** structural commands continue to operate on canonical identity

#### Scenario: Tag filter excludes archived tags
- **WHEN** the tree tag filter control is open
- **THEN** only active tags of the active store are offered as selectable chips
- **AND** archived tags are excluded

#### Scenario: Tag filter is keyboard reachable
- **WHEN** the tree tag filter control is focused
- **THEN** tag chips can be toggled with keyboard input
- **AND** the filter can be cleared without pointer-only interaction
