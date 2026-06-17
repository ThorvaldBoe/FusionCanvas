# FusionCanvas UI Guidelines

## Purpose

These guidelines describe the preferred FusionCanvas application UI direction.

FusionCanvas should draw inspiration from Obsidian's workspace model: a focused desktop shell, persistent navigation, tabbed working documents, fast context switching, and compact tools that stay close to the user's current work.

This is inspiration, not imitation. FusionCanvas has a different job: it manages a structured Print on Demand production workflow. The UI should feel familiar to people who like Obsidian, but it should serve FusionCanvas concepts directly.

## Core UI Direction

FusionCanvas should feel like a local-first creative production workspace:

- calm, dense, and practical
- built for repeated daily use
- optimized for browsing, comparing, refining, and preparing many product ideas
- visually dark by default, with restrained accent color
- context-aware without feeling magical or hidden
- extensible without letting plugin tools make the app feel inconsistent

The interface should avoid marketing-page composition. It should feel like a real working application from the first screen.

## Primary Layout

FusionCanvas should use a two-region shell:

```text
Main Window
|-- Left Navigation Pane
`-- Right Document Window
```

The left navigation pane contains stores, niches, topics, groups, and items.

The right document window contains tabs, workflow stage navigation, and the active stage tool.

## Correct Document Split

The document window should be split horizontally, not vertically.

Correct model:

```text
Document Window
|-- Tabs
|-- Workflow Stage Navigator
`-- Stage Tool Host
```

The workflow boxes belong above the tool area. The stage tool belongs below the workflow boxes.

This should be treated as the preferred product direction if any older design note accidentally suggests a vertical split.

## Obsidian-Inspired Principles

### Persistent Navigation

The navigation pane should remain visible during normal work. It should show the current store and the active tree location for the selected tab.

The active document tab should be able to reveal or highlight its item in the navigation tree.

### Tabbed Work

Users should be able to keep multiple items, topics, or setup views open at once.

Tabs should be compact and functional:

- item title visible
- active tab visually clear
- close action available
- no large decorative tab treatment
- tab activation updates workflow and navigation context

### Contextual Tooling

The lower document area is a stage tool host.

Tools should receive context from the application:

- active store
- active niche
- topic path
- selected topic
- selected item, when applicable
- active workflow stage
- inherited tags and metadata
- relevant nearby work
- available AI and plugin capabilities

Tools should not need to infer context by scraping visible UI state.

### Compact Commands

Common commands should be close to where they are used.

Examples:

- New topic and New item near the navigation tree
- tool selector at the top right of the stage tool area
- stage-specific commands inside each tool
- global command/search entry in the top bar

## Navigation Pane

The navigation pane should support browsing and reshaping work.

Primary elements:

- store selector
- search/filter input
- create topic/item commands
- hierarchical tree
- active item highlight
- compact tree actions

The tree should distinguish topic-like nodes from item-like nodes without requiring heavy visual decoration.

Niches are the default top-level topics inside a store. Groups and subgroups are nested topics. Listings and ideas are item-like work objects.

## Workflow Stage Navigator

The workflow navigator should show:

- Idea
- Concept
- Design
- Listing
- optional Archive state nearby

Current stage should be visually emphasized.

Completed or available stages should be enabled.

Future unavailable stages should be visibly disabled.

Archive may appear as a related state, but it should not compete with the core Idea -> Concept -> Design -> Listing progression.

## Stage Tool Host

The stage tool host appears below the workflow navigator.

It should include:

- title and context summary
- optional tool selector
- the active built-in or plugin tool
- stage-specific actions
- useful empty states when the selected context cannot support the tool

If more than one tool is available for the current stage and context, the tool selector should appear in a consistent top-right location.

## Built-In Tool Patterns

### Ideation Tool

The ideation tool should be fast and low-friction.

It should support:

- context summary
- one-line idea capture
- optional notes
- optional inspiration image area
- Create action
- Generate Ideas action when AI is available
- generated candidates with Create and Reject actions

The tool may work from a selected topic without requiring an existing item.

### Concept Tool

The concept tool works on a single existing item.

It should center on the Design Triangle:

```text
Idea
Phrase
Graphic
```

It should support:

- editable Idea, Phrase, and Graphic nodes
- selected node improvement
- AI suggestions when available
- accept, reject, regenerate, and save alternate actions
- quality/readiness indicator when AI scoring is available
- concept history

### Design Tool

The design tool works on a single existing item and selected concept or design-ready brief.

It should support:

- design brief panel
- import actions
- optional generation actions
- variant workspace
- explicit final selection
- variant detail panel
- tags, notes, source method, and assets

Final selection must be deliberate. Importing or generating artwork should not automatically make it final.

### Listing Tool

The listing tool prepares the sellable product representation.

It should support:

- selected final design panel
- listing metadata editor
- price and status fields
- mockup generation/attachment
- generated mockup gallery
- readiness checklist

The basic listing tool should not directly publish to Printify, Shopify, Etsy, or other marketplaces.

### Mockup Setup

Mockup setup belongs at the store configuration level.

It should support:

- mockup products
- templates
- color variants
- exact provider color names
- design area dimensions
- placement coordinates and size

The first version can use numeric placement fields. A later visual placement editor can improve this.

### Store and Niche Setup

Store and niche setup should preserve durable creative context.

Store setup should capture:

- store name
- brand direction
- general notes
- active/inactive status
- later marketplace or AI configuration hooks

Niche setup should capture:

- audience
- humor style
- visual style guidance
- constraints and risks
- groups and active work areas

## Visual Style

Preferred direction:

- dark desktop workspace
- neutral panels with clear borders
- warm accent color for active state and primary commands
- subtle green for completed or ready states
- muted text for secondary information
- compact controls with 6-8px radius
- readable, dense spacing
- no decorative gradients, oversized hero areas, or marketing-style cards

Cards should be used for tool panels and repeated content, not for every page section.

## Screenshot Concepts

The current screenshot concepts live in [Visuals](Visuals/).

Current assets include:

- [fusioncanvas-core-workspace.png](Visuals/fusioncanvas-core-workspace.png)
- [fusioncanvas-ideation-tool.png](Visuals/fusioncanvas-ideation-tool.png)
- [fusioncanvas-concept-tool.png](Visuals/fusioncanvas-concept-tool.png)
- [fusioncanvas-design-tool.png](Visuals/fusioncanvas-design-tool.png)
- [fusioncanvas-listing-tool.png](Visuals/fusioncanvas-listing-tool.png)
- [fusioncanvas-mockup-setup.png](Visuals/fusioncanvas-mockup-setup.png)
- [fusioncanvas-store-niche-setup.png](Visuals/fusioncanvas-store-niche-setup.png)
- [fusioncanvas-ui-mockups.html](Visuals/fusioncanvas-ui-mockups.html)
