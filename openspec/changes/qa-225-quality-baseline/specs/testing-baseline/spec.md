## ADDED Requirements

### Requirement: Existing compiler and formatting debt is explicitly baselined
FusionCanvas SHALL record known compiler, analyzer, and formatting debt with reproducible verification commands and SHALL distinguish environmental verification failures from clean results.

#### Scenario: Contributor reviews the quality baseline
- **WHEN** a contributor evaluates the repository quality gates
- **THEN** the checked-in baseline identifies the deterministic build command, observed diagnostics, formatter command, and any runner limitation
- **AND** the baseline does not suppress diagnostics or claim a clean formatter result when analysis did not run
