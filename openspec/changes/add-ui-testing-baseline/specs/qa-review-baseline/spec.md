## ADDED Requirements

### Requirement: Full QA review includes comprehensive desktop UI regression
FusionCanvas SHALL include a real desktop UI regression task in every full QA review that covers all accepted and implemented user-facing features.

#### Scenario: Full QA review begins
- **WHEN** a contributor requests a full QA review
- **THEN** the reviewer inventories every accepted and implemented user-facing feature from the capability specifications
- **AND** creates a regression matrix in which every feature is assigned a pass, fail, blocked, or not-applicable result

#### Scenario: Full desktop UI regression executes
- **WHEN** the full QA desktop task exercises the current built application
- **THEN** it verifies each inventoried feature's primary end-to-end workflow
- **AND** covers applicable keyboard, pointer, focus or selection, validation, filtering, destructive confirmation, persistence or restart, recovery, accessibility, and tab or window behavior
- **AND** it uses isolated disposable application data

#### Scenario: Full desktop UI regression is reported
- **WHEN** the full QA review is complete
- **THEN** the consolidated report includes the complete feature matrix, tested build and environment, isolation method, scenario evidence, failures, blocked cases, and justified not-applicable entries
- **AND** no accepted implemented user-facing feature is silently omitted

#### Scenario: Desktop environment is unavailable
- **WHEN** the reviewer cannot access an interactive environment capable of launching the built desktop application
- **THEN** the full QA review reports the UI regression task as not applicable rather than passed
- **AND** identifies the unexecuted feature rows and required environment

### Requirement: Feature-scoped QA includes relevant desktop UI verification
FusionCanvas SHALL include targeted, risk-based real desktop UI verification when a QA review is scoped to a user-facing feature or to testing coverage for such a feature.

#### Scenario: Contributor requests feature QA
- **WHEN** a QA review targets one or more user-facing features without requesting the full review
- **THEN** the reviewer executes the critical and distinct high-risk desktop UI scenarios relevant to those features and reports the scoped feature matrix and selection rationale

#### Scenario: Contributor requests code-only testing review
- **WHEN** the requested scope explicitly excludes desktop UI verification
- **THEN** the report states that limitation and does not imply that the feature or full QA testing requirement has passed
