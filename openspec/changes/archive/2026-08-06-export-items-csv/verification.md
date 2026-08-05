# export-items-csv — Verification

Baseline: `dotnet test .\FusionCanvas.sln` → **all 967 tests pass** (Application 281, Domain 177, Integration 138, App 371). `openspec validate "export-items-csv"` → valid.

## items-csv-export capability

### Requirement: Items can be exported to CSV from a niche or group

| Acceptance scenario | Result | Evidence |
|---|---|---|
| Export a group with nested subgroups | PASS | `ItemCsvExportServiceTests.Project_GroupIncludesGroupAndDescendantItems`; `Project_GroupExcludesItemsInSiblingGroups` |
| Export a niche covering all its items | PASS | `ItemCsvExportServiceTests.Project_NicheIncludesDirectAndGroupItems` |
| Empty group exports only the header | PASS | `ItemCsvExportServiceTests.Project_ZeroItemGroupReturnsEmpty` (empty row list) + `ItemCsvCodecTests.WriteAsync_UsesExactHeaderAndCrLf` (header only) |
| Archived and empty items are omitted | PASS | `Project_ExcludesArchivedAndInactiveItems`; `Project_ExcludesEmptyItems` |
| Items under archived descendants are omitted | PASS | `Project_ExcludesArchivedAndInactiveItems` (`underArchived` item via archived subgroup) |
| Save is canceled | PASS | `WorkspaceTreeViewModelTests.ExportCsv_WithNullPickerWritesNothing` |
| Export fails | PASS | `WorkspaceTreeViewModelTests.ExportCsv_SurfacesErrorWhenDestinationFails` (throwing stream → `_errorMessage`; not reported successful); `ItemCsvCodecTests.WriteAsync_WithMidStreamFailureThrowsAndFlushesPartial` |

### Requirement: Exported items use the defined CSV columns

| Acceptance scenario | Result | Evidence |
|---|---|---|
| Header order matches the specification | PASS | `ItemCsvCodecTests.WriteAsync_UsesExactHeaderAndCrLf` |
| Item fields map to columns | PASS | `ItemCsvExportServiceTests.Project_ColumnsMapToFields` |
| Missing fields export as empty | PASS | `ItemCsvExportServiceTests.Project_MissingFieldsExportAsEmpty`; `ItemCsvCodecTests.WriteAsync_NullFieldsWriteAsEmpty` |

### Requirement: CSV fields are safely escaped

| Acceptance scenario | Result | Evidence |
|---|---|---|
| Semi-colon in a field is preserved | PASS | `ItemCsvCodecTests.WriteAsync_QuotesFieldContainingSemiColon` |
| Multi-line field is preserved | PASS | `ItemCsvCodecTests.WriteAsync_PreservesMultilineFieldWithinOneQuotedField` |
| Embedded double-quote is doubled | PASS | `ItemCsvCodecTests.WriteAsync_QuotesFieldContainingEmbeddedQuote` |

## group-management (MODIFIED delta)

| Acceptance scenario | Result | Evidence |
|---|---|---|
| User opens a group context menu (includes Export to CSV...) | PASS | `ItemsCsvExportViewTests.ExportMenuShowsOnNicheAndGroupRows_AndHidesOnItemRows` (group + niche visible, item hidden); `ExportMenuClick_InvokesExportThroughTreeViewModel` (click reaches the export path) |

## Notes and limitations

- The group-management delta scenario is validated at the XAML/view layer (headless), which is the layer owning the menu wiring change.
- No live desktop run was performed; the write-destination path uses the standard `SaveFilePicker`, which is not exercised headlessly beyond the picker abstraction (Vacant in headless => Null/recording stubs used).
- Deterministic output order: application service orders by `CreatedAt`, then `Id`; tags are ordered `OrdinalIgnoreCase` by name.
