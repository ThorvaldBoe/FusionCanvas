## Why

The `FusionCanvas.Application` project keeps 19 of its 20 workspace-related files in a single catch-all `Workspace/` folder, and most of those files bundle a service's interface, requests, results, state, and implementation into one mega-file. The C# coding standard (`docs/coding-standard.md` §1) forbids the catch-all folder: *"A broad concept such as `Workspace` MAY be a namespace root, but it MUST NOT become the permanent home for every workspace-related type."* §2 requires one primary type per file and explicitly bans `Contracts.cs`-style grouping. The `Application/Settings/` folder and the entire `App` project already follow the capability convention, so this is migration debt. The Application layer is where the cost is highest — `ItemManagement.cs` (14 types, 904 lines) and `GroupManagement.cs` (16 types, 990 lines) are the most visible offenders — so doing it now, immediately after the Domain reorganization, stops the pattern from compounding and keeps the upcoming `Application` feature work from landing in the wrong structure.

## What Changes

- Move Application files from `Workspace/` into cohesive capability folders that mirror the Domain split and the existing OpenSpec capability names: `Workspaces/`, `Stores/`, `Niches/`, `Groups/`, `Items/`, `Tags/`, `Assets/`, `DesignFiles/`, `Navigation/`, `WorkflowNavigation/`, `StageTools/`, `ToolContexts/`, `WorkspaceTree/`. The existing `Settings/` folder stays.
- Split every multi-type `*Management.cs` / `*Service.cs` / `ToolContext.cs` / `WorkspaceTree.cs` / `StageToolHost.cs` / `WorkflowStageNavigatorService.cs` / `NavigationWorkspaceService.cs` / `ItemInspector.cs` / `DesignFileService.cs` / `AssetManagement.cs` / `ItemIdGenerator.cs` / `IWorkspaceFileStore.cs` / `IApplicationSettingsStore.cs` file into one-type-per-file per `coding-standard.md` §2: the interface, each request/result/state/summary/options record, and the implementation each get their own file.
- Update each moved file's namespace to match its folder path (e.g. `FusionCanvas.Application.Workspace.ItemManagementService` → `FusionCanvas.Application.Items.ItemManagementService`).
- Update all `using` directives and namespace references across `Integration`, `App`, and the test projects to keep the solution compiling.
- No production logic changes. The Domain reorganization change must land first because this change references the new Domain namespaces; it builds on `reorganize-domain-capability-folders`.

## Capabilities

### New Capabilities
<!-- None. This change does not introduce a new capability. -->

### Modified Capabilities
- `architecture-guidelines`: Extends the two requirements added by `reorganize-domain-capability-folders` so the capability-folder and one-primary-type-per-file rules explicitly cover the Application layer, not just Domain. This keeps the convention enforceable across all production layers going forward.

## Impact

- **Code**: ~20 production files in `src/FusionCanvas.Application/Workspace/` move and split into ~80–90 files under new capability folders. The `Settings/` folder is untouched. `Integration`, `App`, and `FusionCanvas.Application.Tests` need `using` updates and, for the test project, file-location mirrors per `coding-standard.md` §14.
- **APIs**: Namespace names change (e.g. `FusionCanvas.Application.Workspace.ItemManagementService` → `FusionCanvas.Application.Items.ItemManagementService`). The Application project's contracts are consumed only by `Integration` (adapter implementations) and `App` (view models and composition), both updated in this change; no external consumers.
- **Persistence**: No schema, migration, or serialization format changes. The `SqliteWorkspaceRepository` continues to implement `IWorkspaceRepository`; only its `using` directives change.
- **Specs**: One MODIFIED requirement on `architecture-guidelines` extends the capability-folder and one-type-per-file rules to the Application layer. The existing capability specs (`workspace-management`, `store-management`, `niche-management`, `group-management`, `listing-management`, `tag-management`, `asset-management`, `listing-inspector`, `navigation-tree`, `workflow-stage-navigator`, `stage-tool-host`, `context-aware-tools`, `search-filtering`, `workspace-file-storage`) describe behavior, not namespaces or file layout; their scenarios continue to hold.
- **Tests**: All 150 Application-layer tests and the downstream App/Integration tests must remain green; only `using` directives and test file locations change. Test files mirror the new production folders.
- **Risk**: Larger mechanical surface than the Domain change (more files, more cross-references). Mitigated by sequencing the work one capability folder at a time with `dotnet build` + `dotnet test` green after each step, per `coding-standard.md` §16 step 5. No behavioral risk because no logic changes.
- **Dependency**: Must land after `reorganize-domain-capability-folders`. If the Domain change is revised, this change's `using` directives are revised to match.
- **Non-goals**: No service-size refactors (splitting `ItemManagementService` into smaller collaborators), no invariant additions, no "Item" vs "Listing" naming cleanup, no `Integration/Workspace/` folder reorganization — that is a separate follow-up change. The interface/implementation separation here is purely file-level; the service classes keep their current internal structure.
