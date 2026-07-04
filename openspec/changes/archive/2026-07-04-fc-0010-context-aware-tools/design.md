## Context

FusionCanvas currently has a Clean Architecture solution with early workspace domain types, repository contracts, SQLite/file-store integration, and a minimal Avalonia shell. The PRD positions context-aware tools as a Phase 0 foundation feature that later Stage Tool Host, Idea, Concept, Design, Listing, AI, asset, and automation features will consume.

The central design problem is not a specific tool UI. It is the shared contract for resolving "where the user is" into a durable tool context that application services can use without scraping Avalonia state or coupling future plugin tools directly to storage.

## Goals / Non-Goals

**Goals:**
- Model a shared tool context that can represent active store, niche, topic path, selected topic, selected item, workflow stage, inherited tags, metadata, and nearby work.
- Resolve context through application-layer services from explicit navigation/stage selection inputs.
- Provide predictable defaults for topic-based creation and item-bound refinement.
- Make scope visible to users and allow intentional scope changes through UI-facing state.
- Keep the design compatible with future built-in tools, Stage Tool Host, plugin-provided tools, and AI workflows.

**Non-Goals:**
- Implement detailed AI prompting, provider-specific behavior, semantic duplicate detection, or originality guarantees.
- Build the Stage Tool Host selector or plugin loading system.
- Define full store, niche, group, listing, tag, or metadata editing workflows beyond the context data needed by tools.
- Add mandatory creative automation or automatic stage advancement.

## Decisions

### Decision: Add an application-layer tool context resolver

Introduce an application-layer service that accepts an explicit selection request, such as active workspace, selected topic or item, desired workflow stage, and optional scope override. It returns a tool context DTO that is safe for UI and future tool hosts to consume.

Rationale: context resolution coordinates repository data, domain relationships, inherited tags/metadata, and nearby work. That orchestration belongs in the application layer, while the UI should provide selection inputs and display resolved scope.

Alternatives considered:
- UI-only context composition: faster initially, but it would force tools to depend on Avalonia view state and duplicate resolution rules.
- Domain-only context composition: keeps rules centralized, but the domain should not own repository queries or UI selection concerns.

### Decision: Represent topic and item contexts explicitly

The tool context distinguishes topic-scoped contexts from item-bound contexts. Topic-scoped tools can create new work into the selected topic by default. Item-bound tools require a selected item and receive the item's parent topic path and related stage data.

Rationale: the PRD repeatedly separates "create in place" tools from tools that refine an existing item. A single vague selection model would make it too easy for item tools to run with incomplete context.

Alternatives considered:
- One nullable context object for every tool: simple shape, but ambiguous requirements and weaker validation.
- Separate services for every stage: clear for each tool, but prematurely fragments the shared context contract.

### Decision: Make inherited context part of the resolved result

The resolver should return inherited tags and metadata from the active store, niche, topic path, and selected item where applicable. Creation commands can then apply those inherited values by default while still letting future workflows override or remove values intentionally.

Rationale: tools need a single answer for "what context applies here" instead of independently walking parent relationships. This keeps topic-default placement, inherited classification, and AI/prompt context consistent.

Alternatives considered:
- Apply inheritance only when saving new entities: keeps reads smaller, but tools cannot show or reason about scope before save.
- Store copied tags/metadata eagerly on every child: simpler reads, but harder to keep inheritance understandable as users reorganize work.

### Decision: Return nearby work as curated summaries

The context should expose nearby sibling topics/items and relevant accepted, created, rejected, or archived work as bounded summaries rather than full entity graphs.

Rationale: context-aware tools need enough history to avoid obvious repetition and use negative guidance, but they should not overfetch large object graphs or leak every field into future AI prompts.

Alternatives considered:
- Return full entities: easy for early code, but increases coupling and makes prompt/privacy boundaries blurry.
- Omit nearby work until AI features: simpler for Phase 0, but misses a core context-aware behavior from the PRD.

### Decision: Scope visibility is a UI-facing property of context

The resolved context should include a compact scope summary suitable for display in tool surfaces. Scope override inputs should be explicit, such as current topic, current subtree, niche, or item, rather than hidden tool-specific behavior.

Rationale: users need to understand what scope a tool is using, especially when generation or creation spans more than the selected node.

Alternatives considered:
- Leave scope display to each tool: flexible, but inconsistent and easy to omit.
- Force all tools to current topic only: predictable, but too narrow for later workflows that need subtree or niche scope.

## Risks / Trade-offs

- [Risk] Context DTO grows into a broad dumping ground -> Mitigation: keep the first contract focused on selection, inherited context, nearby summaries, and scope display; add stage-specific details through small nested records when requirements demand them.
- [Risk] Nearby work queries become expensive as workspaces grow -> Mitigation: use bounded result counts and summaries, and keep semantic duplicate detection out of this change.
- [Risk] Future plugin APIs may need more fields than the built-in tools -> Mitigation: keep the resolver application-facing and versionable, with plugins later consuming a stable projection through the Stage Tool Host.
- [Risk] Context inheritance may be confused with permanent copied metadata -> Mitigation: distinguish inherited values from explicitly assigned values in the resolved result and in creation command behavior.
- [Risk] Current domain entities may not yet contain all store/niche/tag/metadata concepts -> Mitigation: implement the resolver incrementally against available workspace concepts while preserving the contract shape for later Phase 1 and Phase 2 features.

## Migration Plan

This is additive. Create the context model and resolver behind application-layer contracts, then connect UI/view-model selection state to the resolver as navigation and stage surfaces become available. No data migration is expected for this change.

Rollback is low risk: remove the resolver, DTOs, UI scope display, and tests without changing existing persisted data. Any future creation command integration should be rolled back by returning to explicit target selection.

## Open Questions

- Which scope override options should ship first: current topic only, current subtree, niche, store, or item?
- What default maximum should apply to nearby sibling and historical work summaries?
- How should unresolved or partially loaded workspace context be represented in the tool surface without adding clutter?
