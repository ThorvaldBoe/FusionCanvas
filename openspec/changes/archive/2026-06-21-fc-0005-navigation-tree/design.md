## Context

FusionCanvas currently has a foundation shell and a proposed core domain model, but it does not yet have accepted behavior for how creators browse workspace content. FC-0005 from the LifeOS PRD defines the navigation tree as the bridge between Phase 0 foundation work and Phase 1 store, niche, group, listing, search, tab, and context-aware tool workflows.

The navigation foundation must be local-first, UI-friendly, and independent of persistence details. It should express stores, topics, and items using domain concepts, expose application-level operations for selection and movement, and let the Avalonia UI render a tree without owning workspace rules.

## Goals / Non-Goals

**Goals:**

- Model stores as top-level navigation contexts.
- Model niches and groups as topic nodes, with niches acting as default top-level topics inside a store.
- Model listings as item nodes that can live inside valid topics.
- Preserve topic subtrees and listing context during moves.
- Track active navigation context for creation tools and document/tab coordination.
- Support UI expansion, collapse, selection, and reveal behavior without coupling domain rules to Avalonia controls.
- Prepare for future filtering by preserving parent context around matching nodes.

**Non-Goals:**

- Implement advanced search, saved views, batch operations, or full filter UI.
- Polish drag-and-drop visuals beyond the movement contract needed by the tree.
- Implement marketplace, AI, plugin, or external storage behavior.
- Implement item-to-topic or empty-topic-to-item conversion in the first foundation release.
- Treat assets as first-class visible navigation items before asset workflows decide that behavior.

## Decisions

### Keep the navigation hierarchy in domain/application concepts

Navigation rules belong in the domain and application layers because later UI, persistence, import, and creation workflows all need the same structure. The UI should render and manipulate view models, but it should not decide whether a topic can contain another topic or listing.

Alternative considered: make the tree a UI-only projection. That would be quicker for a first pane, but it would duplicate rules once persistence, creation tools, and search/filtering arrive.

### Represent visible tree entries as store, topic, and item roles

The tree should expose role-based nodes instead of hard-coding every future product entity into the navigation control. For Phase 0 and early Phase 1, stores are roots, niches and groups are topics, and listings are items.

Alternative considered: represent each entity type with a separate unrelated navigation shape. That would make store, niche, group, and listing screens easy to start, but it would make nested groups, movement, and future filtering harder to handle consistently.

### Store expansion and selection as presentation state over stable domain identities

Expansion, collapse, selected node, and revealed node state should be keyed by stable identifiers. These states are UI/application concerns and should not mutate the underlying workspace hierarchy.

Alternative considered: store expansion directly on domain entities. That would blur durable content with per-window presentation state and would make future multi-window or saved-view behavior more awkward.

### Use application services for movement and active context

Moving nodes and changing active context should go through application-level operations. These operations can validate hierarchy rules, preserve child nodes, and produce updated tree snapshots for the UI.

Alternative considered: let the tree control directly reorder bound collections. That risks invalid parent-child combinations and makes it hard to preserve invariants when persistence is added.

## Risks / Trade-offs

- Navigation may be designed before all Phase 1 entity management details are final -> Mitigation: keep the model role-based and avoid entity-specific fields that belong to later store, niche, group, and listing specs.
- Recursive topic depth can create UI performance issues with large workspaces -> Mitigation: specify arbitrary practical depth while allowing implementation to use lazy loading or projection later.
- Drag-and-drop can expand scope into polish and accessibility work -> Mitigation: define movement behavior now and leave advanced drag-and-drop polish out of scope.
- Active context can become ambiguous when both a topic and listing are selected -> Mitigation: define a single active navigation target and derive creation scope from the nearest valid store/topic context.
