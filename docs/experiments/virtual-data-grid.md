# Virtual data grid trial

## Scope

The local experiment imports the existing Release DLL, references it from the App project, loads its embedded styles, and replaces the bulk-variant preview's independent row controls with a virtual grid. Domain, Application, persistence, and provider validation remain unchanged.

This is a presentation refactor preserving the accepted Variant Management behavior, so an OpenSpec proposal is not required. Any new selection, editing, sorting, or confirmation behavior needs the normal OpenSpec workflow. No new Appium journey is warranted for this small maintenance trial: deterministic headless tests cover real templates, layout, scrolling, input, refresh, and dialog lifetime; existing application tests cover provider validation and persistence. Native visual assessment remains a useful optional follow-up.

## Implemented candidate: bulk-variant preview

Location: `src/FusionCanvas.App/Stores/BulkAddVariantsWindow.axaml`, `BulkPreviewGrid`.

The previous template was a horizontal `StackPanel` containing `SizeName` and `ExclusionReason`. Each reason began immediately after its row's size label, so differing size-label lengths produced inconsistent second-column positions.

This is a small, read-only list in one focused dialog. It uses the existing immutable `BulkVariantCandidate` records and requires no image loading, per-row commands, drag-and-drop, persistence changes, or editing. The accepted Variant Management spec already requires a clear pre-confirmation summary of combinations that will and will not be created.

The grid has two compiled cell templates: **Size** and **Exclusion reason**. The existing summary and explicit Preview/Create/Cancel controls remain authoritative. Sorting, resizing, selection, and header drag reordering are disabled. Eligible combinations retain their blank exclusion reason; excluded combinations retain the service-provided explanation. If a result/status badge is later wanted, the existing `WillCreate` field can drive a template, but that presentation should be decided explicitly.

The preview collection is an `ObservableCollection<BulkVariantCandidate>`, populated by the existing preview command. Although the grid declares `ItemsSource` as `IEnumerable` and its panel subscribes to collection changes, the panel's measure/arrange implementation requires `IDataProvider`. A plain observable collection therefore does not render rows in this implementation. The window owns an `InMemoryDataProvider<BulkVariantCandidate>` snapshot and resets it when the preview collection changes. Subscriptions are detached on DataContext replacement and window close. The existing collection and preview/confirmation commands remain authoritative; no database adapter is needed.

The dialog uses `SizeToContent=Height` and a `StackPanel`. The grid has a finite 180-pixel viewport with 32-pixel rows and vertical scrolling. Long size labels and exclusion reasons use ellipsis with full-text tooltips. The component panel can report a 600-pixel horizontal extent even with narrower columns; a class-qualified template style disables horizontal scrolling for this fixed-width preview. This keeps header and cell positions aligned without changing the upstream DLL.

Try it through **Store → Products → Offering → Manage Variants → Bulk add**. Select an enabled Color and Sizes, then choose **Preview valid Variants**. A provider catalog that can validate those combinations is still required by the existing workflow. Preview creates nothing; **Create previewed Variants** remains the explicit confirmation.

## Other candidates

| Surface | Existing composition | Why it is a later trial |
| --- | --- | --- |
| Sellable Variants in `StoreEditorWindow.axaml` | Per-row `Grid` with Name, SemanticSummary, Archive | A good follow-up for Color/Size/Other columns, but Archive adds command routing, targeting, focus, and lifecycle safeguards. |
| Assets in `AssetsWindow.axaml` | Per-row `Grid` with thumbnail, description, purpose selector, Remove | Tests richer templates, but adds image lifetimes, selection controls, confirmation, and durable file operations. |
| Option values in `OptionValueManagementWindow.axaml` | Grip, value label, ordering/edit/archive controls | Small list, but drag-and-drop and persisted manual order make it more complex than the preview. |

These observations concern the current layout structure; no runtime visual defect or performance bottleneck has been measured.

## Findings from the component source

- `VirtualDataGridTemplateColumn` provides both `CellTemplate` and `EditTemplate` as Avalonia `IDataTemplate` properties. Custom display and editor controls are supported by the API.
- Text columns built with a value getter are suitable for read-only data. Editable `Create<T>` columns write directly to the supplied item's property; do not bind those to immutable application/domain records or bypass FusionCanvas's draft/confirmation paths.
- Template creation calls `CellTemplate.Build(item)`; cells receive the row item as `DataContext`. Use actual bindings where properties can change, rather than capturing a value once in template construction.
- Row height is fixed/configurable. Dynamic multi-line content needs an intentional presentation policy.
- The grid installs a tunneling pointer handler for selection/editing. Interactive buttons and dropdowns require focused input tests before adopting them in a product view.
- `InMemoryDataProvider<T>` implements `IList`, but its Add/Remove paths do not update the separate original-items snapshot used when sorting. Prefer whole-snapshot `Reset` and disable sorting for the first trial.
- The styling includes component-owned translucent theme colors. Passing headless tests in Light and Dark establishes integration and layout, not final visual fit with FusionCanvas's design tokens or accessibility.

The trial makes no claim about million-row performance, async providers, accessibility, or native desktop behavior.

## Verification

Verified locally on 2026-10-09:

- Focused headless filter `FullyQualifiedName~BulkVariantPreviewHeadlessTests|FullyQualifiedName~VirtualDataGridIntegrationTests|FullyQualifiedName~BulkAdd_GridShowsPreview`: passed, 5 tests. Run with `dotnet test tests/FusionCanvas.App.Tests/FusionCanvas.App.Tests.csproj -m:1 --no-restore --filter "<filter>"`.
- `dotnet test .\FusionCanvas.sln -m:1 --no-restore --nologo -v quiet`: passed, 2,322 tests (294 Domain, 671 Application, 352 Integration, 976 App, 29 UI-description).
- `openspec validate --specs --strict --no-interactive`: passed, 64 accepted specs.
- `git diff --check`: passed.
- App and test build output DLL checksums match the imported artifact.

| Preserved requirement / trial check | Evidence |
| --- | --- |
| Shared columns and compiled templates render real candidate labels/reasons | `BulkVariantPreviewHeadlessTests.Preview_templates_align_columns_and_scroll_with_full_text_available`, Light and Dark cases |
| Long content retains full text; bounded viewport reaches the last row without realizing every row | Same test checks tooltips, extent, row count, and scrolling to Size 19 |
| Snapshot replacement, model rebind, and close do not leave stale rows or subscriptions | `BulkVariantPreviewHeadlessTests.Preview_refreshes_after_rebind_and_detaches_when_closed` |
| Pre-confirmation summary includes both an eligible Size and the existing-Variant exclusion | `StoreEditorHeadlessTests.BulkAdd_GridShowsPreviewAndOnlyCreatesAfterExplicitConfirmation` |
| Preview creates nothing; changing a Size invalidates the summary; explicit Create adds only the eligible Variant and closes the dialog | Same rendered-input workflow test, with existing offering services and an in-memory provider catalog |
| Dialog scope, initial focus, cancellation, and mutually exclusive creation dialogs remain intact | Existing `StoreEditorHeadlessTests` baseline coverage |

Changed-scope review: grid-specific types and provider adaptation live only in App and App tests. No application/domain rules, external calls, storage paths, database schema, or accepted specs changed. The upstream binary and its style include are documented under `lib/AvaloniaVirtualDataGrid`.

Existing ImageSharp vulnerability warnings and compiler/analyzer warnings remain. No native desktop run was performed. The trial is limited to the bulk-variant preview; wider grid adoption remains a separate decision.
