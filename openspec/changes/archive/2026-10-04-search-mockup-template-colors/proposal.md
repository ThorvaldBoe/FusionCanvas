## Why

Mockup Template color applicability can contain many provider/catalog values, making it slow to locate the color being configured. A transient search field will reduce repetitive scrolling while preserving the existing color-selection workflow and creator control.

## What Changes

- Add an inline search field to the focused Mockup Template editor's Color applicability area.
- Filter the currently eligible color choices as the user types using case-insensitive substring matching against the displayed color label.
- Show all eligible colors when the search is empty and provide a clear no-match state when the query finds nothing.
- Preserve the selection state of colors hidden by the filter; saving, readiness evaluation, and draft tracking continue to consider all eligible choices.
- Keep the search query transient to the current editor session; do not persist it or alter provider eligibility, archived-value handling, or additional-option choices.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `mockup-template-management`: define searchable Color applicability choices in the focused Mockup Template editor, including empty-query, no-match, and hidden-selection behavior.

## Impact

- Affects the Avalonia Mockup Template editor and its `CatalogSetupViewModel` presentation state.
- Adds focused App-layer tests for filtering and selection preservation, plus headless view coverage for textbox binding and visible results.
- No domain model, application service, persistence schema, external API, or migration changes are expected.
- Real-desktop Appium coverage is not warranted for this small local filtering interaction; deterministic view-model and Avalonia headless tests provide the relevant coverage.
