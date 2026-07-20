## 1. Creative History Event Model

- [ ] 1.1 Add a `CreativeHistoryEvent` record with event kind, timestamp, listing id, related-record references, summary, and optional context metadata.
- [ ] 1.2 Define the event-kind enum covering Concept, Design, Prompt, Mockup, Asset, and Listing categories with an `other` fallback for unknown kinds.
- [ ] 1.3 Add domain tests for event recording, unknown-kind preservation, related-record references, and context metadata.

## 2. Timeline Application Layer

- [ ] 2.1 Add a `CreativeHistoryTimelineService` with a `RecordEvent` writer and a `LoadTimeline` reader scoped to a listing.
- [ ] 2.2 Implement atomic event recording inside the originating operation's snapshot so failed operations record no event and successful operations record exactly one.
- [ ] 2.3 Integrate the writer into concept-versions, design-records, prompt-history, mockup-records, asset-management, listing-management, and listing-metadata-editor operations as event-emission hooks.
- [ ] 2.4 Add application tests for event recording, atomicity with originating operations, listing-scoped loading, and repository-failure behavior.

## 3. Persistence and Migration

- [ ] 3.1 Add a forward-only schema migration that creates the creative-history-event table and increments the snapshot version.
- [ ] 3.2 Migrate existing listings by assigning zero events without data backfill.
- [ ] 3.3 Add SQLite integration tests for event recording across services, listing-scoped loading, filtering, complete reload, and migration of pre-existing snapshots.

## 4. Timeline View and Cross-Capability Integration

- [ ] 4.1 Add a read-only timeline view model and surface for a selected listing with reverse-chronological display, event-kind/stage filtering, and read-only detail with context metadata.
- [ ] 4.2 Link related-record references to their surfaces (concept, design, prompt, mockup, asset) when available.
- [ ] 4.3 Show an empty state for listings with no events.
- [ ] 4.4 Apply compact action sizing, icon tooltips, keyboard-reachable filtering and selection, busy states, and shared desktop control guidance.
- [ ] 4.5 Add view-model and UI tests for load, filter, detail, empty state, read-only enforcement, related-record links, and shared control guidance.

## 5. Verification

- [ ] 5.1 Run domain, application, integration, app, and UI automation test suites and resolve concept-versions, design-records, prompt-history, mockup-records, asset-management, listing-management, and listing-metadata-editor regressions.
- [ ] 5.2 Manually verify event recording across stages, reverse-chronological display, filtering, read-only detail, related-record links, empty state, atomicity, and application reload.
- [ ] 5.3 Run strict OpenSpec validation and confirm every creative-history-timeline scenario is covered by implementation or automated tests.
