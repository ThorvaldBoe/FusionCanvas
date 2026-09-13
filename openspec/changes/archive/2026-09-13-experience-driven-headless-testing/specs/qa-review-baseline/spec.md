## ADDED Requirements

### Requirement: Completion QA reviews user-job evidence
FusionCanvas completion QA SHALL evaluate whether changed user jobs are protected at the appropriate layers and whether critical cross-seam outcomes have truthful headless experience evidence.

#### Scenario: Completion QA reviews a user-facing module
- **WHEN** a module adds or changes a user-facing workflow
- **THEN** the reviewer checks the affected user-job inventory and criterion-level evidence
- **AND** confirms that critical journeys exercise rendered controls and observable outcomes without bypassing material UI seams
- **AND** confirms persisted outcomes include fresh-instance re-entry evidence when applicable

#### Scenario: Completion QA finds component-only evidence for a critical journey
- **WHEN** bindings, commands, or persistence are individually tested but the accepted critical outcome crosses those seams without a complete journey
- **THEN** the reviewer reports an experience-coverage gap
- **AND** the module returns to correction or records an approved not-applicable rationale before completion

### Requirement: QA reviews defect escape learning
FusionCanvas QA SHALL require escaped defects to include concise cause analysis, regression evidence, and a proportionate decision about broader prevention.

#### Scenario: QA reviews a defect correction
- **WHEN** a fix addresses a defect discovered through manual testing, optional desktop testing, review, or use after earlier automated tests passed
- **THEN** the reviewer verifies the regression test or specific automation limitation
- **AND** verifies that the escape cause, similar-risk check, and local-versus-global prevention decision are recorded

#### Scenario: A proposed global lesson is disproportionate
- **WHEN** an isolated defect does not demonstrate a recurring or cross-surface escape mechanism
- **THEN** QA retains the focused regression
- **AND** does not require a global rule, duplicate tests, or broad suite expansion

### Requirement: Headless UI QA evaluates journey quality and suite health
FusionCanvas QA SHALL evaluate semantic experience coverage, determinism, isolation, failure diagnostics, execution time, and flakiness rather than using raw test count or percentage coverage as the headless UI verdict.

#### Scenario: QA-6 reviews the headless suite
- **WHEN** the headless UI coverage task executes
- **THEN** the reviewer maps meaningful surface jobs to focused and journey evidence
- **AND** identifies journeys that bypass rendered interaction, rely on arbitrary sleeps, share mutable persistent state, assert only implementation details, or duplicate lower-layer variants
- **AND** reports missing or misleading experience evidence as findings

#### Scenario: QA evaluates strategy effectiveness
- **WHEN** a delivery-module retrospective or full QA review evaluates the testing strategy
- **THEN** it considers escaped UI defects, repeated escape categories, flake rate, deterministic baseline duration, and failure-localization quality
- **AND** treats test count and line coverage as diagnostic signals rather than success gates

