## 1. Persistence folder

- [ ] 1.1 Create `src/FusionCanvas.Integration/Persistence/`; move `SqliteWorkspaceRepository.cs` from `Workspace/` into it; update its file-scoped namespace to `FusionCanvas.Integration.Persistence`
- [ ] 1.2 Update `using` directives across `App` (composition root) and the test projects to add `using FusionCanvas.Integration.Persistence;` where `SqliteWorkspaceRepository` is referenced; remove the old `using FusionCanvas.Integration.Workspace;` where it is no longer needed
- [ ] 1.3 Run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 2. Files folder

- [ ] 2.1 Create `src/FusionCanvas.Integration/Files/`; move `LocalWorkspaceFileStore.cs` and `InMemoryWorkspaceFileStore.cs` from `Workspace/` into it; update their namespaces to `FusionCanvas.Integration.Files`
- [ ] 2.2 Update `using` directives across `App`, the test projects, and any test-support code that references either file store; remove the old `using FusionCanvas.Integration.Workspace;` where it is no longer needed
- [ ] 2.3 Run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 3. Remove empty Workspace folder

- [ ] 3.1 Confirm `src/FusionCanvas.Integration/Workspace/` is empty and delete it
- [ ] 3.2 Confirm `src/FusionCanvas.Integration/` contains only `Persistence/`, `Files/`, `Settings/`, `IntegrationAssemblyMarker.cs`, and `FusionCanvas.Integration.csproj`
- [ ] 3.3 Run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 4. Test mirror

- [ ] 4.1 Create subfolders under `tests/FusionCanvas.Integration.Tests/` matching the production folders: `Persistence/`, `Files/`, `Settings/`
- [ ] 4.2 Move `SqliteWorkspaceRepositoryTests.cs`, `ItemManagementPersistenceTests.cs`, `ItemInspectorPersistenceTests.cs`, and `AssetManagementPersistenceTests.cs` into `Persistence/`; move `LocalWorkspaceFileStoreTests.cs` into `Files/`; move `Settings/JsonApplicationSettingsStoreTests.cs` into `Settings/`; update `using` directives
- [ ] 4.3 Run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm all 482 tests remain green

## 5. Final verification

- [ ] 5.1 Run `openspec validate reorganize-integration-capability-folders` and confirm clean
- [ ] 5.2 Spot-check that no `src/FusionCanvas.Integration/Workspace/` folder exists, no Integration file contains more than one top-level type, and every file's namespace matches its folder path
- [ ] 5.3 Run the full baseline once more: `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`
