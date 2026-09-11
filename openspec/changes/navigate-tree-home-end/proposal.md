## Why

Creators who manage larger workspaces need a quick way to reach the first or last visible navigation entry. The existing tree supports arrow-key movement, but Home and End do not change the selected navigation context, making keyboard browsing slower than pointer or repeated-arrow navigation.

## What Changes

- Add Home keyboard navigation to select the first visible node in the expanded and filtered workspace tree.
- Add End keyboard navigation to select the last visible node in the expanded and filtered workspace tree.
- Preserve normal Home/End caret behavior while a tree name editor or other text box has focus.
- Keep selection, inspector context, and scrolling synchronized with the selected destination.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `group-management`: extend essential tree keyboard navigation with Home and End boundary navigation.

## Impact

- Affects the App navigation tree keyboard/input handling and its Avalonia headless tests.
- No domain, persistence, public API, or data migration changes.
- Uses the existing visible tree projection, selection coordination, and focus behavior.

