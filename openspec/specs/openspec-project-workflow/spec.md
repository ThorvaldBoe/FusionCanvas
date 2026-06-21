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

### Requirement: Completed and superseded context is preserved
FusionCanvas SHALL preserve completed and superseded specification context instead of casually deleting it.

#### Scenario: Change is accepted
- **WHEN** a change is accepted and archived
- **THEN** the project preserves the accepted behavior and supporting change context for future contributors

#### Scenario: Specification is superseded
- **WHEN** a specification is superseded by later behavior
- **THEN** the project preserves enough context to understand the previous decision and migration path
