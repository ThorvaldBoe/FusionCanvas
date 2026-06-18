## ADDED Requirements

### Requirement: Core domain entities are defined
FusionCanvas SHALL define Store, Niche, Group, Listing, Asset, Prompt, and Tag as the Phase 0 core domain entities.

#### Scenario: Contributor reviews the core model
- **WHEN** a contributor inspects the domain model
- **THEN** Store, Niche, Group, Listing, Asset, Prompt, and Tag are represented as named domain concepts
- **AND** the model does not require future concepts such as Concept, Design, Mockup, Marketplace Product, Performance Record, Plugin Data, or Workflow Template

### Requirement: Store is the top-level business context
A Store SHALL represent the top-level business or brand context for Print-on-Demand work.

#### Scenario: Store owns workspace organization
- **WHEN** a store is represented in the domain model
- **THEN** it can contain niches, listings, assets, prompts, and tags as store-scoped work
- **AND** it is not required to have a parent store or marketplace account

### Requirement: Niches and groups are topic concepts
The model SHALL distinguish topic concepts from item concepts. A Niche SHALL represent a top-level topic area inside a Store, and a Group SHALL represent a flexible nested topic below a Niche or another Group.

#### Scenario: Contributor identifies topic entities
- **WHEN** a contributor classifies core domain entities
- **THEN** Niche is treated as a topic entity
- **AND** Group is treated as a topic entity
- **AND** Listing is not treated as a topic entity

#### Scenario: Nested group path is represented
- **WHEN** a group is represented in the domain model
- **THEN** it can belong under a niche or another group
- **AND** the model supports nested topic organization without requiring a fixed depth

### Requirement: Listing is the primary item and product concept
A Listing SHALL represent the primary item-like product concept that carries creative product work inside a store topic structure.

#### Scenario: Contributor identifies item entities
- **WHEN** a contributor classifies core domain entities
- **THEN** Listing is treated as the initial item entity
- **AND** Store, Niche, and Group are not treated as item entities

#### Scenario: Listing belongs to a store topic context
- **WHEN** a listing is represented in the domain model
- **THEN** it belongs to a store
- **AND** it can be associated with a niche or group topic context
- **AND** it is not required to be a published marketplace product

### Requirement: Assets connect resources to creative work
An Asset SHALL represent a connected file or external resource that can be associated with store-level or item-level creative work.

#### Scenario: Asset preserves resource identity
- **WHEN** an asset is represented in the domain model
- **THEN** it can describe the resource identity or reference needed to reconnect it later
- **AND** it can be associated with a store or listing context
- **AND** it does not require workspace file storage behavior to exist

### Requirement: Prompts preserve prompt-related context
A Prompt SHALL represent preserved prompt-related context for current or future AI-assisted workflows.

#### Scenario: Prompt is connected without executing AI
- **WHEN** a prompt is represented in the domain model
- **THEN** it can preserve prompt text or prompt-related context
- **AND** it can be associated with relevant store, topic, listing, or asset context
- **AND** it does not require an AI provider, execution result, token usage, or prompt library entry

### Requirement: Tags classify domain work flexibly
A Tag SHALL represent a flexible classification label that can be associated with core domain work.

#### Scenario: Tag is reusable across entity types
- **WHEN** a tag is represented in the domain model
- **THEN** it can classify more than one kind of core entity
- **AND** it is not limited to listings only
- **AND** it does not require marketplace keyword behavior

### Requirement: Core entity relationships are understandable
The domain model SHALL make the relationships between stores, niches, groups, listings, assets, prompts, and tags understandable to contributors.

#### Scenario: Contributor explains the relationship chain
- **WHEN** a contributor reviews the model
- **THEN** they can explain that stores contain top-level niches, niches and groups form topic paths, listings are item-like product concepts within store topic context, and assets, prompts, and tags preserve context around creative work

### Requirement: Core model avoids premature advanced entities
The Phase 0 core model SHALL avoid introducing advanced entities before their workflows are specified.

#### Scenario: Contributor inspects Phase 0 implementation scope
- **WHEN** a contributor reviews the FC-0002 implementation
- **THEN** it does not implement complete marketplace products, performance data, concept versioning, design versioning, mockup records, plugin data records, custom workflow models, or persistence mappings
