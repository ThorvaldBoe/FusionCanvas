# Tasks: workspace-transfer

## 1. Domain: subgraph filter and import preflight policy

- [x] 1.1 Add `WorkspaceSnapshotFilter.ForWorkspace(WorkspaceSnapshot, Guid)` in a new `FusionCanvas.Domain` Workspace/Transfer area: include the workspace, stores where `Store.WorkspaceId` matches, and all descendant niches, groups, items, assets, prompts, tags, item-tags, and asset-links (including archived entities); include an `AssetLink` only when both its asset and its target entity are included; return the filtered snapshot plus a dropped-links report
- [x] 1.2 Add `WorkspaceImportPreflight` pure policy: `FindIdentityCollisions(live, package)` across every entity list, and `ResolveImportName(packageName, activeWorkspaceNames)` producing the original name or an automatically suffixed unique name (archived names ignored)
- [x] 1.3 Domain tests in `tests/FusionCanvas.Domain.Tests`: filter completeness (mirror of the delete-cascade ownership set), cross-workspace link dropping, collision detection per entity type, name suffix sequencing and archived-name exemption

## 2. Application: transfer contracts and file-store restore capability

- [x] 2.1 Add contracts/DTOs in `FusionCanvas.Application` Workspaces/Transfer: `IWorkspaceTransferService`, `WorkspaceExportRequest`, `WorkspaceImportRequest`, `WorkspaceTransferResult`/`WorkspaceTransferSummary` (counts, warnings, final name), `WorkspaceTransferProgress` (phase, completed, total), `WorkspacePackageManifest` (formatVersion, schemaVersion, appVersion, workspaceId/name, exportedAtUtc, entityCounts, files, missingFiles, droppedLinkCount)
- [x] 2.2 Add package ports `IWorkspacePackageWriter` and `IWorkspacePackageReader` defining write (filtered snapshot + files + manifest → destination) and read (package → manifest, snapshot, validated restorable file entries) operations with progress and cancellation
- [x] 2.3 Extend `IWorkspaceFileStore` additively with `RestoreAsync(workspaceRelativePath, Stream, CancellationToken)` returning Created/SkippedExisting with the existing path-traversal validation; update `InMemoryWorkspaceFileStore` accordingly
- [x] 2.4 Application tests: extend `WorkspaceFileStoreContractTests` for restore semantics (created, skipped-existing, traversal rejection); fake `IWorkspacePackageReader/Writer` doubles for later service tests

## 3. Integration: ZIP package writer/reader and file-store restore

- [x] 3.1 Implement `LocalWorkspaceFileStore.RestoreAsync` with root-boundary guard and write-only-if-absent behavior; integration tests in `tests/FusionCanvas.Integration.Tests`
- [x] 3.2 Implement `ZipWorkspacePackageWriter` (`System.IO.Compression`): write the filtered snapshot to a temp path via `SqliteWorkspaceRepository` (schema auto-created at current version), stream each referenced file from `IWorkspaceFileStore` into `files/…` entries, emit `manifest.json`, write to a temp package file and move onto the destination only when complete; honor progress and cancellation with temp cleanup
- [x] 3.3 Implement `ZipWorkspacePackageReader`: validate archive shape and manifest (formatVersion/schemaVersion pre-flight with friendly refusal for newer), validate every entry path against traversal before extraction, enforce the existing creative-asset extension allowlist per file (skip + warn), open the embedded DB through `SqliteWorkspaceRepository` so older schemas migrate, map corrupt-package and refuse-newer errors to recoverable results, stream all content
- [x] 3.4 Integration round-trip tests: build a snapshot with real files in an isolated temp root → export → import into an empty repository and root → assert structural equality (identities, relationships, metadata, archive state, workspace-relative paths, file bytes); cover missing-file export, skip-if-exists restore, traversal refusal, allowlist skip, newer-version refusal, older-schema migration (downgrade the embedded DB via direct SQL in the test), corrupt package, and cancellation mid-copy

## 4. Application: transfer service orchestration

- [x] 4.1 Implement `WorkspaceTransferService.ExportWorkspaceAsync`: load snapshot → filter via `WorkspaceSnapshotFilter` → drive `IWorkspacePackageWriter` → return summary with entity/file counts, missing files, dropped-link count
- [x] 4.2 Implement `WorkspaceTransferService.ImportWorkspaceAsync`: pre-flight via reader → `FindIdentityCollisions` refusal before any file copy → `ResolveImportName` → restore files (skip-if-exists, progress, cancel) → merge package snapshot into the live snapshot with the final workspace name → single `SaveAsync` → best-effort cleanup of newly copied files on save failure or cancellation → return summary
- [x] 4.3 Application tests with fakes: duplicate-identity refusal leaves state untouched, name suffixing, skip-if-exists counting, save-failure cleanup, cancellation cleanup, summary contents, progress reporting

## 5. App: dialog commands, overlay action, and pickers

- [x] 5.1 Add `IWorkspacePackagePicker` abstraction (save dialog with `.fcworkspace` filter and `<workspace>-<yyyyMMdd>` default name; open dialog) plus the Avalonia `StorageProvider` implementation, following the `IAssetFilePicker` pattern
- [x] 5.2 Extend `WorkspaceManagementViewModel`: `ExportSelectedWorkspaceCommand`, `ImportWorkspaceCommand`, `CancelTransferCommand`, `IsTransferRunning`, progress and summary/error surface; disable transfer and workspace-mutation commands while a transfer runs; import failure from any entry point sets the error and opens the management window
- [x] 5.3 Update `WorkspaceManagementWindow.axaml`: Export button in the selected-workspace action row, Import button in the list rail, compact progress bar with cancel, and a completion summary area; fixed-width buttons and keyboard reachability per UI guidelines
- [x] 5.4 Add one secondary "Import workspace…" button to the no-workspace overlay in `MainWindow.axaml` bound to the shared `ImportWorkspaceCommand`
- [x] 5.5 Wire post-import refresh in `MainWindowViewModel`: reload the snapshot and select the restored workspace so the overlay dismisses and navigation reflects the import
- [x] 5.6 View-model tests (fake picker + service): command enablement, busy/progress/cancel state, summary and error surfacing, overlay-vs-dialog entry behavior
- [x] 5.7 Headless view tests in `tests/FusionCanvas.App.Tests`: dialog exposes export for the selected workspace and an import action, transfer-in-progress disables conflicting actions and offers cancel, the overlay shows the import action only in the no-workspace state and routes failure to the management surface

## 6. Docs, verification, and baseline

- [x] 6.1 Add the "Workspace package and database compatibility" section to `docs/data-model.md`: every shipped schema version remains migratable by future versions for both local databases and packages; any deliberate break is stated explicitly and ships with a migration path
- [x] 6.2 Create `verification.md` mapping every acceptance scenario in `specs/workspace-transfer/spec.md` to its concrete test evidence (domain/application/integration/headless view) with results; record the `local-sqlite-persistence` delta as review-verified
- [x] 6.3 Run `openspec validate workspace-transfer --strict` (or the repo-standard equivalent) and fix all findings
- [x] 6.4 Run the full baseline `dotnet test .\FusionCanvas.sln` and confirm green, including the pre-existing persistence and workspace-management suites
