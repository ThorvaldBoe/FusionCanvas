# Stage Tool Host

## Purpose

Defines how FusionCanvas hosts workflow-stage-specific tools in the document window, resolves available tools from the active context, and keeps built-in defaults available as future contributed tools are added.

## Requirements

### Requirement: Document window hosts stage tools
The application SHALL provide a Stage Tool Host in the document window detail area that displays the active tool for the current workflow stage and navigation context.

#### Scenario: Host displays stage tool
- **WHEN** the document window has an active workflow stage and at least one available tool for the current context
- **THEN** the lower detail area displays the selected stage tool

#### Scenario: Host updates with active context
- **WHEN** the active tab, selected navigation node, selected item, or workflow stage changes
- **THEN** the Stage Tool Host refreshes its active context and selected tool state

### Requirement: Stage tools declare supported stages and contexts
Stage tools SHALL declare the workflow stages and entity context requirements they support before the host evaluates them for display.

#### Scenario: Tool supports current stage
- **WHEN** a tool declares support for the active workflow stage and its context requirements are satisfied
- **THEN** the tool is included in the available tools for the Stage Tool Host

#### Scenario: Tool does not support current stage
- **WHEN** a tool does not declare support for the active workflow stage
- **THEN** the tool is excluded from the available tools for that stage

#### Scenario: Tool requires selected item
- **WHEN** a tool requires a selected item and the current context only has a selected topic
- **THEN** the tool is not opened as the active stage tool
- **AND** the host can report that the tool requires an item context

### Requirement: Host provides explicit tool context
The Stage Tool Host SHALL provide the selected tool with an explicit context snapshot instead of requiring the tool to scrape UI state.

#### Scenario: Context includes workflow and navigation data
- **WHEN** a stage tool is opened
- **THEN** it receives the active workflow stage and available navigation context, including active store, topic path, selected topic, and selected item when present

#### Scenario: Context includes inherited workspace data
- **WHEN** inherited tags, metadata, nearby sibling topics, nearby sibling items, accepted work, rejected work, archived work, user settings, or provider capabilities are available through application services
- **THEN** the tool context exposes that data through explicit context fields or application-layer accessors

### Requirement: Host exposes available tool choices
The Stage Tool Host SHALL expose the set of tools available for the current workflow stage and context.

#### Scenario: Multiple tools are available
- **WHEN** more than one tool is available for the current workflow stage and context
- **THEN** the document window exposes a compact tool selector in a consistent location within the tool area

#### Scenario: One tool is available
- **WHEN** exactly one tool is available for the current workflow stage and context
- **THEN** the host may hide or minimize the selector without hiding the active tool

#### Scenario: User switches tools
- **WHEN** the user selects a different available tool from the selector
- **THEN** the Stage Tool Host displays that tool using the same active workflow and navigation context

### Requirement: Built-in tools use the host registration path
Built-in default stage tools SHALL be registered through the same host-facing tool registry used to query available stage tools where practical.

#### Scenario: Default tool is registered
- **WHEN** the application starts with no external plugin tools available
- **THEN** built-in default tools can still be discovered by the Stage Tool Host registry

#### Scenario: Later plugin tools are added
- **WHEN** a future plugin integration contributes stage tools through the host-facing registry
- **THEN** the host can evaluate built-in and contributed tools through the same availability model

### Requirement: Default tool remains available during plugin issues
The Stage Tool Host SHALL keep a matching built-in default tool available when a plugin-provided tool is missing, disabled, unavailable, or fails to load.

#### Scenario: Plugin tool fails
- **WHEN** a plugin-provided tool for the current stage fails to load
- **THEN** the host reports the plugin tool as unavailable or failed
- **AND** a matching built-in default tool remains selectable when one exists

#### Scenario: No plugin tool is installed
- **WHEN** no plugin-provided tool supports the current stage and context
- **THEN** the host uses an available built-in default tool when one exists

### Requirement: Tool selection is scoped by stage and context
The Stage Tool Host SHALL preserve tool selection by workflow stage and context kind where useful so a choice in one stage does not unexpectedly replace tools in other stages.

#### Scenario: Selection changes in one stage
- **WHEN** the user selects an alternate tool for the Idea stage
- **THEN** the selected Concept, Design, and Listing tools are not changed by that selection

#### Scenario: Prior selection becomes invalid
- **WHEN** a previously selected tool is not available for the current stage and context
- **THEN** the host selects the default available tool for that stage and context

### Requirement: Hosted tools use application services for durable changes
Hosted stage tools MUST use application services or commands for durable workspace changes instead of writing directly to persistence or workspace storage.

#### Scenario: Tool creates or updates work
- **WHEN** a hosted tool creates an item, updates metadata, attaches assets, changes workflow state, or records generated output
- **THEN** the durable change is performed through an application-layer service or command

#### Scenario: Tool reads context
- **WHEN** a hosted tool needs workspace context beyond the initial context snapshot
- **THEN** it accesses that data through application-layer contracts rather than UI control state or direct storage access
