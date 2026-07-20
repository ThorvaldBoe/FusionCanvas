## Why

FusionCanvas can advance a listing to the Concept stage but cannot preserve alternate creative directions, the reasoning behind a chosen direction, or the superseded concepts a creator may want to revisit later. FC-0202 adds Concept Versions as the item-bound Concept-stage record that keeps idea, phrase, graphic direction, audience reaction, risks, quality notes, and design-triangle score metadata for a listing, with exactly one selected concept version supplying the current direction to the Basic Concept Tool.

## What Changes

- Add a Concept domain entity owned by a listing, scoped to the Concept stage, with stable identity and timestamps.
- Allow a listing to have multiple concept versions, including superseded and rejected versions, without deleting the prior direction.
- Track idea, phrase, graphic direction, audience reaction, risks, quality notes, and design-triangle score metadata on each concept version.
- Allow a concept to be created from an existing idea or directly from a phrase, graphic direction, or other later-stage starting point when the creator has enough direction.
- Mark exactly one concept version per listing as the selected direction; the selected concept supplies the current idea, phrase, and graphic values shown by the Basic Concept Tool.
- Allow accepting a large alternate direction from the Basic Concept Tool to create a new concept version instead of overwriting the selected one.
- Keep concept work item-bound: no free-floating concept exists without a listing.
- Inherit store, niche, topic path, item, tags, and metadata from the current context through the accepted context-aware boundary.

## Capabilities

### New Capabilities

- `concept-versions`: Defines item-bound Concept-stage concept records that preserve alternate creative directions for a listing, track idea/phrase/graphic/audience/risks/quality/score metadata, mark one selected direction, and keep superseded or rejected versions available for later review.

### Modified Capabilities

- `core-domain-model`: Adds the Concept entity to the core domain model and narrows the premature-advanced-entities exclusion so concept versioning is now an accepted Phase 2 concern while design versioning, mockup records, marketplace products, performance data, plugin data records, and custom workflow models remain out of scope.

## Impact

- Adds a Concept domain entity, application contracts and orchestration for create/update/select/supersede/reject, and SQLite persistence mappings through the existing snapshot boundary.
- Introduces a schema migration that adds the concept table and a selected-concept reference on the listing without breaking existing snapshots.
- Reuses `context-aware-tools` for inherited context, `workflow-stage-navigator` for stage ownership, and `listing-management` for item identity and archive ancestry.
- Adds a dependent creative record that listing-management's permanent-deletion guard must treat as a blocker.
- Adds domain, application, integration, app, and UI tests for item-binding, multi-version preservation, selected-direction invariants, supersede/reject, inheritance, persistence, and deletion-guard integration.
