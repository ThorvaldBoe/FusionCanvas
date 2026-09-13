## ADDED Requirements

### Requirement: User-facing verification is organized by user jobs
FusionCanvas SHALL maintain a reviewable inventory that maps each implemented user-facing surface to its meaningful user jobs, representative states, expected observable outcomes, and automated verification or explicit omission rationale.

#### Scenario: A user-facing module becomes implementation-ready
- **WHEN** a module adds or changes a user-facing workflow
- **THEN** its verification plan identifies the affected user jobs and representative success, validation, cancellation, failure, and re-entry states as applicable
- **AND** maps each meaningful outcome to the lowest reliable focused test, a headless experience journey, optional live-desktop evidence, or an explicit not-applicable rationale

#### Scenario: An existing surface is reviewed
- **WHEN** QA reviews an implemented surface for headless coverage
- **THEN** the reviewer evaluates the meaningful jobs and outcomes available from that surface rather than counting windows, controls, test methods, or lines of coverage
- **AND** static markup or framework-owned rendering may be marked not applicable when it does not carry accepted behavior or material regression risk

### Requirement: Critical cross-seam workflows receive headless experience journeys
FusionCanvas SHALL protect critical user jobs with a small number of deterministic Avalonia headless experience journeys when the accepted outcome depends on coordination across rendered controls, bindings or routed input, view-model orchestration, persistence, lifecycle, or re-entry state.

#### Scenario: A critical workflow crosses framework seams
- **WHEN** failure of a user job could escape focused lower-layer tests because its outcome crosses two or more material UI, orchestration, persistence, or lifecycle seams
- **THEN** the change includes a rendered headless journey that exercises the complete critical path
- **AND** lower-risk variants remain covered at focused domain, application, integration, view-model, or component-test layers

#### Scenario: A user expects a mutation to persist
- **WHEN** a critical user job saves or automatically persists durable state
- **THEN** its headless journey asserts the immediate observable result
- **AND** closes and reconstructs the relevant screen with a fresh presentation instance
- **AND** asserts the rehydrated user-visible state and absence of unintended pending changes

#### Scenario: A workflow does not justify an experience journey
- **WHEN** focused tests already prove the complete accepted outcome without crossing a meaningful Avalonia or lifecycle seam
- **THEN** the verification plan records the focused evidence or not-applicable rationale
- **AND** does not add a ceremonial rendered journey solely because a view or control exists

### Requirement: Headless experience journeys preserve the user boundary
FusionCanvas SHALL distinguish user-action execution from fixture setup so a headless experience journey does not bypass the UI seam it claims to verify.

#### Scenario: A journey performs user actions
- **WHEN** the action phase of a headless experience journey begins
- **THEN** the journey operates through rendered controls using routed pointer, keyboard, text, selection, focus, or equivalent control-level interaction supported by the headless platform
- **AND** it does not invoke a bound view-model command, mutate the bound view-model property, or invoke a private event handler directly in place of that user action

#### Scenario: A journey arranges deterministic state
- **WHEN** a journey prepares its initial workspace, collaborator responses, or selected domain context
- **THEN** it may use application contracts, test fixtures, or direct setup state before the action phase
- **AND** setup operations remain visibly separated from user actions in the test

#### Scenario: A journey verifies an outcome
- **WHEN** a journey completes a user action
- **THEN** it asserts the observable control state, feedback, selection, hierarchy, focus, or content that communicates success or failure to the user
- **AND** may additionally inspect isolated persistence to prove a durable outcome

### Requirement: Headless interaction infrastructure is deterministic and isolated
FusionCanvas SHALL provide shared headless-test support for dispatcher coordination, lifecycle cleanup, semantic control discovery, and scenario-scoped mutable resources without relying on arbitrary sleeps or contributor workspace data.

#### Scenario: A journey waits for asynchronous UI state
- **WHEN** an expected UI outcome depends on asynchronous work or dispatcher publication
- **THEN** the test pumps the dispatcher until an observable condition is satisfied or a bounded diagnostic timeout expires
- **AND** does not use an arbitrary delay as evidence that the state should be ready

#### Scenario: A journey uses a persistent workspace
- **WHEN** a headless journey verifies save and re-entry behavior through persistence
- **THEN** it uses a unique disposable database, workspace root, and settings path owned by that scenario
- **AND** reconstructs application and presentation instances from those resources without using the contributor's normal workspace

#### Scenario: Shared test drivers are introduced
- **WHEN** repeated headless interaction is encapsulated in a surface driver
- **THEN** the driver exposes thin user-language actions and observations
- **AND** does not hide assertions, domain decisions, direct view-model shortcuts, unbounded waits, or reliance on incidental visual-tree structure

### Requirement: Escaped defects improve regression protection
FusionCanvas SHALL analyze every corrected defect that reached manual testing or later verification and SHALL use the result to add proportionate local regression coverage and reusable prevention where warranted.

#### Scenario: A defect is fixed
- **WHEN** a contributor corrects an observed defect
- **THEN** the contributor first adds or identifies a deterministic automated test that fails for the user-visible defect and passes after the correction
- **AND** records why the existing suite did not detect the defect
- **AND** checks whether the same escape mechanism plausibly affects similar workflows or surfaces

#### Scenario: Deterministic reproduction is unsuitable
- **WHEN** a defect depends on native-window behavior, operating-system input, assistive-technology exposure, platform integration, visual judgment, or another risk that the deterministic baseline cannot faithfully represent
- **THEN** the correction records the specific limitation and proportionate alternative evidence
- **AND** adds focused deterministic coverage for any separable decision or state behavior

#### Scenario: Escape analysis identifies a reusable pattern
- **WHEN** an escape mechanism has recurred or plausibly affects multiple workflows or surfaces
- **THEN** the contributor updates the appropriate test support, inventory, checklist, specification, or coding guidance
- **AND** verifies at least one representative prevention beyond the original local regression

#### Scenario: Escape analysis identifies an isolated defect
- **WHEN** the cause is specific to one workflow and no broader prevention is justified
- **THEN** the local regression is retained
- **AND** no project-wide rule or duplicate test is added solely to demonstrate process activity

### Requirement: Pull requests enforce the canonical deterministic baseline
FusionCanvas SHALL run one documented solution-level deterministic test command for pull requests and SHALL keep its environment requirements repository-controlled and reproducible.

#### Scenario: A pull request changes the repository
- **WHEN** pull-request automation executes
- **THEN** it restores required dependencies and runs the canonical solution test baseline including Avalonia headless tests
- **AND** a failed build or test prevents the automated check from passing

#### Scenario: The baseline runs in supported contributor environments
- **WHEN** Codex, OpenCode, CI, or a human contributor invokes the documented baseline from the repository root
- **THEN** repository configuration supplies required non-secret environment behavior, including any Avalonia telemetry suppression needed for a non-interactive run
- **AND** contributors do not need an undocumented command variant to obtain the same result

#### Scenario: Real-desktop automation remains separate
- **WHEN** the canonical deterministic pull-request baseline executes
- **THEN** it does not require Appium, Windows Developer Mode, an interactive desktop, or execution of `FusionCanvas.UITests`

