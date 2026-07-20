## Why

Concept work captures what a design is about, but FusionCanvas cannot yet preserve the concrete visual implementations of that concept — the imported source files, generated images, exported artwork, and color/product variants a creator produces for one item. FC-0203 adds Design Records as the item-bound Design-stage entity that tracks each design attempt, its source method, approval state, intended use, related assets, prompt context, and final-selection status so downstream Listing, mockup, export, and marketplace work can rely on selected final artwork.

## What Changes

- Add a Design domain entity owned by a listing, scoped to the Design stage, with stable identity, timestamps, name, version, notes, approval state, source method, intended use, cleanup state, and optional reference to the concept it implements.
- Allow a listing to have one or more design records and preserve draft, rejected, needs-revision, and superseded variants without deletion.
- Add design approval states: draft, needs revision, approved, rejected, exported, and ready for export. Approval state is independent of final selection and is used for tracking and review, not for gating final promotion.
- Allow a design to be associated with assets through the existing asset-management capability and to reference prompt history, provider metadata, and generation settings when AI-assisted.
- Track intended use as structured design metadata (dark shirts, light shirts, specific colors, product types, marketplaces, or export targets), not as design-scoped tags.
- Allow one or more designs to be promoted as final selected artwork at the listing level; promotion is marked by a `final` tag and listing-level collection membership, does not require a specific approval state, and does not delete other variants.
- Track basic cleanup or preparation state (needs upscale, needs transparent cleanup, cropped, print-ready) as design metadata.
- Inherit store, niche, topic path, item, selected concept, tags, and metadata from the current context through the accepted context-aware boundary.

## Capabilities

### New Capabilities

- `design-records`: Defines item-bound Design-stage records that preserve concrete visual implementations of a listing's selected concept, track approval state, source method, intended use, related assets, prompt context, cleanup state, and final selection, and keep superseded or rejected variants available for later review.

### Modified Capabilities

- `core-domain-model`: Adds the Design entity to the core domain model and narrows the premature-advanced-entities exclusion so design versioning is now an accepted Phase 2 concern alongside concept versioning, while mockup records, marketplace products, performance data, plugin data records, and custom workflow models remain out of scope.

## Impact

- Adds a Design domain entity, application contracts and orchestration for create/edit/state-change/promote-final/demote/cleanup, and SQLite persistence mappings through the existing snapshot boundary with a forward-only schema migration.
- Adds a listing-level final-selected-designs collection with invariant enforcement (only approved or ready designs may be promoted; at least one final design required before advancing to Listing).
- Reuses `asset-management` for asset links, `concept-versions` for the implemented-concept reference, `context-aware-tools` for inherited context, `workflow-stage-navigator` for stage ownership, and `listing-management` for item identity and deletion guarding.
- Treats designs as dependent creative records for the listing permanent-deletion guard.
- Adds domain, application, integration, app, and UI tests for item-binding, multi-variant preservation, approval-state transitions, final-selection invariants, intended-use tagging, prompt/asset references, cleanup metadata, persistence, migration, and deletion-guard integration.
