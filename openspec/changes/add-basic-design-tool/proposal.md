## Why

Design Records, Asset Relationships, Prompt History, and the Creative History Timeline together preserve design data, but creators have no default Design-stage tool that hosts variant work, supports manual import, external AI import, and in-app AI generation, tracks approval and cleanup state, promotes final artwork, and advances the item toward Listing. FC-0211 adds the Basic Design Tool as the default built-in Design-stage tool, hosted through the Stage Tool Host, usable with any of the three valid production paths and optional in-app AI generation when an image-generation provider is configured.

## What Changes

- Add the Basic Design Tool as the default built-in Design-stage tool, available from the Design stage for an existing item and unavailable without one.
- Require item context and a selected concept or sufficient listing-level idea/phrase/graphic direction to form a design brief.
- Support manual design import of source files and exported artwork through the existing asset-management capability.
- Support importing externally generated AI artwork and optionally recording the prompt or source tool through prompt-history.
- Support in-app AI design generation when an image-generation provider is configured, building generation context from store, niche, topic path, item, selected concept, idea, phrase, graphic direction, style notes, constraints, inherited tags, and relevant sibling items.
- Allow multiple design variants per item with name, status, notes, source method, intended use, cleanup state, related assets, and tags.
- Allow promoting one or more variants as final selected artwork without deleting rejected, draft, or superseded variants.
- Offer rudimentary cleanup actions through an image-processor port (crop to visible artwork, transparency inspection, transparent-border removal, upscale flag, replacement attachment) without becoming a full image editor.
- Feed important Design-stage events into the Creative History Timeline.
- Advance the item toward Listing once at least one final design variant is selected, through the workflow-stage-navigator.

## Capabilities

### New Capabilities

- `basic-design-tool`: Defines the default built-in Design-stage tool that hosts variant work, supports manual import, external AI import, and optional in-app AI generation, tracks approval and cleanup state, promotes final artwork, feeds the timeline, and advances the item toward Listing.

### Modified Capabilities

None. FC-0211 reuses `design-records` for variant storage and final selection, `asset-relationships` and `asset-management` for asset links, `prompt-history` for prompt context, `creative-history-timeline` for cross-stage history, `stage-tool-host` for hosting and context, `context-aware-tools` for inherited context, `concept-versions` for the selected concept, and `workflow-stage-navigator` for stage advancement without changing their accepted requirements. In-app AI generation is accessed through an abstract image-generation provider port that a future AI-capability change will implement; cleanup actions through an image-processor port that plugins may provide.

## Impact

- Adds a `BasicDesignTool` view model and Avalonia tool registered as the default Design-stage tool through the Stage Tool Host registry.
- Defines an `IImageGenerationProvider` application port for in-app AI generation and an `IImageProcessor` port for cleanup actions; future AI-capability and plugin changes provide implementations.
- Reuses the design-records service for all persistence and the asset-management service for asset import and context links.
- Adds app and UI tests for manual import, external AI import, in-app generation against a deterministic fake provider, variant management, final selection, cleanup actions, timeline feeding, and stage advancement.
- No schema migration is expected; the tool reads and writes through existing services.
