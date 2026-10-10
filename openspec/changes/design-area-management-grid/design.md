## Context

Manage Design Areas is an Offering-scoped collection inside the Store Editor. Its current `ItemsControl` renders each Design Area as a variable-height card with stacked summary text and Edit/Archive buttons. The Store Editor opens at 860 px wide and has a 720 px minimum; at normal width the right-hand management surface is about 524 px wide, falling to about 384 px at minimum width. The application already uses `VirtualDataGrid` for Sellable Variants and Mockup Templates. Its row panel needs an `IDataProvider` snapshot, and its tunneling pointer handler can suppress embedded button clicks unless the containing window captures and routes the pointer action explicitly.

The current Design Area modal editor, row commands, archive confirmation, dependency policy, empty-state text, and Store editability rules remain authoritative. This change replaces the collection presentation only.

## Goals / Non-Goals

**Goals:**

- Align Design Area names, placements, pixel maximums, compatibility summaries, and row actions in a compact table.
- Keep the primary artwork-generation marker visible and provide full-value tooltips/accessibility names for compact cells.
- Keep the complete grid at the Store Editor's normal width and provide horizontal scrolling at its minimum width.
- Preserve per-row Edit/Archive targeting, dialog focus restoration, archive confirmation and safeguards, keyboard activation, the empty state, and Add Design Area.
- Keep row layout fixed-height and compact for predictable virtualization.

**Non-Goals:**

- Search, sorting, column resizing/reordering, selection-driven detail panes, or new Design Area operations.
- Changes to catalog use cases, domain rules, persistence, or archive policy.
- New Appium coverage: the workflow and operations do not change, and deterministic headless tests can exercise the view and routed input.

## Decisions

1. **Use the existing virtual grid and a window-owned in-memory provider.** This matches the neighboring Store Editor collections and avoids introducing another list implementation. `DesignAreaCards` remains the source of truth; the provider is refreshed from its snapshots when the collection changes, and is cleared when the Store Editor detaches from its catalog view model.

2. **Use fixed columns sized for the normal management viewport.** The grid has Name (104 px), Placement (62 px), Maximum size (100 px), Compatibility (106 px), and Actions (112 px) columns. The 484 px total fits the approximately 494 px grid viewport at normal window width. The virtual grid still reports a 600 px horizontal extent, so its scrollbar remains Auto; the actual five columns fit before the blank trailing extent at normal width. At the 720 px minimum width, horizontal scrolling exposes the full row and action column; the heading and Add Design Area button remain outside the scrolling grid. No column is removed or hidden at narrow widths.

3. **Keep compact cell values at a fixed row height with full text on demand.** Name, placement, and compatibility summary use ellipsis when their text exceeds the column. The compatibility cell shows the compatibility summary and, when applicable, a visible **Primary** marker; its tooltip and automation name explain the full primary-for-artwork-generation status. The grid uses the component's fixed 32 px row height. Tooltips and automation names expose full values. The maximum-size cell stays on one line.

4. **Keep Edit and Archive as buttons in each row.** Their existing commands and card command parameters remain bound to the row. Keyboard activation continues through normal Button command handling. Because the virtual grid's tunnel handler can suppress embedded pointer input, extend the Store Editor's existing Sellable Variant pointer-capture workaround to Design Area buttons: capture on press, determine whether release remains inside the same row button, then allow its existing command to run once. Maintain the button `Content` values `Edit` and `Archive`, which the current focus-restoration handlers use to locate the invoking row.

5. **Do not add search or sorting.** Design Areas represent a small set of printable regions for one Offering; the issue provides no evidence of collection sizes that justify extra controls. Direct scanning remains the simplest interaction.

## Risks / Trade-offs

- [Risk] Embedded buttons may stop responding to pointer input under the grid's tunnel handler → Reuse and extend the existing pointer capture/release pattern, with headless pointer tests for both actions and ordinary keyboard activation checks.
- [Risk] The grid may measure wider than the declared columns or the action labels may not fit at 112 px → Verify actual cell extents and viewport at normal and minimum window sizes; adjust compact button padding or column widths within the same total-width target, and retain Auto horizontal scrolling as the safety path.
- [Risk] Ellipsized compatibility or primary status could hide context → Expose the full combined value through a tooltip and an accessible name, and assert both in the rendered view test.
- [Trade-off] At minimum width some columns require horizontal scrolling → This keeps row actions attached to their Design Area and preserves readable columns; the scrollable grid does not displace the Add action.

## Migration Plan

No persisted data or schema changes. Replace the card collection markup with the grid, wire its provider lifecycle and row pointer routing, and retain all existing commands and confirmation windows. Rollback is a view-only restoration of the `ItemsControl` markup and its provider/event wiring.

## Open Questions

None. The column set, normal and minimum width behavior, and action interaction are resolved above.

## Implementation Plan

### Layers and files

- **App view:** `src/FusionCanvas.App/Stores/StoreEditorWindow.axaml` — replace the Design Area `ItemsControl` with `DesignAreaGrid`, define its five fixed columns and fixed row height, set horizontal scrolling to Auto, and keep the heading, empty state, and Add action outside the grid.
- **App view code-behind:** `src/FusionCanvas.App/Stores/StoreEditorWindow.axaml.cs` — own `InMemoryDataProvider<DesignAreaCardViewModel>`, attach/detach `DesignAreaCards.CollectionChanged`, refresh snapshots, and route pointer input for the grid's row buttons. Preserve existing edit/archive dialog focus handlers; keep the `Edit` and `Archive` content labels and row `DataContext` so those handlers continue finding the invoking row.
- **Presentation row model:** `src/FusionCanvas.App/Stores/DesignAreaCardViewModel.cs` — add a computed full compatibility/status accessibility summary only if a binding expression cannot express the same string clearly; do not add state or change catalog behavior.
- **Tests:** `tests/FusionCanvas.App.Tests/StoreEditorHeadlessTests.cs` — add rendered headless coverage for the grid headers/rows and values, empty and add states, tooltip/accessibility values, actions, focus return, archive confirmation/cancellation, and normal/minimum-width scrolling. Reuse the existing Store Editor test setup and fixtures.
- **OpenSpec:** update `tasks.md` as work completes and create `verification.md` with one result per scenario below.

### Sequencing and behavior

1. Add the Design Area provider and its lifecycle alongside `_sellableVariantRows` and `_mockupTemplateRows`. On collection updates, reset it from `catalog.DesignAreaCards.ToArray()` and rebind the grid's `DataContext` if needed to recalculate the virtualized view. On DataContext replacement or window close, detach the collection handler and clear the provider.
2. Replace only the card list. Keep the management panel's automation ID and `IsVisible` binding. Bind the grid rows to `DesignAreaCardViewModel`; expose Name, Placement, `MaximumSizeSummary`, `CompatibilitySummary`, and visible primary status in their respective cells. Bind each action to the same `EditPlaceholderCommand` or `ArchivePlaceholderCommand` and pass its row as `CommandParameter`.
3. Extend pointer routing for Design Area buttons without changing command policy. On pointer press, locate the row/button (visual ancestry or hit test) and capture the button. On release, execute only when released within that captured button. Keyboard invocation continues through bound commands. The action's `DataContext` stays the card, preserving existing focus return after Edit closes or archive confirmation is canceled.
4. Keep Add Design Area visible above the grid, including when the scrollable collection is empty or the window is at minimum width. Preserve Store read-only gating on the action, and do not allow grid pointer routing to bypass command `CanExecute`.
5. Verify each acceptance scenario independently in the headless view tests. Confirm the normal viewport can reach the full Actions column without horizontal scrolling; at minimum width confirm the scrollbar can reach that column while the header and Add action remain in the visible layout. Run the focused Store Editor/catalog regressions, the full solution test baseline, and strict OpenSpec validation. No migration or external-service check is applicable.

### Acceptance-to-verification map

| Acceptance scenario | Verification |
| --- | --- |
| User scans configured Design Areas | Headless rendered grid test asserts the five headers, one aligned row per card, all summary values, primary marker, full tooltip/accessibility value, and row-specific action data contexts. |
| User opens the empty Design Area collection | Headless test asserts empty-state message and visible/enabled Add action for an editable Store, plus existing no-edit behavior for an archived Store. |
| User edits or archives a row | Headless routed pointer and keyboard tests assert the selected card's Edit dialog, row focus restoration, archive confirmation, cancel focus restoration, and existing archive safeguards. |
| User reviews Design Areas at minimum window width | Headless layout test measures the normal and minimum widths, asserts all five declared columns fit in the normal viewport, confirms horizontal scroll extent and action-column reachability at minimum width, and checks the heading and Add action stay visible. |
