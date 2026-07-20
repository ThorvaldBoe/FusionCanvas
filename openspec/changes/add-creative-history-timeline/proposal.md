## Why

Concept, Design, Prompt, Mockup, Asset, and Listing operations all leave traces across their own records, but creators have no single read-only view to understand how a listing evolved without digging through notes, files, and prompt outputs. FC-0209 adds the Creative History Timeline as the read-only per-listing view that aggregates important creative events — concept changes, prompt records, status changes, asset imports, design updates, mockup records, and metadata changes — so a creator can review history without changing current listing state.

## What Changes

- Add a Creative History Timeline view for a selected listing that aggregates important creative events from Concept, Design, Prompt, Mockup, Asset, and Listing operations.
- Define the event categories the timeline records: concept-stage events (manual idea/phrase/graphic edits, AI suggestion requested/accepted/rejected, score updates, restored prior states, suggestions saved as new items), design-stage events (manual asset imports, external AI imports, in-app AI generations, prompt records, variant creation/rejection, cleanup actions, final variant promotion, final selection changes), listing-stage events (mockup records, metadata changes, status changes), and asset/prompt events.
- Preserve useful context on each timeline entry (what changed, when, related records, optional summary).
- Keep the timeline read-only: restoring a prior concept state belongs to the Concept tool or undo/redo, not the timeline.
- Let the Basic Concept Tool's focused history pane and the Basic Design Tool's activity area feed important entries into the broader timeline.

## Capabilities

### New Capabilities

- `creative-history-timeline`: Defines the read-only per-listing timeline that aggregates important creative events, the event categories and context preserved per entry, the writer contract other services use to record events, and the read-only review behavior that never mutates current listing state.

### Modified Capabilities

None. FC-0209 reuses `concept-versions`, `design-records`, `prompt-history`, `mockup-records`, `asset-management`, `listing-management`, and `listing-metadata-editor` as event sources without changing their accepted requirements. Each service records events through the timeline writer as an implementation integration owned by FC-0209.

## Impact

- Adds a `CreativeHistoryEvent` record (event kind, timestamp, listing id, related record references, summary, optional context) and a `CreativeHistoryTimelineService` that writes and reads events through the existing snapshot boundary with a forward-only schema migration.
- Integrates the timeline writer into concept-versions, design-records, prompt-history, mockup-records, asset-management, listing-management, and listing-metadata-editor operations as event-emission hooks.
- Adds a read-only timeline view model and surface for a selected listing with filtering by event kind and stage.
- Adds domain, application, integration, app, and UI tests for event recording, read-only review, filtering, context preservation, and persistence.
