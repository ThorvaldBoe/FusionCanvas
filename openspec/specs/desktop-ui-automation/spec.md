# Desktop UI Automation

## Purpose

Defines the Windows-only code-run desktop UI automation harness that drives the compiled application through an Appium-compatible Windows automation server, including isolated per-session data, stable accessible control identity, an initial end-to-end smoke journey, and selectable/diagnosable test results.

## Requirements

### Requirement: Windows desktop UI automation harness exists
FusionCanvas SHALL provide a Windows-only code-run UI automation test harness that launches the compiled application through an Appium-compatible Windows automation server and drives it through the operating-system accessibility tree.

#### Scenario: Harness launches a compiled application
- **WHEN** a contributor runs the documented selected UI-test command on Windows with the required automation-server prerequisite available
- **THEN** the harness launches the compiled FusionCanvas application and establishes an automation session without interactive test authoring or AI assistance

#### Scenario: Automation prerequisite is unavailable
- **WHEN** the selected UI-test command cannot reach its required Windows automation server
- **THEN** it fails before executing a journey with an actionable message naming the missing prerequisite and the documented setup command or location

### Requirement: UI automation sessions isolate user data
FusionCanvas SHALL give every desktop UI automation session a unique disposable database path, workspace-file root, and settings path, and SHALL pass those paths to the launched application through documented test-only launch arguments without resolving the contributor's normal workspace locations.

#### Scenario: A journey mutates workspace state
- **WHEN** a desktop UI automation journey creates or edits application data
- **THEN** all resulting database, workspace-file, and settings writes are contained within that journey's disposable test root
- **AND** the contributor's normal workspace database, workspace-file root, and settings file remain untouched

#### Scenario: Journey cleanup runs after a failure
- **WHEN** a desktop UI automation journey fails or throws after its application process has started
- **THEN** the harness attempts to close the automation session and launched application
- **AND** removes its disposable test root or reports the retained path as diagnostic cleanup evidence

### Requirement: UI journeys use stable accessible control identity
FusionCanvas SHALL expose stable `AutomationProperties.AutomationId` values for controls that a desktop UI journey uses to initiate actions, enter data, or assert an observable result.

#### Scenario: A journey locates a control
- **WHEN** a desktop UI journey interacts with a control covered by its scenario
- **THEN** it locates that control by its documented automation identifier rather than visible wording, control-tree position, or screen coordinates

### Requirement: Initial end-to-end smoke journey proves a persisted workflow
FusionCanvas SHALL include a Windows desktop UI smoke journey that opens store management, creates a uniquely named store through UI automation, verifies the resulting visible store state, and verifies that the store was persisted in that journey's isolated workspace database.

#### Scenario: Store creation succeeds end-to-end
- **WHEN** the smoke journey starts with a new disposable workspace
- **THEN** it opens the store-management surface, enters the generated store name, and invokes the creation action through accessibility-driven automation
- **AND** it observes the created store as the selected or listed store in the application
- **AND** it verifies the generated name in the isolated persisted workspace after the interaction completes

#### Scenario: Store creation input supports keyboard use
- **WHEN** the smoke journey fills the new-store name field
- **THEN** it uses keyboard text input through the automation session
- **AND** the primary creation action is enabled only after the required name is present

### Requirement: UI automation results are selectable and diagnosable
FusionCanvas SHALL allow contributors to run the desktop UI test project or a selected UI-test collection through standard `dotnet test` filtering and SHALL emit normal test-runner pass/fail results suitable for local and CI diagnosis.

#### Scenario: Contributor selects the smoke suite
- **WHEN** a contributor runs the documented command filtered to the initial smoke suite
- **THEN** the test runner executes only that suite and reports its result through the standard test-runner output and exit code

#### Scenario: A UI journey fails
- **WHEN** a desktop UI journey fails an assertion or automation command
- **THEN** the test result identifies the failing journey and preserves enough diagnostic context to locate the isolated test root when cleanup could not complete
