## Context

The selected-image editor is currently a two-column `Grid` inside an auto-sized `Border`. Its metadata column contains an unconstrained `ItemsControl` for `TemplateColorChoices`, so the desired height grows with every Color. The dialog's final action row is below that editor and can be pushed outside the usable viewport.

The change is presentation-only. The existing `OptionValueChoiceViewModel.IsSelected` bindings remain the source of truth for applicability, and no persistence or draft lifecycle behavior changes.

## Functional Design

- Keep the selected-image editor as the focused master-detail surface.
- Wrap only the Color choice `ItemsControl` in a named `ScrollViewer` with automatic vertical scrolling and a bounded height appropriate for the metadata column.
- Leave the Size/other-option choices and image-space mapping controls outside that Color scroller so they remain visible below it.
- Preserve the existing two-way `IsSelected` binding, `CanEdit` gating, keyboard navigation, and action-row layout.
- Give the scroll region an automation identifier so headless tests can verify the intended visual boundary without relying on incidental control ordering.

The scroll region is intentionally limited to Colors rather than the whole dialog: the user can browse a large collection without hiding the mapping fields or Save/Cancel actions, and the common case with a short list remains visually compact.

## Implementation Plan

1. Update `src/FusionCanvas.App/Stores/MockupTemplateEditorWindow.axaml` so the `TemplateColorChoices` `ItemsControl` is inside a bounded vertical `ScrollViewer`, with `VerticalScrollBarVisibility="Auto"` and an automation identifier.
2. Keep the existing color item template and bindings unchanged; ensure the metadata column can measure the bounded scroller without allowing the outer selected-image editor to grow with the collection.
3. Extend `tests/FusionCanvas.App.Tests/StoreEditorHeadlessTests.cs` with a rendered dialog test that adds more Color choices than the available height, verifies the named scroll region is constrained and configured for vertical scrolling, and verifies the Save and Cancel controls remain effectively visible.
4. Run the focused App tests, the solution baseline, and strict OpenSpec validation. Record criterion-level evidence in `verification.md`.

## Edge Cases and Decisions

- Zero or a small number of Colors: the scroller remains available but does not need to scroll; existing empty/incomplete behavior is unchanged.
- Read-only or Archived Store: the existing `CanEdit` binding still disables each checkbox; scrolling does not enable editing.
- Keyboard users: the standard Avalonia `ScrollViewer` retains focus/keyboard scrolling behavior and the checkbox controls remain in normal tab order.
- No persistence or migration is needed because only layout and test structure change.
- Do not redesign the broader dialog layout, add paging/search, or change applicability semantics in this module.

## Acceptance-to-Verification Mapping

| Acceptance scenario | Verification method |
| --- | --- |
| Many Colors remain reachable and bottom actions stay reachable | Avalonia headless rendered dialog test with a long Color collection and constrained dialog height; focused test plus solution baseline |
| Scrolling preserves selection and read-only behavior | Existing binding/read-only coverage plus focused rendered control assertions in the same App test area; solution baseline |
