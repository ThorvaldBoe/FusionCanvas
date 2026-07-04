## Why

FusionCanvas tools need to understand the user's active workspace context so creation and generation actions produce targeted work instead of generic output. This belongs in the foundation because later Idea, Concept, Design, Listing, AI, and automation tools all depend on a shared context contract.

## What Changes

- Introduce a context-aware tool capability that defines the workspace scope available to creation and refinement tools.
- Define how tools determine active store, niche, topic path, selected topic, selected item, workflow stage, inherited tags, metadata, and nearby work.
- Require topic-based creation tools to place new work in the selected topic by default and apply inherited context where appropriate.
- Require item-bound tools to receive the selected item's full parent context and communicate clearly when no item is selected.
- Allow tools to inspect sibling and nearby accepted, created, rejected, or archived work when it is relevant to avoiding duplication or using negative guidance.
- Require tool surfaces to expose the scope being used and allow intentional scope changes without making context use ambiguous.
- Establish context-aware behavior as a cross-cutting principle for manual, generative, asset, listing, and future automation workflows.

## Capabilities

### New Capabilities
- `context-aware-tools`: Defines the shared workspace context contract and default behavior for tools that create, generate, refine, import, or branch work from the active FusionCanvas context.

### Modified Capabilities

## Impact

- Affected layers: domain context types, application services/use cases for resolving tool context, and UI view models that expose the active navigation and stage scope.
- Future integration points: Stage Tool Host, built-in Idea/Concept/Design/Listing tools, plugin-provided tools, AI provider workflows, asset import, and automation commands.
- Persistence impact: no new persistence engine is required, but the context resolver depends on existing and future store, niche/topic, item, tag, metadata, and work-status data.
- Testing impact: unit tests should cover context resolution, inherited tags/metadata, topic-default placement, item-bound requirements, sibling/nearby work selection, and visible scope summaries.
