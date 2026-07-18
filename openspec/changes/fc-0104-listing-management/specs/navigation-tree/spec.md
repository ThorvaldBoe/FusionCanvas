## MODIFIED Requirements

### Requirement: Navigation tree represents workspace hierarchy
FusionCanvas SHALL represent workspace navigation as a hierarchy of stores, topics, and items, including listings that are placed directly under a store or inside a niche or group topic.

#### Scenario: User browses store hierarchy
- **WHEN** a workspace contains a store with store-level listing items, niche topics, nested group topics, and topic-level listing items
- **THEN** the navigation tree exposes the store as a top-level context
- **AND** the tree exposes store-level listings directly under the store
- **AND** the tree exposes niches as top-level topics inside the store
- **AND** the tree exposes groups as nested topics
- **AND** the tree exposes topic-level listings as items inside their assigned niches or groups

#### Scenario: Topic contains nested content
- **WHEN** a topic contains child topics and listing items
- **THEN** the navigation tree preserves both child topic entries and listing item entries under that topic

### Requirement: Navigation movement preserves context
FusionCanvas SHALL support moving topics and items within valid navigation destinations while preserving contained content and item context, and SHALL allow listings to move directly under their existing store or under a niche or group in that store.

#### Scenario: User moves a topic
- **WHEN** a topic with child topics and listing items is moved to another valid topic destination
- **THEN** the moved topic retains its child topic subtree
- **AND** the moved topic retains its listing items

#### Scenario: User moves a listing item to a topic
- **WHEN** a listing item is moved to a valid niche or group in its existing store
- **THEN** the listing remains the same work item
- **AND** its listing context, status, notes, tags, and related assets remain associated with that listing

#### Scenario: User moves a listing item to its store
- **WHEN** a listing item is moved directly under its existing store
- **THEN** the listing remains the same work item
- **AND** no niche or group is assigned to it
- **AND** its listing context, status, notes, tags, and related assets remain associated with that listing

#### Scenario: User attempts invalid movement
- **WHEN** a move would place a topic outside a valid store/topic hierarchy or place an item outside its existing store
- **THEN** FusionCanvas rejects the move
- **AND** the existing navigation hierarchy remains unchanged

### Requirement: Navigation tree tracks active context
FusionCanvas SHALL expose a single active navigation context that can be used by creation, generation, and editing workflows.

#### Scenario: User selects a topic
- **WHEN** a user selects a topic in the navigation tree
- **THEN** that topic becomes the active navigation context
- **AND** new topic-scoped work defaults to that topic unless a workflow chooses a more specific destination

#### Scenario: User selects a topic-level listing
- **WHEN** a user selects a listing item assigned to a niche or group in the navigation tree
- **THEN** that listing becomes the active navigation context
- **AND** workflows can resolve the listing's parent topic and store

#### Scenario: User selects a store-level listing
- **WHEN** a user selects a listing item placed directly under a store in the navigation tree
- **THEN** that listing becomes the active navigation context
- **AND** workflows can resolve the listing's store without fabricating a parent topic
