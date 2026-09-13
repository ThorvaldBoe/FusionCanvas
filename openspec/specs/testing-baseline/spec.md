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

#### Scenario: Group selection dialog behavior is protected
- **WHEN** a contributor changes or reviews `GroupSelectionWindow`
- **THEN** focused Avalonia headless tests cover its destination and name bindings
- **AND** the tests cover invalid confirmation validation and successful confirmation through the rendered dialog controls
- **AND** the tests use isolated deterministic destinations without opening workspace persistence

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

### Requirement: Desktop UI automation is a separately selectable verification lane
FusionCanvas SHALL keep `dotnet test .\\FusionCanvas.sln -m:1` as the required deterministic, non-interactive baseline and SHALL provide real-desktop UI automation as a separately selectable Windows verification lane.

#### Scenario: Contributor runs the baseline suite
- **WHEN** a contributor runs `dotnet test .\\FusionCanvas.sln -m:1`
- **THEN** the command does not require a Windows automation server, Developer Mode, an interactive desktop, or the desktop UI-test project

#### Scenario: Contributor needs real-desktop coverage
- **WHEN** a contributor or CI job needs to verify a native-window or end-to-end interaction risk covered by the desktop UI suite
- **THEN** it runs the documented Windows UI-test command with its explicit prerequisites
- **AND** records that result as supplemental to, not a substitute for, the required deterministic baseline

### Requirement: Desktop UI scenario scope remains proportionate
FusionCanvas SHALL allocate real-desktop UI journeys to high-value cross-process, native-window, accessibility, focus, or end-to-end workflow risks and SHALL use focused deterministic tests for equivalent lower-risk variants.

#### Scenario: A module adds a UI automation journey
- **WHEN** a delivery module proposes a new desktop UI journey
- **THEN** its OpenSpec verification plan identifies the workflow risk that requires real-desktop automation
- **AND** identifies related behavior retained in focused headless, view-model, application, or integration tests

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

### Requirement: Existing compiler and formatting debt is explicitly baselined
FusionCanvas SHALL record known compiler, analyzer, and formatting debt with reproducible verification commands and SHALL distinguish environmental verification failures from clean results.

#### Scenario: Contributor reviews the quality baseline
- **WHEN** a contributor evaluates the repository quality gates
- **THEN** the checked-in baseline identifies the deterministic build command, observed diagnostics, formatter command, and any runner limitation
- **AND** the baseline does not suppress diagnostics or claim a clean formatter result when analysis did not run

### Requirement: Dependency updates are batched and verified
FusionCanvas SHALL plan dependency updates in bounded compatibility batches and SHALL verify each batch with the deterministic build/test baseline and any affected focused tests before treating it as complete.

#### Scenario: A dependency update is proposed
- **WHEN** a contributor plans package updates with different compatibility or framework risk
- **THEN** the updates are separated into reviewable batches with explicit affected verification

#### Scenario: A dependency batch is completed
- **WHEN** a package batch is applied
- **THEN** the solution build and deterministic tests pass together with the focused tests for the affected framework or integration boundary

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

### Requirement: Critical asset-management jobs receive a rendered headless journey
FusionCanvas SHALL protect a critical store-level asset-management job with a deterministic Avalonia headless experience journey when the outcome crosses rendered controls, file selection, persistence, or preview lifecycle seams.

#### Scenario: A creator imports and reviews a store asset
- **WHEN** a creator opens the store-level Assets surface, chooses a supported image through the import control, confirms the suggested purpose, changes the purpose, and activates the asset preview
- **THEN** the journey exercises those actions through rendered controls and routed input
- **AND** it asserts the visible pending, imported, relabeled, and preview states communicated to the creator
- **AND** it verifies the managed file reference and purpose after reconstructing a fresh presentation instance from scenario-owned persistence

#### Scenario: Asset removal is cancelled
- **WHEN** the creator requests removal from the rendered asset row and cancels the confirmation
- **THEN** the asset row, purpose, managed-file state, and current selection remain unchanged
- **AND** the journey does not claim that lower-layer removal variants are covered by the rendered path

#### Scenario: A deterministic picker or preview boundary is unavailable
- **WHEN** native file-picker behavior or platform image rendering cannot be represented faithfully by the headless harness
- **THEN** the journey uses an explicit deterministic boundary fake and verifies separable UI and persistence decisions
- **AND** the limitation is recorded as supplemental evidence scope rather than silently bypassing the rendered seam

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

