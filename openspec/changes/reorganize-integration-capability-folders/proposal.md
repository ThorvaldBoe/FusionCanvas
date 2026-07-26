## Why

The `FusionCanvas.Integration` project places its persistence adapter (`SqliteWorkspaceRepository.cs`), its file-storage adapter (`LocalWorkspaceFileStore.cs`), the in-memory file-store test fake that PR #57 moved here (`InMemoryWorkspaceFileStore.cs`), and the JSON settings store (under a separate `Settings/` folder) all under a single `Workspace/` folder for the first three. The C# coding standard (`docs/coding-standard.md` §1, "Organize by Layer, Then Capability") forbids the catch-all folder, and the standard's own preferred tree (`coding-standard.md:71-75`) literally shows `Integration/Persistence/SqliteWorkspaceRepository.cs` and `Integration/Files/LocalWorkspaceFileStore.cs` as the intended shape. The `Settings/` folder is already correct. This is the third and final structural-debt cleanup; doing it last lets the Integration `using` directives reference the new Domain and Application namespaces settled by the first two changes.

## What Changes

- Move the three files currently in `Integration/Workspace/` into cohesive capability folders: `Persistence/` for `SqliteWorkspaceRepository.cs` and `Files/` for `LocalWorkspaceFileStore.cs` and `InMemoryWorkspaceFileStore.cs`. The existing `Settings/` folder stays.
- Update each moved file's namespace to match its folder path (e.g. `FusionCanvas.Integration.Workspace.SqliteWorkspaceRepository` → `FusionCanvas.Integration.Persistence.SqliteWorkspaceRepository`).
- Update all `using` directives and namespace references across `App` (composition root) and the test projects to keep the solution compiling.
- No production logic changes, no file splits (every Integration file already contains exactly one top-level type, per `coding-standard.md` §2).

## Capabilities

### New Capabilities
<!-- None. This change does not introduce a new capability. -->

### Modified Capabilities
- `architecture-guidelines`: Extends the two requirements modified by `reorganize-application-capability-folders` so the capability-folder rule explicitly covers the Integration layer (persistence and file-storage adapters), not just Domain and Application. The one-primary-type-per-file rule already covers Integration (it already complies) and is restated for completeness.

## Impact

- **Code**: 3 production files move from `Integration/Workspace/` to `Integration/Persistence/` and `Integration/Files/`. `Settings/JsonApplicationSettingsStore.cs` is untouched. `App` (composition root that constructs `SqliteWorkspaceRepository` and `LocalWorkspaceFileStore`) and `FusionCanvas.Integration.Tests` need `using` updates. `InMemoryWorkspaceFileStore` is referenced by App tests; its `using` directives update there too.
- **APIs**: Namespace names change for three types. The Integration project's concrete adapters are consumed only by `App` composition code and tests, both updated here; no external consumers.
- **Persistence**: No schema, migration, or serialization format changes. SQLite tables/columns and JSON settings format are unaffected because they reference column names and property names, not namespaces.
- **Specs**: One MODIFIED requirement on `architecture-guidelines` extends the capability-folder rule to the Integration layer. The existing `local-sqlite-persistence`, `workspace-file-storage`, and `application-settings` specs describe behavior, not namespaces or folders; their scenarios continue to hold.
- **Tests**: The 50 Integration tests and downstream App tests must remain green; only `using` directives and test file locations change. Test files mirror the new production folders per `coding-standard.md` §14.
- **Risk**: Smallest of the three structural changes — 3 files, few cross-references. Mitigated by running `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln` after each move. No behavioral risk because no logic changes.
- **Dependency**: Must land after `reorganize-domain-capability-folders` and `reorganize-application-capability-folders`. If either is revised, this change's `using` directives are revised to match.
- **Non-goals**: No `SqliteWorkspaceRepository` size refactor (the 795-line class stays intact; splitting schema/migration logic from CRUD/mapping is a separate behavior-adjacent change). No `InMemoryWorkspaceFileStore` relocation back to tests (PR #57 deliberately placed it in Integration as a documented in-memory adapter; it stays in `Files/`). No `App` project folder work — the `App` project already follows the capability convention.
