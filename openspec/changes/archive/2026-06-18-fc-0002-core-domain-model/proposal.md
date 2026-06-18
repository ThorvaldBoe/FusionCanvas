## Why

FusionCanvas needs a shared domain vocabulary before Phase 1 workspace features begin adding screens, persistence, and navigation around Print-on-Demand work. Establishing the core model now keeps stores, topics, listings, creative context, and classification language consistent instead of letting each feature invent its own shape.

## What Changes

- Introduce the Phase 0 core domain model for Store, Niche, Group, Listing, Asset, Prompt, and Tag.
- Define the baseline relationships between top-level business context, topic organization, product items, connected resources, preserved prompt context, and flexible labels.
- Distinguish topic concepts from item concepts so later navigation and workspace features can build on the same language.
- Keep future entities such as concepts, designs, mockups, marketplace products, plugin records, and performance data out of the Phase 0 core model until their workflows are specified.

## Capabilities

### New Capabilities
- `core-domain-model`: Defines the initial FusionCanvas domain entities, their relationships, and the Phase 0 boundaries for topics, items, assets, prompts, and tags.

### Modified Capabilities

## Impact

- Affected code: `FusionCanvas.Domain` will gain the initial domain concepts and tests when implemented.
- Affected specs: a new `core-domain-model` capability will become the accepted behavior after implementation and archive.
- Dependencies: no external services, UI frameworks, persistence engines, marketplace SDKs, AI provider SDKs, or plugin host contracts are required for this change.
