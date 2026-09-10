## Why

The navigation rating filter currently supports only exact ratings and unrated items, making it cumbersome to find ideas above or below a quality threshold. Adding comparison choices lets creators quickly narrow work to rating bands while preserving the existing filter workflow.

## What Changes

- Keep the existing All ratings, Unrated, and exact 1–5 rating choices.
- Add four greater-than choices: greater than 1, greater than 2, greater than 3, and greater than 4.
- Add four less-than choices: less than 5, less than 4, less than 3, and less than 2.
- Apply comparison filters immediately through the existing rating dropdown and tree projection.
- Leave persisted item rating values and unrated semantics unchanged.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `search-filtering`: Extend the navigation rating filter with greater-than and less-than comparisons.

## Impact

- `FusionCanvas.App` rating dropdown and filter view-model mapping.
- `FusionCanvas.Application` workspace-tree query and projection matching logic.
- Focused application-layer and view-model tests; no database migration, API, or new dependency is required.

