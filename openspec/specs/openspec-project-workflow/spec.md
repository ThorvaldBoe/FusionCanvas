# OpenSpec Project Workflow

## Purpose

Defines how FusionCanvas contributors discover and deliver one cohesive feature module at a time through reviewed specifications, implementation-ready guidance, acceptance verification, learning, and archived project history.

## Requirements

### Requirement: Detailed planning advances one delivery module at a time
FusionCanvas SHALL limit detailed feature planning to the next delivery module, where a delivery module is a cohesive and independently verifiable set of features rather than a fixed feature count or code-architecture boundary.

#### Scenario: Contributor defines the next module
- **WHEN** the project is ready to plan more feature behavior
- **THEN** the contributor defines one module outcome, included feature set, dependencies, boundaries, risks, and verification approach
- **AND** explains why the scope is cohesive and reviewable

#### Scenario: Proposed module is too broad
- **WHEN** a proposed module contains independent outcomes, unresolved high-impact decisions, or verification scope too large to diagnose efficiently
- **THEN** the contributor splits or reduces the module before implementation artifacts are approved

#### Scenario: Several small features share delivery cost
- **WHEN** several small features share the same outcome, data model, surface, fixture, and verification pass
- **THEN** they may be grouped when the proposal explains how grouping reduces overhead without hiding unrelated work

### Requirement: Module understanding is established before implementation
FusionCanvas SHALL use collaborative discovery and review to establish shared understanding of a delivery module before implementation begins.

#### Scenario: Module behavior is being discovered
- **WHEN** the human and planning agent define a module
- **THEN** they resolve or record goals, examples, non-goals, edge cases, assumptions, dependencies, and important product or architecture questions
- **AND** the resulting decisions are captured in the change artifacts

#### Scenario: High-impact ambiguity remains
- **WHEN** a product, interaction, data, architecture, or acceptance decision could materially change the implementation
- **THEN** implementation does not begin until the decision is resolved or the user explicitly delegates it

### Requirement: Agent work is assigned by capability and bounded by the delivery package
FusionCanvas SHALL assign module tasks according to the reasoning, implementation, and verification capabilities they require rather than assuming every agent or model is interchangeable.

#### Scenario: Bounded implementation is delegated
- **WHEN** a lower-cost implementation agent receives module work
- **THEN** it receives the approved change name, artifact set, task scope, validation commands, scope prohibitions, and ambiguity escalation conditions

#### Scenario: Implementation exposes a missing decision
- **WHEN** the implementation agent finds an ambiguity that would require a new product, architecture, or acceptance decision
- **THEN** it stops the affected task and returns the ambiguity for higher-reasoning review instead of guessing

#### Scenario: High-judgment work is assigned
- **WHEN** work consists of module discovery, specification, design review, ambiguous correction, or final acceptance review
- **THEN** it is assigned to a human or agent with sufficient reasoning capability for that work

### Requirement: Acceptance criteria are completion gates
FusionCanvas SHALL treat approved acceptance criteria as traceable pass/fail completion gates for a delivery module.

#### Scenario: Implementation readiness is reviewed
- **WHEN** a module is about to enter implementation
- **THEN** every requirement has observable acceptance scenarios
- **AND** every scenario is mapped to a planned verification method or an explicit not-applicable rationale

#### Scenario: Acceptance criterion fails
- **WHEN** verification shows that an acceptance criterion is not met
- **THEN** the module returns to implementation or artifact correction
- **AND** the affected criterion and relevant regression checks are rerun until they pass or the user approves a specification change

#### Scenario: Module is reported complete
- **WHEN** a contributor reports a delivery module complete
- **THEN** verification evidence accounts for every acceptance criterion, required validation command, limitation, and deferred environment-dependent check

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

### Requirement: Planning documents are source material
FusionCanvas SHALL treat historical roadmaps and PRD documents as optional idea sources, not as required context, current plans, or accepted OpenSpec specifications.

#### Scenario: Contributor explores a future module
- **WHEN** a contributor needs additional product ideas or historical rationale
- **THEN** the contributor may consult `docs/LifeOS` and clearly revalidates any useful idea against current user intent, accepted specs, and application state

#### Scenario: Contributor defines current scope
- **WHEN** a contributor scopes or implements a delivery module
- **THEN** the contributor does not infer current priority, feature ordering, requirements, or acceptance criteria from historical LifeOS documents

#### Scenario: Contributor needs accepted behavior
- **WHEN** a contributor needs the current accepted behavior for a capability
- **THEN** the contributor uses `openspec/specs` as the durable source of truth

### Requirement: Change lifecycle is explicit
FusionCanvas SHALL use a delivery lifecycle of discover, define module, propose, review, apply, verify, learn, and archive for significant feature work.

#### Scenario: Module is proposed
- **WHEN** a significant feature module is created
- **THEN** the change includes a module definition, proposal, conceptual and functional design, delta specifications with acceptance scenarios, a detailed implementation plan, implementation tasks, and a planned acceptance-evidence mapping

#### Scenario: Module enters implementation
- **WHEN** the user approves the delivery package or explicitly delegates approval
- **THEN** implementation follows the reviewed artifacts and stays within the approved module boundaries

#### Scenario: Module is completed
- **WHEN** all acceptance criteria and required validation gates have passed
- **THEN** the contributor records verification and learning evidence before accepted behavior is archived or otherwise preserved through the OpenSpec workflow

### Requirement: Specifications define behavior and boundaries
FusionCanvas delivery packages SHALL separate durable requirements and conceptual or functional design from a detailed, change-specific implementation plan.

#### Scenario: Contributor writes module requirements
- **WHEN** a contributor specifies a delivery module
- **THEN** the delta specifications define observable requirements, acceptance scenarios, scope boundaries, dependencies, and unresolved questions without depending on particular source files or types

#### Scenario: Contributor prepares implementation guidance
- **WHEN** the module will be implemented by an agent
- **THEN** `design.md` contains a dedicated implementation plan identifying affected layers and responsibilities, data and UI behavior where relevant, edge cases, sequencing, test locations, migration needs, and decisions the implementer must not reopen
- **AND** `tasks.md` decomposes that plan into ordered, bounded, verifiable steps

#### Scenario: Implementation details change without behavior change
- **WHEN** current type or file choices in the implementation plan become obsolete without changing accepted behavior
- **THEN** the active design and tasks may be corrected without rewriting the behavior requirements

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
