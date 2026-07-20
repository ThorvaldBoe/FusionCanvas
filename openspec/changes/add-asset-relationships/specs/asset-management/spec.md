## MODIFIED Requirements

### Requirement: Assets attach to one active workspace context
FusionCanvas SHALL attach each imported asset to at least one context: a listing, niche, group, store, concept, or design in the active workspace, SHALL validate that the target exists, belongs to the asset's store, and is active including its ancestor chain before import, and SHALL allow additional context links to be added after import through the asset-relationships capability.

#### Scenario: User imports reference material to a broader context
- **WHEN** the user imports a file for an active niche, group, or store
- **THEN** FusionCanvas creates the asset owned by that store and links it to the chosen context

#### Scenario: User imports an asset for a concept or design
- **WHEN** the user imports a file for an active concept or design in the active workspace
- **THEN** FusionCanvas creates the asset owned by the concept or design's store and links it to that concept or design
- **AND** pre-selects a purpose appropriate to the target context kind

#### Scenario: Target context is archived or unavailable
- **WHEN** the requested target is archived, missing, or hidden by an archived ancestor
- **THEN** FusionCanvas blocks the import with actionable guidance
- **AND** creates no asset record, link, or managed copy

### Requirement: Context assets are visible from the relevant work
FusionCanvas SHALL provide a focused asset surface for a selected listing, niche, group, store, concept, or design that lists the assets linked to that context with their names, purposes, and file state, and SHALL list every store-owned asset, including unlinked assets, in the store-level view.

#### Scenario: User reviews listing assets
- **WHEN** the user opens the asset surface for a listing
- **THEN** FusionCanvas shows the assets linked to that listing with purpose labels and managed file names
- **AND** the managed workspace copy is the reference used for file state

#### Scenario: User reviews concept or design assets
- **WHEN** the user opens the asset surface for a concept or design
- **THEN** FusionCanvas shows the assets linked to that concept or design with per-context purpose labels and managed file names

#### Scenario: Store view shows all store assets
- **WHEN** the user opens the asset surface for a store
- **THEN** FusionCanvas lists every asset owned by that store
- **AND** labels each asset with its linked contexts or an unlinked indicator

#### Scenario: Context has no assets
- **WHEN** the user opens the asset surface for a context with no linked assets
- **THEN** FusionCanvas shows an empty state that explains assets can be imported
- **AND** keeps the import action available
