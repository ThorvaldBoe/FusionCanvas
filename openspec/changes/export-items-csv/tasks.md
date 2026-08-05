## 1. Application layer — export projection service

- [x] 1.1 Add `ItemCsvRow` record (`Title, BaseIdea, ConceptIdea, Phrase, Graphic, Notes, Tags`) in `src/FusionCanvas.Application/Items/ItemCsvRow.cs`.
- [x] 1.2 Add `IItemCsvExportService` interface in `src/FusionCanvas.Application/Items/IItemCsvExportService.cs` exposing `IReadOnlyList<ItemCsvRow> Project(WorkspaceSnapshot snapshot, WorkspaceEntityKind topicKind, Guid topicId)`.
- [x] 1.3 Implement `ItemCsvExportService` in `src/FusionCanvas.Application/Items/ItemCsvExportService.cs`:
  - group scope = `GroupHierarchy.GetDescendants(group)` + self;
  - niche scope = items directly on the niche (no group) + items in every group whose effective niche is that niche and their descendants;
  - exclude `!ItemHierarchy.IsEffectivelyActive(...)` (covers archived items and items under archived ancestors);
  - exclude empty items (no title via `string.IsNullOrWhiteSpace(Item.Name)` AND no content in any exported column, including no tags);
  - project via `ItemMetadataCodec` keys (`IdeaKey`, `ConceptIdeaKey`, `PhraseKey`, `GraphicDirectionKey`, `NotesKey`) and tag names from `ItemTags` → `Tag`, joined `OrderBy(name, StringComparer.OrdinalIgnoreCase)`;
  - deterministic output order (document the chosen order).
- [x] 1.4 Add `tests/FusionCanvas.Application.Tests/Items/ItemCsvExportServiceTests.cs` covering group-with-subgroups scope, niche (direct + group) scope, zero-item group/niche (empty row list), archived/effectively-inactive exclusion (including item under an archived subgroup), empty exclusion, field projection, tag join in deterministic order, and missing-field empties.

## 2. Integration layer — CSV codec

- [x] 2.1 Add `IItemCsvCodec` interface in `src/FusionCanvas.Application/Items/IItemCsvCodec.cs` with `Task WriteAsync(Stream, IReadOnlyList<ItemCsvRow>, CancellationToken)`.
- [x] 2.2 Implement `ItemCsvCodec` in `src/FusionCanvas.Integration/Items/ItemCsvCodec.cs` mirroring `SnowcloneCsvCodec`: UTF-8 (throw-on-invalid), `StreamWriter` with `NewLine = "\r\n"`, `leaveOpen: true`, header `Title;Base Idea;Concept Idea;Phrase;Graphic;Notes;Tags`, `;` delimiter, quote field when it contains `;`/`"`/`\r`/`\n`, double embedded `"`.
- [x] 2.3 Add `tests/FusionCanvas.Integration.Tests/Items/ItemCsvCodecTests.cs` covering exact header line, row order, quoting for `;`/`"`/newline, empty values, and UTF-8 output.

## 3. App layer — file picker

- [x] 3.1 Add `IItemCsvFilePicker` interface in `src/FusionCanvas.App/Items/IItemCsvFilePicker.cs` with `Task<Stream?> OpenExportAsync(CancellationToken)`.
- [x] 3.2 Add `AvaloniaItemCsvFilePicker` in `src/FusionCanvas.App/Items/` mirroring `AvaloniaSnowcloneCsvFilePicker` (`Title = "Export items to CSV"`, `SuggestedFileName = "items.csv"`, `DefaultExtension = "csv"`, `*.csv` filter).
- [x] 3.3 Add `NullItemCsvFilePicker` returning `null` for deterministic tests.
- [x] 3.4 Wire `AvaloniaItemCsvFilePicker` where the navigation tree's window is constructed (mirror `SnowcloneLibraryWindow.axaml.cs:28`), and pass it into `WorkspaceTreeViewModel`.

## 4. App layer — menu and orchestration

- [x] 4.1 Add optional `IItemCsvExportService`/`IItemCsvCodec`/`IItemCsvFilePicker` constructor dependencies to `WorkspaceTreeViewModel` (defaulted so existing construction sites and navigator tests compile).
- [x] 4.2 Add `ExportCsv(WorkspaceTreeNodeViewModel node)` method on `WorkspaceTreeViewModel`: resolve rows via the service from `_snapshot` + `node.EntityKind`/`node.EntityId`, ask the picker for a destination stream, write via the codec; cancel (picker returns null) is a no-op; write failures set `_errorMessage` and the export is not reported as successful (a partial file may remain).
- [x] 4.3 Add `<MenuItem Header="Export to CSV..." IsVisible="{Binding IsTopic}" Click="OnContextExport" />` to the tree context menu in `src/FusionCanvas.App/Views/MainWindow.axaml`.
- [x] 4.4 Add `OnContextExport` handler in `src/FusionCanvas.App/Views/MainWindow.axaml.cs` following the existing `OnContextDelete` pattern — resolve the node from `sender.DataContext`, select it, then call `viewModel.WorkspaceTree.ExportCsv(node)`.
- [x] 4.5 Add a framework-free VM test for `ExportCsv`: a `Null` picker (no write, no error) and a stub picker returning a `Stream` whose `WriteAsync`/`FlushAsync` throws (assert `_errorMessage` set and no success path taken).

## 5. UI view tests

- [x] 5.1 Add an Avalonia headless view test asserting the `Export to CSV...` menu item is present on niche and group rows and absent on item rows, and that clicking it invokes the export path.

## 6. Verification

- [x] 6.1 Run `openspec validate` and confirm the change is valid (strict schema, delta spec structure).
- [x] 6.2 Run `dotnet test .\FusionCanvas.sln` and confirm the full baseline passes.
- [x] 6.3 Re-run `openspec status --change "export-items-csv"` and confirm all `applyRequires` artifacts are `done`.
