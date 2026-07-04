## ADDED Requirements

### Requirement: Tools resolve active workspace context
FusionCanvas SHALL provide a tool context that resolves the active store, active niche, current topic path, selected topic, selected item when applicable, and active workflow stage from explicit navigation and stage selection state.

#### Scenario: Resolve topic context
- **WHEN** a tool context is requested while a topic is selected
- **THEN** the resolved context includes the active store, active niche when available, selected topic, full parent topic path, and active workflow stage when available

#### Scenario: Resolve item context
- **WHEN** a tool context is requested while an item is selected
- **THEN** the resolved context includes the selected item, the item's parent topic, the full parent topic path, the active store, active niche when available, and active workflow stage

### Requirement: Tools receive inherited tags and metadata
FusionCanvas SHALL resolve tags and metadata inherited from the active store, niche, topic path, selected topic, and selected item so tools can use applicable context without recalculating parent relationships.

#### Scenario: Resolve inherited context for topic creation
- **WHEN** a topic-scoped creation tool requests context for a selected topic
- **THEN** the context includes inherited tags and metadata from the selected topic and its parent store, niche, and topic path

#### Scenario: Distinguish inherited and explicit values
- **WHEN** inherited tags or metadata are included in a tool context
- **THEN** the context identifies those values as inherited rather than explicit values assigned directly to newly created work

### Requirement: Topic-scoped tools create work in place
FusionCanvas SHALL default new work created from a selected topic to that topic and apply applicable inherited tags and metadata unless the user intentionally changes the target scope or creation values.

#### Scenario: Create new work from selected topic
- **WHEN** a topic-scoped tool creates a new item from a selected topic
- **THEN** the new item is placed under the selected topic by default
- **AND** applicable inherited tags and metadata are available to apply to the new item

#### Scenario: Override creation scope
- **WHEN** the user changes the creation scope before saving new work
- **THEN** FusionCanvas creates the work in the selected override scope instead of the original topic

### Requirement: Item-bound tools require selected item context
FusionCanvas SHALL allow tools to declare that they require a selected item and SHALL report a clear unavailable state when only a topic or broader workspace context is selected.

#### Scenario: Item-bound tool receives item
- **WHEN** an item-bound tool is opened while an item is selected
- **THEN** the tool receives the selected item and its full parent store, niche, topic, workflow stage, tag, and metadata context

#### Scenario: Item-bound tool without item
- **WHEN** an item-bound tool is requested while no item is selected
- **THEN** FusionCanvas reports that the tool requires an item context
- **AND** FusionCanvas does not run item-bound actions against only a topic context

### Requirement: Tools can inspect nearby work summaries
FusionCanvas SHALL provide bounded summaries of nearby sibling topics, sibling items, and relevant accepted, created, rejected, or archived work when that context is available for the requested scope.

#### Scenario: Include sibling work for duplicate avoidance
- **WHEN** a generative or creation tool requests context for a selected topic
- **THEN** the context can include existing sibling items in the selected topic so the tool can avoid obvious duplication

#### Scenario: Include rejected work as negative guidance
- **WHEN** rejected or archived work is relevant to the requested context
- **THEN** the context can include bounded summaries of that work so tools can use it as negative guidance without treating it as active work

### Requirement: Tool scope is visible and adjustable
FusionCanvas SHALL expose the scope being used by a context-aware tool and SHALL allow the user to intentionally change scope when a tool supports more than one scope.

#### Scenario: Display tool scope
- **WHEN** a context-aware tool is displayed
- **THEN** the user can understand whether the tool is using the current item, current topic, parent topic path, niche, store, subtree, or another supported scope

#### Scenario: Change supported scope
- **WHEN** a tool supports changing scope and the user selects a different supported scope
- **THEN** FusionCanvas resolves a new tool context for that selected scope
- **AND** subsequent tool actions use the updated context

### Requirement: Context-aware behavior is cross-cutting
FusionCanvas SHALL treat context-aware behavior as a shared foundation for manual, generative, asset, listing, and automation workflows rather than as behavior limited to AI tools.

#### Scenario: Manual creation uses context
- **WHEN** a user manually creates work from a selected topic or item workflow
- **THEN** FusionCanvas uses the active context for default placement and applicable inherited values

#### Scenario: Generative workflow uses context
- **WHEN** a future generative tool creates or refines work
- **THEN** FusionCanvas provides the active context so the tool can target the current store, niche, topic path, item, stage, metadata, tags, and nearby work
