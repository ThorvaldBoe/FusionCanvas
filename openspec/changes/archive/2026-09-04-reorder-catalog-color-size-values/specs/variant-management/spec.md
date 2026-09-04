## ADDED Requirements

### Requirement: Option Values support explicit persisted ordering
FusionCanvas SHALL maintain an explicit integer order for active Option Values within each Blueprint Offering Option. The order SHALL be persisted with the existing Option Value identity, SHALL be used wherever those values are presented or selected, and SHALL be normalized to contiguous zero-based positions after a successful add, reorder, archive, or restore. Reordering SHALL not recreate values or change any relationship that references their identities.

#### Scenario: User reorders a Color value by its visible handle
- **WHEN** the user drags an active Color value by its dedicated reorder handle to a new position
- **THEN** the Color values are displayed in the requested order
- **AND** the existing value records and their identities remain unchanged

#### Scenario: User reorders a Size value by its visible handle
- **WHEN** the user drags an active Size value by its dedicated reorder handle to a new position
- **THEN** the Size values are displayed in the requested order
- **AND** the existing value records and their identities remain unchanged

#### Scenario: Reorder is persisted across dialog and application sessions
- **WHEN** a user reorders values, closes and reopens the management dialog, and restarts the application
- **THEN** the same order is loaded and displayed for the affected Option

#### Scenario: Ordered values are used by consumers
- **WHEN** a consumer presents or selects Color or Size choices for an Offering
- **THEN** it uses the persisted active-value order rather than insertion order, database row order, or alphabetical order

#### Scenario: New values receive a deterministic position
- **WHEN** a user adds a new active value to an Option
- **THEN** the new value is placed after the existing active values
- **AND** active positions remain contiguous and deterministic

#### Scenario: Existing data receives a stable backfill
- **WHEN** an existing workspace is opened after the order field or ordering behavior is introduced
- **THEN** active values retain their apparent pre-migration order, with stable identity used to break ties
- **AND** no value identity, reference, or link changes

#### Scenario: Archived values do not disturb active ordering
- **WHEN** a value is archived or restored
- **THEN** active values are renumbered contiguously in their current relative order
- **AND** the archived or restored value keeps its stable identity and existing relationships

#### Scenario: Reorder actions are accessible
- **WHEN** keyboard or assistive-technology users reach a Color or Size value row
- **THEN** the dedicated handle exposes a target-specific accessible name and an equivalent move-up/move-down action is available without pointer-only interaction
- **AND** focus order follows the visible value order

#### Scenario: Invalid reorder leaves confirmed values unchanged
- **WHEN** a reorder request targets a different Option, an archived value, an out-of-range position, or stale context
- **THEN** the request is rejected with recoverable guidance
- **AND** the confirmed value order and relationships remain unchanged
