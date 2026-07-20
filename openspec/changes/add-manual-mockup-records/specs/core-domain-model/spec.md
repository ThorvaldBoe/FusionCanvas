## MODIFIED Requirements

### Requirement: Core model avoids premature advanced entities
The Phase 0 core model SHALL avoid introducing advanced entities before their workflows are specified, except for the Phase 2 Concept entity introduced by the concept-versions capability, the Phase 2 Design entity introduced by the design-records capability, and the Phase 2 Mockup entity introduced by the mockup-records capability.

#### Scenario: Contributor inspects Phase 0 implementation scope
- **WHEN** a contributor reviews the FC-0002 implementation
- **THEN** it does not implement complete marketplace products, performance data, plugin data records, custom workflow models, or persistence mappings
- **AND** concept versioning, design versioning, and mockup records are accepted as Phase 2 concerns through their respective capabilities
