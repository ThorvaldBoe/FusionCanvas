## Context

FusionCanvas stores all workspace data in a single `WorkspaceSnapshot` loaded through `IWorkspaceRepository.LoadAsync()`. Items carry their creative text (Base Idea, Concept Idea, Phrase, Graphic, Notes) in `Item.MetadataJson`, decoded by the internal `ItemMetadataCodec` (`src/FusionCanvas.Application/Items/ItemMetadataCodec.cs`). Tags are a many-to-many link via `ItemTags` → `Tag`. The navigation tree is powered by `WorkspaceTreeViewModel` (`src/FusionCanvas.App/Navigation/WorkspaceTreeViewModel.cs`) which already holds `_snapshot` and offers context-menu commands. The tree row context menu is defined in `src/FusionCanvas.App/Views/MainWindow.axaml:298-312` with code-behind handlers in `MainWindow.axaml.cs`.

There is no existing item-export functionality. A close CSV precedent exists: `SnowcloneCsvCodec` (`src/FusionCanvas.Integration/Snowclones/SnowcloneCsvCodec.cs`) writes a UTF-8 CSV with header + rows and a field-quoting `Escape` helper, and `AvaloniaSnowcloneCsvFilePicker` provides a `SaveFilePicker` flow. These are the patterns to mirror, but the item export requires semi-colon delimiting and a different projection.

## Goals / Non-Goals

**Goals:**
- Add `Export to CSV...` to the **niche and group** context-menu rows in the navigation tree.
- Export the row's subtree (group descendants + directly-attached niche items) as a UTF-8, semi-colon-delimited CSV with header `Title;Base Idea;Concept Idea;Phrase;Graphic;Notes;Tags`.
- Exclude archived and empty items; safely quote multi-line and separator-containing fields.
- Keep logic testable at the lowest reliable layers (domain/application/integration), with only thin orchestration in the App.

**Non-Goals:**
- No bulk export across stores, no export of a single item row, no import.
- No CSV import.
- No export of asset files or marketplace metadata beyond the 7 specified columns.
- No change to the CSV behavior of the existing snowclone import/export.

## Decisions

### 1. Export scope and projection live in a new Application service
Add `IItemCsvExportService` (Application) with an implementation that, given a `WorkspaceSnapshot`, a topic kind (`Niche` or `Group`), and a topic id, returns `IReadOnlyList<ItemCsvRow>`.

- **Group scope**: items whose `GroupId` is in the group's `GroupHierarchy.GetDescendants(_snapshot, group)` set plus the group itself.
- **Niche scope**: items attached directly to the niche (no group) plus items in every group whose effective niche is that niche (via `GroupHierarchy.GetEffectiveNiche`), plus those groups' descendants.
- **Filters**: drop items where `!ItemHierarchy.IsEffectivelyActive(_snapshot, item)` (this already returns false for archived items and items under archived ancestors), and drop empty items (no working title — `string.IsNullOrWhiteSpace(Item.Name)` — AND no content in any exported column, including no tags).
- **Projection**: mirror `ItemInspectorService.FindAndBuildState` field reads (`src/FusionCanvas.Application/Items/ItemInspectorService.cs:280-343`) using `ItemMetadataCodec` keys (`IdeaKey`, `ConceptIdeaKey`, `PhraseKey`, `GraphicDirectionKey`, `NotesKey`) and tag names from `ItemTags` → `Tag`. Title = `Item.Name`. Tags SHALL be joined in a deterministic order — `OrderBy(name, StringComparer.OrdinalIgnoreCase)` mirroring `ItemInspectorService`'s `availableTagNames` (`ItemInspectorService.cs:320-324`).
- Rationale: `ItemMetadataCodec` is `internal` to the Application assembly, so any codec-driven field reading must live in Application (not Domain/Integration). Keeping scope + projection together makes them unit-testable without repositories or UI.

### 2. CSV writing is a dedicated Integration codec
Add `IItemCsvCodec` (Application) implemented by `ItemCsvCodec` (Integration), mirroring `SnowcloneCsvCodec`:
- UTF-8 (`UTF8Encoding(false, true)`), `StreamWriter` with `NewLine = "\r\n"`, `leaveOpen: true`.
- Header line `Title;Base Idea;Concept Idea;Phrase;Graphic;Notes;Tags`.
- One data row per `ItemCsvRow`.
- Field delimiter `;`.
- `Escape` mirrors `SnowcloneCsvCodec.Escape` but with delimiter `;`: quote a field when it contains `;`, `"`, `\r`, or `\n`; double embedded `"`.
- Rationale: matches the existing codec structure and its proven leave-open/flush semantics, and keeps file/encoding concerns in Integration.

### 3. Standard CSV quoting (chosen over literal `;;` escaping)
The issue text suggested escaping `;` as `;;`. We adopt standard field quoting (Decision 2) because the creative fields are multi-line by spec (`basic-product-workflow`, `listing-inspector`). Raw newlines inside a field would otherwise break the row structure. This is a deliberate, user-approved deviation captured in the spec.

### 4. File picker follows the snowclone pattern
Add `IItemCsvFilePicker` (Application or App), `AvaloniaItemCsvFilePicker` (App) mirroring `AvaloniaSnowcloneCsvFilePicker` with `Title = "Export items to CSV"`, `SuggestedFileName = "items.csv"`, `DefaultExtension = "csv"`, `*.csv` filter, and `NullItemCsvFilePicker` for deterministic tests. Wire it like `SnowcloneLibraryWindow` sets `viewModel.FilePicker`.

### 5. Orchestration lives on `WorkspaceTreeViewModel`
Add an `ExportCsvRequested`/context command flow:
- Add an `IItemCsvExportService` (+ `IItemCsvCodec`, + `IItemCsvFilePicker`) optional constructor dependency to `WorkspaceTreeViewModel` (defaulted to a `Null` picker and real service/codec) so existing construction sites and navigator tests keep compiling.
- `WorkspaceTreeViewModel.ExportCsv(WorkspaceTreeNodeViewModel node)` computes rows via the export service from `_snapshot` + `node.EntityKind`/`node.EntityId`, asks the picker for a destination stream, and writes via the codec. Cancel (picker returns null) is a no-op. On a write failure the VM sets `_errorMessage` (reuse the existing error surface) and the export is not reported as successful. A partially written destination file may remain on disk; this is an accepted limitation documented in Risks, and the spec wording reflects it.
- A new `MenuItem` in `MainWindow.axaml` with `Header="Export to CSV..."`, `IsVisible="{Binding IsTopic}"`, `Click="OnContextExport"`, and a code-behind handler following `OnContextDelete` — resolve the node from `sender.DataContext` (niche or group), select it, then call `viewModel.WorkspaceTree.ExportCsv(node)`.

Rationale: the VM already owns `_snapshot` and the selected node, so this keeps the export command next to the other tree commands and avoids new UI plumbing.

## Risks / Trade-offs

- **Niche-wide export cost** → Computing effective niches across all groups is O(groups) per export, which is negligible for the snapshot in memory; no mitigation beyond keeping it in-memory.
- **Quoting vs. the issue's literal `;;`** → Standard quoting is interoperable with spreadsheets; noted as a spec deviation and approved.
- **`IsTopic` includes groups and niches only** → Confirmed `WorkspaceTreeNodeViewModel.IsTopic` is exactly `Niche or Group` (`WorkspaceTreeViewModel.cs:99`), so the menu item appears on the right rows.
- **Error handling leaves partial file** → The codec writes directly to the picker's destination stream and flushes, so on a mid-write failure a partial file may remain on disk. This is an accepted trade-off; the spec requires only that the failure be reported and the export not be reported as successful. Mitigation considered (writing to a temp stream then committing) is deferred as a non-goal.

## Migration Plan

None required. This adds new behavior; no existing data or schema changes. The change is implemented in the `items-export` worktree and later archived through the standard OpenSpec workflow.

## Open Questions

None. All high-impact decisions (menu placement on niches+groups, subtree scope, archived/effectively-inactive and empty exclusion, CSV quoting strategy) are resolved and captured in the specs and proposal.

## Implementation Plan

Sequencing, from lowest to highest layer, each step ending in its focused tests.

1. **Application — export service**
   - Files: `src/FusionCanvas.Application/Items/IItemCsvExportService.cs`, `src/FusionCanvas.Application/Items/ItemCsvExportService.cs`, `src/FusionCanvas.Application/Items/ItemCsvRow.cs` (record with `Title, BaseIdea, ConceptIdea, Phrase, Graphic, Notes, Tags`).
   - `ItemCsvExportService.Project(snapshot, WorkspaceEntityKind topicKind, Guid topicId)`:
     - Resolve scope (Decision 1), using `GroupHierarchy.GetDescendants` / `GetEffectiveNiche` and `ItemHierarchy`.
     - Filter archived/inactive and empty items.
     - Project each item into `ItemCsvRow` using `ItemMetadataCodec` keys and tag-name resolution.
   - Order items for stable output (e.g. by created/updated or insertion order; pick a deterministic order and note it).
   - Empty definition helper shared with tests (uses `string.IsNullOrWhiteSpace(Item.Name)` plus no column content).
   - Tests: `tests/FusionCanvas.Application.Tests/Items/ItemCsvExportServiceTests.cs` — scope for group (including subgroups), scope for niche (direct + group items), zero-item group/niche returns an empty row list, archived/effectively-inactive exclusion (including items under an archived descendant), empty exclusion, field projection, tag join in deterministic order, missing-field empties.

2. **Application — CSV codec interface**
   - File: `src/FusionCanvas.Application/Items/IItemCsvCodec.cs` with `Task WriteAsync(Stream, IReadOnlyList<ItemCsvRow>, CancellationToken)`.

3. **Integration — CSV codec**
   - File: `src/FusionCanvas.Integration/Items/ItemCsvCodec.cs` implementing `IItemCsvCodec`, mirroring `SnowcloneCsvCodec` with `;` delimiter and `;`-aware `Escape`.
   - Tests: `tests/FusionCanvas.Integration.Tests/Items/ItemCsvCodecTests.cs` — header line, row order, quoting for `;` / `"` / newline fields, empty values, UTF-8 output.

4. **App — file picker**
   - Files: `src/FusionCanvas.App/Items/IItemCsvFilePicker.cs`, `AvaloniaItemCsvFilePicker.cs`, `NullItemCsvFilePicker.cs` (mirror snowclone equivalents).
   - Wire an instance in the window that owns the tree (mirror `SnowcloneLibraryWindow.axaml.cs:28`).

5. **App — menu + orchestration**
   - `src/FusionCanvas.App/Navigation/WorkspaceTreeViewModel.cs`: add optional `IItemCsvExportService`/`IItemCsvCodec`/`IItemCsvFilePicker` ctor dependencies (defaulted), `ExportCsv(WorkspaceTreeNodeViewModel node)` method setting `_errorMessage` on failure.
   - `src/FusionCanvas.App/Views/MainWindow.axaml`: add `<MenuItem Header="Export to CSV..." IsVisible="{Binding IsTopic}" Click="OnContextExport" />` in the context menu.
   - `src/FusionCanvas.App/Views/MainWindow.axaml.cs`: add `OnContextExport` following the existing `OnContextDelete` pattern — resolve the node from `sender.DataContext`, select it, then call `viewModel.WorkspaceTree.ExportCsv(node)`; wire the `AvaloniaItemCsvFilePicker` from `StorageProvider`.
   - Tests: `tests/FusionCanvas.App.Tests/...` — Avalonia headless view test asserting the menu item exists on niche and group rows (not item rows) and the click handler wires to the export path; plus framework-free VM tests: cancel with a `Null` picker (no write, no error) and a stub picker returning a `Stream` whose `WriteAsync`/`FlushAsync` throws (assert `_errorMessage` set and no success path taken).

6. **Verification**
   - `openspec validate` and change status.
   - `dotnet test .\FusionCanvas.sln` (baseline).

### Acceptance-to-verification mapping
| Acceptance scenario | Verification method |
|---|---|
| Export a group with nested subgroups | Application service test (scope includes descendants) + Integration output |
| Export a niche covering all its items | Application service test (direct + group items) |
| Empty group exports only the header | Application service test (zero-item scope returns empty list) + Integration codec test (empty rows → header only) |
| Archived and empty items are omitted | Application service filter tests |
| Items under archived descendants are omitted | Application service filter test (item under archived subgroup excluded) |
| Save is canceled | VM test with Null picker (no write, no error) |
| Export fails | VM test with stub picker returning a throwing `Stream` (error surfaced, not reported as successful) |
| Header order matches spec | Integration codec test (exact header string) |
| Item fields map to columns | Projection tests in Application service |
| Missing fields export as empty | Projection tests |
| Semi-colon in a field preserved | Integration `Escape` test |
| Multi-line field preserved | Integration `Escape` test |
| Embedded double-quote doubled | Integration `Escape` test |
