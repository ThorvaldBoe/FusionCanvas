## Why

The Prompt entity already exists as a deferred AI-context placeholder, but FusionCanvas cannot yet save prompt inputs and outputs, identify prompt type, record provider and model information, associate prompts with concepts and designs, or preserve useful rejected or superseded outputs for later reuse. FC-0205 adds Prompt History as the capability that turns the Prompt entity into a reviewable, reusable record across Idea, Concept, Design, and Listing work without executing AI or storing secrets.

## What Changes

- Allow users to save prompt inputs and outputs as Prompt records.
- Associate prompts with stores, niches, listings, concepts, designs, or assets.
- Record prompt type (idea, phrase, graphic, image, listing text, critique, other).
- Record provider and model information when available, without storing API keys or secrets.
- Preserve useful rejected or superseded prompt outputs for later review and reuse.
- Surface prompt history per associated context and at store level.
- Keep prompts as dependent records for listing, concept, and design deletion guarding.

## Capabilities

### New Capabilities

- `prompt-history`: Defines saving, associating, typing, reviewing, and reusing Prompt records across stores, niches, listings, concepts, designs, and assets, with provider/model metadata, rejected/superseded preservation, per-context visibility, and atomic persistence.

### Modified Capabilities

- `core-domain-model`: Extends the Prompt entity to associate with concepts and designs in addition to stores, topics, listings, and assets.

## Impact

- Adds a `PromptHistoryService` application service, view model, and focused surface for reviewing prompts per context and at store level.
- Adds SQLite persistence mappings for prompt inputs, outputs, type, provider/model, associations, and lifecycle markers through the existing snapshot boundary with a forward-only schema migration.
- Treats prompts as dependent creative records for listing, concept, and design deletion guards (listing-management already blocks on prompts; concept and design guards gain prompt blocking).
- Reuses `asset-management` for asset associations, `concept-versions` and `design-records` for concept/design associations, and `context-aware-tools` for inherited context.
- Adds domain, application, integration, app, and UI tests for save, associate, type, provider/model, rejected/superseded preservation, per-context visibility, deletion-guard integration, and atomic persistence.
