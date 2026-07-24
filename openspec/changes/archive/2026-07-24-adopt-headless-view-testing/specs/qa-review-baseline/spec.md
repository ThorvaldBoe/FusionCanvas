## MODIFIED Requirements

### Requirement: Delivery modules receive scoped completion QA
FusionCanvas SHALL perform a completion review proportionate to each delivery module without requiring the full repository QA suite after every module.

#### Scenario: Module completion is reviewed
- **WHEN** a contributor prepares to report a module complete
- **THEN** the review checks build and deterministic tests, strict OpenSpec validation, acceptance-evidence completeness, specification drift within the changed scope, and relevant architecture, security, persistence, or UI risks

#### Scenario: Module changes user-facing behavior
- **WHEN** the completed module contains user-facing changes
- **THEN** the completion review checks focused UI decision-logic tests and applicable Avalonia headless view tests
- **AND** live desktop evidence is optional supplemental evidence rather than a completion gate

#### Scenario: Module has broad cross-cutting risk
- **WHEN** the changed scope can plausibly regress unrelated accepted capabilities
- **THEN** the reviewer expands the relevant deterministic QA tasks or recommends a full QA review rather than relying only on module-scoped checks

### Requirement: QA reviews verify the testing baseline
FusionCanvas QA reviews SHALL confirm that the automated testing baseline passes and that behavior is protected by focused tests as defined by the testing baseline specification.

#### Scenario: Testing review executes
- **WHEN** the testing QA task executes
- **THEN** the solution-level baseline suite is run and its result reported
- **AND** behavior in domain, application, integration, and app layers is checked for corresponding focused tests
- **AND** user-facing views are checked for applicable Avalonia headless coverage of meaningful framework behavior

#### Scenario: Coverage gaps are found
- **WHEN** the testing review identifies behavior without adequate tests
- **THEN** the gaps are reported with severity and routed to test additions or to OpenSpec clarification when the behavior itself is unclear

## ADDED Requirements

### Requirement: Full QA remains executable without an interactive desktop
FusionCanvas SHALL keep every mandatory full-QA task executable by contributors and agents without an interactive desktop environment.

#### Scenario: Contributor requests a full QA review
- **WHEN** a full QA review is executed by Codex, OpenCode, or a human contributor
- **THEN** all mandatory QA tasks can complete using repository inspection and deterministic commands
- **AND** applicable Avalonia headless view tests run as part of the automated testing lane

#### Scenario: Reviewer elects to run the desktop application
- **WHEN** a reviewer performs an ad hoc live desktop check for additional confidence
- **THEN** the check is reported as optional supplemental evidence
- **AND** its omission or environment unavailability does not change the full-QA verdict
