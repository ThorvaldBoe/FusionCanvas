# FC-0002 - Core Domain Model

## Summary

The Core Domain Model defines the main concepts FusionCanvas uses to describe Print on Demand work.

Phase 0 should establish the essential language and relationships for stores, niches, groups, listings, assets, prompts, and tags so later features share the same foundation.

## User Need

As a creator or contributor, I need FusionCanvas to have clear domain concepts so product work is organized consistently instead of becoming a collection of disconnected screens and files.

## Goals

- Define the core entities needed for the MVP.
- Make the relationships between entities understandable.
- Support the Phase 1 workspace model.
- Preserve room for future concepts without overbuilding.
- Keep the domain language aligned with creator workflows.

## Requirements

- The model defines Store as the top-level business or brand context.
- The model defines Niche as a top-level topic area inside a store.
- The model defines Group as a flexible nested topic.
- The model defines Listing as the primary item and product concept.
- The model defines Asset as a connected file or external resource.
- The model defines Prompt as preserved AI or prompt-related context for future workflows.
- The model defines Tag as a flexible classification label.
- The model describes how stores, niches, groups, listings, assets, prompts, and tags relate.
- The model distinguishes topics from items at a product-language level.
- The model avoids introducing advanced entities before workflows prove they are needed.

## Core Entities

- Store
- Niche
- Group
- Listing
- Asset
- Prompt
- Tag

## User Workflows Supported

### Organize a Store

The domain model should support a store containing niches, groups, listings, assets, and tags.

### Represent a Product Concept

The domain model should support a listing as a product concept with creative context, not simply a file reference.

### Preserve Context

The domain model should support connecting assets, prompts, tags, and notes to the right creative work.

## Acceptance Criteria

- A contributor can explain the core entities and their relationships.
- Phase 1 features can reference a shared domain language.
- The model supports stores, niches, groups, listings, assets, prompts, and tags.
- The model supports a topic/item navigation concept.
- The model avoids forcing future entities such as designs, concepts, mockups, and performance data into Phase 0 unless necessary.

## Out of Scope

- Complete long-term data model
- Marketplace products
- Performance data
- Concept versioning
- Design versioning
- Plugin data model
- Custom workflow model

## Open Questions

- Should Idea and Phrase remain listing fields until later phases?
- Should prompts be part of the initial model or deferred until the first AI-related feature?
- Should topic and item be explicit user-facing concepts or internal organizing language?

## Related Notes

- [[Phase 0 - Foundation]]
- [[Roadmap]]
- [[Data Model]]
- [[Product]]
- [[Principles]]
