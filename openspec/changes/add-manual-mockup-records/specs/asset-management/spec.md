## MODIFIED Requirements

### Requirement: Assets attach to one active workspace context
FusionCanvas SHALL attach each imported asset to at least one context: a listing, niche, group, store, concept, design, or mockup in the active workspace, SHALL validate that the target exists, belongs to the asset's store, and is active including its ancestor chain before import, and SHALL allow additional context links to be added after import through the asset-relationships capability.

#### Scenario: User imports reference material to a broader context
- **WHEN** the user imports a file for an active niche, group, or store
- **THEN** FusionCanvas creates the asset owned by that store and links it to the chosen context

#### Scenario: User imports an asset for a concept, design, or mockup
- **WHEN** the user imports a file for an active concept, design, or mockup in the active workspace
- **THEN** FusionCanvas creates the asset owned by the concept, design, or mockup's store and links it to that context
- **AND** pre-selects a purpose appropriate to the target context kind

#### Scenario: Target context is archived or unavailable
- **WHEN** the requested target is archived, missing, or hidden by an archived ancestor
- **THEN** FusionCanvas blocks the import with actionable guidance
- **AND** creates no asset record, link, or managed copy
