## Context

The Store Editor currently renders `CatalogSetupViewModel.SellableVariantRows` with an `ItemsControl` containing a repeated `Grid`. `SellableVariantRowViewModel` already resolves each explicit Variant's stable Color, Size, and Other values, and marks unresolved values as `Unavailable value`. The row archive button passes its view model to `CatalogSetupViewModel.ArchiveVariantCommand`; the view model rejects stale identities, calls the existing catalog archive service, and presents dependency or persistence errors. `AvailableVariants` filters archived records, and refreshing the catalog snapshot rebuilds the row collection and count.

The existing virtual-grid experiment and Mockup source-image table show the required app-owned `InMemoryDataProvider<T>` lifecycle and the grid's tunneling pointer behavior. Interactive child buttons need focused rendered-view tests; the grid's data provider must be reset from the full current collection snapshot.

## Goals / Non-Goals

**Goals:**

- Align current Variant names and resolved semantic values in a compact virtual grid.
- Preserve active order, active-only visibility, existing archive command ownership, and the existing count/list refresh.
- Keep long cell text available without variable row height or column overlap.
- Verify per-row Archive targeting and pointer and keyboard operation in Avalonia headless view tests.

**Non-Goals:**

- Search, sorting, paging, or changes to default ordering.
- Showing archived Variants or changing restore workflows.
- A new confirmation step, archive policy, dependency policy, or error message.
- Changes to Variant creation dialogs, domain/application behavior, or persistence.
- A real-desktop Appium journey; the focused headless tests cover the material grid binding, layout, row input, and archive feedback risks.

## Decisions

- Use the existing `VirtualDataGrid` package and fixed 32-pixel rows. This reuses the repository's demonstrated grid pattern; a new control or provider abstraction is not warranted for one table.
- Bind text columns to the existing immutable `SellableVariantRowViewModel` properties `Name`, `Color`, `Size`, and `Other`. Use column headings to identify semantic values, retaining the current `Unavailable value` label when data cannot be resolved. Render absent semantic values as an empty cell; do not invent an option value.
- Keep headers read-only/non-sortable and preserve collection order because no scale evidence justifies new search or sorting behavior. Disable unsupported horizontal overflow and use ellipsis plus full text in tooltips/help text for long values.
- Keep Archive as a row template button whose command parameter is the realized row's `SellableVariantRowViewModel`. Adapt grid-level pointer routing in `StoreEditorWindow` so a click over the child action dispatches the existing command once; retain the Button click path for keyboard activation. Do not change `RunArchive` or add confirmation behavior.
- Let `StoreEditorWindow` own an `InMemoryDataProvider<SellableVariantRowViewModel>`. Reset it from the complete `SellableVariantRows` snapshot when the collection changes or the window's DataContext changes. Detach collection subscriptions on rebind and close so old catalog models are not retained.
- Preserve the existing header count, empty guidance, Add Variant and Bulk add buttons, and focused creation dialogs. A successful archive refreshes the existing view model collection; provider reset then updates the grid.

## Risks / Trade-offs

- [Risk] Grid pointer routing can intercept clicks intended for the Archive button. → Resolve the realized row and action from pointer coordinates, suppress duplicate dispatch, and test pointer plus keyboard activation on rendered rows.
- [Risk] Long names or Other values can hide important detail in compact columns. → Keep full strings in accessible help text/tooltips and test representative long values at the minimum editor width.
- [Risk] Stale provider snapshots could show archived rows after the catalog refreshes. → Reset from complete collection changes and cover a successful archive, rebind, and close in headless tests.
- [Trade-off] Rows do not offer built-in search or sorting. → Preserve the existing order and interaction scope; revisit only with evidence of a catalog-scale need.

## Migration Plan

No data migration or persistence change is required. Replace the row ItemsControl with the virtual grid, wire the view-owned provider and input adapter, and keep the existing view model and catalog archive path. Rollback consists of restoring the original ItemsControl template; stored Variant data is unchanged.

## Open Questions

None. The issue's claim that an archive confirmation already exists was checked against current code and accepted behavior: the existing row invokes Archive directly, with stale-target and dependency safeguards. This change preserves that behavior and does not introduce a confirmation.

## Implementation Plan

1. Update the Sellable Variants markup in `src/FusionCanvas.App/Stores/StoreEditorWindow.axaml`: replace the `ItemsControl` with a named `VirtualDataGrid`, set fixed 32-pixel rows, provide fixed aligned Name, Color, Size, Other, and Action columns, disable sorting and horizontal scrolling, and retain section header/count, empty-state messages, and creation buttons. Use ellipsis and full text tooltips/help text for long values.
2. In `src/FusionCanvas.App/Stores/StoreEditorWindow.axaml.cs`, add a view-owned `InMemoryDataProvider<SellableVariantRowViewModel>` and named grid binding. Reset the full snapshot on `SellableVariantRows.CollectionChanged`; detach old handlers when the Store Editor context changes and on close. Adapt tunneled pointer handling for the Archive cell using the focused pattern from the mockup source grid; let keyboard Button activation execute through its normal click/command path.
3. Extend `tests/FusionCanvas.App.Tests/StoreEditorHeadlessTests.cs` with focused rendered-view coverage for column alignment and semantic value placement, fixed rows and long-value help text, active order/archived exclusion, scroll/provider refresh, DataContext rebind and close cleanup, and correct Archive row identity via pointer and keyboard. Retain the existing blocked-dependency test as regression evidence; add any fixture setup needed to verify successful removal/count refresh through the same command.
4. Record criterion-level results in `verification.md`; run focused App tests, `openspec validate --specs --strict --no-interactive`, change validation, and `dotnet test .\\FusionCanvas.sln -m:1`.

## Acceptance-to-Verification Mapping

| Acceptance scenario | Planned verification |
| --- | --- |
| Creator scans sellable Variants | Headless rendered-view test asserts all five aligned columns, row data, order, and exclusion of archived records. |
| A cell value exceeds its visible width | Headless rendered-view test lays out long names and semantic values at the minimum width and checks ellipsis, full help text, and fixed row height. |
| Creator archives a sellable Variant | Headless pointer and keyboard tests assert the correct row object reaches the existing command; existing/new view-model tests verify dependency/stale errors and successful row/count refresh. |
