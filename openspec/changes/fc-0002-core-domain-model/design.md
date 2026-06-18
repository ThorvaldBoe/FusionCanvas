## Context

FusionCanvas is still in Phase 0. The application shell and Clean Architecture project structure exist, but the product-facing domain vocabulary has not been made durable in OpenSpec or code.

The FC-0002 planning PRD identifies Store, Niche, Group, Listing, Asset, Prompt, and Tag as the first shared concepts. Later roadmap items will add persistence, workspace files, navigation behavior, listing workflows, AI history, concept versions, designs, mockups, marketplace products, analytics, and plugins. This change should give those future features stable language without implementing their full behavior early.

## Goals / Non-Goals

**Goals:**

- Add the initial core domain concepts to `FusionCanvas.Domain`.
- Express parent/child relationships between stores, topics, listings, connected resources, prompts, and tags.
- Represent the Phase 0 distinction between topic-like organization and item-like product work.
- Keep domain types independent of UI, persistence, file system, AI, marketplace, and plugin concerns.
- Cover core invariants with unit tests in `FusionCanvas.Domain.Tests`.

**Non-Goals:**

- No SQLite schema, repositories, migrations, or persistence mapping.
- No workspace folder layout or asset file copying.
- No navigation tree UI, drag/drop, search, filtering, or move commands.
- No complete listing lifecycle, concept versioning, design records, mockups, marketplace products, performance data, or plugin data model.
- No AI provider integration or prompt execution.

## Decisions

### Model Phase 0 domain concepts in the domain layer

The first implementation should add focused domain types under `FusionCanvas.Domain` because these concepts are business language and invariants, not UI state or persistence details.

Alternative considered: define the model only in docs until persistence work begins. That would delay useful validation and leave Phase 1 features without concrete shared types.

### Keep entities persistence-neutral

Domain types should use simple identifiers, names, optional notes/context fields where needed, and relationship identifiers or collections that do not assume a database schema, ORM, filesystem path strategy, or serialization format.

Alternative considered: shape the model around SQLite tables immediately. That would couple FC-0002 to FC-0003 and make early domain behavior harder to revise before persistence requirements are accepted.

### Treat Niche and Group as topic concepts

Niche and Group should be modeled as topic-like organization inside a Store. A Niche is a top-level topic area inside a store. A Group is a nested topic below a niche or another group. Listing is the first item-like product concept that can live under a topic.

Alternative considered: model topic as the only public concept and hide Niche and Group as type values. The PRD names Niche and Group explicitly, and keeping them visible now helps Phase 1 management features map to creator language.

### Keep Prompt as preserved context, not an AI run model

Prompt should preserve prompt-related context that can be connected to creative work, but it should not require provider metadata, execution status, response history, token usage, or prompt libraries yet.

Alternative considered: defer Prompt until AI features. The FC-0002 PRD includes Prompt in the core model because preserved creative context is central to FusionCanvas. A minimal prompt concept can support later AI work without implementing it.

### Keep Tag flexible and lightweight

Tag should be a reusable classification label that can be associated with core entities, but this change should not implement advanced filtering, tag colors, tag hierarchies, aliases, or marketplace keyword behavior.

Alternative considered: restrict tags to listings only. The Phase 1 roadmap expects tags to classify topics, listings, assets, ideas, phrases, and future entities, so the core model should not bake in a listing-only rule.

## Risks / Trade-offs

- [Risk] The first model could become too broad and preempt later workflow specs -> Mitigation: limit FC-0002 to named entities, relationships, and core invariants; leave workflow-specific entities to later changes.
- [Risk] Persistence work may need a slightly different shape -> Mitigation: keep domain types persistence-neutral and let FC-0003 define mapping decisions.
- [Risk] Topic/item language may be confusing if exposed too early in the UI -> Mitigation: define it as domain language now; let navigation and UI specs decide exact presentation.
- [Risk] Prompt may look like a complete AI history model -> Mitigation: specify it only as preserved prompt-related context and explicitly exclude provider execution details.
