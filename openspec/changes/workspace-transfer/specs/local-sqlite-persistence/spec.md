# Local SQLite Persistence (delta)

## MODIFIED Requirements

### Requirement: Phase 0 persistence avoids advanced storage scope
The Phase 0 SQLite persistence capability SHALL avoid storage behavior that belongs to later workflow or platform changes. Single-workspace import/export packages are no longer excluded: they are provided by the workspace-transfer capability, which reuses this persistence layer rather than extending it.

#### Scenario: Contributor reviews Phase 0 persistence scope
- **WHEN** a contributor reviews the FC-0003 implementation
- **THEN** it does not implement cloud sync, multi-user collaboration, encryption, full backup/restore, marketplace synchronization, AI provider history, plugin data stores, or advanced search optimization
- **AND** single-workspace import/export packages are understood to belong to the workspace-transfer capability, not to the persistence layer itself
