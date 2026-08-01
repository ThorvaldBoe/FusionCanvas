## MODIFIED Requirements

### Requirement: OpenSpec is the feature workflow
FusionCanvas SHALL use OpenSpec as the standard workflow and authoritative behavior source for significant feature behavior changes. GitHub Issues MAY record candidate work, reports, discussion, triage, priority, ownership, and delivery tracking, but SHALL NOT replace OpenSpec requirements, acceptance scenarios, design, implementation tasks, verification, or archive for a significant behavior change.

#### Scenario: Contributor starts a roadmap feature
- **WHEN** a contributor begins work on a significant roadmap feature
- **THEN** the contributor creates or continues an OpenSpec change before implementation begins
- **AND** a related GitHub Issue, when one exists, links to the change without replacing its requirements or acceptance scenarios

#### Scenario: Contributor makes a small maintenance change
- **WHEN** a contributor makes a small maintenance change that does not alter accepted feature behavior
- **THEN** the contributor may avoid a full OpenSpec proposal
- **AND** a separately tracked bug issue may remain the delivery record for that direct maintenance work
