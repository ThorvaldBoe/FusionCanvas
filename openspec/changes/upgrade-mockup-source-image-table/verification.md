# Verification

## Acceptance scenarios

| Criterion | Verification | Result |
| --- | --- | --- |
| Sort source rows by File, Applicability, and Status; reverse direction on repeat; announce the active column and direction | `MockupSourceTable_SortHeadingsRespondToPointerAndKeyboardWithAccessibleDirection` drives each rendered heading by pointer and keyboard, checks order, arrow, and accessible name. `LocalSourceSortingUsesVisibleKeysAndPreservesSelectedDraft` checks selection/active-row preservation in the view model. | PASS |
| Show preview-read failures visibly, expose the complete explanation to pointer and assistive-technology users, and retain fixed row height | `MockupSourceGrid_PreviewWarningStaysVisibleAndExposesFullAccessibleText` checks the warning glyph, complete tooltip/help text, 32-pixel row height, and visible incomplete status. | PASS |
| Keep row selection and the active detail row behavior intact | `MockupSourceRow_SelectsFromCellsWhitespaceAndKeyboard_WithoutArchiving` covers plain pointer selection from cells and whitespace plus Enter/Space. `MockupSourceTable_ModifierGesturesSelectRangeAndArchiveSelectedRows` covers Ctrl toggle, Shift range, selected highlighting, and active row. Existing `LocalSourceSelectionSupportsReplaceToggleRangeAndBulkArchive` protects the view-model contract. | PASS |
| Keep each Archive action separate from row selection and retain selection reconciliation after removal | `MockupSourceGrid_ArchiveButtonDoesNotSelectItsRow` exercises pointer and keyboard activation without selecting the archived row. `MockupSourceTable_ModifierGesturesSelectRangeAndArchiveSelectedRows` checks post-archive selection reconciliation. | PASS |
| Preserve empty, incomplete, selected, alternate, long-text, and lower detail-editor states | `MockupSourceEditor_WithoutImageShowsUploadGuidanceAndNoPlacementEditor`, `MockupSourceGrid_PreviewWarningStaysVisibleAndExposesFullAccessibleText`, `MockupSourceTable_FiveRowsShareFullWidthSeparatorsAndSelectedRowTreatment`, `MockupPreview_WithSelectedSourceSynchronizesPlacementRectangleAndMappingFields`, and `SelectedSourceEditor_IsAbsentUntilSourceIsSelected` inspect these states. | PASS |
| Keep the provider current through collection changes, sorting, virtualization, DataContext replacement, and close | `MockupSourceGrid_RefreshesProviderAndDetachesAcrossRebindAndClose` checks the complete 40-row snapshot, bounded row realization, scrolling to the last row, sort and removal refresh, replacement-context refresh, stale-context isolation, and clearing on close. | PASS |

## Required checks

| Check | Result |
| --- | --- |
| Focused source-grid headless suite: 7 tests covering sorting, presentation, warnings, selection, archive, and provider lifecycle | PASS (7 passed, 0 failed) |
| `dotnet test .\FusionCanvas.sln -m:1` | PASS (2,341 passed, 0 failed, 0 skipped across Domain, Application, Integration, App, and UiDescription test projects) |
| `openspec validate --specs --strict --no-interactive` | PASS (65 passed, 0 failed) |
| Changed-scope review | PASS — changes are limited to the App source-image table, its headless tests, and this OpenSpec change. No Domain, Application, Integration, persistence, file-storage, or accepted-spec behavior changed. No performance claim is made. |

The full solution run emitted existing NU1900 warnings because the NuGet vulnerability endpoint was unreachable, plus ImageSharp 3.1.12 vulnerability advisories in Application and Integration test project restore. These did not affect the test result and are outside this presentation change's scope.

No Appium journey was added: the change is a presentation refactor with deterministic headless coverage for the routed input, focus, selection, scrolling, and visual-tree behavior. No optional live desktop visual check was run.
