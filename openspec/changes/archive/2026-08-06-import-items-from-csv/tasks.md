# Import Items from CSV — Tasks

## 1. Application contracts (framework-free)

- [x] 1.1 Add `src/FusionCanvas.Application/Items/Import/ItemCsvRow.cs` — record with `Title`, `BaseIdea`, `ConceptIdea`, `Phrase`, `Graphic`, `Notes`, `IReadOnlyList<string> Tags`, and `int LineNumber`.
- [x] 1.2 Add `src/FusionCanvas.Application/Items/Import/ItemCsvParseError.cs` — record `(int LineNumber, string Message)`.
- [x] 1.3 Add `src/FusionCanvas.Application/Items/Import/ItemCsvParseResult.cs` — `IReadOnlyList<ItemCsvRow> Rows`, `IReadOnlyList<ItemCsvParseError> Errors`, plus `IReadOnlyList<string> ErrorText` that yields `Error on line N` messages.
- [x] 1.4 Add `src/FusionCanvas.Application/Items/Import/IItemCsvCodec.cs` — `ItemCsvParseResult Parse(string source)` and `string WriteSample()`.
- [x] 1.5 Add `src/FusionCanvas.Application/Items/Import/ItemCsvImportRequest.cs` and `ItemCsvImportResult.cs` — request `(ItemTopicReference Target, IReadOnlyList<ItemCsvRow> Rows)`; result `(bool Succeeded, int ImportedCount, IReadOnlyList<string> Errors)`.
- [x] 1.6 Add `src/FusionCanvas.Application/Items/Import/IItemCsvImportService.cs` — `Task<ItemCsvImportResult> ImportAsync(ItemCsvImportRequest request, CancellationToken ct)`.

## 2. CSV codec (Integration)

- [x] 2.1 Add `src/FusionCanvas.Integration/Items/Import/ItemCsvCodec.cs` implementing `IItemCsvCodec` with a bespoke `;`/`;;` parser (decode `;;` as literal `;`, lone `;` as delimiter, handle trailing empty field and CRLF/LF and final newline).
- [x] 2.2 Detect and skip an optional header row whose fields match the seven column headings (case-insensitive).
- [x] 2.3 Validate exactly 7 fields per row and record a structural error with the physical line number for `Error on line N`.
- [x] 2.4 Report a blank Title as a row-level validation error (still disables import).
- [x] 2.5 Implement `WriteSample()` returning a header plus example rows that demonstrate `;;` escaping and comma-separated tags.

## 3. Import service (Application)

- [x] 3.1 Implement `ItemCsvImportService` depending on `IWorkspaceRepository`, `IToolContextResolver`, `Func<DateTimeOffset>`, `IItemIdGenerator` (mirroring `ItemManagementService`'s constructor fallbacks); load the snapshot and resolve the target topic to `storeId`/`nicheId`/`groupId` (reusing the existing topic-resolution contract).
- [x] 3.2 For each parsed row (blank-title rows never reach the service since the codec excludes them): normalize/validate the title (`ItemMetadataCodec.NormalizeName` + `ValidateName`).
- [x] 3.3 Build the metadata dict from inherited context metadata for the target topic (via `IToolContextResolver.Resolve` + `ResolveCreationDefaults`, marking inherited values with `InheritedFromPrefix`), then apply the row's CSV values so CSV overrides inherited values for the same key. Apply per-column normalization: Title→`NormalizeName`+`ValidateName`; Phrase→`NormalizeSingleLine`; Base Idea/Concept Idea/Graphic/Notes→`NormalizeOptional`.
- [x] 3.4 Choose the stage: `Concept` when any Concept Idea/Phrase/Graphic is populated, otherwise `Idea`; create each item as `Draft` at that stage beneath the resolved niche/group.
- [x] 3.5 Resolve-or-create tags by name matching **active** tags only (mirror `ItemManagementService.ResolveTagIds`'s `!candidate.IsArchived` filter, not `ItemInspectorService`'s archived-inclusive match), union with inherited tags from `ResolveCreationDefaults`, dedupe, and add `ItemTag` links for the created items.
- [x] 3.6 Build the single updated `WorkspaceSnapshot`, call `SaveAsync` once, and return the count/errors; do not modify `ItemManagementService.CreateItemAsync`.

## 4. Integration tests

- [x] 4.1 Add `tests/FusionCanvas.Integration.Tests/Items/Import/ItemCsvCodecTests.cs` covering valid parse, header skip, `;;` decoding, wrong column count → `Error on line N`, missing title (excluded from `Rows`), comma tags, CRLF handling, empty source → zero rows, header-vs-data-row indistinguishable case, and `WriteSample` shape.

## 5. Application tests

- [x] 5.1 Add `tests/FusionCanvas.Application.Tests/Items/Import/ItemCsvImportServiceTests.cs` using the in-memory `TestRepository`/`Sample` harness: items created at the niche and at the group, Idea vs Concept stage selection, metadata keys written, per-column normalization (esp. Phrase single-line), inherited context metadata applied for non-CSV keys with CSV override, inherited tags merged with CSV tags, archived-tag name creates a new active tag (not linked to archived), tags created-and-linked, single-save behavior, and duplicate titles both imported.

## 6. App UI

- [x] 6.1 Add `src/FusionCanvas.App/Items/Import/IItemCsvFilePicker.cs`, `NullItemCsvFilePicker.cs`, and `AvaloniaItemCsvFilePicker.cs` (open import stream + save sample, `*.csv` filter) mirroring `AvaloniaSnowcloneCsvFilePicker`.
- [x] 6.2 Add `src/FusionCanvas.App/Items/Import/ItemImportViewModel.cs` with `TargetLabel`, editable `RawSource`, read-only `PreviewRows`, `ErrorMessages`, `CanImport` (true only when `Rows` is non-empty and there are no errors), `HasImportCompleted`, and `PickFile`/`ExportSample`/`RunPreview`/`Import`/`Cancel` commands with property-injected `IItemCsvFilePicker`/`IItemCsvCodec`/`IItemCsvImportService`. `PickFile` reads the picked stream as strict UTF-8 into `RawSource` (decoder failures surface as a load error) before running the preview.
- [x] 6.3 Add `src/FusionCanvas.App/Items/Import/ItemImportWindow.axaml` + `.axaml.cs` (modal, mirroring `SnowcloneLibraryWindow`): raw-source text box, read-only preview list, "Pick file"/"Export sample" buttons, error text, Import/Cancel row; attach VM on `Opened`, inject the Avalonia picker from `StorageProvider`, raise close on completion.
- [x] 6.4 Add `OnContextImport` and an `IsTopic` (Niche or Group) selection helper to `MainWindow.axaml.cs`; add the `<MenuItem Header="Import…" IsVisible="{Binding IsTopic}" Click="OnContextImport"/>` to the tree context menu in `MainWindow.axaml`; open the dialog targeted at the selected niche/group and refresh the tree on success. This context-menu entry is owned by the new `item-csv-import` capability; do not modify the `group-management` or `niche-management` specs.
- [x] 6.5 Register the new services (`IItemCsvImportService`, `IItemCsvCodec`, file picker) in the DI composition root `src/FusionCanvas.App/Workspace/AppWorkspaceFactory.cs`.

## 7. App tests

- [x] 7.1 Add `tests/FusionCanvas.App.Tests/Items/Import/ItemImportViewModelTests.cs` (framework-free): pick file hydrates `RawSource` (including UTF-8 decoder-failure → load error), `RunPreview` populates preview and toggles `CanImport` on errors and on empty source, `Error on line N` formatting, Import invokes the service and sets completion, Cancel leaves no mutation.
- [x] 7.2 Add headless view test `tests/FusionCanvas.App.Tests/Items/Import/ItemImportWindowTests.cs` (using `HeadlessTestApp`/`MainWindowFixture`): dialog construction, raw-source/preview bindings, Import enabled/disabled states, and the context-menu Import entry visibility for niche/group rows where framework risk is material.

## 8. Verification gates

- [x] 8.1 Run strict OpenSpec validation: `openspec validate --changes import-items-from-csv` passes.
- [x] 8.2 Run the solution baseline: `dotnet test .\FusionCanvas.sln` passes green.
- [x] 8.3 Map every acceptance scenario in the delta specs to passing test evidence and record results in `verification.md`.
- [x] 8.4 Perform scoped completion QA per `docs/qa-review.md` (build, tests, spec drift review, architecture/security/persistence/UI checks relevant to this change).
