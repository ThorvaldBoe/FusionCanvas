## 1. Application behavior

- [x] 1.1 Edit `src/FusionCanvas.Application/Snowclones/SnowcloneLibraryService.cs` `InitializeAsync` to load the library only: remove the `StarterLibraryInitialized` short-circuit and the bundled-source `ImportCoreAsync` block, returning the loaded, search-projected state (delegating to `LoadAsync` is fine). Do not change the signature, `ImportBundledAsync`, other CRUD methods, or the repository.
- [x] 1.2 Confirm `AppWorkspaceFactory` and `SnowcloneLibraryViewModel` compile unchanged against the modified `InitializeAsync` (both call it and expect a loaded state).

## 2. Test updates

- [x] 2.1 Replace `SnowcloneLibraryServiceTests.InitializeAsync_ImportsOnceAndPersistsMarker` with `InitializeAsync_LeavesLibraryEmptyAndImportsNothing` (asserts empty state, zero bundled reads, zero saves, marker stays false).
- [x] 2.2 Remove `InitializeAsync_AfterStarterDeletionDoesNotResurrectIt` and `InitializeAsync_InvalidBundleDoesNotSaveOrSetMarker`; ensure invalid-bundle and duplicate-skip/no-overwrite behavior remains covered under the explicit `ImportBundledAsync` path.

## 3. Verification and validation

- [x] 3.1 Run `dotnet test .\FusionCanvas.sln` and confirm the baseline passes with the updated tests.
- [x] 3.2 Run `openspec validate snowclone-library-empty-by-default --strict` and the repository-required validation scope; correct errors and rerun.
- [x] 3.3 Create `verification.md` mapping every acceptance scenario to its exact test result and evidence, and record the dependency/coordination note that this change archives only after `snowclone-library` and `default-snowclones` are synchronized/archived.
