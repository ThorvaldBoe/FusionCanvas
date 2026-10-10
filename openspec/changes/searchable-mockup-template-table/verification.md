# Verification

## Acceptance scenarios

| Scenario | Evidence | Result |
| --- | --- | --- |
| User scans Mockup Template rows | `StoreEditorHeadlessTests.MockupTemplateTable_SearchesRefreshesAndRetainsFullSummaryText`; asserts Offering-scoped rows, five aligned columns, bounded grid, pointer selection, full details, and provider refresh. | Passed |
| User searches displayed template details | `CatalogSetupViewModelTests.MockupTemplateSearchFiltersEveryDisplayedFieldCaseInsensitivelyAndPreservesOrder`; checks all displayed fields, stable order, case-insensitive matching, and no catalog mutation. | Passed |
| User clears search or receives no matches | The view-model search test covers whitespace clearing; `StoreEditorHeadlessTests.MockupTemplateTable_SearchesRefreshesAndRetainsFullSummaryText` checks empty results guidance and editable query. | Passed |
| User reads a long summary | `StoreEditorHeadlessTests.MockupTemplateTable_SearchesRefreshesAndRetainsFullSummaryText`; checks complete tooltips and accessible names plus selected-row details. | Passed |
| Offering has no Design Areas | `StoreEditorHeadlessTests.MockupTemplateTable_PreservesBlockedGuidanceWhenOfferingHasNoDesignAreas`; checks the blocked message and route remain available. | Passed |
| Search text is not persisted | `CatalogSetupViewModelTests.MockupTemplateSearchFiltersEveryDisplayedFieldCaseInsensitivelyAndPreservesOrder` checks Offering reset; `StoreEditorHeadlessTests.MockupTemplateTable_SearchesRefreshesAndRetainsFullSummaryText` checks reset when leaving management. No persistence model or storage path changed. | Passed |
| User edits or duplicates the selected template | `StoreEditorHeadlessTests.MockupTemplateTable_RowActionsAndArchiveConfirmationSupportPointerAndKeyboard` and `StoreEditorHeadlessTests.MockupTemplateManagement_EditDialogPopulatesAndReturnsFocusOnCancel`; check selected identity routing and existing workflows. | Passed |
| User selects a template with a keyboard or assistive technology | `StoreEditorHeadlessTests.MockupTemplateTable_SearchesRefreshesAndRetainsFullSummaryText` raises Up/Down key events on the focusable grid and checks selected-row detail updates; `StoreEditorHeadlessTests.MockupTemplateTable_RowActionsAndArchiveConfirmationSupportPointerAndKeyboard` checks pointer selection, action accessible names, and Tab stops. | Passed |
| User cancels archive confirmation | `CatalogSetupViewModelTests.MockupTemplateArchiveRequiresConfirmationAndCancelPreservesTemplateAndRevision`; `StoreEditorHeadlessTests.MockupTemplateTable_RowActionsAndArchiveConfirmationSupportPointerAndKeyboard` checks Cancel focus, Escape, no mutation, and focus restoration. | Passed |
| User confirms archive | `CatalogSetupViewModelTests.MockupTemplateArchiveRequiresConfirmationAndCancelPreservesTemplateAndRevision`; `StoreEditorHeadlessTests.MockupTemplateTable_RowActionsAndArchiveConfirmationSupportPointerAndKeyboard` checks row removal. Existing `MockupTemplateSetupServiceTests.ArchivingTemplateArchivesBindingsAndRestoreReactivatesTemplate` covers soft-archive applicability and retained revisions. | Passed |

## Verification commands

- Focused App coverage: `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj -m:1 --no-restore --filter "FullyQualifiedName~StoreEditorWindow_DetachesViewModelAndCatalogSubscriptionsOnRebindAndClose|FullyQualifiedName~MockupTemplateManagement_EditDialogPopulatesAndReturnsFocusOnCancel|FullyQualifiedName~MockupTemplateTable_" --logger "console;verbosity=minimal"` — passed, 5 tests.
- Full solution baseline: `dotnet test .\FusionCanvas.sln -m:1 --no-restore` — passed; Domain 294, Application 671, Integration 352, App 1,003, UI-description 29 (2,349 total).
- `openspec validate --strict --type change searchable-mockup-template-table` — passed.
- `openspec validate --strict --all` — passed, 87 items.
- `git diff --check` — passed.

## Changed-scope review

Implementation changes are limited to Store Editor presentation state, the virtual table and confirmation window, and focused App tests. No domain, application service, database, migration, external API, dependency, or Appium changes were needed. The archive path reuses the existing reversible soft-archive workflow. There are no additional reusable lessons to record; the existing `docs/experiments/virtual-data-grid.md` already captures the grid's data-provider and cell pointer-routing constraints.
