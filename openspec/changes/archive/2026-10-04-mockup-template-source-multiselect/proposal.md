# Mockup Template Source Image Multi-Selection

## Outcome

Creators can select several local Mockup Template source-image rows in the revised source-image table and apply the table's supported archive action to that selection, while one active row continues to drive the detail editor.

## Included

- Add session-only multi-selection to the source-image table.
- Use plain click or Enter/Space to replace the selection, Ctrl-click or Ctrl+Enter/Space to toggle a row, and Shift-click or Shift+Enter/Space to select the visible range from the selection anchor.
- Keep the most recently selected row active for the existing metadata and placement editor.
- Show the selected-row count and make Archive selected archive every selected row.
- Add focused view-model and Avalonia headless coverage for selection gestures, action targeting, and accessible presentation.

## Non-goals

- No persistence of selection state.
- No bulk metadata or placement editing; those remain owned by the active row.
- No changes to source-image storage, readiness evaluation, upload behavior, sorting semantics, or provider mockup workflows.

## UX preflight

This is a frequent management action in the focused Mockup Template editor, not a primary-workspace action. The table remains the compact surface and keeps one detail editor below it. An empty table remains unchanged. A single selected row behaves as before; multiple selection exposes the count and applies only the archive action to all selected rows. Removing the active row makes the last remaining selected row active, or clears the detail editor when none remain. Keyboard users can perform the same replace, toggle, and range gestures through the focused row.

## Verification

Focused view-model tests cover replace/toggle/range selection and archive targeting. Avalonia headless tests cover modifier gestures, selected-row styling/count, keyboard access, and that archive does not accidentally affect unselected rows. The full solution baseline and strict OpenSpec validation are required.
