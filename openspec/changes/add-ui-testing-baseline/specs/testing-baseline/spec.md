## ADDED Requirements

### Requirement: User-facing delivery modules receive targeted real desktop UI verification
FusionCanvas SHALL require every user-facing delivery module to plan targeted verification through the built desktop application before the change is considered complete, with execution required when the contributing agent has an interactive desktop environment.

#### Scenario: Contributor completes a user-facing module
- **WHEN** a delivery module adds or modifies behavior that a user can see or operate in the Avalonia application
- **THEN** the change includes a real desktop UI verification plan covering the module's critical end-to-end workflow and distinct high-risk accepted behavior
- **AND** the verification launches the built application and interacts through actual keyboard, pointer, or accessibility-driven input as applicable

#### Scenario: Module has multiple interaction risks
- **WHEN** a user-facing module depends on keyboard shortcuts, pointer actions, focus or selection, validation, filtering, destructive confirmation, persistence, restart, recovery, accessibility exposure, state synchronization, or tabs and windows
- **THEN** its UI verification prioritizes distinct risks according to user impact, integration risk, novelty, previous failures, and information value
- **AND** records why selected scenarios provide sufficient confidence and which equivalent low-risk variants remain covered by deterministic tests

#### Scenario: Interactive desktop is unavailable
- **WHEN** the contributing agent cannot access an interactive desktop environment
- **THEN** it records desktop verification as not applicable rather than passed
- **AND** preserves the targeted scenario matrix for handoff to a capable agent or human

#### Scenario: Change has no user-facing path
- **WHEN** a change affects only internal behavior with no desktop interaction or visible outcome
- **THEN** its verification explicitly marks real desktop UI testing as not applicable with a concise reason

### Requirement: Desktop UI verification is isolated and evidence-backed
FusionCanvas SHALL execute desktop UI verification against isolated test state and retain enough evidence to reproduce and evaluate the result.

#### Scenario: Desktop verification uses application data
- **WHEN** a UI test can create, modify, move, archive, restore, or delete workspace data
- **THEN** it uses a disposable database or workspace fixture rather than the contributor's normal workspace
- **AND** destructive scenarios do not mutate normal user data

#### Scenario: Desktop verification completes
- **WHEN** a desktop UI verification pass is reported
- **THEN** the record identifies the tested build and environment, exercised scenarios, result, data-isolation method, and any limitations or blocked cases
- **AND** screenshots or automation logs are retained when they materially demonstrate a visual or interaction result

### Requirement: Desktop UI verification remains a separate test lane
FusionCanvas SHALL keep real desktop UI verification separate from the fast solution-level automated test baseline.

#### Scenario: Contributor runs the solution-level baseline
- **WHEN** a contributor runs `dotnet test .\FusionCanvas.sln`
- **THEN** the suite remains deterministic and does not require an interactive desktop session or running Avalonia application

#### Scenario: Contributor verifies a user-facing change
- **WHEN** the code-level baseline passes for a user-facing change
- **THEN** passing code-level tests does not replace the required real desktop UI verification pass

### Requirement: User-facing OpenSpec modules plan UI verification
FusionCanvas SHALL include explicit risk-based real desktop UI verification scope in every delivery-module OpenSpec change that adds or modifies user-facing behavior.

#### Scenario: User-facing change artifacts are created
- **WHEN** proposal, design, specification, and task artifacts are prepared for a user-facing change
- **THEN** the critical workflow and distinct high-risk acceptance scenarios are translated into a desktop UI verification plan and completion task
- **AND** the plan identifies its selection rationale, deterministic coverage for equivalent low-risk variants, isolation strategy, environment handoff, and evidence to record

## MODIFIED Requirements

### Requirement: Navigation behavior is testable where practical
FusionCanvas SHALL protect navigation decision logic with focused code-level tests and SHALL exercise user-facing navigation behavior through the real desktop application.

#### Scenario: Navigation logic changes
- **WHEN** a change adds or modifies navigation tree construction, selection behavior, ordering, or application-owned navigation decisions
- **THEN** the change includes focused code-level tests for the decision logic without launching Avalonia
- **AND** critical and distinct high-risk navigation paths are included in the module's targeted real desktop UI verification

### Requirement: Testing scope remains focused
FusionCanvas SHALL keep the fast solution-level testing baseline focused on deterministic behavior tests while running required real desktop UI verification as a separate lane.

#### Scenario: Contributor evaluates test scope
- **WHEN** a contributor adds tests to the solution-level baseline
- **THEN** `dotnet test .\FusionCanvas.sln` does not require a running desktop UI, complete end-to-end UI automation, visual regression infrastructure, performance benchmarking, marketplace integration access, AI provider access, or manual QA setup
- **AND** this headless scope does not remove the separate targeted real desktop UI verification requirement or environment handoff for user-facing delivery modules
