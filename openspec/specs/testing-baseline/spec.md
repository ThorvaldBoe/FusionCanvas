# Testing Baseline

## Purpose

Defines the automated testing baseline, priority coverage areas, run expectations, and contribution expectations for foundational FusionCanvas behavior.

## Requirements

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

### Requirement: Automated test baseline exists
FusionCanvas SHALL provide an automated testing baseline that contributors can run to verify foundational behavior.

#### Scenario: Contributor runs the baseline suite
- **WHEN** a contributor runs the documented solution-level test command from the repository root
- **THEN** the automated test baseline executes without requiring external services, marketplace accounts, AI provider credentials, network access, or a running desktop UI

#### Scenario: Contributor inspects test project layout
- **WHEN** a contributor reviews the solution test projects
- **THEN** the test projects mirror the production layer projects for domain, application, integration, and app behavior where those production projects contain testable responsibilities

### Requirement: Domain behavior is covered first
FusionCanvas SHALL prioritize focused tests for foundational domain rules, core entity relationships, invariants, and workflow decisions.

#### Scenario: Domain behavior changes
- **WHEN** a change adds or modifies domain rules, entity relationships, invariants, calculations, or workflow decisions
- **THEN** the change includes focused domain tests that verify the behavior without depending on UI frameworks, persistence engines, external services, or file system adapters

### Requirement: Application behavior is testable
FusionCanvas SHALL make application-level use cases and orchestration behavior testable through application contracts and deterministic collaborators.

#### Scenario: Application use case changes
- **WHEN** a change adds or modifies application orchestration, workflow coordination, or use-case behavior
- **THEN** the change includes focused application tests that verify the behavior through domain and application contracts

### Requirement: Persistence boundaries are protected
FusionCanvas SHALL include local deterministic tests for persistence boundaries that could otherwise cause data loss, relationship errors, or broken workspace reconstruction.

#### Scenario: Persistence boundary changes
- **WHEN** a change adds or modifies local persistence behavior, repository mapping, serialization, or workspace reconstruction
- **THEN** the change includes tests that verify data can be saved and loaded with expected identities, relationships, and required fields intact

#### Scenario: Persistence tests run in isolation
- **WHEN** persistence boundary tests execute
- **THEN** they use isolated local test resources and do not require a shared developer database or external service

### Requirement: Asset reference behavior is protected
FusionCanvas SHALL include tests for asset reference behavior that could affect reconnecting files or preserving resource identity.

#### Scenario: Asset reference behavior changes
- **WHEN** a change adds or modifies workspace file storage, asset reference generation, asset lookup, or asset relationship behavior
- **THEN** the change includes tests that verify asset identity or references remain stable enough for the implemented workflow

### Requirement: Navigation behavior is testable where practical
FusionCanvas SHALL test navigation behavior where it contains application state, ordering, selection, filtering, tree construction, or command decisions.

#### Scenario: Navigation logic changes
- **WHEN** a change adds or modifies navigation tree construction, selection behavior, ordering, or application-owned navigation decisions
- **THEN** the change includes focused tests for the decision logic without requiring full visual UI automation

### Requirement: Specification acceptance behavior informs tests
FusionCanvas SHALL use accepted OpenSpec requirements and scenarios to guide automated test coverage for foundational behavior.

#### Scenario: Contributor implements accepted behavior
- **WHEN** a contributor implements behavior described by accepted OpenSpec requirements or scenarios
- **THEN** the implementation includes automated tests that cover the important acceptance behavior or documents why automated coverage is not practical

### Requirement: New foundational behavior includes relevant tests
FusionCanvas SHALL generally require new foundational behavior to include relevant automated tests before the change is considered complete.

#### Scenario: Contributor completes a foundational change
- **WHEN** a contributor marks a foundational change complete
- **THEN** the change includes relevant automated tests for domain, application, integration boundary, navigation, asset reference, or specification-driven behavior as applicable
- **AND** any omitted automated tests are justified by an explicit documented reason

### Requirement: Testing scope remains focused
FusionCanvas SHALL keep the Phase 0 testing baseline focused on useful automated behavior tests rather than broad UI automation or expensive validation suites.

#### Scenario: Contributor evaluates test scope
- **WHEN** a contributor adds tests under the Phase 0 baseline
- **THEN** the tests do not require complete end-to-end UI automation, visual regression infrastructure, performance benchmarking, marketplace integration access, AI provider access, or manual QA process setup
