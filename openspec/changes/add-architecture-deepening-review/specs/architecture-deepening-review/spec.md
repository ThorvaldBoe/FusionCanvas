## ADDED Requirements

### Requirement: Factory supports a scoped architecture deepening review

The Software Factory SHALL provide a review procedure for discovering and evaluating opportunities to deepen shallow modules within an explicitly recorded scope. The procedure SHALL be available for a changed area, selected architecture hotspot, or user-named subsystem, and SHALL remain separate from product behavior, audit certification, and mandatory remediation.

#### Scenario: Review starts from an explicit scope

- **WHEN** an architecture deepening review begins
- **THEN** the review record identifies the requested scope, source revision or working-tree state, review owner, applicable architecture guidance, and reason the scope was selected

#### Scenario: Review does not become an additional audit axis

- **WHEN** a review produces an architecture improvement candidate
- **THEN** the candidate is recorded as an improvement recommendation and does not change an audit result, certification status, source-extract assessment, or audit-axis scope by itself

### Requirement: Review uses deep-module vocabulary and evidence

The procedure SHALL define and use the terms module, interface, seam, adapter, depth, leverage, locality, deletion test, and interface-as-test-surface. The review SHALL describe the behavioral interface, including relevant invariants, ordering constraints, error modes, cancellation or lifecycle expectations, and resource ownership, rather than treating a type signature as the complete interface.

#### Scenario: Candidate describes a shallow module

- **WHEN** a candidate is proposed
- **THEN** the record identifies the current interface, the implementation or complexity leaked to callers, the callers affected, and the concrete maintenance, coupling, or testing harm

#### Scenario: Candidate applies the deletion test

- **WHEN** a module is considered shallow
- **THEN** the record states whether deleting the module would concentrate complexity in one deeper owner or merely move the same complexity among callers

#### Scenario: Candidate identifies leverage and locality

- **WHEN** the review recommends deepening a module
- **THEN** the record explains what additional behavior becomes available through the proposed interface and where future changes, defects, and verification would be localized

### Requirement: Review applies earned-seam and test-surface discipline

The procedure SHALL require the reviewer to classify dependencies as in-process, locally substitutable, remotely owned, or truly external when that classification affects seam placement. It SHALL not require a new interface solely because a class exists. A proposed seam SHALL identify the real variation, I/O, lifecycle, or testing reason it earns an abstraction, and tests SHALL be planned against the highest stable interface that exposes the accepted behavior.

#### Scenario: Pure in-process logic is deepened

- **WHEN** a candidate contains only deterministic in-process dependencies
- **THEN** the review may recommend a deeper concrete module without adding a port or adapter solely for testing

#### Scenario: External or volatile dependency crosses the seam

- **WHEN** a candidate crosses a persistence, filesystem, network, provider, or other volatile dependency
- **THEN** the review identifies the inward-owned contract, production adapter, test adapter or substitute, and failure or cancellation semantics required at that seam

#### Scenario: Interface is the test surface

- **WHEN** tests are planned for a deepened module
- **THEN** the primary tests assert observable outcomes through the proposed interface and do not require access to internal implementation state

### Requirement: Review separates candidate selection from implementation

The procedure SHALL present one or more candidates with their trade-offs before implementation design is committed. A selected candidate SHALL receive an explicit design decision, and materially important interfaces SHALL have at least two considered shapes or alternatives before one is chosen. The procedure SHALL not prescribe code changes merely because a candidate was discovered.

#### Scenario: User or reviewer selects a candidate

- **WHEN** multiple deepening candidates are identified
- **THEN** the review records which candidate was selected, deferred, or rejected and why

#### Scenario: Important interface is designed twice

- **WHEN** the selected candidate introduces or materially reshapes a cross-layer, plugin, provider, or other high-impact interface
- **THEN** the design record compares at least two credible interface shapes by depth, leverage, locality, seam placement, dependency strategy, and testability

#### Scenario: Candidate is rejected without implementation

- **WHEN** the deletion test, risk, or expected leverage does not justify deepening
- **THEN** the review records the rejection or deferral and leaves the existing code and audit result unchanged

### Requirement: Review hands accepted work into the authoritative workflow

The procedure SHALL route an accepted behavior change or public architectural contract change into the repository's OpenSpec workflow before implementation. Internal maintenance work that does not change accepted behavior MAY use the repository's maintenance path, but it SHALL still identify affected tests and the required post-change architecture verification. Review records SHALL preserve provenance and SHALL not replace OpenSpec requirements, design, tasks, verification, or Software Factory audit evidence.

#### Scenario: Deepening changes accepted behavior

- **WHEN** implementing the selected deepening candidate would change user-visible behavior, a public extension contract, persistence semantics, or another accepted requirement
- **THEN** the review links or creates the corresponding OpenSpec change before implementation begins

#### Scenario: Deepening is an internal refactor

- **WHEN** the selected deepening candidate preserves accepted behavior and public contracts
- **THEN** the review records the maintenance classification, focused regression evidence, and affected architecture revalidation without creating a new product requirement

#### Scenario: Audit evidence remains authoritative

- **WHEN** an architecture audit and a deepening review concern the same types
- **THEN** the review links to the relevant audit evidence while leaving the append-only source extract, assessments, findings, challenges, and certification records unchanged
