## Why

The Available choices Color card currently renders every active value in one wrapping text string. Long provider lists make the Variant page hard to scan and make it difficult to locate one color. A paged, searchable Color grid keeps the values readable while preserving the configured order and existing value-management workflow.

## What Changes

- Replace only the Color card's wrapped value summary with a read-only virtual grid. Keep the Size and custom Option cards unchanged.
- Add case-insensitive search across all active Color values, a showing-count summary, previous/next paging, and 10, 25, or 50 records per page (10 initially).
- Add a Color sort selector with Configured order, Name A–Z, and Name Z–A. Return to the saved `SortOrder` for Configured order; browsing never persists a sort change.
- Keep the grid within the existing bordered Color card, with alternating semantic row surfaces, full-name tooltips for truncated values, and the existing Manage values action.
- Preserve search and view preferences while refreshing the same Offering's values; reset them when the user changes Offering.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `variant-management`: define searching, paging, sorting, row presentation, and empty states for the Available choices Color card; update the card and long-value presentation requirements.

## Impact

- UI: `StoreEditorWindow.axaml`, its code-behind, `CatalogSetupViewModel`, and a focused Color-grid presentation view model in `FusionCanvas.App`.
- Tests: focused state tests plus Avalonia headless tests for bindings, search, sorting, paging, stripes, empty states, and the existing Manage values action.
- Dependencies and persistence: reuse the vendored AvaloniaVirtualDataGrid and existing active `OfferingOptionValue.SortOrder`; no NuGet, schema, or persistence changes.
- Verification: map each delta scenario to focused tests, run strict OpenSpec validation, then run `dotnet test .\FusionCanvas.sln -m:1`.
- Scope: one low-frequency setup/browsing surface with one outcome. No Appium journey is warranted for this contained read-only view enhancement; headless tests exercise its meaningful framework behavior.
- Non-goals: editing or selecting values in the grid, changing saved Color order, changing Size/custom Option cards, adding color swatches or provider metadata, modifying bulk-add preview behavior, or measuring large-catalog performance.
