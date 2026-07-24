## Context

The `FusionCanvas.Integration` project currently holds 5 production files across two folders: `Integration/Workspace/` contains `SqliteWorkspaceRepository.cs`, `LocalWorkspaceFileStore.cs`, and `InMemoryWorkspaceFileStore.cs` (the in-memory file-store adapter PR #57 moved out of the App production code); `Integration/Settings/` contains `JsonApplicationSettingsStore.cs`; `IntegrationAssemblyMarker.cs` lives at the project root. The `Settings/` folder already follows the capability convention; `Workspace/` is a catch-all that mixes the SQLite persistence adapter with the file-storage adapters.

The C# coding standard (`docs/coding-standard.md` §1) forbids the catch-all folder and its own preferred tree (`coding-standard.md:71-75`) literally shows `Integration/Persistence/SqliteWorkspaceRepository.cs` and `Integration/Files/LocalWorkspaceFileStore.cs` as the intended shape. Unlike the Domain and Application layers, the Integration project is already compliant with §2 (one primary type per file) — every file already contains exactly one top-level type. So this change is purely the folder/namespace split plus `using` updates: no file splits, no logic changes.

This is the third and final structural-debt cleanup. It depends on `reorganize-domain-capability-folders` and `reorganize-application-capability-folders` landing first, because the `using` directives updated here reference the new Domain and Application namespaces.

## Goals / Non-Goals

**Goals:**
- Split the `Integration/Workspace/` catch-all folder into `Persistence/` (for `SqliteWorkspaceRepository.cs`) and `Files/` (for `LocalWorkspaceFileStore.cs` and `InMemoryWorkspaceFileStore.cs`), matching the standard's preferred tree.
- Update each moved file's namespace to match its folder path per §1.
- Mirror the new folder structure in `tests/FusionCanvas.Integration.Tests/` per §14.
- Keep `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln` green after each bounded step.
- Leave the existing `Settings/` folder and `IntegrationAssemblyMarker.cs` untouched.

**Non-Goals:**
- No `SqliteWorkspaceRepository` size refactor — the 795-line class stays one file; splitting schema/migration logic from CRUD/mapping is a behavior-adjacent change handled separately.
- No `InMemoryWorkspaceFileStore` relocation back to tests — PR #57 deliberately placed it in Integration as a documented in-memory adapter; it moves to `Files/` but stays in the Integration project.
- No `App` project folder work — the App project already follows the capability convention.
- No spec behavior changes beyond the one `architecture-guidelines` requirement extension that generalizes the capability-folder rule to the Integration layer.
- No new tests — existing tests continue to verify behavior; only `using` directives and file locations change.

## Decisions

### Decision 1: Folder mapping

| Folder | Namespace | Files |
|---|---|---|
| `Persistence/` | `FusionCanvas.Integration.Persistence` | `SqliteWorkspaceRepository.cs` |
| `Files/` | `FusionCanvas.Integration.Files` | `LocalWorkspaceFileStore.cs`, `InMemoryWorkspaceFileStore.cs` |
| `Settings/` | `FusionCanvas.Integration.Settings` | `JsonApplicationSettingsStore.cs` (unchanged) |
| *(project root)* | `FusionCanvas.Integration` | `IntegrationAssemblyMarker.cs` (unchanged) |

**Rationale:** This is the exact shape the standard's preferred tree shows (`coding-standard.md:71-75`). `Persistence/` and `Files/` are technical subfolders, which §1 explicitly permits at the Integration layer: *"Technical subfolders such as `Persistence`, `Files`, `Views`, and `Converters` are appropriate where the layer itself makes the capability clear."* (`coding-standard.md:85`).

**Alternatives considered:**
- Put `SqliteWorkspaceRepository` in a `Workspaces/` capability folder mirroring Domain/Application — rejected: there is only one persistence adapter and one file-store adapter in the Integration layer; a capability folder with a single file adds a layer of nesting without aiding locality, and the standard's example uses `Persistence/`/`Files/`, not a capability name, for Integration.
- Keep `InMemoryWorkspaceFileStore` in a `Testing/` folder — rejected: it lives in the production Integration project (deliberately, per PR #57), so it follows the same folder convention as the real file store; a separate `Testing/` folder would fragment file-store adapters across two folders.

### Decision 2: `Workspace/` folder removed entirely

After the three files move, `src/FusionCanvas.Integration/Workspace/` is empty and deleted. The Integration layer has no cross-cutting primitives that warrant a shared root (unlike the Domain layer's `Workspace/`).

### Decision 3: Update consumers with `using` directives, not full qualification

Same as the Domain and Application changes: add `using` directives for the new namespaces and remove the old `using FusionCanvas.Integration.Workspace;` where it no longer covers referenced types. Matches existing style; avoids mixing a style change with a mechanical move (§16 step 4). The composition root in `App` (which constructs `SqliteWorkspaceRepository` and `LocalWorkspaceFileStore`) and the App tests that reference `InMemoryWorkspaceFileStore` get the updated `using` directives.

### Decision 4: No namespace compatibility shims

Same as the prior two changes: clean break. The Integration adapters are constructed only in `App` composition code and tests, all updated here. No external consumers.

## Risks / Trade-offs

- **[Risk] Missed `using` directive breaks composition root or tests.** → Mitigation: the solution-wide build after each move covers `App` and all test projects, so a missed reference surfaces immediately. Only 3 files move, so the surface is small.
- **[Trade-off] Smallest payoff of the three changes (3 files, 2 new folders).** → Accepted: it completes the structural cleanup so all three inner layers follow the same convention, and it matches the standard's own example. Skipping it would leave the Integration layer as the lone holdout and undermine the §1 rule's enforceability.

## Migration Plan

This is a code-internal refactor with no persistence or runtime migration. Deployment is a single pull request landing after the Domain and Application changes; there is no runtime state to migrate and no rollback beyond reverting the commit. All projects use SDK-style implicit globbing, so moving files requires no `.csproj` edits.

### Implementation Plan (sequenced by folder, each step ends with green build + tests)

1. **`Persistence/`** — create `src/FusionCanvas.Integration/Persistence/`; move `SqliteWorkspaceRepository.cs`; update its namespace to `FusionCanvas.Integration.Persistence`; update `using` directives across `App` and the test projects; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`.
2. **`Files/`** — create `src/FusionCanvas.Integration/Files/`; move `LocalWorkspaceFileStore.cs` and `InMemoryWorkspaceFileStore.cs`; update namespaces to `FusionCanvas.Integration.Files`; update `using` directives across `App` and the test projects; run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`.
3. **Remove empty `Workspace/` folder** and confirm `src/FusionCanvas.Integration/` contains only `Persistence/`, `Files/`, `Settings/`, `IntegrationAssemblyMarker.cs`, and the `.csproj`.
4. **Test mirror** — move the 6 test files under `tests/FusionCanvas.Integration.Tests/` into matching subfolders (`Persistence/` for `SqliteWorkspaceRepositoryTests.cs` and the persistence-roundtrip tests, `Files/` for `LocalWorkspaceFileStoreTests.cs`, `Settings/` for `JsonApplicationSettingsStoreTests.cs`); update `using` directives. The four persistence-roundtrip tests (`ItemManagementPersistenceTests.cs`, `ItemInspectorPersistenceTests.cs`, `AssetManagementPersistenceTests.cs`) move to `Persistence/` because they exercise `SqliteWorkspaceRepository` through application services.
5. **Final verification** — `dotnet build .\FusionCanvas.sln`, `dotnet test .\FusionCanvas.sln`, `openspec validate reorganize-integration-capability-folders`, and a spot check that no `Integration/Workspace/` folder remains and every file's namespace matches its folder path.

### Decisions not to reopen

- The folder mapping (Decision 1) is final; the standard's own example dictates `Persistence/` and `Files/`.
- `InMemoryWorkspaceFileStore` stays in the Integration project (PR #57's decision); only its folder changes.
- The `SqliteWorkspaceRepository` size refactor is out of scope.

### Verification approach

- **Build:** `dotnet build .\FusionCanvas.sln` after each step.
- **Tests:** `dotnet test .\FusionCanvas.sln` after each step. All 482 tests must remain green; no test logic changes, only `using` directives and file locations.
- **OpenSpec validation:** `openspec validate reorganize-integration-capability-folders` after the final step.
- **Coding-standard spot check:** confirm no `src/FusionCanvas.Integration/Workspace/` folder exists and every file's namespace matches its folder path.
- **Acceptance scenarios:** the existing `SqliteWorkspaceRepositoryTests`, `LocalWorkspaceFileStoreTests`, `JsonApplicationSettingsStoreTests`, and the three persistence-roundtrip tests continue to pass unchanged — they are the behavioral verification that the refactor preserved behavior. No new desktop or manual verification is warranted for a pure namespace/folder move; UX preflight is not applicable.

## Open Questions

None. The mapping, sequencing, and verification approach are decided.
