## ADDED Requirements

### Requirement: GitHub Issues provide structured work intake
FusionCanvas SHALL use GitHub Issues for externally reported bugs and feature requests, separately tracked internal bugs, and high-level planned features. The repository SHALL provide required Bug Report and Feature Request Issue Forms, SHALL disable blank issues, and SHALL warn reporters not to submit credentials, personal information, private workspace data, or other sensitive content.

#### Scenario: External contributor reports a bug
- **WHEN** an external contributor selects the Bug Report form
- **THEN** the form requires version or commit, operating system, expected behavior, actual behavior, reproduction steps, and frequency information
- **AND** the created issue receives `type: bug` and `status: needs-triage` labels

#### Scenario: External contributor requests a feature
- **WHEN** an external contributor selects the Feature Request form
- **THEN** the form requires the problem, current workaround, desired outcome, affected product area, and alternatives
- **AND** the created issue receives `type: feature` and `status: needs-triage` labels

#### Scenario: Reporter needs to disclose sensitive information
- **WHEN** a reporter opens either Issue Form
- **THEN** the form warns that public issues must not contain credentials, personal information, private workspace data, or other sensitive material
- **AND** contributor guidance directs security-sensitive reports away from public issues

### Requirement: Issues are triaged with a small taxonomy
FusionCanvas SHALL give every active tracked issue exactly one type label and one workflow-state label. The type SHALL be `type: bug` or `type: feature`; the workflow state SHALL be one of `status: needs-triage`, `status: needs-information`, `status: accepted`, `status: in-progress`, `status: blocked`, or `status: declined`. Maintainers MAY add one priority label from `priority: next`, `priority: soon`, or `priority: backlog` when prioritization is useful.

#### Scenario: Maintainer triages a new issue
- **WHEN** a maintainer reviews a new issue
- **THEN** the maintainer classifies its type and workflow state, requests information, identifies a duplicate, or accepts or declines it
- **AND** the issue retains no competing label in the same type or workflow-state category

#### Scenario: Issue is completed or cannot proceed
- **WHEN** an issue is completed, duplicate, declined, or cannot be reproduced after reasonable follow-up
- **THEN** the maintainer closes it with a concise reason
- **AND** a duplicate links to its canonical issue when one exists

### Requirement: Issues and OpenSpec have distinct authority
FusionCanvas SHALL treat GitHub Issues as the authority for report origin, discussion, triage, priority, ownership, and delivery tracking. FusionCanvas SHALL treat OpenSpec as the authority for approved significant product behavior, acceptance scenarios, design, implementation tasks, verification, and archived product history.

#### Scenario: Issue description conflicts with a promoted specification
- **WHEN** an issue has been promoted to an OpenSpec change and its text conflicts with the change artifacts
- **THEN** the OpenSpec change governs behavior and acceptance criteria
- **AND** the issue is updated with a link to the change rather than used as an alternative specification

### Requirement: Issue work is promoted and linked predictably
FusionCanvas SHALL promote an accepted high-level feature to the existing OpenSpec lifecycle when it is selected as the next delivery module. The promoted change's `proposal.md` SHALL contain an `## Origin` section with the primary issue number and URL, and the issue SHALL link to the OpenSpec change. One independently deliverable module SHALL have one primary issue; a broad issue split into independently deliverable modules SHALL remain a tracking issue with linked child primary issues.

#### Scenario: Accepted feature becomes the next module
- **WHEN** maintainers select an accepted high-level feature for detailed delivery
- **THEN** they use OpenSpec discovery, proposal, implementation, verification, and archive before implementation
- **AND** the issue and change link to one another

#### Scenario: Broad feature requires independent modules
- **WHEN** an accepted issue contains independently deliverable outcomes
- **THEN** maintainers retain it as a tracking issue and create linked child issues
- **AND** each child issue owns the OpenSpec change and pull request for its independently deliverable module

### Requirement: Direct maintenance bugs remain traceable
FusionCanvas SHALL allow a confirmed bug that only restores accepted behavior to be implemented directly from its issue without a dedicated OpenSpec proposal. The pull request SHALL link the issue, include focused regression coverage where practical, and use a closing keyword.

#### Scenario: Existing accepted behavior regresses
- **WHEN** a bug issue describes an implementation that contradicts accepted behavior and the correction introduces no new behavior decision
- **THEN** maintainers may fix it directly from the issue
- **AND** the merged pull request closes the issue without creating a separate OpenSpec change

### Requirement: Pull requests close primary issues without replacing OpenSpec completion
FusionCanvas work branches SHALL use `codex/<issue-number>-<slug>`. Pull requests SHALL link the relevant primary issue, identify the OpenSpec change when one exists, and use `Closes #<issue-number>` so merging closes the issue. OpenSpec verification and archive SHALL remain required for a promoted significant-behavior change regardless of issue closure.

#### Scenario: Pull request implements a promoted module
- **WHEN** a pull request implements an OpenSpec change with a primary issue
- **THEN** the pull request identifies the change and uses `Closes #<issue-number>`
- **AND** the issue closes only on merge while the change remains subject to its OpenSpec verification and archive gates
