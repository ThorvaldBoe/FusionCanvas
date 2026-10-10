## Context

Manual Listing Details currently renders `Variants` in an `ItemsControl` with one independent three-column row per Variant. The app already uses `AvaloniaVirtualDataGrid` in other focused surfaces. Its provider requires an `IDataProvider`, so the view must expose a refreshed in-memory snapshot while the existing observable Variant rows remain authoritative.

## Goals / Non-Goals

**Goals:**
- Present Variant name, selling price, and fulfillment cost in aligned columns with virtualized rows.
- Keep direct, always-visible TextBox editors so users can click and type without a separate edit gesture.
- Preserve row-model identity, input feedback, save semantics, and the existing editability gate.

**Non-Goals:**
- No changes to validation, pricing, persistence, application services, or database schema.
- No sorting, search, paging, selection, or new listing behavior.
- No Appium journey for this focused presentation replacement; headless Avalonia covers grid layout and keyboard interaction while existing service tests cover persistence rules.

## Decisions

1. Use three non-sortable columns with `SelectionMode=None`. Put the two TextBoxes directly in their column `CellTemplate`s because the grid does not automatically enter its separate `EditTemplate` on a normal click. TextBox bindings remain TwoWay to `SellingPrice` and `FulfillmentCost`; the cell's data context is the stable `VariantListingTermsViewModel` instance. Verify keyboard focus traversal among realized editors; the existing provider-reset test covers virtualized row refresh separately.
2. Keep a single `InMemoryDataProvider<VariantListingTermsViewModel>` owned by `MainWindow`. On DataContext changes, detach from the old `ManualListingDetailsStageToolViewModel.Variants.CollectionChanged`, subscribe to the new collection, and `Reset` the provider from the current collection snapshot. Assign the provider to the named grid once; detach on window close. The observable collection and rows stay authoritative.
3. Bound the grid height to a compact workspace region with fixed row height. Retain the surrounding Listing Details guidance, shipping controls, gross profit summary, error/status, and Save details control unchanged.
4. If headless testing cannot establish keyboard focus traversal among direct CellTemplate editors, do not ship an unreliable grid: retain the existing ItemsControl and update the change to document the fallback. The existing list remains the rollback path because no persisted data or APIs change.

## Risks / Trade-offs

- [Risk] Grid editor focus may not follow expected Tab order → Verify focus traversal across realized cells; fallback to the list if that interaction is unreliable.
- [Risk] Provider snapshots can become stale when switching Items or data contexts → Refresh on the Variants collection change and detach/rebind on DataContext replacement and close.
- [Trade-off] Always-visible editors take more horizontal space than display-only cells → Keep fixed compact price/cost columns and allow the Variant name column to fill remaining width.

## Migration Plan

No data migration is needed. The change only replaces the presentation of the current in-memory row collection. Rollback consists of restoring the existing ItemsControl markup and removing the provider adapter.

## Open Questions

None. Keyboard behavior is an implementation acceptance gate; the fallback decision above is already approved.

## Implementation Plan

1. **App view and lifecycle:** In `src/FusionCanvas.App/Views/MainWindow.axaml`, replace the pricing `ItemsControl` with a named `VirtualDataGrid`, three non-sortable columns, direct TextBox cell templates with accessible names and TwoWay bindings, a fixed row height, a bounded viewport, and `CanEditFulfillmentTerms` gating. Preserve neighboring status and save controls. In `src/FusionCanvas.App/Views/MainWindow.axaml.cs`, own the provider, initialize its empty snapshot, bind it to the named grid, and subscribe/unsubscribe to the current manual-listing Variant collection on DataContext replacement and window close.
2. **Tests:** Finalize `tests/FusionCanvas.App.Tests/VirtualDataGridIntegrationTests.cs` with a headless test that verifies aligned columns, direct editor realization, row-specific TwoWay edits, and keyboard traversal among realized cells. Add/extend a MainWindow headless view test for the named grid and its disabled state when `CanEditFulfillmentTerms` is false. Keep application and persistence behavior covered by existing Manual Listing Details tests.
3. **Verification evidence:** Add `openspec/changes/virtualize-manual-listing-variant-pricing/verification.md` mapping each scenario below to its focused test and result, then run the full solution baseline, strict OpenSpec validation, and changed-scope review.
4. **Compatibility:** No API, dependency, storage, migration, or serialized-format changes. Do not expand scope to other Listing Details controls or later issue work.

## Acceptance Scenario Verification

| Scenario | Planned verification |
| --- | --- |
| User edits Variant pricing | Avalonia headless grid test asserts field binding updates the exact row model; existing Listing Details view-model/service tests cover validation, gross profit, dirty state, and save mapping. |
| User moves between price editors with the keyboard | Avalonia headless test tabs from price to cost and through realized Variant rows, verifying focus after each step. |
| Listing details cannot edit fulfillment terms | MainWindow headless test sets no active Offering or protected state and asserts grid and TextBox editors disabled while surrounding guidance/save controls retain their bindings. |
