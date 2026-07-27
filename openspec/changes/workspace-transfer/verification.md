# Workspace Transfer Verification

## Acceptance evidence

| Acceptance scenario | Verification method | Result | Evidence and limitations |
| --- | --- | --- | --- |
| User exports an active workspace | Integration round trip and application export orchestration tests | Passed | `WorkspacePackageIntegrationTests.ExportThenImport_RoundTripsSnapshotAndManagedFileBytes`; `WorkspaceTransferServiceTests.ExportWorkspaceAsync_WritesFilteredPackageAndReturnsSummary`. |
| Export includes archived content | Domain filter and integration round-trip tests | Passed | `WorkspaceTransferPolicyTests.ForWorkspace_IncludesCompleteOwnedSubgraphAndArchivedEntities`; round trip preserves archived asset state. |
| User exports an archived workspace | Domain filter plus view-model archived selection/export test | Passed | `WorkspaceTransferPolicyTests.ForWorkspace_IncludesCompleteOwnedSubgraphAndArchivedEntities`; `WorkspaceTransferViewModelTests.ArchivedWorkspace_CanBeReviewedAndExportedWithoutRestoring`. |
| Export target already exists | Integration replacement and cancellation tests | Passed | `WorkspacePackageIntegrationTests.ExportSuccess_ReplacesExistingDestinationOnlyWithCompletePackage`; `ExportCancellation_PreservesExistingDestination`. Platform overwrite confirmation itself is delegated to Avalonia's save picker. |
| Asset file is missing during export | Integration export/import test | Passed | `WorkspacePackageIntegrationTests.Export_MissingFileIsRecordedAndDoesNotFail` verifies manifest/summary reporting and missing state after import. |
| Importer pre-flights a package | Integration version-refusal tests | Passed | `WorkspacePackageIntegrationTests.Reader_RefusesNewerFormatOrSchemaVersion`; reader tests manifest before extracting the database or touching live state. |
| Package is imported into an installation without that workspace | Real ZIP/SQLite/file-store round trip | Passed | `WorkspacePackageIntegrationTests.ExportThenImport_RoundTripsSnapshotAndManagedFileBytes`. |
| Package is imported into an empty installation | Integration round trip plus view-model refresh test | Passed | `WorkspacePackageIntegrationTests.ExportThenImport_RoundTripsSnapshotAndManagedFileBytes`; `WorkspaceTransferViewModelTests.ImportSuccess_ReloadsAndSelectsRestoredWorkspace`. |
| Archived workspace package is imported | Application orchestration test | Passed | `WorkspaceTransferServiceTests.ImportWorkspaceAsync_ActivatesArchivedWorkspaceAndSuffixesActiveNameConflict`; domain filter test covers descendant archive preservation. |
| Same package is imported twice | Application preflight test | Passed | `WorkspaceTransferServiceTests.ImportWorkspaceAsync_RefusesIdentityCollisionBeforeOpeningFiles`. |
| Imported name conflicts with an active workspace | Application orchestration test | Passed | `WorkspaceTransferServiceTests.ImportWorkspaceAsync_ActivatesArchivedWorkspaceAndSuffixesActiveNameConflict`. |
| Imported name matches only an archived workspace | Application orchestration test | Passed | `WorkspaceTransferServiceTests.ImportWorkspaceAsync_ArchivedOnlyNameConflictKeepsOriginalName`. |
| Restore over files orphaned by a deleted workspace | Integration skip-if-exists test | Passed | `WorkspacePackageIntegrationTests.Import_ExistingManagedFileIsKeptAndCounted`. |
| Package requires a newer application | Integration format and schema preflight test | Passed | `WorkspacePackageIntegrationTests.Reader_RefusesNewerFormatOrSchemaVersion`. |
| Older package migrates on import | Integration embedded-database migration test | Passed | `WorkspacePackageIntegrationTests.Reader_MigratesOlderEmbeddedDatabase` downgrades `PRAGMA user_version` and the manifest before import. |
| Package is corrupt or not a workspace package | Integration corrupt-input test | Passed | `WorkspacePackageIntegrationTests.Reader_RefusesTraversalEntryAndCorruptPackage`. |
| Package contains a traversal entry | Integration hostile-archive test | Passed | `WorkspacePackageIntegrationTests.Reader_RefusesTraversalEntryAndCorruptPackage`; no extraction occurs before full entry validation. |
| Package contains an unsupported file type | Integration allowlist test | Passed | `WorkspacePackageIntegrationTests.Import_UnsupportedFileIsSkippedAndAssetMarkedMissing`. |
| User cancels an import midway | Application cleanup test | Passed | `WorkspaceTransferServiceTests.ImportWorkspaceAsync_CancellationCleansFilesAndDoesNotSave`. |
| User cancels an export midway | Integration temp-package test | Passed | `WorkspacePackageIntegrationTests.ExportCancellation_PreservesExistingDestination`. |
| Transfer is in progress | View-model and headless view tests | Passed | `WorkspaceTransferViewModelTests.TransferBusyState_DisablesMutationsAndCancelStopsOperation`; `WorkspaceTransferViewTests.TransferInProgress_DisablesActionsAndOffersCancel`. |
| Persistence fails after files were copied | Application cleanup test | Passed | `WorkspaceTransferServiceTests.ImportWorkspaceAsync_SaveFailureCleansNewFilesAndLeavesRepositoryUnchanged`. |
| Import completes | Application summary and selection tests | Passed | `WorkspaceTransferViewModelTests.ExportSuccess_SurfacesCompletionSummary`; `ImportSuccess_ReloadsAndSelectsRestoredWorkspace`; integration round trip covers imported counts/files. |
| User exports from workspace management | Headless dialog test | Passed | `WorkspaceTransferViewTests.WorkspaceDialog_ExposesImportAndSelectedWorkspaceExport`. |
| User imports with no workspaces present | Headless overlay and refresh tests | Passed | `WorkspaceTransferViewTests.MainWindow_NoWorkspaceOverlayShowsImportOnlyForNoWorkspaceState`; `WorkspaceTransferViewModelTests.ImportSuccess_ReloadsAndSelectsRestoredWorkspace`. |
| Import from the no-workspace state fails | View-model routing test | Passed | `WorkspaceTransferViewModelTests.ImportFailure_OpensManagementSurfaceAndKeepsError`. |
| Main window stays free of persistent transfer controls | Headless visibility test | Passed | `WorkspaceTransferViewTests.MainWindow_NoWorkspaceOverlayShowsImportOnlyForNoWorkspaceState`. |
| Contributor plans a schema-affecting change | Documentation review | Passed | `docs/data-model.md`, “Workspace package and database compatibility”. No runtime test applies to a contributor-policy statement. |

## Modified capability review

The `local-sqlite-persistence` delta was reviewed against the implementation: package behavior is owned by the workspace-transfer application/integration paths, while `SqliteWorkspaceRepository` remains the shared schema/migration adapter. Existing persistence tests remain part of the full baseline gate.

## Validation runs

| Gate | Result | Evidence |
| --- | --- | --- |
| Domain transfer policies | Passed | `dotnet test tests/FusionCanvas.Domain.Tests/FusionCanvas.Domain.Tests.csproj`: 99 tests passed. |
| Application transfer service and restore contract | Passed | Focused runs: 3 restore-contract tests and 7 transfer-service tests passed. |
| Integration package/file-store boundaries | Passed | Focused runs: 13 local-file-store tests and 9 package integration tests passed. |
| Transfer view-model and headless views | Passed | Focused run: 8 transfer tests passed. |
| Strict OpenSpec validation | Passed | `openspec validate workspace-transfer --strict`: change is valid. |
| Full deterministic baseline | Passed | `dotnet test .\FusionCanvas.sln -m:1 -v minimal`: 513 tests passed (Domain 99, Application 159, Integration 61, App 194). |

## Limitations

- Native platform file-picker overwrite confirmation is delegated to Avalonia and the operating system; deterministic tests verify suggested extension/command wiring and atomic replacement behavior behind the picker.
- No live desktop check is required. Headless Avalonia tests cover the material binding, visibility, enablement, and routing risks.
