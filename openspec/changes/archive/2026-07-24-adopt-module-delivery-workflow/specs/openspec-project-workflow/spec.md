## ADDED Requirements

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

## MODIFIED Requirements

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
