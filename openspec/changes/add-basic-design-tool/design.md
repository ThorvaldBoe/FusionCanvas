## Context

The in-flight design-records change adds the Design entity, approval-state machine, final-selected collection, intended-use and cleanup metadata, and asset/prompt references. The in-flight asset-relationships and prompt-history changes add multi-context asset linking and prompt records. The in-flight creative-history-timeline change adds the cross-stage event log. The accepted stage-tool-host, context-aware-tools, and workflow-stage-navigator capabilities define hosting, context, and stage advancement. The Phase 2 PRD makes the Basic Design Tool the default Design-stage tool: it hosts variant work, supports three production paths (manual, external AI, in-app AI), tracks approval and cleanup, promotes final artwork, and advances the item toward Listing. In-app AI generation is optional and depends on a configured image-generation provider.

What is missing is the tool itself: the hosting registration, the variant workspace, the import and generation flows, the cleanup actions, the final-selection UX, the timeline feeding, and the stage-advancement action.

## Goals / Non-Goals

**Goals:**

- Provide the default Design-stage tool for a single selected item, hosted through the Stage Tool Host.
- Support manual design import, external AI import, and optional in-app AI generation.
- Allow multiple design variants with name, status, notes, source method, intended use, cleanup state, related assets, and tags.
- Allow promoting one or more variants as final selected artwork without deleting other variants.
- Offer rudimentary cleanup actions through an image-processor port without becoming a full image editor.
- Feed important Design-stage events into the Creative History Timeline.
- Advance the item toward Listing once at least one final design variant is selected.

**Non-Goals:**

- Do not implement full raster or vector editing, layer-based authoring, guaranteed print quality, guaranteed originality, full legal validation, automatic marketplace product creation, mockup generation, listing metadata generation, or bulk multi-item generation.
- Do not implement the AI image provider or AI settings; those belong to future AI-capability changes.
- Do not implement design storage; that belongs to FC-0203.
- Do not implement asset import mechanics; that belongs to FC-0109 and FC-0204.

## Decisions

### 1. The tool is a Stage Tool Host built-in default

The Basic Design Tool is registered as the default Design-stage tool through the host-facing tool registry. It declares support for the Design stage and requires a selected-item context. When no item is selected, the host shows an empty state or creation path. Future plugin-provided Design-stage tools can coexist and remain selectable.

Alternative considered: open the tool without an item. Rejected because the PRD and stage-tool-host require item context for Design work.

### 2. A design brief is derived from the selected concept or sufficient listing fields

The tool derives a design brief from the selected concept when one exists. When no concept is selected, the tool uses listing-level idea, phrase, and graphic direction only when they are sufficient to form a brief, and otherwise shows a readiness note that a concept should be selected or brief fields supplied. The user can edit Design-stage notes and constraints without modifying the underlying concept unless they explicitly choose to update the concept.

Alternative considered: require a selected concept before opening the tool. Rejected because the PRD allows starting Design work from a strong phrase and graphic direction.

### 3. Three production paths share one variant model

Manual import, external AI import, and in-app AI generation all create or update design variants through the design-records service, with the appropriate source method. Manual and external-AI imports use the asset-management service to import files and link them to the design. In-app generation uses an `IImageGenerationProvider` port, imports the generated image as an asset, and records a prompt-history entry with provider/model and generation settings. Generated variants remain drafts until the user approves or promotes them.

Alternative considered: separate flows per source. Rejected because the variant model and approval states are shared.

### 4. In-app generation context is built from the full creative context

In-app generation requests include store, niche, topic path, item title, selected concept, idea, phrase, graphic direction, visual style notes, constraints, inherited tags and metadata, relevant sibling items, and accepted/rejected/superseded concept and design history. The user can describe or adjust Design-stage generation instructions (visual style, composition, text, constraints, output intent). AI generation never overwrites final selected artwork without explicit approval.

Alternative considered: send only the phrase. Rejected because the PRD requires rich context for useful generation.

### 5. Final selection is explicit and multi-variant

Promoting a variant as final adds it to the listing's final-selected collection through the design-records service. One or more variants can be final (e.g., separate final artwork for dark and light shirts). Importing or generating an image does not automatically make it final. Demoting a final variant removes it from the collection without deleting it. The item can advance to Listing only when at least one final variant is selected.

Alternative considered: auto-promote the latest import. Rejected because final selection is a key Design-stage decision that must be explicit.

### 6. Cleanup actions go through an image-processor port

Rudimentary cleanup actions (crop to visible artwork, transparency inspection, transparent-border removal, upscale flag, replacement attachment, mark needs revision) are offered through an `IImageProcessor` port. Built-in default processors may provide the simplest actions; plugins may provide advanced processors. Outcomes are recorded as asset or design metadata so later tools know whether artwork is ready. FusionCanvas does not become a full image editor.

Alternative considered: implement all cleanup in-app. Rejected because the PRD explicitly defers advanced cleanup to plugins.

### 7. Stage advancement goes through the workflow-stage-navigator

The tool's "proceed to Listing" action requests the workflow-stage-navigator to advance the listing. The navigator enforces the "at least one final-selected design" invariant accepted by design-records. The tool does not set the stage field directly.

Alternative considered: set the stage directly. Rejected to avoid duplicating stage-transition rules.

## Risks / Trade-offs

- [Image-generation port may drift before FC-0401/FC-0407 land] -> Keep the port minimal (generate-variants) and document it as provisional.
- [Cleanup port may fragment across plugins] -> Keep built-in defaults for the simplest actions and let plugins extend.
- [Variant accumulation can clutter the workspace] -> Show final and draft variants distinctly and default the listing-facing view to final variants.

## Migration Plan

No database migration is expected. The tool reads and writes through existing and in-flight services. Existing listings load with the tool available when their stage is Design.

## Open Questions

- Should in-app generation support batch generation of multiple variants in one request, or one variant per request? (Default: support batch generation of multiple variants in one request, since AI image tools commonly return several options.)
- Should cleanup actions be undoable within the tool, or recorded as metadata only? (Default: metadata only; undo/redo is out of scope for FC-0211.)
