## Context

`SnowcloneLibraryService.InitializeAsync` currently auto-imports the bundled `starter-snowclones.csv` (the curated 31-record set) into a fresh library on first load, then sets the `starter_initialized` marker so it never happens again. `AppWorkspaceFactory.Create` invokes it at startup (`AppWorkspaceFactory.cs:65-66`), and `SnowcloneLibraryViewModel.OpenAsync` invokes it when the dialog opens (`SnowcloneLibraryViewModel.cs:274`). The user wants the library to start **empty** and only gain the curated defaults through the existing "Import bundled library" button (`SnowcloneLibraryWindow.axaml:43-47` → `ImportBundledCommand` → `ImportBundledAsync`).

## Goals / Non-Goals

**Goals:**
- A fresh Snowclone Library opens empty (no auto-populated defaults) for every data store.
- The curated 31-record bundled CSV remains shipped and reachable only via the explicit "Import bundled library" action, which adds unique records and never overwrites local guidance.
- Keep the schema and public contracts stable; minimal churn.

**Non-Goals:**
- No new UI button or dialog (the "Import bundled library" button already exists).
- No schema or migration change.
- No change to `ImportBundledAsync`, the CSV codec, the bundled resource, or the management dialog behaviors.
- No "reset defaults" or per-store upgrade overlay.

## Decisions

**D1 — `InitializeAsync` becomes a plain load (no bundled import).**
`SnowcloneLibraryService.InitializeAsync` stops reading the bundled source and stops consulting/setting the starter marker. It is reduced to the same behavior as `LoadAsync`: load the snapshot and build the search-projected state. The method and `ISnowcloneLibraryService` signature stay so existing callers (`AppWorkspaceFactory`, `SnowcloneLibraryViewModel.OpenAsync`) work unchanged; they now simply get the loaded (empty) library.

**D2 — The `starter_initialized` marker becomes inert but is retained.**
The `snowclone_library_state` table/column stays in the schema (v9) to avoid a migration, but no code path auto-imports or gates on it. `ImportBundledAsync` already passes `markStarterInitialized: false`, so the marker simply stays false/unused. `SnowcloneLibraryState.StarterLibraryInitialized` remains in the API but no longer affects population.

**D3 — Update the auto-import service tests to the new behavior.**
Replace the three auto-initialization tests with tests that assert: (a) `InitializeAsync` on a fresh library yields empty and imports nothing (no bundled read, no save); (b) `ImportBundledAsync` still adds the full curated set uniquely and preserves existing guidance. The invalid-bundle behavior still holds for the explicit import.

**D4 — `ImportBundledAsync` is the only way bundled defaults enter.**
The explicit import path is unchanged and remains the verified mechanism for adding the 31 curated defaults, including its "invalid bundle" and "skip duplicates / no overwrite" guarantees.

## Risks / Trade-offs

- **[A fresh library is now empty, so some users may not discover the bundled set]** → The "Import bundled library" action and its ImportCSV sibling remain visible in the dialog toolbar; discovery is a copy/UX concern, already present.
- **[Marker left in schema but unused]** → Intentional compatibility choice; documented so a future cleanup can drop it in a deliberate migration.
- **[Ideation Snowclones mode depends on a populated library]** → Library emptiness is already modeled as a blocked/empty state for Snowclones mode; unchanged behavior.
- **[Test churn on removed auto-import]** → The removed tests are replaced, not merely deleted, so coverage of both empty-default and explicit-import behavior remains.

## Implementation Plan

Layers affected: **Application** (service) and **Application tests**.

1. **Edit `src/FusionCanvas.Application/Snowclones/SnowcloneLibraryService.cs`** `InitializeAsync`:
   - Remove the bundled-source read and `ImportCoreAsync` call block (lines 43-62 in the current body) and the `StarterLibraryInitialized` short-circuit (lines 38-41).
   - Leave the method returning the loaded, search-projected state (`BuildState`), i.e. the same body as `LoadAsync`. If preferred, delegate: `return await LoadAsync(searchText, cancellationToken);`.
   - Do NOT modify `ISnowcloneLibraryService`, `ImportBundledAsync`, `ImportCoreAsync`, `LoadAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `ExportAsync`, or the repository.
   - The `_bundledSource` dependency remains (still used by `ImportBundledAsync`); keep the constructor.
2. **Update `tests/FusionCanvas.Application.Tests/Snowclones/SnowcloneLibraryServiceTests.cs`**:
   - Replace `InitializeAsync_ImportsOnceAndPersistsMarker` with `InitializeAsync_LeavesLibraryEmptyAndImportsNothing` (asserts empty, no bundled read, no save, marker stays false).
   - Remove `InitializeAsync_AfterStarterDeletionDoesNotResurrectIt` (no auto-import to resurrect) and `InitializeAsync_InvalidBundleDoesNotSaveOrSetMarker` (invalid-bundle gating now applies to explicit import only).
   - Keep and rely on `ImportBundledAsync_AddsUniqueAndPreservesExistingGuidance` (full curated set merges uniquely) and add/keep an invalid-bundle assertion under the explicit-import path if not already present.
3. **App/Integration** — no code changes. Confirm `AppWorkspaceFactory` and `SnowcloneLibraryViewModel` still compile (they call `InitializeAsync`, unchanged signature/semantics of "load").
4. **Regression** — `dotnet test .\FusionCanvas.sln`.
5. **Validation** — `openspec validate snowclone-library-empty-by-default --strict` plus the repository-required scope.

### Acceptance-to-verification mapping

| Scenario | Planned verification method |
|---|---|
| Snowclone library is empty by default on first load (no auto-import) | New `SnowcloneLibraryServiceTests.InitializeAsync_LeavesLibraryEmptyAndImportsNothing` (empty state, zero bundled reads, zero saves) |
| Creator imports the bundled library explicitly (full curated set, unique, no overwrite, counts) | Existing `SnowcloneLibraryServiceTests.ImportBundledAsync_AddsUniqueAndPreservesExistingGuidance`; `SnowcloneCsvCodecTests.EmbeddedStarterResource_UsesTheNormalCsvContract` (resource = 31 rows) |
| Bundled starter data is invalid (no partial import, recoverable error) | Existing `SnowcloneLibraryServiceTests.ImportBundledAsync_...` invalid/read-failure coverage plus `SnowcloneCsvCodecTests` rejection cases |

### Decisions not to reopen

- The library starts empty by default; bundled defaults are opt-in via the existing "Import bundled library" action.
- The `starter_initialized` marker/schema is retained but inert (no migration now).
- `InitializeAsync` keeps its name/signature and simply loads.
