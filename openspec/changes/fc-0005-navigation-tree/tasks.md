## 1. Domain Navigation Model

- [ ] 1.1 Review the active `FusionCanvas.Domain.Workspace` entities and relationships for Store, Niche, Group, and Listing alignment with navigation roles.
- [ ] 1.2 Add or update domain modeling for navigation node identity, node role, parent-child relationships, and ordered child collections.
- [ ] 1.3 Add domain rules that allow stores to contain top-level topic nodes, topics to contain child topics, and topics to contain listing item nodes.
- [ ] 1.4 Add domain rules that reject invalid hierarchy states, including orphaned topics/items, items outside valid topic context, and cycles in topic ancestry.
- [ ] 1.5 Add domain behavior for moving a topic while preserving its descendant topic and item subtree.
- [ ] 1.6 Add domain behavior for moving a listing item while preserving listing identity and associated context.

## 2. Application Navigation Contracts

- [ ] 2.1 Add application-facing contracts or use cases for loading a navigation tree projection from workspace data.
- [ ] 2.2 Add application-facing contracts or use cases for selecting a store, topic, or listing as the active navigation context.
- [ ] 2.3 Add application-facing contracts or use cases for resolving the creation scope from the active navigation context.
- [ ] 2.4 Add application-facing contracts or use cases for moving topics and listing items through validated operations.
- [ ] 2.5 Add application-facing behavior for revealing a topic or listing path from a stable node or workspace item identity.
- [ ] 2.6 Keep application contracts independent of Avalonia controls, SQLite provider details, marketplace APIs, AI providers, and plugin host details.

## 3. UI Tree Presentation

- [ ] 3.1 Add UI-owned presentation state for expanded nodes, collapsed nodes, selected node, and revealed node keyed by stable identities.
- [ ] 3.2 Add a navigation tree view model or equivalent presentation model that can render stores, topics, and listing items from the application projection.
- [ ] 3.3 Wire selection changes so the UI updates active navigation context through the application boundary.
- [ ] 3.4 Wire topic expansion and collapse so descendants are hidden or shown without mutating workspace hierarchy.
- [ ] 3.5 Add reveal behavior that expands required parent topics and highlights the related topic or listing.
- [ ] 3.6 Provide movement entry points suitable for future drag-and-drop while keeping advanced drag-and-drop polish out of scope.

## 4. Tests

- [ ] 4.1 Add `FusionCanvas.Domain.Tests` coverage for valid store, niche, group, and listing tree shapes.
- [ ] 4.2 Add `FusionCanvas.Domain.Tests` coverage for nested groups and practical topic depth.
- [ ] 4.3 Add `FusionCanvas.Domain.Tests` coverage proving topic moves preserve descendant subtrees.
- [ ] 4.4 Add `FusionCanvas.Domain.Tests` coverage proving listing moves preserve listing identity and associated context.
- [ ] 4.5 Add `FusionCanvas.Domain.Tests` coverage proving invalid moves are rejected without changing the hierarchy.
- [ ] 4.6 Add `FusionCanvas.Application.Tests` coverage for active navigation context and creation-scope resolution.
- [ ] 4.7 Add `FusionCanvas.Application.Tests` or `FusionCanvas.App.Tests` coverage for reveal-path and expansion/collapse decision logic.
- [ ] 4.8 Add tests or assertions that search/filtering, saved views, batch operations, item/topic conversion, marketplace, AI, plugin loading, and production persistence polish are not introduced by this change.

## 5. Validation

- [ ] 5.1 Run `dotnet test` for the solution or affected test projects.
- [ ] 5.2 Run `openspec status --change "fc-0005-navigation-tree"` and verify the change is apply-ready.
- [ ] 5.3 Update task checkboxes to reflect completed implementation work before archive.
