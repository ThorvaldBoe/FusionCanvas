## MODIFIED Requirements

### Requirement: Core domain entities are defined
FusionCanvas SHALL define Workspace, Store, Niche, Group, Listing, Asset, Prompt, and Tag as the Phase 0 and Phase 1 core organizational domain entities.

#### Scenario: Contributor reviews the core model
- **WHEN** a contributor inspects the domain model
- **THEN** Workspace, Store, Niche, Group, Listing, Asset, Prompt, and Tag are represented as named domain concepts
- **AND** the model does not require future concepts such as Concept, Design, Mockup, Marketplace Product, Performance Record, Plugin Data, or Workflow Template

### Requirement: Store is the top-level business context
A Store SHALL represent a business, brand, client, or publishing context inside a Workspace.

#### Scenario: Store owns workspace-local organization
- **WHEN** a store is represented in the domain model
- **THEN** it belongs to a workspace
- **AND** it can contain niches, listings, assets, prompts, and tags as store-scoped work
- **AND** it is not required to have a parent store or marketplace account

## ADDED Requirements

### Requirement: Workspace is the top-level organizational context
A Workspace SHALL represent the highest-level user-facing organizational scope for separating personal, client, portfolio, or project settings.

#### Scenario: Workspace owns store contexts
- **WHEN** a workspace is represented in the domain model
- **THEN** it can contain stores as workspace-scoped business contexts
- **AND** it is not required to have a marketplace account, cloud account, or user permission model

#### Scenario: Contributor explains the workspace hierarchy
- **WHEN** a contributor reviews the model
- **THEN** they can explain that workspaces contain stores, stores contain top-level niches, niches and groups form topic paths, and listings are item-like product concepts inside store topic context
