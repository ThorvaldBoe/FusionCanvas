## Why

The Available choices cards currently collapse every Size and custom Option Value into a wrapped text summary. Large provider catalogs make those summaries difficult to scan and can make wrapped cards tall and uneven. A compact virtual grid keeps the saved choices legible within a bounded card while preserving the established value-management workflow.

## What Changes

- Replace the read-only text summaries for Size and custom Option cards with fixed-height, vertically scrollable virtual grids.
- Keep the current Color grid unchanged, preserve each Option's saved value order, and retain the existing **Manage values** and archive actions.
- Keep each card compact and bounded inside the existing wrapped layout; show an empty-state message for an Option with no active values.
- Make long values available in full through tooltips and automation text while visually trimming them to fit each row.
- Do not add search, sorting, paging, editing, or selection to these summaries.

The primary workflow is scanning configured choices while preparing an Offering's Variants; this is a frequent, read-only task in the existing Variant management workspace. **Manage values** remains the explicit focused action for edits. The interaction is limited to scrolling the summary grid; there are no drafts, selection state, destructive actions, or new focus-return behavior. Deterministic Avalonia headless coverage is warranted because the grid is nested in wrapped cards and its bindings, virtualization, and accessibility text must remain correct. A new Appium journey is not warranted: the existing desktop workflow is unchanged and its material risks are testable headlessly.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `variant-management`: Size and custom Option cards show bounded, read-only virtual grids in saved order and expose full long values accessibly.

## Impact

- `FusionCanvas.App`: Option choice group view model/provider setup and the Available choices card template in `StoreEditorWindow`.
- `FusionCanvas.App.Tests`: focused headless coverage for wrapped cards, saved order, bounded virtualization, full long values, unchanged Color behavior, and existing management/archive actions.
- `openspec/specs/variant-management`: clarify Size/custom summary behavior. No domain, persistence, API, or migration changes are expected.
- Main risk: per-card grids must refresh when the selected Offering or its active Option Values change without altering saved order or making the wrapped layout unbounded.
