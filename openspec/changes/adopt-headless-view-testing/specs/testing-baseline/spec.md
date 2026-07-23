## ADDED Requirements

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

## MODIFIED Requirements

### Requirement: Acceptance criteria are traceable to verification
FusionCanvas SHALL map every delivery-module acceptance scenario to planned verification and final evidence.

#### Scenario: Verification is planned
- **WHEN** a module becomes implementation-ready
- **THEN** each acceptance scenario is mapped to focused automated tests, integration tests, Avalonia headless view tests, manual inspection, optional ad hoc live-desktop checks, or an explicit not-applicable rationale

#### Scenario: Verification is recorded
- **WHEN** implementation is complete
- **THEN** the module verification record identifies the result and material evidence for every acceptance scenario
- **AND** an aggregate solution test pass does not substitute for criterion-level evidence

### Requirement: Automated test baseline exists
FusionCanvas SHALL provide an automated testing baseline that contributors can run to verify foundational behavior and applicable headless view behavior.

#### Scenario: Contributor runs the baseline suite
- **WHEN** a contributor runs the documented solution-level test command from the repository root
- **THEN** the automated test baseline executes without requiring external services, marketplace accounts, AI provider credentials, network access, or an interactive desktop session

#### Scenario: Contributor inspects test project layout
- **WHEN** a contributor reviews the solution test projects
- **THEN** the test projects mirror the production layer projects for domain, application, integration, and app behavior where those production projects contain testable responsibilities

### Requirement: Navigation behavior is testable where practical
FusionCanvas SHALL test navigation decision logic and meaningful view-owned navigation behavior at the lowest reliable layer.

#### Scenario: Navigation logic changes
- **WHEN** a change adds or modifies navigation tree construction, selection behavior, ordering, or application-owned navigation decisions
- **THEN** the change includes focused tests for the decision logic without requiring an interactive desktop
- **AND** uses Avalonia headless tests when framework binding, routed input, focus, selection, or visual-tree behavior is material to the accepted outcome

### Requirement: Testing scope remains focused
FusionCanvas SHALL keep the automated baseline deterministic and proportionate while allowing focused Avalonia headless view tests.

#### Scenario: Contributor evaluates test scope
- **WHEN** a contributor adds tests to the solution-level baseline
- **THEN** the tests do not require complete end-to-end desktop automation, an interactive desktop, visual regression infrastructure, performance benchmarking, marketplace integration access, AI provider access, or manual QA process setup
- **AND** headless view tests target meaningful framework behavior rather than static-markup existence or framework implementation details

## REMOVED Requirements

### Requirement: Desktop UI verification is risk-based and budget-conscious

**Reason**: Routine real-desktop verification is too slow and environment-dependent to serve as a cross-agent completion requirement. Avalonia headless view testing becomes the routine framework-level UI lane.

**Migration**: Map meaningful view risks to deterministic headless tests. Use live desktop testing only as optional ad hoc supplemental verification.
