# QA Review Baseline

## Purpose

Defines the recurring QA review process, its review areas, execution modes (full or per-task), finding severity and reporting expectations, and how findings are routed to direct fixes or OpenSpec changes.

## Requirements

### Requirement: QA review process is documented and repeatable
FusionCanvas SHALL define a recurring QA review process as a set of independent QA tasks documented in an operational playbook, so that any contributor or AI coding agent can execute the same review consistently.

#### Scenario: Contributor requests a full QA review
- **WHEN** a contributor requests a full QA review
- **THEN** every QA task defined in the playbook is executed and a single consolidated report is produced

#### Scenario: Contributor requests a specific QA task
- **WHEN** a contributor requests one or more QA tasks by area or identifier
- **THEN** only the requested tasks are executed, following the same review protocol and report format as a full review

#### Scenario: Playbook details evolve
- **WHEN** checklists, commands, or report formats in the playbook need refinement without changing what QA must cover
- **THEN** the playbook is updated directly without an OpenSpec proposal

### Requirement: QA reviews verify design and architecture conformance
FusionCanvas QA reviews SHALL evaluate code design and architecture against accepted expectations: pragmatic SOLID principles and Clean Architecture as defined by the architecture guidelines specification.

#### Scenario: Design review executes
- **WHEN** the design QA task executes
- **THEN** production code is evaluated for focused responsibilities, explicit dependencies, justified abstractions, and the absence of god classes and tight coupling, without demanding dogmatic over-abstraction

#### Scenario: Architecture review executes
- **WHEN** the architecture QA task executes
- **THEN** layer responsibilities, project dependency direction, and domain independence from frameworks are verified against the accepted architecture guidelines

### Requirement: QA reviews verify the testing baseline
FusionCanvas QA reviews SHALL confirm that the automated testing baseline passes and that behavior is protected by focused tests as defined by the testing baseline specification.

#### Scenario: Testing review executes
- **WHEN** the testing QA task executes
- **THEN** the solution-level baseline suite is run and its result reported, and behavior in domain, application, integration, and app layers is checked for corresponding focused tests

#### Scenario: Coverage gaps are found
- **WHEN** the testing review identifies behavior without adequate tests
- **THEN** the gaps are reported with severity and routed to test additions or to OpenSpec clarification when the behavior itself is unclear

### Requirement: QA reviews verify security hygiene
FusionCanvas QA reviews SHALL check dependency health, repository secret hygiene, and code-level attack vectors relevant to the current feature set.

#### Scenario: Security review executes
- **WHEN** the security QA task executes
- **THEN** NuGet packages are checked for vulnerabilities, deprecations, and available updates; tracked files are scanned for committed secrets; and relevant injection vectors (SQL, path traversal, prompt injection) are reviewed

#### Scenario: A secret is found in the repository
- **WHEN** the security review identifies a committed credential, API key, or token
- **THEN** the finding is reported as critical without quoting the secret value, with a recommendation to revoke or rotate the credential

### Requirement: QA reviews detect specification and documentation drift
FusionCanvas QA reviews SHALL verify that accepted OpenSpec specifications, documentation, and implemented behavior remain aligned.

#### Scenario: Drift review executes
- **WHEN** the drift QA task executes
- **THEN** contradictions between accepted specs, docs, and code are identified, including behavior implemented but not specified, specified behavior contradicted by code, stale active changes, and inconsistent documentation

### Requirement: QA findings are routed through the correct resolution path
FusionCanvas SHALL resolve QA findings through paths proportional to their impact: running a QA review requires no OpenSpec ceremony, while findings that change accepted behavior go through the OpenSpec workflow.

#### Scenario: Finding is internal maintenance
- **WHEN** a finding does not alter accepted behavior (style, structure, missing tests for accepted behavior, dependency updates)
- **THEN** it is fixed directly as maintenance work with the test baseline run afterwards

#### Scenario: Finding changes accepted behavior or reveals drift
- **WHEN** a finding would change accepted behavior, contradicts a spec, or reveals specification drift
- **THEN** it is resolved through an OpenSpec change that fixes the code or amends the spec, rather than by silently editing one side

#### Scenario: QA review itself is requested
- **WHEN** a contributor or agent runs a QA review
- **THEN** no OpenSpec proposal is required for the review itself, just as none is required for running the test suite
