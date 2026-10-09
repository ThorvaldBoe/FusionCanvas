## Context

Variant management already separates possible Option Values from sellable Variants and renders one card per Option. `AvailableChoiceGroups` contains active values in persisted `SortOrder`, and `OfferingChoiceGroupViewModel.ValuesSummary` joins every value into one wrapping string. The Colors catalog is long enough in the supplied screenshot to make that summary hard to scan. The already integrated `VirtualDataGrid` is read-only by configuration and virtualizes rows from an `IDataProvider`, but it does not provide application-level search or pagination.

The change replaces only the Colors summary. The Color grid and its controls remain inside that Option's existing card. Size and custom Options keep their current summaries. All displayed values are already loaded as local catalog state; this is not an external-provider refresh or a persistence workflow.

## Goals / Non-Goals

**Goals:**

- Make the active Colors easy to find and scan from Variant management.
- Bound the Color card's height and let the user control the maximum records in each page.
- Make filter and sort behavior predictable across page boundaries and catalog refreshes.
- Keep Color order edits and Variant creation out of this read-only browsing surface.
- Keep the view usable by keyboard and assistive technology and visually coherent in Light and Dark themes.

**Non-Goals:**

- Change provider catalog, Color, Option, or Variant domain/application rules.
- Add, edit, archive, reorder, select, or persist Colors in the grid. Existing **Manage values** continues to own these operations.
- Change Size or custom Option summaries, the bulk-add preview, database schema, saved order, NuGet dependencies, or the existing virtual grid binary.
- Add swatches, infer color values from names/metadata, persist browsing preferences, introduce column reordering, or claim large-catalog performance.
- Add a real-desktop Appium journey. This contained view state has no new cross-process or durable workflow; headless tests cover the framework risks.

## Decisions

### Keep filter, sort, and page state in a focused App presentation model

Add `AvailableColorValuesGridViewModel` under `FusionCanvas.App.Stores`. It owns the search text, selected page size, current page, sort choice, filtered/sorted result count, a `PageSummary`, previous/next commands, and an `InMemoryDataProvider<AvailableColorGridRowViewModel>` containing only the current page. It receives the active Color values and selected Offering identity from `CatalogSetupViewModel.RefreshOfferingCollections`; it does not call catalog services or write workspace state.

Add `AvailableColorGridRowViewModel` to pair each immutable `OfferingOptionValue` with a display name and an alternating-row flag calculated from its index in the complete filtered/sorted result, before paging. This keeps stripe continuation correct on the second page, including a 25-record page. `CatalogSetupViewModel` owns one stable browser instance so a same-Offering catalog refresh can replace values without discarding search and view preferences. Changing the Offering resets those transient preferences. A refresh clamps the current page to the new result count.

### Search and sort the complete active Color set before paging

The browser receives only non-archived values of the selected Offering's semantic `OptionKind.Color`. It preserves their saved `SortOrder` as the default. Search trims its input and applies an ordinal, case-insensitive substring match to the displayed Color value. A search change returns to page one while retaining the selected sort and page size.

The accessible **Sort Colors** selector exposes **Configured order**, **Name A–Z**, and **Name Z–A**, defaulting to Configured order. Name comparison is ordinal and case-insensitive; equal names are ordered by saved `SortOrder` and then stable value identity. Sorting changes only the displayed collection and returns to page one. Changing sort or page size also returns to page one. Page count uses ceiling division, zero matches show a zero summary, and a shrink in values clamps the page so it cannot point past the last result.

This selector is explicit rather than relying on the component's clickable column header: the component's default provider sort operates on its current `ItemsSource`, while the requirement is to sort the complete result set before taking a page. The single **Color** header is therefore display-only and has resizing and sorting disabled. This also makes sort order keyboard-operable.

### Keep the table bounded and give the user an honest count

Only the Color choice card widens to 390 pixels, leaving a 368-pixel inner surface after its padding and border; the containing card retains its existing border, header, kind label, overflow archive action, and **Manage values** button. A fixed 280-pixel vertical viewport contains one non-resizable **Color** column. Search, page size, sorting, result summary, and Previous/Next controls sit directly above the grid in the same card. The grid can internally scroll when the selected page contains more rows than fit in the viewport; the page-size control still describes the complete page, and the summary reports how many matching Colors exist.

The grid uses `SelectionMode=None`, a template column, and a fixed row height. Color names use ellipsis when needed and retain their full string in a tooltip. The empty Option state says no Colors are configured; a non-empty search with zero matches says no Colors match the query. Keep controls hidden only for the empty Option state, not for a search with no results. Descriptive `AutomationProperties.Name` and stable automation IDs identify the search, page-size, sort, page navigation, grid, and summary controls.

### Stripe rows with the existing semantic theme palette

Bind the alternating row's background to the existing `Token.Color.SurfaceSubtle` resource, whose Light and Dark variants are already defined. Unalternated rows remain transparent on the Color card surface. The row template is read-only and contains no `Button`, `CheckBox`, or editor. The grid style leaves its one column header visible as a label and disables selection, resizing, and sorting; users do not gain another route to archive or manage data.

### Keep preference and data lifetimes explicit

Search, page size, and sort are in-memory presentation state scoped to the selected Offering. Preserve them when `RefreshOfferingCollections` reloads values for the same Offering, clamp the page after the active list shrinks, and reset them when the selected Offering identity changes. Clearing search reveals all current active Colors in the selected sort mode. No migration or rollback is needed because no durable state is changed.

## Risks / Trade-offs

- **The virtual grid requires `IDataProvider` and row content has fixed height** → Bind its rows to the focused view model's snapshot provider, constrain the viewport, keep cells single-line, and test refreshed page snapshots and tooltips.
- **Nested Color-grid scrolling can make 50-row pages require scrolling inside the bounded card** → Keep page count and total-match summary visible and test the first and last rows at page sizes 10/25/50.
- **Transient search/sort state could leak between Offerings** → Key its lifetime to the selected Offering identity and cover same-Offering refresh, context switch, and list-shrink cases.
- **The new card is wider than Size and custom cards** → The existing WrapPanel keeps cards side by side when space allows and stacks them at supported narrow widths; headless tests cover both existing card layout and the Color grid's own bounded width.

## Migration Plan

No data migration. Existing values retain their identity and `SortOrder`; the view derives its initial presentation from current catalog state. Rollback removes the browsing presentation state and restores the existing Color summary binding without changing stored records.

## Open Questions

None. The module remains Colors-only, with sizes 10/25/50, a 10-row default, explicit sort choices, and configured order as the default and return state.

## Implementation Plan

1. **App presentation state**
   - Add `src/FusionCanvas.App/Stores/AvailableColorValuesGridViewModel.cs` with read-only `AvailableColorGridRowViewModel` rows and documented public-to-view state; keep all catalog mutation in the existing services and commands.
   - Give the view model 10/25/50 page sizes, page one by default, normalized case-insensitive search, complete-set filtering/sorting before paging, stable tie-breaking, a zero-aware summary, navigation commands, context reset, and page clamping after a source refresh.
   - Add one stable browser property to `CatalogSetupViewModel` and feed it current-Offering active Color values from `RefreshOfferingCollections`. Reset only when the Offering ID changes.
   - Add `IsColor` to `OfferingChoiceGroupViewModel` for compiled conditional templates while preserving `ValuesSummary` for Size and custom Options.

2. **View composition**
   - In `src/FusionCanvas.App/Stores/StoreEditorWindow.axaml`, conditionally replace `ValuesSummary` only for Color with search, page-size and sort ComboBoxes, accessible page controls, a result summary, empty messages, and the bounded one-column `VirtualDataGrid`.
   - Add a row template bound to the row wrapper for the Color name, full-name tooltip, and alternating `Token.Color.SurfaceSubtle` background. Keep card heading, actions, overflow menu, and standard style resources in the current card boundary.
   - Preserve the prior compact, wrapping summary for Size and custom Options and keep the parent card WrapPanel responsive.
   - Do not change the published DLL, assembly references, styles include, Application or Domain code, persistence, or provider APIs.

3. **Focused verification**
   - Add framework-free view-model tests in `tests/FusionCanvas.App.Tests/AvailableColorValuesGridViewModelTests.cs` for default configured order, global case-insensitive search, all page sizes/counts, previous/next bounds, ascending/descending and restoration, equal-name ties, reset/clamp behavior, and Offering-context reset.
   - Add deterministic Avalonia headless coverage in `tests/FusionCanvas.App.Tests/StoreEditorHeadlessTests.cs` or a focused companion for rendered Color-only conversion, Light/Dark row fills and tooltips, accessible control identities, search across a page boundary, sort across page boundaries, each page size, both empty states, and unchanged Manage values routing. Keep existing Size/custom-card layout and lifecycle tests passing.
   - Keep the sorted list read-only: assert browsing does not alter `OfferingOptionValue.SortOrder` or `SellableVariantRows`.

## Acceptance-to-Verification Plan

| Acceptance scenario | Planned evidence |
| --- | --- |
| User scans configured Colors | Avalonia headless Color card test renders rows, default order, alternating semantic fills in Light and Dark, and verifies variants and persisted values remain unchanged. |
| User searches across all pages | View-model test puts a match beyond page one, filters case-insensitively, verifies first-page results, and clears search to restore the full selected order. |
| User changes page size | View-model tests verify each allowed size, page boundaries, visible range, count, and disabled navigation at either end. |
| User sorts Color values | View-model test verifies full-result A–Z/Z–A ordering, case-insensitive ties, and that only the displayed rows move. |
| User returns to saved order | View-model test verifies configured SortOrder after sorting while entity values remain unchanged. |
| Color grid has no rows | Headless view tests separately assert the no-configured-Colors and no-search-matches messages, zero count, and disabled navigation. |
| User reaches controls and truncated names | Headless test locates all controls by automation ID/name, drives search/page/sort/navigation with keyboard input, and verifies the full-name tooltip. |
| User edits Colors through the existing action | Existing and focused headless test invoke **Manage values**, assert the selected Option ID, and retain existing dialog lifecycle behavior. |
| User scans available choices as cards | Existing responsive card test plus new Color-card assertions verify header/actions remain inside the bordered card and grid is present only on Color. |
| Empty Option uses same card treatment | Headless Color-card test verifies no-value empty state within the existing border and distinguishes it from zero search matches. |
| Custom Option kind uses same card treatment | Existing headless card tests assert Size/custom summaries are unchanged; new view test asserts only Color has the grid. |
| Cards align and stack responsively | Existing card-width headless test checks default side-by-side and minimum-width wrapping; new test checks Color grid stays bounded within its card. |
| Long content remains readable | Headless test checks row ellipsis and full-name tooltip while existing card test covers wrapped Option names and summaries. |
