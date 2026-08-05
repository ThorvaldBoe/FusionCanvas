# Import Items from CSV — Verification

Baseline before feature edits: `dotnet test .\FusionCanvas.sln` green.

## Completion gates

| Gate | Command | Result |
| --- | --- | --- |
| Strict OpenSpec validation | `openspec validate --changes import-items-from-csv` | Pass |
| Full deterministic test baseline | `dotnet test .\FusionCanvas.sln` | Pass — Domain 177, Application 284, Integration 143, App 379 = 983 tests. All new behavior covered by focused tests; each acceptance scenario mapped below. |

New tests added by this change:
- `FusionCanvas.Integration.Tests/Items/Import/ItemCsvCodecTests.cs` (13 tests)
- `FusionCanvas.Application.Tests/Items/Import/ItemCsvImportServiceTests.cs` (14 tests)
- `FusionCanvas.App.Tests/Items/Import/ItemImportViewModelTests.cs` (10 tests)
- `FusionCanvas.App.Tests/Items/Import/ItemImportWindowTests.cs` (4 headless view tests)

## `item-csv-import`

### Requirement: Item CSV import defines a standard text format

| Scenario | Verification | Result |
| --- | --- | --- |
| User reads the documented column order | Columns are the seven-item `Title;Base Idea;Concept Idea;Phrase;Graphic;Notes;Tags` order in `ItemCsvCodec` (+ `WriteSample`); `ItemCsvCodecTests.WriteSample...` verifies the header exactly | Pass |
| Columns are separated by a single semi-colon | `ItemCsvCodecTests.Parse_ParsesValidRowsAndSplitsTags` (7-field rows parsed) + wrong-column-count error in `Parse_WrongColumnCountReportsErrorOnLine` | Pass |
| Fields are quoted using standard CSV quoting (matching export) | `ItemCsvCodecTests.Parse_QuotedFieldPreservesLiteralSemicolon`, `Parse_QuotedFieldAllowsEmbeddedQuotesAndLineBreaks`, and round-trip `Parse_ImportsRowsWrittenByExportCodec` | Pass |
| Empty fields are preserved in any column position | `ItemCsvCodecTests.Parse_EmptyMiddleFieldIsPreserved` (empty Notes between Graphic and Tags) | Pass |
| Optional header row is detected and ignored | `ItemCsvCodecTests.Parse_AcceptsHeaderAndSkipsIt`, `Parse_HeaderDetectionIsCaseInsensitive`, `Parse_HeaderOnlyProducesZeroRowsWithoutErrors` | Pass |
| A data row equal to the headings is indistinguishable from a header | `ItemCsvCodecTests.Parse_DataRowEqualToHeadingsOnFirstLineIsTreatedAsHeader` + documented limitation | Pass |
| Tags are comma-separated within their column | `ItemCsvCodecTests.Parse_ParsesValidRowsAndSplitsTags` (`funny,caffeine` → two tags) | Pass |
| Tag names containing commas are not supported | Verified by the comma-split behavior in `Parse_ParsesValidRowsAndSplitsTags`; documented limitation (comma is always a separator) | Pass |

### Requirement: Item CSV import is reachable from the niche and group context menu

| Scenario | Verification | Result |
| --- | --- | --- |
| User opens Import from a niche row | `MainWindow` context menu adds `<MenuItem ... IsVisible="{Binding IsTopic}" Click="OnContextImport"/>`; `ItemImportWindowTests.ContextMenuImportTargetsTopicRows` verifies niche nodes are `IsTopic`; `OnContextImport` resolves a Niche `ItemTopicReference` and opens the dialog | Pass |
| User opens Import from a group row | `ItemImportWindowTests.ContextMenuImportTargetsTopicRows` verifies group nodes are `IsTopic`; `OnContextImport` resolves a Group `ItemTopicReference` | Pass |

### Requirement: Item CSV import creates items at the targeted niche or group

| Scenario | Verification | Result |
| --- | --- | --- |
| User imports from a niche | `ItemCsvImportServiceTests.ImportIntoNiche_CreatesTopLevelDraftItems` (NicheId set, GroupId null, StoreId matches) | Pass |
| User imports from a group | `ItemCsvImportServiceTests.ImportIntoGroup_CreatesItemsInThatGroup` (GroupId = target group, effective NicheId, StoreId) | Pass |
| Import destination is persisted with the items | Same tests assert persisted `Item.NicheId`/`GroupId`/`StoreId` matching the destination | Pass |

### Requirement: Item CSV import maps columns to item fields and chooses the stage

| Scenario | Verification | Result |
| --- | --- | --- |
| Title becomes the working title | `ImportIntoNiche_CreatesTopLevelDraftItems` asserts persisted `Item.Name` | Pass |
| Base Idea maps to the Idea field | `ItemCsvImportServiceTests.Import_WritesMetadataKeysFromColumns` asserts `MetadataJson["idea"]` | Pass |
| Concept Idea, Phrase, and Graphic map to concept fields and Concept stage | `Import_WritesMetadataKeysFromColumns` (keys `concept.idea`/`phrase`/`graphicDirection`) + `Import_ChoosesConceptStageWhenConceptFieldsPresentElseIdea` (Concept stage) | Pass |
| Item with only a Base Idea is created at the Idea stage | `Import_ChoosesConceptStageWhenConceptFieldsPresentElseIdea` (Idea stage) | Pass |
| Notes map to item notes | `Import_WritesMetadataKeysFromColumns` asserts `MetadataJson["notes"]` | Pass |
| Tags are linked to the item (creating the tag if absent) | `Import_WritesMetadataKeysFromColumns` (2 links) + `Import_ValidTagsAreLinkedToCreatedItems` (tags created and linked) + `Import_ArchivedTagNameCreatesNewActiveTag` | Pass |
| Inherited context metadata applies to non-CSV keys with CSV override | `Import_AppliesInheritedMetadataForNonCsvKeysAndCsvOverrides` (`idea` overridden by CSV, `idea.audience` inherited, inherited markers correct) | Pass |
| Inherited tags merge with imported tags (deduped) | `Import_MergesInheritedTagsWithCsvTagsAndDeduplicates` (union linked, no dedupe duplications) | Pass |

### Requirement: Item CSV import validates rows and requires a title

| Scenario | Verification | Result |
| --- | --- | --- |
| Row without a title is rejected (excluded + error, Import disabled) | `ItemCsvCodecTests.Parse_BlankTitleExcludesRowAndReportsError` (excluded from `Rows`, error reported) | Pass |
| Rows with duplicate titles are imported as-is | `ItemCsvImportServiceTests.Import_DuplicateTitlesAreBothImported` | Pass |
| Archived tag name is not reused by import | `ItemCsvImportServiceTests.Import_ArchivedTagNameCreatesNewActiveTag` (archived untouched; new active tag created and linked) | Pass |

### Requirement: Item CSV import provides a dialog with raw source, preview, and syntax check

| Scenario | Verification | Result |
| --- | --- | --- |
| User opens the import dialog from a contextual target | `OnContextImport` opens `ItemImportWindow` with a `TargetLabel`; `ItemImportWindowTests.Window_ConstructsWithRequiredControls` | Pass |
| User selects a CSV file | `ItemImportViewModelTests.PickFile_HydratesRawSourceAndRunsPreview` (file contents → `RawSource`, preview runs) + decoder-failure → `HasLoadError` | Pass |
| User edits the raw source | `ItemImportWindow` `RawSourceBox` is editable (constructed in `Window_ConstructsWithRequiredControls`); `RunPreview` re-parses edited source | Pass |
| User exports a sample file | `ItemImportViewModelTests.ExportSample_WritesCodecSampleToExportStream` + `ItemCsvCodecTests.WriteSample...` | Pass |
| Preview runs a syntax check and disables Import on error | `ItemImportViewModelTests.RunPreview_WithErrorDisablesImport` (detailed `Line N: <reason>`) + `ItemImportWindowTests.ImportButtonIsEnabledOnlyWhenPreviewIsValid` (disabled); `ItemCsvCodecTests.Parse_WrongColumnCountReportsErrorOnLine` names the expected columns and count, `Parse_BlankTitleExcludesRowAndReportsError` names the Title field | Pass |
| Preview shows a valid import and enables Import | `ItemImportViewModelTests.RunPreview_PopulatesPreviewAndEnablesImport` + `ItemImportWindowTests.ImportButtonIsEnabledOnlyWhenPreviewIsValid` (enabled) | Pass |
| Empty source disables Import | `ItemImportViewModelTests.RunPreview_EmptySourceDisablesImport` | Pass |

## Scoped completion QA summary (see docs/qa-review.md)

- **Build & deterministic tests:** all four test projects green; `dotnet test .\FusionCanvas.sln` passes (983 tests). One integration test flaked once during a parallel-solution run and passed consistently on re-runs (unrelated to this change); the final baseline run is green.
- **OpenSpec validation:** `openspec validate --changes import-items-from-csv` passes.
- **Architecture:** All creative-field/tag/business logic lives in Application/Domain (`ItemCsvImportService`, `ItemMetadataCodec`, `ItemCsvCodec` contracts); UI (`ItemImportWindow`/VM/picker/`OnContextImport`) is in App; CSV parsing is in Integration. No new persistence schema; reuses `IWorkspaceRepository` snapshot save. Dependencies point inward (App → Application → Domain; Integration → Application).
- **Security:** Item CSV content is treated as untrusted input — the parser validates column counts and title presence, UTF-8 is strict, and no raw file content is executed or interpolated into SQL; the import goes through the existing typed snapshot save path.
- **UI risk:** Headless view tests (`ItemImportWindowTests`) cover dialog construction, raw-source/preview bindings, and Import enabled/disabled states. Live desktop observation was not performed (ad hoc, not a completion gate).

## Limitations

- **Empty fields in any column position are supported** via standard CSV quoting, matching the export format; consecutive separators represent an empty field rather than an escaped semi-colon. (The earlier `;;`-escape convention is no longer used.)
- **Tag names containing commas are not supported** (the comma is always a tag separator in the `Tags` column).
- **Header detection is exact-match on the first line only**: a real item row whose seven fields literally equal the column headings is treated as a header.
- Full export of existing items is a separate module and out of scope here (only sample-file export is included).
