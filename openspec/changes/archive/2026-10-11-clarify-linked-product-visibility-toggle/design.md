## Context

The Printify import dialog already hides linked products by default and filters its visible rows when the `ShowLinked` state changes. Its button currently retains a fixed “Show/hide linked products” label. This maintenance change makes the existing action and state understandable without changing the import workflow.

## Goals / Non-Goals

**Goals:**

- Bind the button label and accessible name to the same view-model state.
- Preserve current product filtering, selection, default visibility, and linked-product import restrictions.
- Verify the view-model binding and accessibility tree in a deterministic Avalonia headless view test.

**Non-Goals:**

- Change which products are eligible for import or the initial hidden state.
- Change dialog placement, layout, or the Printify import flow.
- Add a real-desktop Appium journey. This low-impact label and state binding is fully observable in headless coverage.

## Decisions

- Use imperative action labels: “Show linked products” when linked rows are hidden and “Hide linked products” when they are visible. This names the next action, consistent with the UX guidance to make expand/collapse state clear through its label or state.
- Bind both `Button.Content` and `AutomationProperties.Name` to one computed view-model property so visual and accessible wording cannot drift.
- Keep `ShowLinked` as the sole state and preserve `VisibleProducts` filtering; add property-change notification for the computed label.
- No data, persistence, API, migration, or compatibility changes are needed.

## Risks / Trade-offs

- [A label may become stale if ShowLinked changes without notifying the computed property] → Raise a `LinkedProductsToggleLabel` property change alongside `ShowLinked` and assert both label surfaces after each toggle in a headless view test.

## Migration Plan

No migration is required. The view-model property and bound label can be rolled back together without affecting stored data.

## Open Questions

None.

## Implementation Plan

1. Update `src/FusionCanvas.App/Stores/PrintifyListingImportViewModel.cs` to expose a computed `LinkedProductsToggleLabel` and notify it whenever `ShowLinked` changes.
2. Update `src/FusionCanvas.App/Stores/PrintifyListingImportWindow.axaml` to bind the button content and `AutomationProperties.Name` to that property.
3. Extend `tests/FusionCanvas.App.Tests/PrintifyListingImportWindowTests.cs` with a headless view test that includes linked and unlinked rows, inspects the button's visible and accessible labels and visible rows before toggling, activates the command, and verifies the updated labels/list while the linked row remains non-importable.
4. Run the focused test, the solution test baseline (`dotnet test .\FusionCanvas.sln -m:1`), and `openspec validate --all --strict`.
5. Record criterion-level evidence in `verification.md` and confirm the final change has no unrelated diff.

## Acceptance-to-verification mapping

| Acceptance scenario | Planned verification |
| --- | --- |
| Linked products are hidden by default | Headless Avalonia view test checks initial visible rows, button content, and accessible name. |
| User shows linked products | The same headless test activates the button and checks linked row visibility/indicator, ineligibility, and both labels. |
| User hides linked products again | The same headless test activates the button a second time and checks list and both labels return to the hidden state. |
