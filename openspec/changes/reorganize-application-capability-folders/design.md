## Context

The `FusionCanvas.Application` project currently places 19 of its 20 workspace-related files in a single catch-all `Workspace/` folder (the 20th, `Settings/`, is already correctly organized). Most of those files bundle a service's interface, its request/result/state/summary records, and its implementation into one file — `ItemManagement.cs` holds 14 top-level types plus the 805-line service, `GroupManagement.cs` holds 16 types plus an ~880-line service, `StageToolHost.cs` holds 13+ types, `ToolContext.cs` holds 11. The C# coding standard (`docs/coding-standard.md` §1) forbids the catch-all folder and §2 requires one primary type per file (banning `Contracts.cs`-style grouping at `coding-standard.md:114`).

This change is the second of three planned maintenance refactors. It depends on `reorganize-domain-capability-folders` landing first, because the `using` directives updated here reference the new Domain namespaces. It is itself a prerequisite for `reorganize-integration-capability-folders` (the third change), which references the new Application namespaces.

The Application layer is where the structural debt is most expensive: the `*Management.cs` bundles hide which records belong to which use case, force reviewers to scroll past hundreds of lines of service body to find a request type, and make it easy for new work to keep appending to the wrong file. Splitting them pays off immediately.

## Goals / Non-Goals

**Goals:**
- Split the Application `Workspace/` catch-all folder into cohesive capability folders that mirror the Domain layer's split and the existing OpenSpec capability names: `Workspaces/`, `Stores/`, `Niches/`, `Groups/`, `Items/`, `Tags/`, `Assets/`, `DesignFiles/`, `Navigation/`, `WorkflowNavigation/`, `StageTools/`, `ToolContexts/`, `WorkspaceTree/`, plus the existing `Settings/`.
- Split every multi-type file into one-type-per-file per §2: the interface, each request/result/state/summary/options record, and the implementation each get their own file.
- Update each moved file's namespace to match its folder path per §1.
- Mirror the new folder structure in `tests/FusionCanvas.Application.Tests/` per §14.
- Keep `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln` green after each bounded step.

**Non-Goals:**
- No service-size refactors — `ItemManagementService` stays one class even though it is 805 lines; splitting its responsibilities is a separate behavior-adjacent change. This change only moves and splits files, never logic.
- No invariant additions, no "Item" vs "Listing" naming cleanup, no deduplication of `ParseMetadata`/`TrySaveAsync` helpers.
- No `Integration/Workspace/` folder reorganization — that is the third change.
- No spec behavior changes beyond the two `architecture-guidelines` requirement extensions that generalize the folder/file rules from Domain to all production layers.
- No new tests — existing tests continue to verify behavior; only their file locations and `using` directives change.

## Decisions

### Decision 1: Capability folder mapping

Each file maps to the capability whose use case or concept it serves. Folder names mirror the Domain capability folders and the existing OpenSpec spec names where they exist.

| Folder | Namespace | Source files (current) → resulting files |
|---|---|---|
| `Workspaces/` | `FusionCanvas.Application.Workspaces` | `WorkspaceManagement.cs` → `IWorkspaceManagementService.cs`, `WorkspaceContext.cs`, `WorkspaceManagementCreateRequest.cs`, `WorkspaceManagementUpdateRequest.cs`, `WorkspaceManagementDeleteRequest.cs`, `WorkspaceSummary.cs`, `WorkspaceManagementState.cs`, `WorkspaceManagementResult.cs`, `WorkspaceManagementService.cs` |
| `Stores/` | `FusionCanvas.Application.Stores` | `StoreManagement.cs` → `IStoreManagementService.cs`, `StoreContext.cs`, `StoreManagementCreateRequest.cs`, `StoreManagementUpdateRequest.cs`, `StoreManagementDeleteRequest.cs`, `StoreSummary.cs`, `StoreManagementState.cs`, `StoreManagementResult.cs`, `StoreManagementService.cs` |
| `Niches/` | `FusionCanvas.Application.Niches` | `NicheManagement.cs` → `INicheManagementService.cs`, `NicheContext.cs`, `NicheManagementCreateRequest.cs`, `NicheManagementUpdateRequest.cs`, `NicheManagementDeleteRequest.cs`, `NicheSummary.cs`, `NicheManagementState.cs`, `NicheManagementResult.cs`, `NicheManagementService.cs` |
| `Groups/` | `FusionCanvas.Application.Groups` | `GroupManagement.cs` → `IGroupManagementService.cs`, `GroupParentReference.cs`, `GroupContext.cs`, `GroupManagementCreateRequest.cs`, `GroupManagementUpdateRequest.cs`, `GroupPlacementKind.cs`, `GroupPlacement.cs`, `GroupManagementMoveRequest.cs`, `GroupManagementCopyRequest.cs`, `GroupManagementDeleteRequest.cs`, `GroupCreationDestinationResult.cs`, `GroupSummary.cs`, `GroupDestination.cs`, `GroupManagementState.cs`, `GroupManagementResult.cs`, `GroupManagementService.cs` (16 files; `WorkspaceTreeSelection` moves to `WorkspaceTree/` instead) |
| `Items/` | `FusionCanvas.Application.Items` | `ItemManagement.cs` → `IItemManagementService.cs`, `ItemTopicReference.cs`, `ItemContext.cs`, `ItemManagementCreateRequest.cs`, `ItemManagementUpdateRequest.cs`, `ItemManagementMoveRequest.cs`, `ItemManagementDuplicateRequest.cs`, `ItemManagementRestoreRequest.cs`, `ItemManagementDeleteRequest.cs`, `ItemManagementSetStatusRequest.cs`, `ItemManagementMoveStageRequest.cs`, `ItemCreationDestinationResult.cs`, `ItemDestination.cs`, `ItemSummary.cs`, `ItemManagementState.cs`, `ItemManagementResult.cs`, `ItemManagementService.cs`; `ItemInspector.cs` → `IItemInspectorService.cs`, `ItemInspectorCreativeFields.cs`, `ItemInspectorTagEntry.cs`, `ItemInspectorAssetEntry.cs`, `ItemInspectorState.cs`, `ItemInspectorSaveRequest.cs`, `ItemStageSavePayload.cs`, `ItemStageAwareSaveRequest.cs`, `ItemInspectorSaveResult.cs`, `ItemInspectorService.cs`; `ItemIdGenerator.cs` → `IItemIdGenerator.cs`, `GuidItemIdGenerator.cs`, `DelegateItemIdGenerator.cs` (the internal class becomes its own file); `ItemMetadataCodec.cs` stays as one file (it is a single internal static class) |
| `Tags/` | `FusionCanvas.Application.Tags` | `TagManagement.cs` → `ITagManagementService.cs`, `TagSummary.cs`, `TagManagementState.cs`, `TagManagementCreateRequest.cs`, `TagManagementUpdateRequest.cs`, `TagManagementDeleteRequest.cs`, `ApplyTagRequest.cs`, `RemoveTagRequest.cs`, `ApplyOrCreateTagRequest.cs`, `TagApplicationResult.cs`, `TagManagementResult.cs`, `TagManagementService.cs` |
| `Assets/` | `FusionCanvas.Application.Assets` | `AssetManagement.cs` → `IAssetManagementService.cs`, `AssetContextReference.cs`, `AssetContextDescriptor.cs`, `AssetSummary.cs`, `AssetManagementState.cs`, `AssetManagementImportRequest.cs`, `AssetManagementRelabelRequest.cs`, `AssetManagementRemoveRequest.cs`, `AssetManagementResult.cs`, `AssetPurposePolicy.cs`, `AssetManagementService.cs` |
| `DesignFiles/` | `FusionCanvas.Application.DesignFiles` | `DesignFileService.cs` → `IDesignFileService.cs`, `DesignFileSummary.cs`, `DesignFileImportResult.cs`, `DesignFileRemoveResult.cs`, `DesignFileService.cs` |
| `Navigation/` | `FusionCanvas.Application.Navigation` | `NavigationWorkspaceService.cs` → `IWorkspaceNavigationService.cs`, `NavigationTargetKind.cs`, `NavigationTarget.cs`, `NavigationCreationScope.cs`, `WorkspaceNavigationService.cs` |
| `WorkflowNavigation/` | `FusionCanvas.Application.WorkflowNavigation` | `WorkflowStageNavigatorService.cs` → `IWorkflowStageNavigatorService.cs`, `ActiveItemWorkflowContext.cs`, `WorkflowStageNavigatorEntry.cs`, `WorkflowStageNavigatorState.cs`, `WorkflowStageNavigatorService.cs` |
| `StageTools/` | `FusionCanvas.Application.StageTools` | `StageToolHost.cs` → `IStageToolRegistry.cs`, `InMemoryStageToolRegistry.cs`, `IStageToolHostService.cs`, `StageToolHostService.cs`, `StageToolSourceKind.cs`, `StageToolAvailabilityKind.cs`, `StageToolContextKind.cs`, `StageToolDescriptor.cs`, `StageToolAvailability.cs`, `StageToolSelectionKey.cs`, `StageToolHostRequest.cs`, `StageToolHostState.cs`, `BuiltInStageTools.cs`, `IStageToolCommandGateway.cs` |
| `ToolContexts/` | `FusionCanvas.Application.ToolContexts` | `ToolContext.cs` → `IToolContextResolver.cs`, `ToolContextResolver.cs` (the implementation currently lives in `ToolContextResolver.cs`; keep it there), `ToolContextScopeKind.cs`, `ToolContextSelectionKind.cs`, `NearbyWorkState.cs`, `ToolContextEntityReference.cs`, `ToolContextInheritedValue.cs`, `ToolContextNearbyWorkSummary.cs`, `ToolContextScopeSummary.cs`, `ToolContextResolveRequest.cs`, `ToolContextResolution.cs`, `ToolContext.cs`, `ToolContextCreationDefaults.cs` |
| `WorkspaceTree/` | `FusionCanvas.Application.WorkspaceTree` | `WorkspaceTree.cs` → `WorkspaceTreeQuery.cs`, `WorkspaceTreeProjectionNode.cs`, `WorkspaceTreeProjection.cs`, `WorkspaceTreeProjector.cs`, `WorkspaceTreeSelection.cs` (the record already lives in `GroupManagement.cs`; move it here instead), `WorkspaceTreeSelectionCoordinator.cs`, `WorkspaceTreeClipboardMode.cs`, `WorkspaceTreeClipboardPayload.cs`, `WorkspaceTreeClipboard.cs` |
| `Workspaces/` (shared ports) | `FusionCanvas.Application.Workspaces` | `IWorkspaceRepository.cs`, `IWorkspaceFileStore.cs`, `ManagedWorkspaceFile.cs` (split out of `IWorkspaceFileStore.cs`) |
| `Settings/` | `FusionCanvas.Application.Settings` | `IApplicationSettingsStore.cs` → `IApplicationSettingsStore.cs`, `ApplicationSettingsLoadResult.cs`, `ApplicationSettingsSaveResult.cs`; `ApplicationSettings.cs` stays |

**Rationale for non-obvious placements:**
- `WorkspaceTreeSelection` (currently in `GroupManagement.cs`) moves to `WorkspaceTree/`: it is the selection model used by the tree projection and the clipboard, not a group-management concept. The `ItemManagement` and `GroupManagement` services reference it; they add a `using FusionCanvas.Application.WorkspaceTree;`.
- `IWorkspaceRepository` and `IWorkspaceFileStore` go in `Workspaces/` rather than a `Persistence/` folder: the standard's example (`coding-standard.md:71-73`) assigns `Persistence/` to the Integration layer, and these are application-facing ports owned by the workspace capability.
- `ItemMetadataCodec` stays as one file: it is a single internal static class, so it already satisfies the one-type-per-file rule.
- `BuiltInStageTools` is a static factory; it gets its own file in `StageTools/` rather than staying glued to the host service.

**Alternatives considered:**
- Put all ports (`IWorkspaceRepository`, `IWorkspaceFileStore`, `IItemManagementService`, …) in a single `Ports/` folder — rejected: §1 forbids technical grouping folders, and each port belongs to its capability.
- Put `WorkspaceTreeSelection` in `Items/` because `ItemManagementService` is its heaviest consumer — rejected: the tree projection and clipboard own it; `Items/` would invert the dependency direction.
- Keep `ToolContext.cs` and `ToolContextResolver.cs` together — rejected: §2 requires interface/implementation separation; they are already in separate files but share a folder with 11 supporting types that must also split.

### Decision 2: `Workspace/` folder removed entirely

Unlike the Domain layer (which keeps a small shared `Workspace/` root for `WorkspaceEntity`, `WorkspaceEntityKind`, `WorkspaceSnapshot`), the Application layer has no cross-cutting primitives that don't belong to a capability. `WorkspaceManagement.cs` is the workspace capability itself and moves to `Workspaces/`. After the move, `src/FusionCanvas.Application/Workspace/` is empty and deleted. `ApplicationAssemblyMarker.cs` stays at the project root (it already lives there, not under `Workspace/`).

### Decision 3: Update consumers with `using` directives, not full qualification

Same as the Domain change: add `using` directives for the new namespaces and remove the old `using FusionCanvas.Application.Workspace;` where it no longer covers all referenced types. Matches existing style; avoids mixing a style change with a mechanical move (§16 step 4).

### Decision 4: No namespace compatibility shims

Same as the Domain change: clean break in one change. The Application project's contracts are consumed only by `Integration` and `App`, both updated here. No external consumers.

### Decision 5: Test files mirror production capability folders

The 16 test files in `tests/FusionCanvas.Application.Tests/` move into matching capability subfolders (`Workspaces/`, `Stores/`, `Niches/`, `Groups/`, `Items/`, `Tags/`, `Assets/`, `DesignFiles/`, `Navigation/`, `WorkflowNavigation/`, `StageTools/`, `ToolContexts/`, `WorkspaceTree/`) per §14. Test bodies do not change; only `using` directives and file locations. The contract tests (`WorkspaceRepositoryContractTests.cs`, `WorkspaceFileStoreContractTests.cs`) move to `Workspaces/`.

## Risks / Trade-offs

- **[Risk] Larger mechanical surface than the Domain change** (~80–90 new files vs ~30). → Mitigation: sequence by capability folder, run `dotnet build` + `dotnet test` after each, fix `using` directives before proceeding. Each step ends green.
- **[Risk] A `using` directive is missed and a downstream project fails to build.** → Mitigation: the build after each capability step covers `Integration` and `App` (the whole solution builds), so a missed reference surfaces immediately, not at the end.
- **[Risk] A type ends up in the wrong capability folder.** → Mitigation: the mapping in Decision 1 is decided now; reviewers check it before implementation. `WorkspaceTreeSelection` is the only genuinely contested placement and is resolved here.
- **[Trade-off] Very large diff for a mechanical change** (mostly file renames and `using` edits). → Accepted: §16 explicitly authorizes this as a dedicated maintenance change. The diff is reviewable per capability folder.
- **[Trade-off] The `ItemManagementService` and `GroupManagementService` classes stay oversized** (805 and ~880 lines). → Intentional: splitting a service's responsibilities is a behavior-adjacent refactor that must not be smuggled into a mechanical move. This change makes the oversized classes visible as single files in `Items/`, which actually surfaces the debt for the next decision rather than hiding it inside a 904-line bundle.

## Migration Plan

This is a code-internal refactor with no persistence or runtime migration. Deployment is a single pull request landing after `reorganize-domain-capability-folders`; there is no runtime state to migrate and no rollback beyond reverting the commit. All projects use SDK-style implicit globbing, so moving files requires no `.csproj` edits.

### Implementation Plan (sequenced by capability, each step ends with green build + tests)

Each step is a bounded mechanical move: create the target folder, split the source file(s) into one-type-per-file, move the files, update each file-scoped `namespace`, update `using` directives across all consumers, then run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`.

1. **Shared ports in `Workspaces/`** — split `ManagedWorkspaceFile.cs` out of `IWorkspaceFileStore.cs`; move both ports and the result record into `Workspaces/`. Update consumers.
2. **`Workspaces/` management** — split `WorkspaceManagement.cs` into 9 files; update consumers.
3. **`Stores/`** — split `StoreManagement.cs` into 9 files; update consumers.
4. **`Niches/`** — split `NicheManagement.cs` into 9 files; update consumers.
5. **`Groups/`** — split `GroupManagement.cs` into 16 files, moving `WorkspaceTreeSelection` to `WorkspaceTree/` in step 13 instead; update consumers.
6. **`Items/` management** — split `ItemManagement.cs` into 17 files; update consumers.
7. **`Items/` inspector** — split `ItemInspector.cs` into 10 files; move `ItemIdGenerator.cs` into 3 files; keep `ItemMetadataCodec.cs` as one file; update consumers.
8. **`Tags/`** — split `TagManagement.cs` into 12 files; update consumers.
9. **`Assets/`** — split `AssetManagement.cs` into 11 files; update consumers.
10. **`DesignFiles/`** — split `DesignFileService.cs` into 5 files; update consumers.
11. **`Navigation/`** — split `NavigationWorkspaceService.cs` into 5 files; update consumers.
12. **`WorkflowNavigation/`** — split `WorkflowStageNavigatorService.cs` into 5 files; update consumers.
13. **`StageTools/`** — split `StageToolHost.cs` into 14 files; update consumers.
14. **`ToolContexts/`** — split `ToolContext.cs` into 13 files; keep `ToolContextResolver.cs` as the implementation file; update consumers.
15. **`WorkspaceTree/`** — split `WorkspaceTree.cs` into 9 files, pulling `WorkspaceTreeSelection` from `GroupManagement.cs` here; update consumers.
16. **`Settings/`** — split `IApplicationSettingsStore.cs` into 3 files (`IApplicationSettingsStore.cs`, `ApplicationSettingsLoadResult.cs`, `ApplicationSettingsSaveResult.cs`); `ApplicationSettings.cs` stays; update consumers.
17. **Delete the now-empty `Workspace/` folder** and confirm `src/FusionCanvas.Application/` contains only capability folders plus `ApplicationAssemblyMarker.cs` and the `.csproj`.
18. **Test mirror** — move the 16 test files under `tests/FusionCanvas.Application.Tests/` into matching capability subfolders and update `using` directives.
19. **Final verification** — `dotnet build .\FusionCanvas.sln`, `dotnet test .\FusionCanvas.sln`, `openspec validate reorganize-application-capability-folders`, and a spot check that no Application file contains more than one top-level type and no `Workspace/` folder remains.

### Decisions not to reopen

- The capability folder mapping (Decision 1) is final for this change; a misplaced type is corrected in a follow-up, not relitigated mid-implementation.
- The service classes keep their current internal structure; no responsibility splitting, no helper deduplication, no renaming here.
- The third change (`reorganize-integration-capability-folders`) is separate; do not expand scope here.

### Verification approach

- **Build:** `dotnet build .\FusionCanvas.sln` after each step.
- **Tests:** `dotnet test .\FusionCanvas.sln` after each step. All 482 tests must remain green; no test logic changes, only `using` directives and file locations.
- **OpenSpec validation:** `openspec validate reorganize-application-capability-folders` after the final step.
- **Coding-standard spot check:** confirm no `src/FusionCanvas.Application/Workspace/` folder exists, no Application file contains more than one top-level type (excluding permitted §2 exceptions), and every file's namespace matches its folder path.
- **Acceptance scenarios:** the existing `ItemManagementServiceTests`, `GroupManagementServiceTests`, `StoreManagementServiceTests`, `NicheManagementServiceTests`, `TagManagementServiceTests`, `AssetManagementServiceTests`, `DesignFileServiceTests`, `ItemInspectorServiceTests`, `StageToolHostServiceTests`, `ToolContextResolverTests`, `WorkflowStageNavigatorServiceTests`, `WorkspaceNavigationServiceTests`, `WorkspaceTreeTests`, `WorkspaceManagementServiceTests`, `WorkspaceRepositoryContractTests`, `WorkspaceFileStoreContractTests` continue to pass unchanged — they are the behavioral verification that the refactor preserved behavior. No new desktop or manual verification is warranted for a pure namespace/file move; UX preflight is not applicable.

## Open Questions

None. The mapping, sequencing, and verification approach are decided. Any unresolved placement discovered during implementation is returned for review rather than guessed.
