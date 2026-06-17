## ADDED Requirements

### Requirement: Document Window Has A Stable Lower Detail Area That Can Host A Stage Tool
FusionCanvas SHALL ensure that the document window has a stable lower detail area that can host a stage tool.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The document window has a stable lower detail area that can host a stage tool

### Requirement: Stage Tool Can Declare Which Workflow Stages It Supports
FusionCanvas SHALL ensure that a stage tool can declare which workflow stages it supports.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** A stage tool can declare which workflow stages it supports

### Requirement: Stage Tool Can Declare Which Entity Contexts It Supports, Such As Selected Topic, Selected Listing, Or Selected Store
FusionCanvas SHALL ensure that a stage tool can declare which entity contexts it supports, such as selected topic, selected listing, or selected store.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** A stage tool can declare which entity contexts it supports, such as selected topic, selected listing, or selected store

### Requirement: Host Provides The Active Navigation Context To The Selected Tool
FusionCanvas SHALL ensure that the host provides the active navigation context to the selected tool.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The host provides the active navigation context to the selected tool

### Requirement: Host Provides The Active Workflow Stage To The Selected Tool
FusionCanvas SHALL ensure that the host provides the active workflow stage to the selected tool.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The host provides the active workflow stage to the selected tool

### Requirement: Host Exposes Available Tools For The Current Context Through A Compact Tool Selector
FusionCanvas SHALL ensure that the host exposes available tools for the current context through a compact tool selector.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The host exposes available tools for the current context through a compact tool selector

### Requirement: Tool Selector Appears In A Consistent Location, Preferably The Top Right Of The Tool Area
FusionCanvas SHALL ensure that the tool selector appears in a consistent location, preferably the top right of the tool area.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The tool selector appears in a consistent location, preferably the top right of the tool area

### Requirement: If Only One Tool Is Available, The Selector May Be Hidden Or Visually Minimized
FusionCanvas SHALL ensure that if only one tool is available, the selector may be hidden or visually minimized.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** If only one tool is available, the selector may be hidden or visually minimized

### Requirement: Built-in Tools Use The Same Registration Path As Plugin-provided Tools Where Practical
FusionCanvas SHALL ensure that built-in tools use the same registration path as plugin-provided tools where practical.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Built-in tools use the same registration path as plugin-provided tools where practical

### Requirement: Tool Selection Is Per Stage And Context Where Useful, So Choosing An Advanced Ideation Tool Does Not Unexpectedly Replace Design Or Listing Tools
FusionCanvas SHALL ensure that tool selection is per stage and context where useful, so choosing an advanced ideation tool does not unexpectedly replace design or listing tools.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Tool selection is per stage and context where useful, so choosing an advanced ideation tool does not unexpectedly replace design or listing tools

### Requirement: Some Stage Tools Can Require A Selected Item. For Example, The Basic Concept Tool Requires An Existing Item, While The Basic Ideation Tool Can Operate From A Selected Topic To Create New Items
FusionCanvas SHALL ensure that some stage tools can require a selected item. For example, the Basic Concept Tool requires an existing item, while the basic ideation tool can operate from a selected topic to create new items.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Some stage tools can require a selected item. For example, the Basic Concept Tool requires an existing item, while the basic ideation tool can operate from a selected topic to create new items

### Requirement: Missing, Disabled, Or Failed Plugin Tool Does Not Block The Default Tool
FusionCanvas SHALL ensure that a missing, disabled, or failed plugin tool does not block the default tool.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** A missing, disabled, or failed plugin tool does not block the default tool

### Requirement: Tools Should Use Application Services And Commands For Durable Changes Instead Of Writing Directly To Storage
FusionCanvas SHALL ensure that tools should use application services and commands for durable changes instead of writing directly to storage.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Tools should use application services and commands for durable changes instead of writing directly to storage

### Requirement: Acceptance Criteria Remain Satisfied
FusionCanvas SHALL satisfy the acceptance criteria captured in the source PRD.

#### Scenario: Lower Detail Area Can Display A Stage-specific Tool
- **WHEN** the corresponding capability is delivered
- **THEN** The lower detail area can display a stage-specific tool.

#### Scenario: Active Navigation And Workflow Context Are Passed To The Active Tool
- **WHEN** the corresponding capability is delivered
- **THEN** The active navigation and workflow context are passed to the active tool.

#### Scenario: Application Can List Tools Available For The Current Stage And Context
- **WHEN** the corresponding capability is delivered
- **THEN** The application can list tools available for the current stage and context.

#### Scenario: User Can Switch Between Multiple Available Tools When More Than One Exists
- **WHEN** the corresponding capability is delivered
- **THEN** The user can switch between multiple available tools when more than one exists.

#### Scenario: Host Can Distinguish Tools That Work From Topic Context From Tools That Require A Selected Item
- **WHEN** the corresponding capability is delivered
- **THEN** The host can distinguish tools that work from topic context from tools that require a selected item.

#### Scenario: Default Built-in Tool Remains Available When No Plugin Tool Is Installed
- **WHEN** the corresponding capability is delivered
- **THEN** A default built-in tool remains available when no plugin tool is installed.

#### Scenario: Plugin-provided Tool Can Be Unavailable Without Breaking The Document Window
- **WHEN** the corresponding capability is delivered
- **THEN** A plugin-provided tool can be unavailable without breaking the document window.

## Source PRD

# FC-0011 - Stage Tool Host

## Summary

The Stage Tool Host displays the active tool for the current workflow stage in the lower detail area of the document window.

The host should support built-in default tools and future plugin-provided tools through the same model. This keeps the first version simple while preserving the long-term goal that specialized ideation, concept, design, listing, and automation tools can be added without rebuilding the main application.

## User Need

As a creator, I need the main work area to show the right tool for the current stage while letting me switch to a different tool when more than one is available.

As a plugin author, I need a stable way to contribute tools that can work inside the selected store, niche, topic, item, workflow stage, and metadata context.

## Goals

- Treat the lower document-window detail area as a host for stage tools.
- Support a default tool for each important workflow stage.
- Allow plugins to register additional tools for supported stages and contexts.
- Let users switch between available tools without losing the selected navigation context.
- Give tools enough context to create, update, or generate work in the right place.
- Keep early FusionCanvas features extensible without requiring a full plugin marketplace.

## Requirements

- The document window has a stable lower detail area that can host a stage tool.
- A stage tool can declare which workflow stages it supports.
- A stage tool can declare which entity contexts it supports, such as selected topic, selected listing, or selected store.
- The host provides the active navigation context to the selected tool.
- The host provides the active workflow stage to the selected tool.
- The host exposes available tools for the current context through a compact tool selector.
- The tool selector appears in a consistent location, preferably the top right of the tool area.
- If only one tool is available, the selector may be hidden or visually minimized.
- Built-in tools use the same registration path as plugin-provided tools where practical.
- Tool selection is per stage and context where useful, so choosing an advanced ideation tool does not unexpectedly replace design or listing tools.
- Some stage tools can require a selected item. For example, the Basic Concept Tool requires an existing item, while the basic ideation tool can operate from a selected topic to create new items.
- A missing, disabled, or failed plugin tool does not block the default tool.
- Tools should use application services and commands for durable changes instead of writing directly to storage.

## Tool Context

The host should provide a context object that can include:

- active store
- active niche
- full topic path
- selected topic
- selected item, when applicable
- workflow stage
- inherited tags and metadata
- nearby sibling topics and items
- existing accepted or created items in scope
- rejected or archived items in scope when relevant
- user settings relevant to the tool
- available AI provider capabilities, when relevant

The context object should be explicit enough that tools do not need to scrape UI state.

## Built-In Tool Principle

The default modules should be implemented as built-in tools that follow the plugin-facing contracts as much as possible.

This does not mean the first version needs external plugin loading for every module. It means the core ideation, concept, design, and listing tools should be shaped like replaceable or companion tools from the beginning.

## User Workflows

### Use the Default Tool

The user selects a listing in the navigation pane. The workflow stage navigator shows the current stage. The lower detail area opens the default tool for that stage.

### Switch Tools

The user is in the Idea stage and has multiple ideation tools available. The user opens the tool selector and switches from the basic ideation tool to another installed tool. The selected topic, listing, and stage context remain intact.

### Open an Item-Bound Concept Tool

The user opens an existing item in the Concept stage. The host provides the store, niche, topic path, item, selected concept version, tags, metadata, and nearby work to the Basic Concept Tool. If the user selects only a topic without an item, the host does not open item-bound concept work and can offer to create an item first.

### Open an Item-Bound Design Tool

The user opens an existing item in the Design stage. The host provides the store, niche, topic path, item, selected concept version, design records, related assets, tags, metadata, nearby work, and available AI or image-processing capabilities to the Basic Design Tool. If the user selects only a topic without an item, the host does not open item-bound design work and can offer to create an item first.

### Plugin Tool Fallback

A plugin tool fails to load or is disabled. FusionCanvas keeps the default tool available and reports the plugin issue without blocking the creator's work.

## Acceptance Criteria

- The lower detail area can display a stage-specific tool.
- The active navigation and workflow context are passed to the active tool.
- The application can list tools available for the current stage and context.
- The user can switch between multiple available tools when more than one exists.
- The host can distinguish tools that work from topic context from tools that require a selected item.
- A default built-in tool remains available when no plugin tool is installed.
- A plugin-provided tool can be unavailable without breaking the document window.

## Out of Scope

- Online plugin marketplace
- Arbitrary custom layout for every plugin
- Full scripting engine
- Multi-pane tool composition
- Real-time collaboration between tools

## Open Questions

- Should tool selection persist globally, per workspace, per stage, or per entity type?
- Should plugin tools be allowed to add secondary panels around the main tool area?
- How much UI freedom should a plugin tool have before consistency suffers?

## Related Notes

- [[FC-0009 - Tabbed Document Window]]
- [[FC-0010 - Context-Aware Tools]]
- [[FC-0404 - Idea Generation]]
- [[FC-0210 - Basic Concept Tool]]
- [[FC-0211 - Basic Design Tool]]
- [[FC-0503 - Plugin Dependency Registration]]
- [[FC-0504 - Plugin Command Contributions]]
- [[FC-0507 - Plugin Settings UI]]
