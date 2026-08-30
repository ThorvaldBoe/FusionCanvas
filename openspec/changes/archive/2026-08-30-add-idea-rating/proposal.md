## Why

Creators currently have no durable, glanceable way to record how promising an idea feels or to return to the strongest candidates later. Issue #184 requests a five-star score with an explicit unrated state and filtering, which fits the existing Item-based idea workflow and is timely while navigation filtering is already established.

## What Changes

- Add a persisted 0–5 potential rating to each Item, where 0 means unrated.
- Show an accessible five-star rating control for active Items, with clicking a star setting that value and clearing the rating returning it to unrated.
- Keep the rating associated with the original idea as the Item advances through Concept, Design, and Listing.
- Add an exact-rating navigation filter with options for All, Unrated, and one through five stars.
- Combine rating filtering with existing text, tag, scope, stage, status, and archive filters using the existing AND behavior.
- Preserve rating through save/reload, archive/restore, duplication rules, CSV/workspace transfer paths, and authoritative refreshes as applicable.
- Add focused domain/application/integration/UI tests, including deterministic headless coverage for the star control and filter bindings.

## Capabilities

### New Capabilities

- `idea-rating`: Item rating semantics, editing behavior, persistence representation, and rating-filter behavior.

### Modified Capabilities

None. The rating and its filter are introduced as a self-contained capability; existing filtering and Item persistence rules remain applicable through the new capability's explicit integration scenarios.

## Impact

- Domain/Application: Item rating validation/defaulting, metadata or Item-state mapping, inspector state/save requests, and workspace-tree query/projection.
- Integration: workspace persistence and transfer/import/export compatibility for the chosen representation; no new external dependency is expected.
- App: Item inspector star control, accessible names/tooltips, navigation rating selector, filter state, and headless view tests.
- Existing Items remain unrated by default and must load without migration failure. The implementation should avoid a destructive schema migration unless discovery proves a first-class column is required.
- Origin: [FusionCanvas issue #184](https://github.com/ThorvaldBoe/FusionCanvas/issues/184).
