## Context

The Store Editor's Printify catalog import currently presents each loaded shop product in an `ItemsControl` containing a full-height `CheckBox` and three text lines. This materializes every row's visual controls. The project already uses the `AvaloniaVirtualDataGrid` control for large and fixed-row tabular surfaces, with headless tests for virtualization, column alignment, scrolling, and tooltip behavior.

The issue decision is to virtualize only rendering. Retrieval and the `ObservableCollection<PrintifyCatalogImportItemViewModel>` remain fully materialized before the selection grid is shown. Product identity, selected-state storage, explicit “Load selected” confirmation, load/error/cancel behavior, and Store Editor draft handling stay as they are.

The workflow is an occasional Store setup action, opened from Catalog & mockups in the Store Editor. It remains in the existing focused panel and does not consume persistent workspace area.

## Goals / Non-Goals

**Goals:**

- Render the loaded shop products in a fixed-height virtualized grid with aligned selection, Blueprint, product title, and product/provider identity columns.
- Keep checkbox selection keyboard-accessible and stable when visual rows are recycled.
- Keep complete identifying text available through tooltips and the checkbox accessible name.
- Prove the viewport realizes fewer than all loaded rows and that selected product IDs survive scrolling and are submitted only after explicit confirmation.

**Non-Goals:**

- Virtualize provider retrieval or the full result collection.
- Add search, sorting, paging, row selection, product editing, or new import behavior.
- Change Store scoping, async loading/error/cancel flow, local draft behavior, or import persistence/idempotency.
- Add Appium: deterministic headless view coverage exercises the new row realization, checkbox input, accessible names, and scroll/confirm journey. No native OS integration risk is introduced.

## Decisions

- Replace only the Printify product `ItemsControl` in `StoreEditorWindow.axaml` with the already referenced `VirtualDataGrid`. Keep `Blueprints` as the source so the import view model remains responsible for selected IDs and commands.
- Use a 72-pixel fixed row with four non-sortable, non-resizable columns: a 48-pixel checkbox column, a 170-pixel Blueprint column, a 280-pixel product-title column, and a 210-pixel product/provider identity column. Permit horizontal scrolling for the Store Editor's minimum-width layout rather than hiding identifying fields or collapsing their aligned columns.
- Set grid selection mode to `None`; the checkbox is the only selection owner. Use two-way binding to each item view model's `IsSelected`, preserving selection independent of realized row instances.
- Add a `SelectionAccessibleName` computed from the full Blueprint name, shop-product title, and product identity. Bind it to each checkbox's `AutomationProperties.Name`. Keep Blueprint name and product title as separate cells; give each trimmed `TextBlock` a tooltip containing its untrimmed value. The identity column displays the product ID and provider ID, with its full subtitle as tooltip/help text.
- Keep the grid fixed to a 360-pixel viewport and headers non-interactive. This shows several choices while keeping the confirmation and cancellation controls nearby; it does not add a search/filter row.
- Keep panel focus, Escape cancellation, existing busy/error/empty state, and the explicit `Load selected` command unchanged.
- Alternatives considered: a larger virtualized list of stacked checkboxes would lower row density and leave Blueprint/title scanning unaligned; paging or sorting is explicitly excluded; virtualization of retrieval is outside the accepted decision.

## Risks / Trade-offs

- [The grid is wider than the Store Editor's narrowest content region] → Keep all requested aligned columns and permit horizontal scrolling at narrow widths; automation names and tooltips preserve identifying text without relying on scrolling horizontally.
- [A recycled row could display the wrong checked state if selection were held in a control] → Bind each checkbox to the stable item view-model instance and verify its selected identity after scrolling away and back.
- [Tooltips may not be discoverable by all assistive technology] → Include the full Blueprint name and product title in the checkbox accessible name; tooltip is a second access path for pointer users.

## Migration Plan

No data or package migration is required. The existing loaded collection and import command contract remain unchanged. Reverting the grid XAML restores the old presentation without changing saved data.

## Open Questions

None. Issue #882 resolves the desired presentation and scope. The existing Store Editor context determines that this stays in its current focused panel.

## Implementation Plan

1. Update `src/FusionCanvas.App/Stores/PrintifyCatalogImportItemViewModel.cs` with a computed accessible name containing complete identifying fields; keep the existing `Id` and `IsSelected` as source of truth.
2. Update the Printify import panel in `src/FusionCanvas.App/Stores/StoreEditorWindow.axaml` to use `VirtualDataGrid` and `VirtualDataGridTemplateColumn` with the fixed row height and four specified columns. Bind `Blueprints`; place a two-way checkbox in the first column, disable row selection, sorting, and resizing, provide full-text tooltips/accessibility, and allow horizontal scrolling.
3. Extend `HeadlessCatalogClient` and `tests/FusionCanvas.App.Tests/StoreEditorHeadlessTests.cs` with a large-product fixture and a headless test that verifies the grid's dimensions/column policy, viewport row virtualization, checkbox accessibility/full text, selection through scroll recycling, and exact submitted product IDs after confirmation. Verify no selection submission occurs before the user activates `Load selected`.
4. Preserve and rerun current focused Printify import view-model tests for Store scope, empty/error/cancel, draft preservation, and confirmation behavior.
5. Run focused UI and view-model tests, the solution baseline (`dotnet test .\FusionCanvas.sln -m:1`), strict OpenSpec validation, and diff checks. Record scenario-level evidence in `verification.md`.

## Acceptance-to-verification mapping

| Acceptance scenario | Planned verification |
| --- | --- |
| Many products are displayed in a virtualized grid | Headless Store Editor test loads 120 products, verifies aligned fixed columns and non-interactive headers, sees fewer realized rows than products, and observes scroll extent beyond the viewport. |
| Selection survives row recycling | The headless test clicks checkboxes for top and bottom products with scrolling between, verifies selected state after returning to each row, then confirms and asserts the client receives precisely those stable product IDs. |
| Long product text remains available | The headless test uses long Blueprint and product titles, verifies visible truncation, complete tooltip values, and full checkbox accessible name. |
