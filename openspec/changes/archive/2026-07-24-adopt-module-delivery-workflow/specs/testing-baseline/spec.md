## ADDED Requirements

### Requirement: Acceptance criteria are traceable to verification
FusionCanvas SHALL map every delivery-module acceptance scenario to planned verification and final evidence.

#### Scenario: Verification is planned
- **WHEN** a module becomes implementation-ready
- **THEN** each acceptance scenario is mapped to focused automated tests, integration tests, targeted real-desktop scenarios, manual inspection, or an explicit not-applicable rationale

#### Scenario: Verification is recorded
- **WHEN** implementation is complete
- **THEN** the module verification record identifies the result and material evidence for every acceptance scenario
- **AND** an aggregate solution test pass does not substitute for criterion-level evidence

### Requirement: Desktop UI verification is risk-based and budget-conscious
FusionCanvas SHALL select real-desktop verification scenarios according to user impact, integration risk, novelty, and information value while preserving a separate deterministic test baseline.

#### Scenario: User-facing module plans desktop verification
- **WHEN** a delivery module adds or changes user-facing behavior and an interactive desktop is available
- **THEN** the plan covers the critical end-to-end workflow and applicable high-risk behavior such as persistence, destructive actions, state synchronization, framework wiring, input or focus complexity, recovery, accessibility, and windows or tabs
- **AND** records why the selected scenarios provide sufficient confidence

#### Scenario: Variants are equivalent and low risk
- **WHEN** multiple UI variants exercise the same wiring and rule with no distinct high-risk behavior
- **THEN** the contributor may test representative variants on the real desktop while covering the remaining rule combinations with deterministic tests

#### Scenario: Interactive desktop is unavailable
- **WHEN** the contributing agent cannot access an interactive desktop session
- **THEN** the desktop lane is recorded as not applicable rather than passed
- **AND** the unexecuted targeted scenarios are preserved for handoff to a capable agent or human

#### Scenario: Broad regression is warranted
- **WHEN** a milestone, release candidate, broad shell change, or accumulated cross-feature risk warrants comprehensive verification
- **THEN** the full all-features desktop regression is run separately from the module-scoped pass
