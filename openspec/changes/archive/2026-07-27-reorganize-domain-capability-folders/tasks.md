## 1. Shared Workspace root

- [x] 1.1 Split `WorkspaceEntityKind` out of `Workspace/WorkspaceRelationships.cs` into `Workspace/WorkspaceEntityKind.cs` (namespace stays `FusionCanvas.Domain.Workspace`); remove the now-empty `WorkspaceRelationships.cs` once all its types are relocated in later steps
- [x] 1.2 Confirm `Workspace.cs`, `WorkspaceDefaults.cs`, `WorkspaceEntity.cs`, `WorkspaceSnapshot.cs` remain in `Workspace/` with the existing namespace; no moves needed
- [x] 1.3 Run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green before proceeding

## 2. Stores capability

- [x] 2.1 Create `src/FusionCanvas.Domain/Stores/` and move `Store.cs` into it; update its file-scoped namespace to `FusionCanvas.Domain.Stores`
- [x] 2.2 Update `using` directives across `Application`, `Integration`, `App`, and test projects to add `using FusionCanvas.Domain.Stores;` where `Store` is referenced; remove redundant `using FusionCanvas.Domain.Workspace;` where it is no longer needed
- [x] 2.3 Run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 3. Niches capability

- [x] 3.1 Create `src/FusionCanvas.Domain/Niches/` and move `Niche.cs`; update namespace to `FusionCanvas.Domain.Niches`
- [x] 3.2 Update `using` directives across consumers; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 4. Groups capability

- [x] 4.1 Create `src/FusionCanvas.Domain/Groups/` and move `TopicGroup.cs` and `GroupHierarchy.cs`; update namespaces to `FusionCanvas.Domain.Groups`
- [x] 4.2 Update `using` directives across consumers; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 5. Items capability

- [x] 5.1 Create `src/FusionCanvas.Domain/Items/` and move `Item.cs`, `ItemHierarchy.cs`, `ItemDisplayNameFormatter.cs`; update namespaces to `FusionCanvas.Domain.Items`
- [x] 5.2 Split `ItemWorkflowPolicy.cs` into four files in `Items/`: `ItemOperationKind.cs`, `ItemEditDecision.cs`, `ItemStatusTransitionDecision.cs`, `ItemWorkflowPolicy.cs`; update namespaces to `FusionCanvas.Domain.Items`
- [x] 5.3 Split `ItemStatus` and `ItemStatuses` out of `Workspace/WorkspaceRelationships.cs` into `Items/ItemStatus.cs` and `Items/ItemStatuses.cs`; update namespaces to `FusionCanvas.Domain.Items`
- [x] 5.4 Update `using` directives across consumers; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 6. Tags capability

- [x] 6.1 Create `src/FusionCanvas.Domain/Tags/` and move `Tag.cs`; update namespace to `FusionCanvas.Domain.Tags`
- [x] 6.2 Split `ItemTag` out of `Workspace/WorkspaceRelationships.cs` into `Tags/ItemTag.cs`; update namespace to `FusionCanvas.Domain.Tags`
- [x] 6.3 Update `using` directives across consumers; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 7. Assets capability

- [x] 7.1 Create `src/FusionCanvas.Domain/Assets/` and move `Asset.cs` and `WorkspaceFileReference.cs`; update namespaces to `FusionCanvas.Domain.Assets`
- [x] 7.2 Split `AssetKind` and `AssetLink` out of `Workspace/WorkspaceRelationships.cs` into `Assets/AssetKind.cs` and `Assets/AssetLink.cs`; update namespaces to `FusionCanvas.Domain.Assets`
- [x] 7.3 Verify `Workspace/WorkspaceRelationships.cs` is now empty and delete it
- [x] 7.4 Update `using` directives across consumers; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 8. Prompts capability

- [x] 8.1 Create `src/FusionCanvas.Domain/Prompts/` and move `Prompt.cs`; update namespace to `FusionCanvas.Domain.Prompts`
- [x] 8.2 Update `using` directives across consumers; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 9. Workflow capability

- [x] 9.1 Create `src/FusionCanvas.Domain/Workflow/` and split `WorkflowStage.cs` into `Workflow/WorkflowStage.cs` and `Workflow/WorkflowStages.cs`; update namespaces to `FusionCanvas.Domain.Workflow`
- [x] 9.2 Update `using` directives across consumers; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 10. Navigation capability

- [x] 10.1 Create `src/FusionCanvas.Domain/Navigation/` and split `NavigationTree.cs` into five files: `NavigationNodeRole.cs`, `NavigationNode.cs`, `NavigationTopicReference.cs`, `NavigationTreeSnapshot.cs`, `WorkspaceNavigation.cs`; update namespaces to `FusionCanvas.Domain.Navigation`
- [x] 10.2 Delete the now-empty `Workspace/NavigationTree.cs`
- [x] 10.3 Update `using` directives across consumers; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 11. Test mirror

- [x] 11.1 Move `tests/FusionCanvas.Domain.Tests/ItemWorkflowPolicyTests.cs`, `ItemLifecycleStatusTests.cs`, `ItemHierarchyTests.cs` into `Items/` and update `using` directives
- [x] 11.2 Move `NavigationTreeTests.cs` into `Navigation/`; move `WorkflowStageTests.cs` into `Workflow/`; move `WorkspaceFileStorageModelTests.cs` into `Assets/`; move `DomainPersistenceBoundaryTests.cs` into `Workspace/`; update `using` directives
- [x] 11.3 Run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm all 482 tests remain green

## 12. Final verification

- [x] 12.1 Run `openspec validate` and confirm 25/25 (or current count) clean with no spec drift
- [x] 12.2 Spot-check that no `src/FusionCanvas.Domain/Workspace/` file contains more than the agreed shared-root types (`Workspace`, `WorkspaceDefaults`, `WorkspaceEntity`, `WorkspaceEntityKind`, `WorkspaceSnapshot`); confirm no Domain file contains more than one top-level type
- [x] 12.3 Confirm `WorkspaceRelationships.cs` and the old `NavigationTree.cs` and `WorkflowStage.cs` (pre-split) no longer exist
- [x] 12.4 Run the full baseline once more: `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`
