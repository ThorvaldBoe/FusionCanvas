# FusionCanvas Product

## Overview

FusionCanvas is a desktop application for managing the complete lifecycle of Print on Demand products.

Rather than treating designs as isolated image files, FusionCanvas organizes the entire creative process from the first idea to published products and performance analysis.

The application combines project organization, creative refinement, AI assistance, automation, and marketplace integrations into a single workspace.

## Target Users

FusionCanvas is designed for creators who build Print on Demand businesses.

Typical users include:

- Shopify store owners
- Etsy sellers
- Printify users
- Merch by Amazon creators
- Creative Fabrica contributors
- digital product creators
- designers managing multiple brands or stores

The application is optimized for creators who regularly produce many designs rather than occasional hobbyists.

## Core Philosophy

Every product begins as an idea.

FusionCanvas helps transform that idea into a successful listing through a structured workflow while preserving all of the creative knowledge generated along the way.

The application should support creativity without forcing rigid processes.

## Core Workflow

FusionCanvas is built around a core creative workflow:

```text
Idea
Concept
Design
Listing
```

This four-step workflow is a top-level product concept. It should be visible in the application and tied to the current item being edited.

The workflow is flexible. A creator may begin with a rough idea, a clear concept, a phrase, an existing image direction, or even a nearly complete design. FusionCanvas should allow users to skip ahead when they already have enough information, while still preserving the context of earlier stages where it exists.

Idea-stage tools may work from a selected topic and create new items. Concept-stage tools are different: concept work belongs to a single existing item. A concept may start from an idea, phrase, or graphic direction, but the item must already exist and must live in the navigation structure under a store, niche, topic, or subtopic.

Design-stage tools are also item-bound. The selected concept or design-ready creative brief provides the context for creating, importing, generating, comparing, and selecting final artwork variants. A creator may make the design manually in Affinity Designer or Photoshop, generate it in an external AI tool and import it, or generate it inside FusionCanvas when an AI image provider is configured.

The Design stage should allow experimentation without confusion. Many variants can exist, but one or more must be deliberately promoted as final selected artwork before later listing, mockup, export, or product-variant work treats them as ready.

Listing-stage tools are item-bound as well. The selected final design variant provides the artwork for local mockup generation, listing metadata, price, status, and marketplace preparation. In the basic product, Listing supports mockup generation from manually configured product templates and keeps enough provider, color, and pricing data to support manual Printify or Shopify setup.

Direct Printify and Shopify product creation should remain outside the basic Listing tool. Those capabilities belong to later integration specs or optional plugins, while FusionCanvas remains the local source of truth for the creative and listing preparation data.

Archive is a related retained state for work that is rejected, retracted, paused indefinitely, or useful only for learning. Archive may appear near the workflow, but it should not obscure the core Idea -> Concept -> Design -> Listing progression.

## Workflow Stage Navigator

The document window should include a workflow stage navigator for the current item.

The navigator should show Idea, Concept, Design, and Listing as distinct stage boxes. The current stage should be visually emphasized. Completed or available prior stages should be enabled. Future stages that the item has not reached should be disabled.

Clicking an enabled stage should open the relevant information for that stage. Clicking a disabled future stage should not navigate.

This gives the creator a constant answer to three questions:

- Where is this item in the workflow?
- Which previous stages can I review?
- What has not been created yet?

## Document Window

FusionCanvas should use a tabbed document window inspired by Obsidian.

The left side of the application is the navigation pane. The right side is the document window. The document window should support multiple tabs so creators can work across several ideas, concepts, designs, or listings without losing their place.

When a tab becomes active, the navigation pane should update to show where the active item lives in the store, niche, topic, and item hierarchy.

The document window should be split into:

- a top workflow stage area
- a larger stage detail area below it

The top area shows the workflow stage navigator. The lower area shows the editor, inspector, or tool relevant to the current workflow stage.

## Workspace Organization

FusionCanvas organizes work hierarchically.

```text
Store
  Niche
    Group
      Listing
    Group
  Niche
```

### Store

A store represents a business, client, or brand.

Each store has its own settings, publishing destinations, AI context, and metadata.

### Niche

A niche is more than a folder.

It represents a target audience together with accumulated knowledge.

A niche may contain:

- audience description
- humor style
- preferred artwork
- successful phrases
- prompt templates
- style guides
- legal considerations
- AI instructions
- trend observations

This information becomes increasingly valuable over time.

### Groups

Groups organize listings.

Groups may represent:

- collections
- campaigns
- holidays
- design series
- experiments
- temporary work

Groups may contain subgroups.

### Listings

Listings are the primary working objects.

A listing represents a product concept rather than simply an image.

A listing may contain:

- idea
- phrase
- graphic
- prompts
- exported artwork
- mockups
- listing text
- keywords
- tags
- marketplace status
- performance history

## Navigation and Filtering

The main navigation should make it very easy to see what exists, narrow the visible set, and select or create the next thing to work on.

Navigation behaves like a folder structure even if the UI does not always render it as a traditional file explorer.

By default, the top-level folders inside a store are niches. A niche may contain any number of nested topics, and those topics may contain more topics or items.

### Topics and Items

FusionCanvas should distinguish between two navigation object types:

- A topic is a folder-like grouping of related ideas, concepts, listings, campaigns, experiments, or work areas.
- An item is a single idea, concept, listing, or other concrete unit of work.

In the current product language, niches and groups are topics, while listings are items. Future workflows may introduce other item types, such as raw ideas, concepts, prompts, or reusable phrase entries, but the navigation behavior should remain consistent.

Users should be able to create a topic or item from the current navigation context with minimal friction. The chosen parent should be obvious, and newly created objects should appear where the user expects them.

FusionCanvas tools should be context-aware. When the user is standing in a store, niche, topic, or item, creation and generation tools should understand that context.

This context-awareness should apply beyond ideation. Concept refinement, phrase work, design generation, listing metadata, validation, automation, and analytics should all use the current context where practical.

### Structure Manipulation

The navigation structure should be easy to reshape as the creator's understanding changes.

Expected operations include:

- create a new topic
- create a new item
- rename a topic or item
- move a topic or item to another valid location
- drag and drop topics and items where practical
- convert an item into a topic
- convert an empty topic into an item
- expand or collapse individual topics
- expand or collapse all visible topics

An item can always be converted into a topic. This supports the common workflow where a single idea becomes a grouping for many related items.

A topic can only be converted into an item when it has no child topics or child items. This prevents hidden work from being lost or orphaned.

Moving a topic should move its full subtree. Moving an item should preserve its metadata, tags, status, assets, and creative history.

### Filtering

The navigation tree should support powerful filtering without making browsing feel heavy.

Filtering should support:

- text search across names, titles, and other important searchable text
- workflow stage, such as idea, concept, design, listing, or archive
- tags on both topics and items
- structural scope, such as store, niche, topic, or subtree

By default, all top-level topics should be visible until a filter is applied.

When filtering is active, visible results should still preserve enough hierarchy for the user to understand where each result lives. Matching child items should make their parent topics visible as context, even if the parent topic does not directly match the filter.

Expand all and collapse all should apply to the currently visible tree, including filtered results.

## The Design Triangle

FusionCanvas treats every design as a balance between three elements.

```text
Idea
Phrase
Graphic
```

Strong products align all three.

Every design has an idea. Some designs do not have a phrase, and some do not have a graphic, but the remaining parts should still be evaluated in relation to the idea.

The default Concept-stage experience should make this triangle interactive. The creator should be able to improve the idea, phrase, or graphic direction in context of the other elements, either manually or with optional AI assistance. Promising alternate directions can branch into new items without leaving the current concept workflow.

The default Design-stage experience should use the selected concept as its creative brief. It should preserve manually created files, externally generated AI images, in-app AI generations, prompts, design variants, cleanup notes, and final selection decisions in the context of the current item.

The default Listing-stage experience should use selected final artwork to generate or attach product mockups, prepare title, description, price, status, and marketplace notes, and preserve product/template/color choices for later publishing work.

## AI Integration

AI is integrated throughout the workflow.

Possible AI capabilities include:

- brainstorming
- concept refinement
- phrase generation
- artwork prompting
- prompt improvement
- critique
- listing generation
- metadata suggestions
- niche research
- quality scoring

The creator always remains in control.

## Asset Management

FusionCanvas manages more than images.

Assets may include:

- SVG files
- PNG exports
- Affinity documents
- prompts
- textures
- brushes
- fonts
- mockups
- product mockup templates
- supplier product and color configuration
- references

Every asset should remain connected to the listing it belongs to.

Design assets should also remain connected to the design variant they support, such as a source file, a transparent PNG export, an AI-generated draft, an upscaled replacement, or a cleaned final file for dark or light shirts.

## Batch Workflow

FusionCanvas is optimized for working with many listings simultaneously.

Examples include:

- exporting multiple designs
- generating mockups
- publishing products
- updating metadata
- running AI operations
- organizing listings

Batch operations should be first-class features.

## Marketplace Integration

FusionCanvas should integrate with external services such as:

- Shopify
- Printify
- Etsy
- Amazon Merch
- Creative Fabrica

Integrations should synchronize information rather than becoming the primary source of truth.

FusionCanvas owns the creative workflow.

## Extensibility

The application should expose extension points through plugins.

Possible plugin categories include:

- marketplace integrations
- AI providers
- import/export
- image processing
- mockup generation
- analytics
- workflow automation
- validation

The goal is to allow the ecosystem to evolve independently of the core application.

## Long-Term Direction

FusionCanvas should gradually become the central workspace for creative businesses.

Instead of replacing specialized software, it coordinates the creative process across multiple tools while preserving context, history, and accumulated knowledge.

Its greatest value is not individual features, but the way those features work together to help creators make better decisions over time.
