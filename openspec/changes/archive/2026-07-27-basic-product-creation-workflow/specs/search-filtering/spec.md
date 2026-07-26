## ADDED Requirements

### Requirement: Item filters synchronize with workflow mutations
Stage, status, archive, Tag, and text filters SHALL evaluate Item state and SHALL refresh after authoritative Item mutations without leaving stale visible rows or document state.

#### Scenario: Status change leaves the active filter
- **WHEN** an Item changes status and no longer matches the active status filter
- **THEN** its row leaves the filtered tree
- **AND** document and canonical selection resolve consistently without overwriting the new status

#### Scenario: Empty Item is searched
- **WHEN** an Item has no working title
- **THEN** its derived fallback supports stable presentation
- **AND** the fallback is not treated as persisted creative text

