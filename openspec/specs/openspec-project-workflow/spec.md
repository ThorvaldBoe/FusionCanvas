# OpenSpec Project Workflow

## Purpose

Defines how FusionCanvas contributors use OpenSpec to move significant feature behavior from roadmap and PRD intent into reviewed specifications, implementation, validation, and archived project history.

## Requirements

### Requirement: OpenSpec is the feature workflow
FusionCanvas SHALL use OpenSpec as the standard workflow for significant feature behavior changes.

#### Scenario: Contributor starts a roadmap feature
- **WHEN** a contributor begins work on a significant roadmap feature
- **THEN** the contributor creates or continues an OpenSpec change before implementation begins

#### Scenario: Contributor makes a small maintenance change
- **WHEN** a contributor makes a small maintenance change that does not alter accepted feature behavior
- **THEN** the contributor may avoid a full OpenSpec proposal

### Requirement: Planning documents are source material
FusionCanvas SHALL treat roadmap and PRD documents as planning source material, not as accepted OpenSpec specifications.

#### Scenario: Contributor scopes a PRD item
- **WHEN** a contributor scopes a feature from `docs/lifeos/prd`
- **THEN** the contributor translates only the relevant product intent into OpenSpec change artifacts

#### Scenario: Contributor needs accepted behavior
- **WHEN** a contributor needs the current accepted behavior for a capability
- **THEN** the contributor uses `openspec/specs` as the durable source of truth

### Requirement: Change lifecycle is explicit
FusionCanvas SHALL use a change lifecycle of propose, review, apply, test, and archive for significant feature work.

#### Scenario: Change is proposed
- **WHEN** a significant feature change is created
- **THEN** the change includes proposal, design, specification, and implementation task artifacts required by the active OpenSpec schema

#### Scenario: Change is implemented
- **WHEN** implementation begins
- **THEN** the implementation follows the reviewed OpenSpec artifacts for the change

#### Scenario: Change is completed
- **WHEN** the implementation has been validated
- **THEN** the accepted behavior is archived or otherwise preserved through the OpenSpec workflow

### Requirement: Specifications define behavior and boundaries
FusionCanvas specifications SHALL describe behavior and scope well enough to guide implementation without becoming line-by-line implementation plans.

#### Scenario: Contributor writes a feature spec
- **WHEN** a contributor writes a feature specification
- **THEN** the specification includes requirements, scenarios or acceptance criteria, scope boundaries, and open questions where relevant

#### Scenario: Contributor reviews scope
- **WHEN** a contributor reviews an active change
- **THEN** the specification identifies what belongs in the change and what is out of scope

### Requirement: User-facing changes receive a UX preflight
FusionCanvas SHALL review user-facing changes against the shared UI and UX guidance before implementation.

#### Scenario: Contributor proposes a user-facing change
- **WHEN** a change adds or modifies a user-facing workflow
- **THEN** the proposal or design identifies the primary workflow, expected action frequency, appropriate interaction surface, and acceptable workspace footprint
- **AND** the design resolves progressive disclosure, relevant interaction states, selection, focus, unsaved changes, cancellation, and destructive actions before leaving those decisions to implementation

#### Scenario: Contributor proposes a non-user-facing change
- **WHEN** a change has no user-facing interaction
- **THEN** the change may mark the UX preflight as not applicable

### Requirement: Feedback-driven adjustments are captured
FusionCanvas SHALL capture user feedback that invalidates an implementation or design assumption while a change is active.

#### Scenario: Validation reveals an unplanned requirement or correction
- **WHEN** user validation reveals that an assumption, interaction, requirement, or implementation behavior must change
- **THEN** the contributor updates the relevant active specification, design, or tasks
- **AND** records the original assumption, observed problem, approved correction, applicability, classification, and potential promotion target in the change retrospective

#### Scenario: Validation reveals an ordinary implementation defect
- **WHEN** user validation reveals a defect without establishing a reusable product or engineering rule
- **THEN** the retrospective may classify it as an implementation defect
- **AND** the defect is not promoted into normative guidance solely because it occurred

### Requirement: Archive includes a learning review
FusionCanvas SHALL complete a learning review before archiving a significant change.

#### Scenario: Change contains reusable lessons
- **WHEN** the learning review identifies a reusable lesson
- **THEN** the contributor promotes it to the narrowest durable source of truth or records an explicit deferral with rationale
- **AND** preserves the detailed evidence in `retrospective.md` with the archived change

#### Scenario: Change contains no reusable lessons
- **WHEN** the learning review identifies no reusable lesson
- **THEN** `retrospective.md` explicitly records that result before archive

#### Scenario: Git history is unavailable or incomplete
- **WHEN** the learning review cannot reconstruct useful implementation history from Git
- **THEN** the contributor uses recorded feedback, artifact evolution that is available, and the final approved behavior
- **AND** does not infer lessons from a raw diff alone

### Requirement: Lessons have a durable promotion target
FusionCanvas SHALL route reusable knowledge to the narrowest authoritative project document.

#### Scenario: Contributor classifies a lesson
- **WHEN** a retrospective identifies reusable knowledge
- **THEN** capability behavior is promoted to its accepted OpenSpec specification
- **AND** interaction principles are promoted to UX guidance
- **AND** visual or layout rules are promoted to UI guidance
- **AND** structural engineering rules are promoted to architecture guidance
- **AND** OpenSpec process rules are promoted to the OpenSpec workflow specification or repository skill instructions
- **AND** change-specific rationale remains in the archived design and retrospective

### Requirement: Completed and superseded context is preserved
FusionCanvas SHALL preserve completed and superseded specification context instead of casually deleting it.

#### Scenario: Change is accepted
- **WHEN** a change is accepted and archived
- **THEN** the project preserves the accepted behavior and supporting change context for future contributors

#### Scenario: Specification is superseded
- **WHEN** a specification is superseded by later behavior
- **THEN** the project preserves enough context to understand the previous decision and migration path
