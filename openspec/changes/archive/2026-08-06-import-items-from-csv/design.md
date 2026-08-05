## Context

FusionCanvas has no way to bulk-import existing designs. GitHub issue #118 asks for an item import from a standard text format, reachable from the navigation-tree context menu on a niche (top level) or group. The CSV/serialization precedent is the Snowclone library: an `IItemCsvCodec`-style codec plus a `StorageProvider`-backed file picker behind an interface, and a modal editor window. Items are stored as `Item` records whose creative fields live in `MetadataJson` under fixed keys defined by `ItemMetadataCodec` (Application). Items are created through `ItemManagementService.CreateItemAsync`, which today always creates a `Draft` item at the `Idea` stage and links pre-resolved tag IDs.

This module is one coherent, user-visible outcome: import a list of designs as items into a chosen niche or group. It deliberately does **not** include full export of existing items (a separate module), nor editing/deleting/dedup of existing items, nor workspace-level transfer.

## Goals / Non-Goals

**Goals:**
- Define an authoritative item CSV text format (7 columns, `;` delimiter, standard double-quote field quoting matching the export format, optional header, comma-separated tags).
- Provide an "Import…" context-menu action on niche and group rows that opens a targeted import dialog.
- Dialog supports file pick, sample-file export, an editable raw-source field, a read-only parsed preview, and a syntax check that disables Import and shows a detailed `Line N: <reason>` error naming the offending line and column on malformed input.
- Create imported items at the chosen niche/group with correct field→metadata mapping and stage (Concept when a concept field is present, else Idea), creating-and-linking tags by name.
- Deterministic tests: domain/application/CSV/headless-view coverage in the `dotnet test` baseline.

**Non-Goals:**
- Full/partial export of existing items (sample-file export only).
- Editing, overwriting, or deduplicating existing items.
- Duplicate-title handling beyond importing them as-is.
- New persistence schema (reuses `WorkspaceSnapshot` save path).

## Decisions

### 1. A dedicated `ItemCsvImportService` composes the snapshot and saves once
`ItemManagementService.CreateItemAsync` always creates at `Idea` and only links pre-resolved tag IDs — it cannot express Concept stage placement or create tags by name. Rather than distort it, add `IItemCsvImportService` / `ItemCsvImportService` in Application that loads the snapshot once, builds every imported item + tag + `ItemTag` link in memory, and calls `IWorkspaceRepository.SaveAsync` once. It reuses the healthy parts of `ItemManagementService`/`ItemInspectorService` logic (name normalization/validation, `Idea`/`concept.idea`/`phrase`/`graphicDirection`/`notes` keys, tag normalize + resolve-or-create). Keeps `CreateItemAsync` untouched (no refactor risk) and yields one atomic-ish persisted change per import.

The service depends on `IToolContextResolver` (like `ItemManagementService`) so imported items inherit applicable context metadata and tags from the target niche/group, satisfying the accepted `context-aware-tools` spec. Inherited metadata is applied for keys **not** supplied by the CSV row (CSV values override inherited values for the same key, mirroring `ItemManagementService.ResolveCreationMetadata` + `ApplyContextMetadata(replaceExplicitMetadata: true)`), and inherited tags are merged with the row's CSV tags (deduped). Archived tags are never matched: tag-name resolution matches **active** tags only (mirroring `ItemManagementService.ResolveTagIds`, which filters `!candidate.IsArchived`); a name with no active match creates a new active tag. Interactive archived-tag restoration is out of scope for bulk import (decision not to reopen).
- Alternative considered: loop `CreateItemAsync` per row. Rejected because it cannot set Concept stage and would do N saves and error-prone partial commits.
- Alternative considered: skip inheritance entirely. Rejected because it contradicts `context-aware-tools` ("Topic-scoped tools create work in place … apply applicable inherited tags and metadata").

### 2. CSV codec lives in Integration behind an Application contract
Follow the Snowclone precedent exactly: an `IItemCsvCodec` (Application) implemented by `ItemCsvCodec` (Integration). The Snowclone codec uses `TextFieldParser` with a comma delimiter; that does **not** fit the item format, which uses a `;` delimiter with an optional header. So implement a bespoke tokenizing parser in `ItemCsvCodec` that reads the whole source as a character stream:
- A field is a run of characters. A field that begins with a double quote is quoted; it is ended by the matching closing quote, `""` inside a quoted field decodes to a literal `"`, and a quoted field may contain `;`, CR, or LF. A single `;` outside quotes is a field delimiter. This is the same standard quoting the export feature emits, so exported rows round-trip.
- A `;` outside quotes splits fields; an unquoted `\n`, lone `\r`, or `\r\n` ends the record. Empty fields are represented by empty space between separators and may appear in any column (including in the middle), so an empty Notes between Graphic and Tags is preserved rather than misread as an escaped semi-colon.
- Expect exactly 7 fields per row; otherwise record a structural error for that physical line number naming the expected columns and the count found.
- Header detection: if the first data line's fields match the known headings (Title, Base Idea, Concept Idea, Phrase, Graphic, Notes, Tags) case-insensitively, skip it as a header. A real item row whose seven fields literally equal the headings is indistinguishable from a header — this is an accepted, documented limitation of exact header detection.
- Build an `ItemCsvRow` (Title, BaseIdea, ConceptIdea, Phrase, Graphic, Notes, and parsed tags list). A blank Title is a row-level validation error: the row is **excluded from `Rows`** and reported only via `Errors`, so Import (gated on no errors) never fires while a title is missing. The import service therefore only ever receives valid rows.
- Tag parsing splits the `Tags` field on commas; tag names must not contain commas (no comma escaping is defined for this format) — a comma is always a separator.
- `Parse(sourceText)` returns `ItemCsvParseResult(Rows, Errors)` where `Errors` carries `(lineNumber, message)` rendered as a detailed `Line N: <reason>` that names the affected column and why it is incorrect. Also provide `WriteSample()` returning a header + example rows demonstrating quoting and comma tags.

The codec works on `string` source (raw dialog field), so a stream-based read is only used once at file-pick time to hydrate the raw field (read as strict UTF-8; a decoder failure is reported as a load error rather than crashing the dialog).

### 3. Syntax check gates the Import action
`RunPreview` calls `IItemCsvCodec.Parse(rawSource)`. `CanImport` is true only when `Rows` is non-empty **and** there are no errors (structural or missing-title). On any error (or an empty source) the dialog is non-editable-import state: Import disabled and an error text shown as `Line N: <reason>` naming the line and column (first error), so users know which field to fix. This is deterministic and framework-free, so it is unit-tested in App view-model tests; the codec and service are tested in Integration/Application tests respectively.

### 4. Stage decision on import
Per the resolved decision: an imported item is created at `Concept` stage when any of Concept Idea, Phrase, or Graphic is populated; otherwise at `Idea`. `Base Idea` is always stored under the `idea` key regardless of stage. The item is created `Draft` at the chosen stage, consistent with existing item creation. (Metadata keys are cumulative; storing `idea` plus concept fields is consistent with how items move through stages.)

### 5. File picker behind an interface with a null test double
Add `IItemCsvFilePicker` with `OpenImportAsync` and `OpenExportAsync` (sample), implemented by `AvaloniaItemCsvFilePicker` (uses `IStorageProvider`) mirroring `AvaloniaSnowcloneCsvFilePicker`, plus a `NullItemCsvFilePicker` for framework-free VM tests. The dialog VM receives the picker by property injection in code-behind (as `SnowcloneLibraryWindow` does), so VM tests stay framework-free.

### 6. Tree context-menu wiring for niche and group targets
`WorkspaceTreeNodeViewModel.IsTopic` (true for Niche or Group) already exposes exactly the rows that should offer Import. Add one menu item bound to `IsTopic`, and a code-behind handler that needs niche selection support. Existing `TrySelectContextNode` requires `HasContextActions` (Group or Item) and cannot select a niche, so extend the handler to also select a Niche node (call `SelectNodeCommand`) and open the dialog targeted at that niche's top level. On a successful import, refresh the tree so the new items appear. The `Import…` context-menu entry and its behavior are owned by the new `item-csv-import` capability; no existing capability (`group-management` or `niche-management`) is modified, avoiding cross-capability duplication.
- Location: `src/FusionCanvas.App/Views/MainWindow.axaml` context menu (lines ~298–312) and `MainWindow.axaml.cs` handlers (~591–679 + the `TrySelect*` helpers at 681–711).

### 7. UX preflight (primary workflow, surface placement, states)
- **Who/workflow:** a creator with an existing list of designs bulk-loading them into a target niche/group. Low frequency, occasional bulk action.
- **Surface:** a focused modal dialog (`ItemImportWindow`) reached from the tree context menu; nothing permanent in the main workspace — matches "keep occasional bulk/management in a focused surface."
- **Progressive disclosure:** dialog shows target label, file pick + "Export sample", then the editable raw-field and read-only preview with the Import action; advanced/error detail only as needed.
- **States:** empty (no file yet); loaded (raw + preview); error (`Error on line N`, Import disabled); ready (preview valid, Import enabled); success (items created, dialog closes, tree refreshes, focus returns to the invoked tree node); cancel (discards raw input, no persistence). Import creates items only on explicit activation, so cancellation never surprises the user with new items.

## Risks / Trade-offs

- **Custom parser correctness** (standard quoting, optional header, empty fields in any column, CRLF) → Cover thoroughly with framework-free `ItemCsvCodec` unit tests (valid, header, quoted `;` preserved, empty middle field, wrong column count → detailed `Line N` error, missing title, duplicate titles imported, round-trip with the exporter).
- **Niche selection plumbing is new** (existing context helpers skip Niche) → Small, isolated change in `MainWindow.axaml.cs`; verify with a headless tree test or focused handler test before relying on live UI.
- **Atomicity of bulk import** → Single snapshot save; if it fails, nothing is created and the error is surfaced, so no partial batch. Per-row failures are collected and reported rather than stopping mid-import.
- **Stage semantics** (Concept vs Idea) is a product decision resolved here and recorded in the spec; implementation must not reopen it.
- **No duplicate handling** may import rows users consider duplicates → Deliberate per resolved decision; can be revisited later without schema change.

## Open Questions

None blocking. The two product decisions (stage placement, duplicate-title behavior) are resolved. (Full item export is explicitly a separate module and not designed here.)

## Implementation Plan

Layered order, each step verifiable, ending with the full baseline.

### Application contracts (framework-free)
1. Add `src/FusionCanvas.Application/Items/Import/ItemCsvRow.cs` — record with `Title`, `BaseIdea`, `ConceptIdea`, `Phrase`, `Graphic`, `Notes`, and `IReadOnlyList<string> Tags`, plus the physical `LineNumber`.
2. Add `IItemCsvParseError`/`ItemCsvParseError.cs` — `(int LineNumber, string Message)`.
3. Add `ItemCsvParseResult.cs` — `IReadOnlyList<ItemCsvRow> Rows`, `IReadOnlyList<ItemCsvParseError> Errors`, and helper `IReadOnlyList<string> ErrorText` producing detailed `Line N: <reason>` strings.
4. Add `IItemCsvCodec.cs` (Application) — `ItemCsvParseResult Parse(string source)` and `string WriteSample()`.
5. Add `IItemCsvImportService.cs` + request/result records: `ItemCsvImportRequest(Target, Rows)` where `Target` is an `ItemTopicReference` (Niche or Group, from `ItemManagementService`/`ItemTopicReference`), and `ItemCsvImportResult(bool Succeeded, int ImportedCount, IReadOnlyList<string> Errors)`; plus `IItemCsvImportService.ImportAsync(request, ct)`.

### Integration
6. Add `src/FusionCanvas.Integration/Items/Import/ItemCsvCodec.cs` implementing `IItemCsvCodec` with the bespoke `;`-delimited tokenizing parser using standard double-quote field quoting (matching the exporter), header detection, 7-column validation, and `WriteSample`. (Place next to or near the Snowclone codec pattern.)
7. Implement `ItemCsvImportService` in Application (framework-free, uses `IWorkspaceRepository`, `IToolContextResolver`, `Func<DateTimeOffset> clock`, `IItemIdGenerator` idGenerator — mirroring `ItemManagementService`'s dependencies and constructor fallbacks):
   - Load snapshot; resolve target topic → storeId/nicheId/groupId (reuse the same resolution contract as `ItemManagementService.ResolveCreateTopicAsync`/`TryResolveActiveTopic` — reuse or minimally expose what's needed).
   - For each parsed row: normalize/validate title (`ItemMetadataCodec.NormalizeName` + `ValidateName`, blank rows never reach the service since the codec excludes them); apply per-column normalization — Title→`NormalizeName`+`ValidateName`; Phrase→`NormalizeSingleLine`; Base Idea/Concept Idea/Graphic/Notes→`NormalizeOptional`; Tags→per-tag trim + single-line validation mirroring `NormalizeTagNames`.
   - Build metadata dict starting from inherited context metadata for the target topic (via `_contextResolver.Resolve` + `ResolveCreationDefaults`, marking inherited values with `InheritedFromPrefix`), then apply the row's CSV values so CSV overrides inherited values for the same key (`idea`, `concept.idea`, `phrase`, `graphicDirection`, `notes`).
   - Choose stage (Concept iff any concept field present else Idea); create each item as `Draft` at that stage beneath the resolved niche/group.
   - Resolve-or-create tags by name matching **active** tags only (mirror `ItemManagementService.ResolveTagIds`'s `!candidate.IsArchived` filter, not `ItemInspectorService`'s archived-inclusive match), union with inherited tags from `ResolveCreationDefaults`, dedupe, and add `ItemTag` links.
   - Build the single updated `WorkspaceSnapshot`, `SaveAsync` once, and return result with count/errors.
   - Do not reuse `CreateItemAsync` directly (it is Idea-only + needs tag IDs); keep it unchanged.

### App (UI)
8. Add `IItemCsvFilePicker.cs` + `NullItemCsvFilePicker.cs` and `AvaloniaItemCsvFilePicker.cs` (App, mirror `AvaloniaSnowcloneCsvFilePicker`; open import stream + save sample, `*.csv` filter). The picker returns a `Stream`; the VM reads it as strict UTF-8 into `RawSource` before calling `Parse` (decoder failures are surfaced as a load error, not an exception).
9. Add `ItemImportViewModel.cs` (App): `TargetLabel`, `RawSource` (editable), read-only `PreviewRows` (parsed titles/preview lines), `ErrorMessages`, `CanImport`, `HasImportCompleted`; commands `PickFile`, `ExportSample`, `RunPreview`, `Import`, `Cancel`; property-injected `IItemCsvFilePicker`/`IItemCsvCodec`/`IItemCsvImportService` for test injection. On `RunPreview`/`Import`, apply the syntax-gate logic.
10. Add `ItemImportWindow.axaml` + `.axaml.cs` (modal `Window`, `SizeToContent`, `CenterOwner`, mirroring `SnowcloneLibraryWindow` visual/styling): raw-source `TextBox`, preview list, "Export sample" + "Pick file" buttons, error text, Import/Cancel row; wired in code-behind (attach VM on `Opened`, inject the Avalonia picker from `StorageProvider`, raise `CloseRequested`).
11. Wire the tree: in `MainWindow.axaml` context menu add `<MenuItem Header="Import…" IsVisible="{Binding IsTopic}" Click="OnContextImport"/>`; in `MainWindow.axaml.cs` add `OnContextImport` that selects the Niche or Group node (extend selection helper to accept `IsTopic`) and awaits `ItemImportWindow.ShowDialog(this)`, then refreshes the workspace tree on success. Register the new services in the DI composition root (`src/FusionCanvas.App/Workspace/AppWorkspaceFactory.cs`).

### Tests (mirror production, xUnit v3)
12. `tests/FusionCanvas.Integration.Tests/Items/Import/ItemCsvCodecTests.cs` — valid parse, header skipped, quoted `;`/`""`/newline decoding, empty middle field preserved, wrong column count → detailed `Line N` error, missing title, comma tags, CRLF handling, round-trip with the exporter, `WriteSample` shape.
13. `tests/FusionCanvas.Application.Tests/Items/Import/ItemCsvImportServiceTests.cs` — using an in-memory `TestRepository`/`Sample` harness (as `ItemManagementServiceTests`) verify items created at the niche/group, Idea vs Concept stage selection, metadata keys written, tags created-and-linked, missing-title error skips no import (or reports), single-save behavior, duplicate titles both imported.
14. `tests/FusionCanvas.App.Tests/Items/Import/ItemImportViewModelTests.cs` — framework-free VM tests: pick file hydrates `RawSource`, `RunPreview` populates preview + toggles `CanImport` on errors, detailed `Line N` error formatting, Import invokes service and sets completion, Cancel leaves no mutation.
15. Headless view test `tests/FusionCanvas.App.Tests/Items/Import/ItemImportWindowTests.cs` (with `HeadlessTestApp`/`MainWindowFixture` pattern) for the dialog: construction, raw-source/preview bindings, Import disabled/enabled states, and the context-menu Import entry visibility for niche/group rows where framework risk is material.

### Decisions not to reopen
- Stage = Concept when any Concept Idea/Phrase/Graphic present, else Idea; Base Idea always under `idea`.
- Duplicate titles import as-is.
- Inherited context metadata/tags from the target niche/group ARE applied (CSV overrides inherited values for the same key; inherited tags merge with CSV tags); archived tags are never matched (active-only), and interactive archived-tag restore is out of scope for bulk import.
- Blank-title rows are excluded from importable `Rows` and reported only as errors; Import is disabled while any error exists, so the service only receives valid rows.
- Tag names must not contain commas (no comma escaping).
- Sample-file export belongs to this module; full item export is separate.
- Single-snapshot single-save bulk import via `ItemCsvImportService`; `ItemManagementService.CreateItemAsync` is not modified.
