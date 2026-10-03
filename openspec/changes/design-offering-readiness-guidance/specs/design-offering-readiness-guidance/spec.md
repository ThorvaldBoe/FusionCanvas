## ADDED Requirements

### Requirement: Design explains normalized Offering setup readiness

When Design has a selected normalized Blueprint Offering, FusionCanvas SHALL expose the current Offering-scoped catalog/mockup readiness summary and SHALL present its creator-facing issue guidance before the Item-specific color and artwork workflow. The summary SHALL remain distinct from Item artwork readiness.

#### Scenario: Design opens with incomplete Offering setup

- **WHEN** an editable Item uses a normalized Offering with missing active Variants, Design Areas, or Mockup Templates
- **THEN** Design shows that catalog setup is incomplete
- **AND** names each missing prerequisite with a next-step description
- **AND** keeps the existing color, slot, and artwork operations governed by their existing readiness rules

#### Scenario: Design opens with incomplete Mockup Templates

- **WHEN** an editable Item uses a normalized Offering whose active Mockup Templates are all Draft
- **THEN** Design shows that Mockup Templates need attention
- **AND** names each incomplete template and its current readiness blockers
- **AND** does not claim that the Item is ready for Listing or mockup output

#### Scenario: Design opens with a ready Mockup Template

- **WHEN** an editable Item uses a normalized Offering with active Variants, Design Areas, and at least one ready Mockup Template
- **THEN** Design shows the exact ready-template count and catalog/mockup-ready status
- **AND** separately continues to report Item-specific artwork or selected-color blockers when present

#### Scenario: Legacy or unavailable configuration has no normalized readiness projection

- **WHEN** Design uses a legacy-only configuration or cannot resolve a normalized Blueprint Offering
- **THEN** Design preserves its existing configuration, stale-state, and artwork guidance behavior
- **AND** does not fabricate normalized catalog counts or blockers

### Requirement: Design readiness guidance is read-only and accessible

The Design readiness summary SHALL be derived from the current load snapshot, SHALL reuse the authoritative Offering and Mockup Template readiness rules, SHALL not mutate persisted records, and SHALL be available as ordinary accessible text in both editable and read-only Design states.

#### Scenario: Protected Design remains non-mutating

- **WHEN** Design is read-only because the Item is protected or its configuration is stale
- **THEN** readiness guidance remains visible when available
- **AND** no readiness control enables catalog or Item mutation
