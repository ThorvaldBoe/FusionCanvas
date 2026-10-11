## Context

The focused Assets window currently places all assets in a default `ItemsControl` within a `ScrollViewer`. The default item panel is not virtualized, so every row control is created even when most assets are outside the viewport. Each row combines a preview button, name and managed filename, optional missing-file and context labels, a purpose `ComboBox`, and a confirmed-removal action. The window also owns import confirmation, errors, busy feedback, and the empty state.

The product decision is to preserve this one-column thumbnail-first row layout and virtualize the rows. The window remains a focused management surface and no new main-workspace controls are introduced.

## Goals / Non-Goals

**Goals:**

- Realize only viewport rows plus any bounded virtualization buffer while scrolling a large collection.
- Preserve asset ordering, thumbnail preview, accessible full text, purpose editing, missing/context state, confirmed removal, import, and empty/loading/error feedback.
- Preserve the existing no-row-selection interaction model and keyboard access to row controls.

**Non-Goals:**

- Search, filtering, sorting, grouping, paging, multi-column comparison, or a tile gallery.
- Changes to asset data, persistence, image decoding policy, domain behavior, or import/removal semantics.
- Changing row ordering or adding a new dependency.

## Decisions

1. **Keep the current row and use Avalonia's virtualizing stack panel.** Configure the existing `ItemsControl` with `VirtualizingStackPanel` and keep it in a vertically constrained scrolling region. This retains item-template behavior and does not introduce `ListBox` selection semantics into a surface that currently has no row selection. Avalonia's built-in `ItemsControl` plus `VirtualizingStackPanel` supports this arrangement without an additional package.

   Alternatives considered:
   - A multi-column `VirtualDataGrid` can virtualize rows, but the Assets workflow is thumbnail-led, its name and state fields vary, and purpose editing plus removal do not gain value from table headers. A grid also adds pressure on the existing 780px window width.
   - A thumbnail tile gallery changes the established reading order and gives less room to edit purpose and review file state at the window's 560px minimum width.
   - A `ListBox` virtualizes by default, but introduces selection behavior that is not part of the current asset surface. Keeping `ItemsControl` avoids that behavioral change.

2. **Keep content and row order unchanged.** The existing view model collection is the source for the list. No sorting or filtering is introduced. Recycled or newly realized rows continue to bind to the same `AssetRowViewModel` properties and parent commands.

3. **Keep text readable and recoverable.** Name and managed filename remain trimmed visually; their full values are exposed through tooltips and automation help/name. Missing-file and context indicators remain explicit text, not color alone.

4. **Keep window states and actions in their existing locations.** Import confirmation, removal confirmation, busy status, error text, and the empty state keep their current behavior. Row controls remain keyboard reachable; preview is available only when an image thumbnail exists, while purpose editing and removal remain available for missing and non-image assets.

## UX Preflight

- **Primary workflow and frequency:** creators occasionally review or manage asset files from the focused Assets window; import, purpose correction, preview, and removal are asset-level actions.
- **Surface and footprint:** keep the focused window and its existing fixed header/footer. The asset collection uses the remaining bounded viewport and scrolls vertically.
- **Progressive disclosure:** do not add persistent search, sort, or extra metadata controls. Keep filenames trimmed with full values available by tooltip/automation text.
- **States:** preserve initial loading/busy, empty collection, populated collection, import confirmation, removal confirmation, recoverable error, and close behavior.
- **Selection and focus:** there is no collection selection state today; keep keyboard navigation among row controls and do not add selected-row behavior. Virtualization may recycle off-screen containers, so bindings must continue to identify the current row correctly.
- **Unsaved changes and destructive actions:** purpose changes persist immediately as before. Removal still requires explicit confirmation; cancel preserves data and selection context. Import still requires confirmation and supports cancellation.
- **Desktop automation:** no Appium journey is warranted for this focused presentation/performance change. Existing asset journey covers import, relabel, preview, cancellation, and rehydration; the new headless view test will exercise scrolling and realized-row bounds.

## Implementation Plan

1. In `src/FusionCanvas.App/Assets/AssetsWindow.axaml`, configure the asset `ItemsControl` to use `Avalonia.Controls.VirtualizingStackPanel` for its vertical items panel. Preserve the existing row `DataTemplate` and bindings. Keep the panel constrained by the window's existing star-sized content viewport so that the virtualizer can determine the effective viewport. Avoid introducing a `ListBox` or row selection.
2. Review the `ScrollViewer` and surrounding content layout in `AssetsWindow.axaml`. Ensure the list's virtualizing panel can observe the bounded viewport rather than being measured as an unbounded child of the surrounding `StackPanel`; if needed, isolate the collection in a star-sized row while keeping error/empty/import/removal feedback reachable in the same scroll surface.
3. Add an Avalonia headless test in `tests/FusionCanvas.App.Tests/AssetsWindowHeadlessTests.cs` that loads a collection substantially larger than the viewport, confirms fewer than all row controls are realized, scrolls to a later asset and confirms it becomes realized, and verifies long text accessibility and row actions remain bound. Keep existing `StoreAssetJourney_ImportsRelabelsPreviewsCancelsRemovalAndRehydrates` coverage intact.
4. Run the focused asset headless tests, the full `dotnet test .\FusionCanvas.sln -m:1` baseline, strict OpenSpec validation, and `git diff --check`. Record criterion-level results in `verification.md` before archiving.

## Acceptance Verification

| Scenario | Planned evidence |
| --- | --- |
| User reviews a large asset collection | Headless view test loads more assets than fit in the viewport and asserts only a bounded subset of row controls is realized. |
| User interacts with an asset row | Existing asset journey verifies preview, relabel, removal confirmation/cancellation, and durable state; headless scrolling test verifies later-row bindings/actions remain available. |
| Asset details exceed available row width | Headless view test verifies full tooltip/automation text on a trimmed long name; verify missing and context labels remain visible/bound in existing asset coverage. |
| User scrolls after removing an asset | Existing asset journey covers confirmed/cancelled removal and empty/update behavior; add assertions to the focused view test if needed for removal from a realized later row. |

## Risks / Trade-offs

- **[Virtualization is disabled by an unbounded parent measure] →** Keep the collection under a bounded viewport and verify realized row counts in a headless test, rather than relying only on XAML configuration.
- **[Container recycling displays stale row data] →** Bind the template directly to each item and test scrolling from the first to a later row with distinct names and purpose options.
- **[Virtualization reduces controls but all row view models and thumbnails remain loaded] →** Keep this module scoped to visual-row realization; image-loading memory policy is separate work and is not changed here.

## Migration Plan

No data or persistence migration is required. Rollback consists of restoring the existing default item panel; all asset records and interaction behavior remain compatible.

## Open Questions

None. The list layout is approved; the remaining implementation risk is covered by the headless viewport/realization test.
