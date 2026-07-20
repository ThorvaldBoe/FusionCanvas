## MODIFIED Requirements

### Requirement: Core model avoids premature advanced entities
The Phase 0 core model SHALL avoid introducing advanced entities before their workflows are specified, except for the Phase 2 Concept entity introduced by the concept-versions capability.

#### Scenario: Contributor inspects Phase 0 implementation scope
- **WHEN** a contributor reviews the FC-0002 implementation
- **THEN** it does not implement complete marketplace products, performance data, design versioning, mockup records, plugin data records, custom workflow models, or persistence mappings
- **AND** concept versioning is accepted as a Phase 2 concern through the concept-versions capability
