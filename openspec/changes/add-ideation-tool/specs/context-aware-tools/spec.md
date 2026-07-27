## ADDED Requirements

### Requirement: Ideation context includes scoped active and rejected ideas
FusionCanvas SHALL assemble Ideation context from the resolved active store and niche, the exact selected or parent group when present, every active Item Idea in the applicable scope, and every applicable rejected Idea with optional reasoning.

#### Scenario: Ideation runs for a selected group
- **WHEN** Ideation resolves to a selected or parent group
- **THEN** approved context includes every non-archived, non-Rejected Item directly in that exact group
- **AND** rejected context includes Item Ideas with Rejected lifecycle status and recorded ideation rejections associated with that exact group
- **AND** Items from child, sibling, and parent groups are excluded

#### Scenario: Ideation runs without a selected group
- **WHEN** Ideation resolves to an active niche but no group
- **THEN** approved context includes every non-archived, non-Rejected Item at the niche root and throughout that niche's groups
- **AND** rejected context includes Rejected Item Ideas and recorded ideation rejections throughout that niche
- **AND** work from other niches is excluded

#### Scenario: Active Item has no Idea text
- **WHEN** an otherwise applicable active Item has no non-whitespace original Idea
- **THEN** it contributes no fabricated Idea text to generation context

#### Scenario: Recorded rejection has no reason
- **WHEN** an applicable ideation rejection has no reason
- **THEN** its rejected Idea text remains in negative context
- **AND** no reason is fabricated

### Requirement: Ideation sends creative context without operational or secret data
FusionCanvas SHALL send the fake generator all applicable user-authored names, descriptions, and metadata for the active store, niche, and optional group together with guidance, mode, selected Snowclone template, active Idea text, and rejected Idea text/reasons, and MUST exclude credentials and operational fields that do not support creative generation.

#### Scenario: Generation payload is assembled
- **WHEN** an Ideation generation request is created
- **THEN** it contains the applicable user-authored store, niche, and optional group fields and metadata
- **AND** it contains the user's guidance, selected mode or template, active Ideas, and rejected Ideas with available reasoning

#### Scenario: Operational fields exist
- **WHEN** source entities contain identifiers, timestamps, archive flags, file paths, internal provenance, or placeholder API access
- **THEN** those fields are excluded from the generator request
- **AND** no credential value is exposed through generation diagnostics
