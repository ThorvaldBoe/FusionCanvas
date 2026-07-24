## ADDED Requirements

### Requirement: Delivery modules receive scoped completion QA
FusionCanvas SHALL perform a completion review proportionate to each delivery module without requiring the full repository QA suite after every module.

#### Scenario: Module completion is reviewed
- **WHEN** a contributor prepares to report a module complete
- **THEN** the review checks build and deterministic tests, strict OpenSpec validation, acceptance-evidence completeness, specification drift within the changed scope, and relevant architecture, security, persistence, or UI risks

#### Scenario: Module changes user-facing behavior
- **WHEN** the completed module contains user-facing changes
- **THEN** the completion review includes the targeted desktop verification result or an explicit unavailable-environment handoff

#### Scenario: Module has broad cross-cutting risk
- **WHEN** the changed scope can plausibly regress unrelated accepted capabilities
- **THEN** the reviewer expands the relevant QA tasks or recommends a full QA review rather than relying only on module-scoped checks

### Requirement: Completion review verifies implementation readiness assumptions
FusionCanvas SHALL review whether delegated implementation stayed within the approved delivery package and escalated missing decisions correctly.

#### Scenario: Delegated implementation is reviewed
- **WHEN** a lower-cost or bounded implementation agent completes module tasks
- **THEN** the reviewer checks for unapproved scope, invented product behavior, architecture divergence, missing acceptance coverage, and unresolved implementation assumptions

#### Scenario: Review finds a failed gate
- **WHEN** completion review finds a failed acceptance criterion, validation command, or material package divergence
- **THEN** the module returns to correction and re-verification rather than being accepted with the failure hidden in a summary
