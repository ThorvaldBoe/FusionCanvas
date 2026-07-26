## Why

The `FusionCanvas.Domain` project keeps every workspace-related type in a single catch-all `Workspace/` folder. The C# coding standard (`docs/coding-standard.md` §1, "Organize by Layer, Then Capability") explicitly forbids this: *"A broad concept such as `Workspace` MAY be a namespace root, but it MUST NOT become the permanent home for every workspace-related type."* The standard's preferred tree (`coding-standard.md:50-61`) shows `Domain/Items/`, `Domain/Assets/`, `Domain/Navigation/` as the intended shape, and the `Application/Settings/` folder plus the entire `App` project already follow the capability-folder convention — so this is migration debt, not a missing convention. Doing it now, while the Domain layer is small (~20 files), is cheap and keeps later Application-layer work from inheriting the wrong structure.

## What Changes

- Move Domain files from `Workspace/` into cohesive capability folders under `FusionCanvas.Domain/`: `Items/`, `Stores/`, `Niches/`, `Groups/`, `Tags/`, `Assets/`, `Prompts/`, `Workflow/`, `Navigation/`, and a small shared root for cross-capability primitives.
- Update each moved file's namespace to match its new folder path (e.g. `FusionCanvas.Domain.Workspace.Item` → `FusionCanvas.Domain.Items.Item`), per `coding-standard.md` §1: *"Namespaces MUST normally match the project and folder path."*
- Update all `using` directives and namespace references across `Application`, `Integration`, and `App` to keep the solution compiling.
- Split the remaining multi-type Domain files into one-type-per-file per `coding-standard.md` §2: `NavigationTree.cs` (5 types), `ItemWorkflowPolicy.cs` (4 types), `WorkspaceRelationships.cs` (6 types), `WorkflowStage.cs` (2 types). The vague file name `WorkspaceRelationships.cs` is disallowed by `coding-standard.md:114` and is retired as part of the split.
- No production logic changes. No public API surface is consumed outside this repo, so namespace moves are not a compatibility break for any external contract.

## Capabilities

### New Capabilities
<!-- None. This change does not introduce a new capability. -->

### Modified Capabilities
- `architecture-guidelines`: Adds two requirements that promote the coding-standard rules for cohesive capability folders (`coding-standard.md` §1) and one-primary-type-per-file (`coding-standard.md` §2) into accepted architectural behavior for the Domain layer, so the convention is enforceable going forward and not just doc guidance.

## Impact

- **Code**: ~20 production files in `src/FusionCanvas.Domain/Workspace/` move and split into ~30 files under new capability folders. Every project that references `FusionCanvas.Domain.Workspace.*` namespaces (`Application`, `Integration`, `App`, and the four test projects) needs `using` updates. Test files that mirror production capabilities also move to match.
- **APIs**: Namespace names change (e.g. `FusionCanvas.Domain.Workspace.Item` → `FusionCanvas.Domain.Items.Item`). The Domain project has no external consumers; this is not a published-API break.
- **Persistence**: No schema, migration, or serialization format changes. SQLite columns and JSON metadata are unaffected because they reference column names and property names, not namespaces.
- **Specs**: Two ADDED requirements on `architecture-guidelines` codify the capability-folder and one-type-per-file rules as accepted behavior. `openspec/specs/core-domain-model/spec.md` is untouched; its entity/relationship scenarios continue to hold.
- **Tests**: Test files under `tests/FusionCanvas.Domain.Tests/` move to mirror the new production folders per `coding-standard.md` §14 ("Mirror production capabilities in the corresponding test project"). Test bodies do not change.
- **Risk**: Mechanical moves can break compilation if a `using` is missed. Mitigated by running `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln` after each bounded step (per `coding-standard.md` §16 step 5). No behavioral risk because no logic changes.
- **Non-goals**: The `Application/Workspace/` and `Integration/Workspace/` catch-all folders are out of scope for this change — they are larger and will be handled by a follow-up maintenance change. No service-size refactors, no invariant additions, no naming ("Item" vs "Listing") cleanup here.
