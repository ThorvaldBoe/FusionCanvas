## 1. Shared Workspace root

- [ ] 1.1 Split `WorkspaceEntityKind` out of `Workspace/WorkspaceRelationships.cs` into `Workspace/WorkspaceEntityKind.cs` (namespace stays `FusionCanvas.Domain.Workspace`); remove the now-empty `WorkspaceRelationships.cs` once all its types are relocated in later steps
- [ ] 1.2 Confirm `Workspace.cs`, `WorkspaceDefaults.cs`, `WorkspaceEntity.cs`, `WorkspaceSnapshot.cs` remain in `Workspace/` with the existing namespace; no moves needed
- [ ] 1.3 Run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green before proceeding

## 2. Stores capability

- [ ] 2.1 Create `src/FusionCanvas.Domain/Stores/` and move `Store.cs` into it; update its file-scoped namespace to `FusionCanvas.Domain.Stores`
- [ ] 2.2 Update `using` directives across `Application`, `Integration`, `App`, and test projects to add `using FusionCanvas.Domain.Stores;` where `Store` is referenced; remove redundant `using FusionCanvas.Domain.Workspace;` where it is no longer needed
- [ ] 2.3 Run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 3. Niches capability

- [ ] 3.1 Create `src/FusionCanvas.Domain/Niches/` and move `Niche.cs`; update namespace to `FusionCanvas.Domain.Niches`
- [ ] 3.2 Update `using` directives across consumers; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 4. Groups capability

- [ ] 4.1 Create `src/FusionCanvas.Domain/Groups/` and move `TopicGroup.cs` and `GroupHierarchy.cs`; update namespaces to `FusionCanvas.Domain.Groups`
- [ ] 4.2 Update `using` directives across consumers; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 5. Items capability

- [ ] 5.1 Create `src/FusionCanvas.Domain/Items/` and move `Item.cs`, `ItemHierarchy.cs`, `ItemDisplayNameFormatter.cs`; update namespaces to `FusionCanvas.Domain.Items`
- [ ] 5.2 Split `ItemWorkflowPolicy.cs` into four files in `Items/`: `ItemOperationKind.cs`, `ItemEditDecision.cs`, `ItemStatusTransitionDecision.cs`, `ItemWorkflowPolicy.cs`; update namespaces to `FusionCanvas.Domain.Items`
- [ ] 5.3 Split `ItemStatus` and `ItemStatuses` out of `Workspace/WorkspaceRelationships.cs` into `Items/ItemStatus.cs` and `Items/ItemStatuses.cs`; update namespaces to `FusionCanvas.Domain.Items`
- [ ] 5.4 Update `using` directives across consumers; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 6. Tags capability

- [ ] 6.1 Create `src/FusionCanvas.Domain/Tags/` and move `Tag.cs`; update namespace to `FusionCanvas.Domain.Tags`
- [ ] 6.2 Split `ItemTag` out of `Workspace/WorkspaceRelationships.cs` into `Tags/ItemTag.cs`; update namespace to `FusionCanvas.Domain.Tags`
- [ ] 6.3 Update `using` directives across consumers; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 7. Assets capability

- [ ] 7.1 Create `src/FusionCanvas.Domain/Assets/` and move `Asset.cs` and `WorkspaceFileReference.cs`; update namespaces to `FusionCanvas.Domain.Assets`
- [ ] 7.2 Split `AssetKind` and `AssetLink` out of `Workspace/WorkspaceRelationships.cs` into `Assets/AssetKind.cs` and `Assets/AssetLink.cs`; update namespaces to `FusionCanvas.Domain.Assets`
- [ ] 7.3 Verify `Workspace/WorkspaceRelationships.cs` is now empty and delete it
- [ ] 7.4 Update `using` directives across consumers; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 8. Prompts capability

- [ ] 8.1 Create `src/FusionCanvas.Domain/Prompts/` and move `Prompt.cs`; update namespace to `FusionCanvas.Domain.Prompts`
- [ ] 8.2 Update `using` directives across consumers; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 9. Workflow capability

- [ ] 9.1 Create `src/FusionCanvas.Domain/Workflow/` and split `WorkflowStage.cs` into `Workflow/WorkflowStage.cs` and `Workflow/WorkflowStages.cs`; update namespaces to `FusionCanvas.Domain.Workflow`
- [ ] 9.2 Update `using` directives across consumers; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 10. Navigation capability

- [ ] 10.1 Create `src/FusionCanvas.Domain/Navigation/` and split `NavigationTree.cs` into five files: `NavigationNodeRole.cs`, `NavigationNode.cs`, `NavigationTopicReference.cs`, `NavigationTreeSnapshot.cs`, `WorkspaceNavigation.cs`; update namespaces to `FusionCanvas.Domain.Navigation`
- [ ] 10.2 Delete the now-empty `Workspace/NavigationTree.cs`
- [ ] 10.3 Update `using` directives across consumers; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm green

## 11. Test mirror

- [ ] 11.1 Move `tests/FusionCanvas.Domain.Tests/ItemWorkflowPolicyTests.cs`, `ItemLifecycleStatusTests.cs`, `ItemHierarchyTests.cs` into a `Tests/Items/` subfolder (or the matching capability folder) and update `using` directives
- [ ] 11.2 Move `NavigationTreeTests.cs` into `Tests/Navigation/`; move `WorkflowStageTests.cs` into `Tests/Workflow/`; move `WorkspaceFileStorageModelTests.cs` into `Tests/Assets/`; move `DomainPersistenceBoundaryTests.cs` into `Tests/Workspace/`; update `using` directives
- [ ] 11.3 Run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; confirm all 482 tests remain green

## 12. Final verification

- [ ] 12.1 Run `openspec validate` and confirm 25/25 (or current count) clean with no spec drift
- [ ] 12.2 Spot-check that no `src/FusionCanvas.Domain/Workspace/` file contains more than the agreed shared-root types (`Workspace`, `WorkspaceDefaults`, `WorkspaceEntity`, `WorkspaceEntityKind`, `WorkspaceSnapshot`); confirm no Domain file contains more than one top-level type
- [ ] 12.3 Confirm `WorkspaceRelationships.cs` and the old `NavigationTree.cs` and `WorkflowStage.cs` (pre-split) no longer exist
- [ ] 12.4 Run the full baseline once more: `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`
