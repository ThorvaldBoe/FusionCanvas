# Testing Baseline

## Purpose

Defines the automated testing baseline, priority coverage areas, run expectations, and contribution expectations for foundational FusionCanvas behavior.

## Requirements

### Requirement: Acceptance criteria are traceable to verification
FusionCanvas SHALL map every delivery-module acceptance scenario to planned verification and final evidence.

#### Scenario: Verification is planned
- **WHEN** a module becomes implementation-ready
- **THEN** each acceptance scenario is mapped to focused automated tests, integration tests, Avalonia headless view tests, manual inspection, optional ad hoc live-desktop checks, or an explicit not-applicable rationale

#### Scenario: Verification is recorded
- **WHEN** implementation is complete
- **THEN** the module verification record identifies the result and material evidence for every acceptance scenario
- **AND** an aggregate solution test pass does not substitute for criterion-level evidence

### Requirement: User-facing views receive headless verification where valuable
FusionCanvas SHALL use Avalonia headless tests as the routine framework-level verification lane for user-facing views when rendering, bindings, control state, routed input, focus, or visual-tree behavior carries meaningful risk.

#### Scenario: A user-facing view changes
- **WHEN** a delivery module adds or changes a view with meaningful Avalonia framework behavior
- **THEN** the change includes focused Avalonia headless tests for the applicable behavior
- **AND** those tests run through the documented solution-level test command on both Codex and OpenCode

#### Scenario: Decision logic can be tested without Avalonia
- **WHEN** UI-owned behavior is fully expressed by a view model, command, projector, or other framework-independent type
- **THEN** the change uses focused code-level tests for that behavior
- **AND** does not add a superficial headless view test that only repeats the same assertion

#### Scenario: Static markup has no meaningful behavior
- **WHEN** a view change is limited to static markup or framework-owned rendering with no material binding, state, input, focus, or visual-tree risk
- **THEN** headless view testing may be omitted with a concise rationale

### Requirement: Live desktop UI verification is optional and ad hoc
FusionCanvas SHALL treat live testing through the built desktop application as an optional verification activity rather than a routine completion gate.

#### Scenario: Contributor completes a user-facing module
- **WHEN** required deterministic tests and criterion-level verification pass
- **THEN** the module may be completed without a live desktop UI pass
- **AND** no unavailable-environment handoff is required

#### Scenario: A risk is not represented by headless testing
- **WHEN** a contributor wants additional confidence in platform integration, native window behavior, assistive-technology exposure, operating-system input, visual appearance, or a difficult interaction defect
- **THEN** the contributor may perform an isolated live desktop check ad hoc
- **AND** records its result as supplemental evidence without turning it into a standing completion requirement

#### Scenario: Live desktop testing is unavailable
- **WHEN** an agent has no interactive desktop environment
- **THEN** routine implementation and QA proceed using the deterministic baseline
- **AND** the absence of a live desktop run is not reported as a failed, blocked, or not-applicable gate

### Requirement: Automated test baseline exists
FusionCanvas SHALL provide an automated testing baseline that contributors can run to verify foundational behavior and applicable headless view behavior.

#### Scenario: Contributor runs the baseline suite
- **WHEN** a contributor runs the documented solution-level test command from the repository root
- **THEN** the automated test baseline executes without requiring external services, marketplace accounts, AI provider credentials, network access, or an interactive desktop session

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
FusionCanvas SHALL test navigation decision logic and meaningful view-owned navigation behavior at the lowest reliable layer.

#### Scenario: Navigation logic changes
- **WHEN** a change adds or modifies navigation tree construction, selection behavior, ordering, or application-owned navigation decisions
- **THEN** the change includes focused tests for the decision logic without requiring an interactive desktop
- **AND** uses Avalonia headless tests when framework binding, routed input, focus, selection, or visual-tree behavior is material to the accepted outcome

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
FusionCanvas SHALL keep the automated baseline deterministic and proportionate while allowing focused Avalonia headless view tests.

#### Scenario: Contributor evaluates test scope
- **WHEN** a contributor adds tests to the solution-level baseline
- **THEN** the tests do not require complete end-to-end desktop automation, an interactive desktop, visual regression infrastructure, performance benchmarking, marketplace integration access, AI provider access, or manual QA process setup
- **AND** headless view tests target meaningful framework behavior rather than static-markup existence or framework implementation details

### Requirement: Headless view tests isolate workspace data
FusionCanvas SHALL construct headless view tests with an in-memory or disposable workspace and SHALL NOT use any application factory, repository, or path that opens the contributor's real on-disk workspace database or workspace file root, so automated view tests never read or mutate the contributor's normal workspace data.

#### Scenario: Headless view test uses isolated workspace data
- **WHEN** a headless view test constructs a window or view model that needs workspace state
- **THEN** the test uses the in-memory sample workspace or an explicit disposable test repository
- **AND** does not call any factory that resolves the real on-disk workspace database path

#### Scenario: Real-workspace factory is not used by tests
- **WHEN** an application entry point exposes a factory that opens the contributor's real on-disk workspace
- **THEN** automated tests do not call that factory
- **AND** any view model that needs a workspace in tests is constructed with isolated data instead
