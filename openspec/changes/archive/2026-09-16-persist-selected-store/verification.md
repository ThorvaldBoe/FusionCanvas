# persist-selected-store Verification

## Acceptance Criteria

| Criterion | Result | Evidence |
| --- | --- | --- |
| Selected store survives restart | Pass | `JsonApplicationSettingsStoreTests.SaveAsync_PersistsPreferenceAndReloadsIt`; `StoreManagementServiceTests.LoadAsync_RestoresInitialSelectedActiveStore` |
| Missing or stale selected store falls back safely | Pass | `StoreManagementServiceTests.LoadAsync_IgnoresInitialSelectedStoreFromAnotherWorkspace`; existing active/archive validation in `StoreManagementService.BuildState` |
| Existing settings remain backward compatible | Pass | `JsonApplicationSettingsStoreTests.LoadAsync_WithoutSelectedStorePreferenceRemainsBackwardCompatible` |
| Selector persists selected store | Pass | `SettingsViewModelTests.UpdateActiveStore_QueuesSelectedStoreForSave`; `MainWindowViewModel` subscribes to successful active-store state changes |
| Saved store is no longer selectable | Pass | Service state validation and workspace-scoped active-store tests; full application test suite |
| Failed selection is not persisted | Pass | Existing rejected-selector service paths do not raise `ActiveStoreChanged`; settings update is attached only to that success event |

## Commands

- `dotnet build .\FusionCanvas.sln --no-restore -v quiet` — Pass, 0 errors.
- `dotnet test .\tests\FusionCanvas.Application.Tests\FusionCanvas.Application.Tests.csproj --no-restore --no-build --filter FullyQualifiedName~StoreManagementServiceTests.LoadAsync -v minimal` — Pass, 3/3.
- `openspec validate persist-selected-store` — Pass.
- `dotnet test .\FusionCanvas.sln -m:1 --no-restore -v minimal` — Pass: 248 Domain, 434 Application, 221 Integration, 645 App, 27 UiDescription; 1,575 total, 0 failed, 0 skipped.

## Limitations

- No live desktop restart was required; the startup and persistence path is covered deterministically through the settings, service, and application tests.
- The build retains pre-existing analyzer warnings; there are no new compilation errors.
