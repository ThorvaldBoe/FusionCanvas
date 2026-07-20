## Context

The in-flight concept-versions, design-records, prompt-history, and mockup-records changes add the creative records whose lifecycle the timeline must aggregate. The accepted listing-management, asset-management, and listing-lifecycle-status capabilities already perform operations whose history matters. The Phase 2 PRD requires a Creative History Timeline that shows important listing events, preserves useful context per entry, supports review without changing current state, and accepts important entries from the Basic Concept Tool's history pane and the Basic Design Tool's activity area. Restoring a prior concept state belongs to the Concept tool or undo/redo, not the timeline.

What is missing is the event model, the writer contract, the read-only view, and the event-emission integration into existing services.

## Goals / Non-Goals

**Goals:**

- Add a read-only per-listing timeline that aggregates important creative events.
- Define event categories and the context preserved per entry.
- Provide a writer contract that concept-versions, design-records, prompt-history, mockup-records, asset-management, listing-management, and listing-metadata-editor use to record events.
- Keep the timeline read-only and never mutate current listing state.
- Persist events through the existing atomic snapshot model with a forward-only schema migration.

**Non-Goals:**

- Do not implement full audit logging, undo/redo, collaboration comments, or automated recommendations.
- Do not implement the Basic Concept Tool history pane or Basic Design Tool activity area UIs; those belong to FC-0210 and FC-0211.
- Do not restore prior concept states from the timeline; that belongs to the Concept tool or undo/redo.
- Do not implement cross-listing timelines.

## Decisions

### 1. Creative history events are a separate append-only record

A `CreativeHistoryEvent` is stored as its own record with event kind, timestamp, listing id, related record references (concept, design, prompt, mockup, asset, listing), a short summary, and optional context metadata. Events are append-only: the timeline never edits or deletes events except through explicit workspace-level cleanup (out of scope). The timeline reads events scoped to a listing.

Alternative considered: compute the timeline on demand from each service's records. Rejected because events like "AI suggestion rejected" or "prior state restored" are not recoverable from current state.

### 2. A writer contract lets services record events without coupling

The `CreativeHistoryTimelineService` exposes a `RecordEvent` writer that other application services call after a successful operation. The writer accepts an event kind, the listing id, related record references, a summary, and optional context. Services integrate by calling the writer as part of FC-0209's implementation; the other capabilities' specs do not change because the emission is an implementation detail owned by the timeline.

Alternative considered: have the timeline subscribe to a domain event bus. Rejected because FusionCanvas does not currently have a domain event bus and introducing one for FC-0209 would be speculative.

### 3. Event categories cover Concept, Design, Listing, Asset, and Prompt stages

The event-kind enum covers: concept manual edit, concept AI suggestion requested, concept AI suggestion accepted, concept AI suggestion rejected, concept score updated, concept version created, concept prior state restored, suggestion saved as new item, design manual import, design external AI import, design in-app AI generation, design prompt recorded, design variant created, design variant rejected, design cleanup action, design final promotion, design final selection change, mockup record created, mockup superseded, listing metadata change, listing status change, asset import, asset removal, prompt saved, prompt rejected, prompt superseded. Unknown kinds are preserved as `other` so future operations remain visible.

Alternative considered: free-form event text. Rejected because filtering and review benefit from a known enum.

### 4. The timeline is read-only and context-preserving

The timeline view lists events in reverse-chronological order with their kind, timestamp, summary, and related-record references. Selecting an event opens a read-only detail with the full context metadata. The timeline never offers restore, edit, or delete actions; restoring a prior concept state belongs to the Concept tool or undo/redo. The view supports filtering by event kind and stage.

Alternative considered: allow restore from the timeline. Rejected because the PRD explicitly assigns restore to the Concept tool or undo/redo.

### 5. Forward-only schema migration

A new creative-history-event table is added through the existing snapshot and migration path. Existing listings load with zero events. No down-migration is provided.

Alternative considered: store events inside listing JSON. Rejected because per-listing event queries and future cross-listing analytics benefit from a typed table.

## Risks / Trade-offs

- [Event emission can fail silently] -> Record events inside the same atomic snapshot as the originating operation so a failed save records no event, and a successful save records exactly one event.
- [Event enum can drift as capabilities evolve] -> Keep an `other` kind and preserve unknown kinds so future operations remain visible without a migration.
- [Timeline can grow large] -> Keep per-listing pagination in the view and defer retention policies to a later change.

## Migration Plan

Increment the workspace snapshot version, add the creative-history-event table, and migrate existing listings by assigning zero events. No data backfill is required. Down-migration is unsupported.

## Open Questions

- Should the timeline show asset and prompt events that are not directly about the selected listing but reference one of its records? (Default: yes, include any event whose related-record set references the listing.)
- Should the timeline support export for external review? (Default: no, defer to a later change.)
