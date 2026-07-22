# FusionCanvas

FusionCanvas is a desktop application for Print on Demand creators who want a structured, AI-assisted workflow for developing, organizing, refining, and publishing product designs.

The goal is to help creators move from scattered ideas and manual production steps into a more systematic creative pipeline.

FusionCanvas is especially focused on:

- organizing stores, niches, listings, concepts, designs, mockups, and metadata
- supporting batch-oriented production workflows
- helping creators refine ideas using the Design Triangle: idea, phrase, and graphic
- preparing designs and listings for platforms such as Shopify, Printify, Etsy, and other marketplaces
- making AI assistance practical, contextual, and grounded in each store or niche
- supporting extensibility through plugins
- remaining useful as a local-first desktop application

## Current Status

FusionCanvas is being rebuilt as a new application, separate from the earlier FusionCanvas legacy tooling.

The current direction is:

- Avalonia desktop application
- open source core
- SQLite for structured data
- JSON metadata fields for flexible, evolving information
- plugin-based architecture
- OpenSpec-driven development
- AI-assisted workflows as a core long-term feature

The application is still in early development. Work advances through one cohesive delivery module at a time; current planning lives in `docs/roadmap.md`, and accepted behavior lives in `openspec/specs`.

## Delivery Approach

FusionCanvas uses a rolling OpenSpec workflow: discover the next module collaboratively, define observable requirements and conceptual/functional design, add a detailed implementation plan for the assigned agent, implement bounded tasks, verify every acceptance criterion, learn from the result, and archive the change. Later opportunities remain lightweight until the current module is complete.

The original LifeOS planning files under `docs/LifeOS` are preserved only as optional historical idea sources. They may be stale and do not define current priorities, ordering, requirements, or acceptance criteria.

## Core Concept

FusionCanvas treats Print on Demand creation as a pipeline:

```text
Idea
Concept
Design
Listing
Publishing / Sync
Feedback
Iteration
```

Mockup generation is part of the Listing stage rather than a separate core stage. The basic application should support local mockup generation and listing preparation. Direct Printify, Shopify, and other marketplace publishing can be added later through integrations or plugins.

Instead of treating each design as an isolated file, FusionCanvas keeps track of the surrounding context:

- Which store does it belong to?
- Which niche does it target?
- What idea is behind it?
- What phrase or hook does it use?
- What graphic or visual concept supports it?
- What style constraints apply?
- What listing metadata is needed?
- What mockups exist?
- Which products, templates, colors, and prices are being prepared?
- What performance signals are available?

## The Design Triangle

A central idea in FusionCanvas is the Design Triangle.

Every strong design is treated as a combination of three elements:

```text
Idea
Phrase
Graphic
```

The idea defines the emotional or situational core. The phrase gives the design its verbal hook. The graphic gives the design its visual identity.

FusionCanvas should help evaluate whether these three elements support each other, conflict with each other, or need refinement.

After a concept is clear, the Design stage turns it into final shirt artwork. FusionCanvas should support manual design work in external tools, imported AI-generated artwork, and in-app AI generation where configured. The important product behavior is preserving variants, related assets, source context, and final selection decisions for the current item.

## Stores, Niches, Groups, and Listings

FusionCanvas should support multiple stores.

A store may represent a brand, shop, client, or business unit.

Inside a store, the user can organize work into a flexible navigation tree of topics and items.

A topic is a folder-like grouping of related ideas, concepts, listings, campaigns, experiments, or work areas. The top-level topics inside a store are niches by default. Topics can contain any number of nested topics.

An item is a single idea, concept, listing, or other concrete unit of work. Listings are the primary item type and may eventually become products.

A niche is more than a folder. It can contain important knowledge about audience, style, humor, constraints, popular themes, legal risks, successful patterns, and AI guidance.

Navigation should make it easy to create topics or items, move them, convert an item into a topic, convert an empty topic into an item, and expand or collapse visible topics.

Filtering should support text search, tags on both topics and items, item status, and structural scope. By default, all top-level topics should be visible until a filter is applied.

## Local-First Philosophy

FusionCanvas should be useful without depending on cloud services.

The application should store its core data locally, most likely in SQLite, while keeping room for later integrations with cloud storage, marketplaces, AI providers, and automation tools.

Local-first does not mean offline-only. It means the creator should own their workflow and data.

## Extensibility

FusionCanvas should be extensible through plugins.

Possible plugin areas include:

- importers
- exporters
- mockup generators
- listing publishers
- AI providers
- image processors
- marketplace integrations
- analytics integrations
- prompt engines
- validation tools

The core application should provide the structure, while plugins provide specialized capabilities.

## AI Direction

AI should assist the creator, not replace the creator.

FusionCanvas should use AI for:

- idea generation
- concept refinement
- phrase improvement
- graphic prompt generation
- image generation where a provider supports it
- style guidance
- listing text generation
- metadata suggestions
- critique and scoring
- niche research summaries
- workflow automation

The user should remain in control of taste, direction, approval, and publishing.
