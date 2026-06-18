## Why

FusionCanvas needs a navigation model that lets creators browse and reshape stores, topics, and listings before Phase 1 workspace features add full management screens. Establishing this foundation now gives creation tools, tabs, search, and future persistence a shared concept of the current workspace context.

## What Changes

- Introduce a navigation tree capability for browsing stores, top-level niche topics, nested group topics, and listing items.
- Define active navigation context behavior so new creation and generation actions can inherit the selected store, topic, or listing scope.
- Support conceptual movement of topics and items while preserving contained subtrees and item context.
- Define expansion, collapse, selection, and reveal behavior needed by the application shell and future tabbed document window.
- Keep full search/filtering, saved views, batch operations, advanced drag-and-drop polish, and item/topic conversion implementation out of this foundation change.

## Capabilities

### New Capabilities
- `navigation-tree`: Defines the hierarchical workspace navigation model for stores, topics, listings, active context, movement, expansion, and reveal behavior.

### Modified Capabilities

## Impact

- Affected code: `FusionCanvas.Domain` for navigation concepts and invariants, `FusionCanvas.Application` for navigation/query/use-case contracts, and `FusionCanvas.App` for tree presentation state when implemented.
- Affected specs: a new `navigation-tree` capability will become accepted behavior after implementation and archive.
- Dependencies: builds on the `core-domain-model` planning direction, but does not require external services, marketplace SDKs, AI provider SDKs, plugin loading, or production persistence polish.
