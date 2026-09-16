## ADDED Requirements

### Requirement: Store primary Design Area supplies the generation default
FusionCanvas SHALL allow a Store to persist one optional primary Design Area preference. When an Item's selected offering contains that primary Design Area, the Design-stage generation selector SHALL default to it. If the preference is absent, archived, belongs to another offering, or is not available for the active Item, the selector SHALL choose no target or the existing deterministic first available target without changing the Store preference.

#### Scenario: Primary target is available
- **WHEN** a Store has a primary Design Area and the active Item's selected offering contains it
- **THEN** Generate Artwork selects that Design Area by default
- **AND** the user can choose another available Design Area

#### Scenario: Primary target is unavailable
- **WHEN** the Store primary Design Area is archived, missing, belongs to another offering, or is not part of the active Item's offering
- **THEN** the generation selector does not select it
- **AND** the Store preference is preserved without repair or silent reassignment

#### Scenario: Store preference is changed
- **WHEN** the user saves a different active Design Area as the Store primary
- **THEN** later Design-stage generation sessions for eligible Items use the new preference by default
- **AND** an already explicit in-session target is not silently changed

### Requirement: Primary Design Area preference respects Store ownership and editability
FusionCanvas SHALL accept a primary Design Area only when it belongs to an active offering owned by the Store. Archived Stores and read-only catalog contexts SHALL expose the preference read-only and SHALL not persist changes.

#### Scenario: Cross-Store target is requested
- **WHEN** a primary Design Area request refers to a Design Area from another Store
- **THEN** the request is rejected
- **AND** the previous Store preference remains unchanged

