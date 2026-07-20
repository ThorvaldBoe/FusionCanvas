## ADDED Requirements

### Requirement: Assets support multiple context relationships
FusionCanvas SHALL allow an asset to carry more than one context link after import, SHALL validate each additional link against the active workspace, the asset's store, and the target context's active ancestry, and SHALL reject duplicate links to the same context with the same purpose.

#### Scenario: User adds a second context link to an existing asset
- **WHEN** the user adds a context link from an existing asset to a second active context in the same store
- **THEN** FusionCanvas creates the additional link atomically
- **AND** preserves the original link, managed file, and asset identity
- **AND** the asset appears in both context asset views

#### Scenario: User attempts a duplicate link
- **WHEN** the user adds a context link to a context the asset already links to with the same purpose
- **THEN** FusionCanvas rejects the duplicate link
- **AND** leaves existing links and the asset unchanged

#### Scenario: User attempts a cross-store link
- **WHEN** the user adds a context link to a context outside the asset's store
- **THEN** FusionCanvas rejects the link
- **AND** explains that assets may only link within their store

### Requirement: Asset relationships preserve per-context purpose
FusionCanvas SHALL label each context link with its own purpose, SHALL pre-select a purpose derived from the target context kind when adding a link, and SHALL allow the user to change a link's purpose independently of other links.

#### Scenario: Adding a design source link pre-selects source purpose
- **WHEN** the user adds a context link from an asset to an active design
- **THEN** FusionCanvas pre-selects a purpose appropriate to a design source or export
- **AND** lets the user confirm or change the purpose before completing the link

#### Scenario: User changes a per-context purpose
- **WHEN** the user changes the purpose of one context link on a multi-context asset
- **THEN** FusionCanvas persists the new purpose for that link atomically
- **AND** leaves other links' purposes unchanged

### Requirement: Asset relationships survive creative-record lifecycle
FusionCanvas SHALL preserve asset records and all unrelated context links when a concept or design is edited, superseded, rejected, or removed, and SHALL remove only the affected context link when a concept or design is removed.

#### Scenario: Concept is superseded
- **WHEN** a concept that has asset context links is superseded by a new concept version
- **THEN** the asset records, links, purposes, and managed files are unchanged
- **AND** the superseded concept's asset context view still shows the same assets

#### Scenario: Design is removed
- **WHEN** a design that has asset context links is removed
- **THEN** FusionCanvas removes the design's context links
- **AND** preserves the asset records and managed files
- **AND** the assets appear in the store-level asset view with an unlinked indicator when no context links remain

#### Scenario: Listing moves to another topic
- **WHEN** a listing whose concepts or designs have asset context links moves to another active topic in its store
- **THEN** the asset records, links, purposes, and managed files are unchanged
- **AND** the concept and design asset context views show the same assets after the move

### Requirement: Concept and design asset context views are surfaced
FusionCanvas SHALL provide focused asset surfaces for a selected concept or design that list the assets linked to that context with their per-context purposes and file state, alongside the existing listing, niche, group, and store asset surfaces.

#### Scenario: User reviews concept assets
- **WHEN** the user opens the asset surface for a concept
- **THEN** FusionCanvas shows the assets linked to that concept with per-context purposes and managed file names
- **AND** the managed workspace copy is the reference used for file state

#### Scenario: User reviews design assets
- **WHEN** the user opens the asset surface for a design
- **THEN** FusionCanvas shows the assets linked to that design with per-context purposes and managed file names

#### Scenario: Context has no assets
- **WHEN** the user opens the asset surface for a concept or design with no linked assets
- **THEN** FusionCanvas shows an empty state that explains assets can be linked
- **AND** keeps the link action available

### Requirement: Asset relationship operations persist atomically
FusionCanvas SHALL persist additional-link, per-context purpose relabel, and context-link removal operations as atomic snapshot operations in the active workspace database, reloading the latest snapshot before each mutation and saving once per operation.

#### Scenario: Workspace reloads after relationship operations
- **WHEN** a successful relationship operation is followed by an application or workspace database reload
- **THEN** the resulting context links, per-context purposes, and file references match the last successful persisted state

#### Scenario: Persistence fails during a relationship operation
- **WHEN** the repository cannot save a relationship operation after an optimistic projection
- **THEN** FusionCanvas reports a recoverable error
- **AND** restores the last confirmed asset, context link, and per-context purpose state
- **AND** retains recoverable user input needed to retry when applicable
