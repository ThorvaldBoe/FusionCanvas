## 1. Shared ports in Workspaces

- [ ] 1.1 Create `src/FusionCanvas.Application/Workspaces/`; split `ManagedWorkspaceFile.cs` out of `IWorkspaceFileStore.cs`; move `IWorkspaceRepository.cs`, `IWorkspaceFileStore.cs`, `ManagedWorkspaceFile.cs` into `Workspaces/`; update namespaces to `FusionCanvas.Application.Workspaces`
- [ ] 1.2 Update `using` directives across `Integration`, `App`, and test projects; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 2. Workspaces management

- [ ] 2.1 Split `WorkspaceManagement.cs` into 9 files in `Workspaces/`: `IWorkspaceManagementService.cs`, `WorkspaceContext.cs`, `WorkspaceManagementCreateRequest.cs`, `WorkspaceManagementUpdateRequest.cs`, `WorkspaceManagementDeleteRequest.cs`, `WorkspaceSummary.cs`, `WorkspaceManagementState.cs`, `WorkspaceManagementResult.cs`, `WorkspaceManagementService.cs`; update namespaces
- [ ] 2.2 Update `using` directives across consumers; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 3. Stores capability

- [ ] 3.1 Create `src/FusionCanvas.Application/Stores/`; split `StoreManagement.cs` into 9 files; update namespaces to `FusionCanvas.Application.Stores`
- [ ] 3.2 Update `using` directives across consumers; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 4. Niches capability

- [ ] 4.1 Create `src/FusionCanvas.Application/Niches/`; split `NicheManagement.cs` into 9 files; update namespaces to `FusionCanvas.Application.Niches`
- [ ] 4.2 Update `using` directives across consumers; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 5. Groups capability

- [ ] 5.1 Create `src/FusionCanvas.Application/Groups/`; split `GroupManagement.cs` into 16 files (leaving `WorkspaceTreeSelection` for step 15); update namespaces to `FusionCanvas.Application.Groups`
- [ ] 5.2 Update `using` directives across consumers; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 6. Items management

- [ ] 6.1 Create `src/FusionCanvas.Application/Items/`; split `ItemManagement.cs` into 17 files; update namespaces to `FusionCanvas.Application.Items`
- [ ] 6.2 Update `using` directives across consumers; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 7. Items inspector and id generator

- [ ] 7.1 Split `ItemInspector.cs` into 10 files in `Items/`; split `ItemIdGenerator.cs` into 3 files (`IItemIdGenerator.cs`, `GuidItemIdGenerator.cs`, `DelegateItemIdGenerator.cs`); keep `ItemMetadataCodec.cs` as one file in `Items/`; update namespaces
- [ ] 7.2 Update `using` directives across consumers; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 8. Tags capability

- [ ] 8.1 Create `src/FusionCanvas.Application/Tags/`; split `TagManagement.cs` into 12 files; update namespaces to `FusionCanvas.Application.Tags`
- [ ] 8.2 Update `using` directives across consumers; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 9. Assets capability

- [ ] 9.1 Create `src/FusionCanvas.Application/Assets/`; split `AssetManagement.cs` into 11 files; update namespaces to `FusionCanvas.Application.Assets`
- [ ] 9.2 Update `using` directives across consumers; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 10. DesignFiles capability

- [ ] 10.1 Create `src/FusionCanvas.Application/DesignFiles/`; split `DesignFileService.cs` into 5 files; update namespaces to `FusionCanvas.Application.DesignFiles`
- [ ] 10.2 Update `using` directives across consumers; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 11. Navigation capability

- [ ] 11.1 Create `src/FusionCanvas.Application/Navigation/`; split `NavigationWorkspaceService.cs` into 5 files; update namespaces to `FusionCanvas.Application.Navigation`
- [ ] 11.2 Update `using` directives across consumers; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 12. WorkflowNavigation capability

- [ ] 12.1 Create `src/FusionCanvas.Application/WorkflowNavigation/`; split `WorkflowStageNavigatorService.cs` into 5 files; update namespaces to `FusionCanvas.Application.WorkflowNavigation`
- [ ] 12.2 Update `using` directives across consumers; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 13. StageTools capability

- [ ] 13.1 Create `src/FusionCanvas.Application/StageTools/`; split `StageToolHost.cs` into 14 files; update namespaces to `FusionCanvas.Application.StageTools`
- [ ] 13.2 Update `using` directives across consumers; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 14. ToolContexts capability

- [ ] 14.1 Create `src/FusionCanvas.Application/ToolContexts/`; split `ToolContext.cs` into 13 files; keep `ToolContextResolver.cs` as the implementation file; update namespaces to `FusionCanvas.Application.ToolContexts`
- [ ] 14.2 Update `using` directives across consumers; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 15. WorkspaceTree capability

- [ ] 15.1 Create `src/FusionCanvas.Application/WorkspaceTree/`; split `WorkspaceTree.cs` into 9 files; pull `WorkspaceTreeSelection` out of `Groups/GroupManagement.cs` (or wherever it landed after step 5) into `WorkspaceTree/WorkspaceTreeSelection.cs`; update namespaces to `FusionCanvas.Application.WorkspaceTree`
- [ ] 15.2 Update `using` directives across consumers (including the Groups and Items services that reference `WorkspaceTreeSelection`); run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 16. Settings capability split

- [ ] 16.1 Split `IApplicationSettingsStore.cs` in `Settings/` into 3 files (`IApplicationSettingsStore.cs`, `ApplicationSettingsLoadResult.cs`, `ApplicationSettingsSaveResult.cs`); `ApplicationSettings.cs` stays; update namespaces (stay `FusionCanvas.Application.Settings`)
- [ ] 16.2 Update `using` directives across consumers; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 17. Remove empty Workspace folder

- [ ] 17.1 Confirm `src/FusionCanvas.Application/Workspace/` is empty and delete it; confirm `src/FusionCanvas.Application/` contains only capability folders plus `ApplicationAssemblyMarker.cs` and `FusionCanvas.Application.csproj`
- [ ] 17.2 Run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 18. Test mirror

- [ ] 18.1 Move the 16 test files under `tests/FusionCanvas.Application.Tests/` into matching capability subfolders (`Workspaces/`, `Stores/`, `Niches/`, `Groups/`, `Items/`, `Tags/`, `Assets/`, `DesignFiles/`, `Navigation/`, `WorkflowNavigation/`, `StageTools/`, `ToolContexts/`, `WorkspaceTree/`); update `using` directives
- [ ] 18.2 Run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm all 482 tests remain green

## 19. Final verification

- [ ] 19.1 Run `openspec validate reorganize-application-capability-folders` and confirm clean
- [ ] 19.2 Spot-check that no `src/FusionCanvas.Application/Workspace/` folder exists, no Application file contains more than one top-level type (excluding permitted §2 exceptions), and every file's namespace matches its folder path
- [ ] 19.3 Run the full baseline once more: `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`
