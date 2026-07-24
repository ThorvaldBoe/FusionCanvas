## Context

The `FusionCanvas.Domain` project currently places all ~20 production files under a single `Workspace/` folder with the namespace `FusionCanvas.Domain.Workspace`. The C# coding standard (`docs/coding-standard.md` §1, "Organize by Layer, Then Capability") requires grouping by cohesive product capability and forbids a catch-all technical folder; §2 requires one primary type per file. PR #57 already split `WorkspaceEntities.cs` into per-entity files, but those files landed back in `Workspace/`, and several multi-type files remain (`NavigationTree.cs`, `ItemWorkflowPolicy.cs`, `WorkspaceRelationships.cs`, `WorkflowStage.cs`). The `Application/Settings/` folder and the entire `App` project already follow the capability-folder convention, so this is migration debt, not a missing pattern.

This change is a maintenance refactor: it moves and splits files and updates namespaces, with no production logic changes. It is the first of two planned maintenance changes — this one for `Domain`, a follow-up for `Application` and `Integration`.

## Goals / Non-Goals

**Goals:**
- Split the Domain `Workspace/` catch-all folder into cohesive capability folders: `Workspace/` (shared root), `Stores/`, `Niches/`, `Groups/`, `Items/`, `Tags/`, `Assets/`, `Prompts/`, `Workflow/`, `Navigation/`.
- Update each moved file's namespace to match its folder path per `coding-standard.md` §1.
- Split the remaining multi-type Domain files into one-type-per-file per §2, retiring the disallowed vague file name `WorkspaceRelationships.cs`.
- Mirror the new folder structure in `tests/FusionCanvas.Domain.Tests/` per `coding-standard.md` §14.
- Keep `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln` green after each bounded step.

**Non-Goals:**
- No production logic changes, no new invariants, no naming cleanup ("Item" vs "Listing"), no service-size refactors.
- No `Application/Workspace/` or `Integration/Workspace/` folder reorganization — those are a follow-up change.
- No spec deltas — accepted behavior is unchanged. `openspec/specs/core-domain-model/spec.md` describes entities and relationships, not namespaces or folders.
- No persistence schema, migration, or serialization format changes.
- No new tests — existing tests continue to verify behavior; only their file locations and `using` directives change.

## Decisions

### Decision 1: Keep a small shared `Workspace/` namespace root

**Choice:** Retain `FusionCanvas.Domain.Workspace/` as a narrow namespace for truly cross-cutting primitives: `Workspace`, `WorkspaceDefaults`, `WorkspaceEntity`, `WorkspaceEntityKind`, `WorkspaceSnapshot`.

**Rationale:** `coding-standard.md:46` explicitly permits `Workspace` as a namespace root. These five types are referenced across every capability (`WorkspaceEntity` is the abstract base of Store, Niche, TopicGroup, Item, Asset, Prompt, Tag; `WorkspaceEntityKind` is the enum used by Navigation, AssetLink, ToolContext, and the Application layer; `WorkspaceSnapshot` aggregates all entities). Moving them into a capability folder would force every other capability to depend on that capability, so they belong at the shared root.

**Alternatives considered:**
- Move `WorkspaceEntity`/`WorkspaceEntityKind` into a `Shared/` or `Primitives/` folder — rejected: the standard discourages technical/catch-all folder names (`coding-standard.md:85`, `:114`), and `Workspace` is already the accepted namespace root.
- Put `WorkspaceSnapshot` in `Items/` (its largest collection) — rejected: the snapshot owns all entity collections and is workspace-scoped, not item-scoped.

### Decision 2: Capability folder assignment

Each type maps to the capability whose behavior or change reasons it shares. Cross-cutting types stay in `Workspace/` per Decision 1.

| Folder | Namespace | Files (after split) |
|---|---|---|
| `Workspace/` | `FusionCanvas.Domain.Workspace` | `Workspace.cs`, `WorkspaceDefaults.cs`, `WorkspaceEntity.cs`, `WorkspaceEntityKind.cs`, `WorkspaceSnapshot.cs` |
| `Stores/` | `FusionCanvas.Domain.Stores` | `Store.cs` |
| `Niches/` | `FusionCanvas.Domain.Niches` | `Niche.cs` |
| `Groups/` | `FusionCanvas.Domain.Groups` | `TopicGroup.cs`, `GroupHierarchy.cs` |
| `Items/` | `FusionCanvas.Domain.Items` | `Item.cs`, `ItemStatus.cs`, `ItemStatuses.cs`, `ItemOperationKind.cs`, `ItemEditDecision.cs`, `ItemStatusTransitionDecision.cs`, `ItemWorkflowPolicy.cs`, `ItemHierarchy.cs`, `ItemDisplayNameFormatter.cs` |
| `Tags/` | `FusionCanvas.Domain.Tags` | `Tag.cs`, `ItemTag.cs` |
| `Assets/` | `FusionCanvas.Domain.Assets` | `Asset.cs`, `AssetKind.cs`, `AssetLink.cs`, `WorkspaceFileReference.cs` |
| `Prompts/` | `FusionCanvas.Domain.Prompts` | `Prompt.cs` |
| `Workflow/` | `FusionCanvas.Domain.Workflow` | `WorkflowStage.cs`, `WorkflowStages.cs` |
| `Navigation/` | `FusionCanvas.Domain.Navigation` | `NavigationNodeRole.cs`, `NavigationNode.cs`, `NavigationTopicReference.cs`, `NavigationTreeSnapshot.cs`, `WorkspaceNavigation.cs` |

**Rationale for non-obvious placements:**
- `ItemTag` → `Tags/` (not `Items/`): it is the join between an item and a tag and is consumed by tag-application logic; it changes with tag linking behavior.
- `AssetLink` → `Assets/` (not `Workspace/`): it is the join that anchors an asset to an entity and is consumed by asset-linking behavior.
- `WorkspaceFileReference` → `Assets/`: its only domain consumer is `Asset` (the `Asset` constructor calls `WorkspaceFileReference.Normalize`). The application and integration layers reference it for file storage, but the domain concept exists to support asset paths.
- `ItemStatus`/`ItemStatuses` → `Items/`: they describe the item lifecycle, distinct from the workflow stage.
- `WorkflowStage`/`WorkflowStages` → `Workflow/`: workflow stages are a distinct concept referenced by `Item`, `ItemWorkflowPolicy`, and the application layer; they change for their own reasons (new stages), not with item rules.

**Alternatives considered:**
- Put `WorkflowStage` in `Items/` — rejected: stages are referenced by non-item code (tool host, navigator) and evolve independently.
- Put `WorkspaceFileReference` in a `Files/` folder — rejected: the domain has no other file concepts; `Files/` would be a single-file folder that the standard's example assigns to the Integration layer (`coding-standard.md:74`), not Domain.

### Decision 3: Multi-type file splits

The four remaining multi-type Domain files split as follows, each new file containing exactly one top-level type:

- `NavigationTree.cs` (5 types) → `NavigationNodeRole.cs`, `NavigationNode.cs`, `NavigationTopicReference.cs`, `NavigationTreeSnapshot.cs`, `WorkspaceNavigation.cs` (all in `Navigation/`).
- `ItemWorkflowPolicy.cs` (4 types) → `ItemOperationKind.cs`, `ItemEditDecision.cs`, `ItemStatusTransitionDecision.cs`, `ItemWorkflowPolicy.cs` (all in `Items/`).
- `WorkspaceRelationships.cs` (6 types) → `ItemStatus.cs` + `ItemStatuses.cs` (in `Items/`), `AssetKind.cs` (in `Assets/`), `WorkspaceEntityKind.cs` (in `Workspace/`), `ItemTag.cs` (in `Tags/`), `AssetLink.cs` (in `Assets/`). The vague file name is retired per `coding-standard.md:114`.
- `WorkflowStage.cs` (2 types) → `WorkflowStage.cs` + `WorkflowStages.cs` (in `Workflow/`).

**Rationale:** §2 requires one primary type per file and forbids grouping by technical category. The enum + companion lookup-class pairs (`ItemStatus`/`ItemStatuses`, `WorkflowStage`/`WorkflowStages`) are technically two top-level types and split for consistency, even though they are tightly coupled.

### Decision 4: Update consumers with `using` directives, not full qualification

**Choice:** In `Application`, `Integration`, `App`, and the four test projects, add `using` directives for the new namespaces and remove the old `using FusionCanvas.Domain.Workspace;` where it is no longer the sole namespace. Keep fully-qualified references only where they resolve genuine ambiguity.

**Rationale:** Matches existing style (the codebase already uses `using FusionCanvas.Domain.Workspace;` rather than full qualification). Adding a handful of `using` directives per file is the minimal, readable change.

**Alternatives considered:**
- Global `using` directives in a `GlobalUsings.cs` — rejected: not currently used in the repo; introducing it here would mix a style change with a mechanical move (§16 step 4: separate mechanical moves from behavior changes).

### Decision 5: No staged namespace compatibility shims

**Choice:** Do not keep the old `FusionCanvas.Domain.Workspace` namespace on moved types via `[assembly: InternalsVisibleTo]` or `using Alias`-based forwarding. Do a clean break in one change.

**Rationale:** The Domain project has no external consumers; the only references are in this repo's `Application`, `Integration`, `App`, and test projects, all updated in this same change. `coding-standard.md:87` permits a staged migration only "when compatibility requires it," which it does not here.

## Risks / Trade-offs

- **[Risk] Missed `using` directive breaks compilation.** → Mitigation: run `dotnet build .\FusionCanvas.sln` after each capability folder's move (per the Implementation Plan sequencing); fix references before proceeding to the next folder. Each step ends green.
- **[Risk] Test files reference old namespaces and break.** → Mitigation: move and update test `using` directives in the same step as their production counterpart.
- **[Risk] A type ends up in the wrong capability folder and must move again later.** → Mitigation: the mapping in Decision 2 is decided now; reviewers check it before implementation. The shared `Workspace/` root absorbs genuinely cross-cutting types, so no type is forced into an ill-fitting capability.
- **[Trade-off] Large diff for a mechanical change.** → Accepted: §16 explicitly authorizes large cleanup as a dedicated maintenance change with verification after each bounded step. The diff is mostly file renames and `using` edits, which reviewers can scan by capability folder.
- **[Trade-off] Namespace moves touch every project in one change.** → Preferred over a half-migrated state where some types are in `Items/` and others still in `Workspace/`; partial migration would violate §1 in two ways at once.

## Migration Plan

This is a code-internal refactor with no persistence or runtime migration. Deployment is a single pull request; there is no runtime state to migrate and no rollback beyond reverting the commit.

### Implementation Plan (sequenced by capability, each step ends with green build + tests)

Each step is a bounded mechanical move: create the target folder, move/rename the file(s), update the file-scoped `namespace`, split multi-type files where applicable, update `using` directives across all consumers, then run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`.

1. **Shared `Workspace/` root** — leave `Workspace.cs`, `WorkspaceDefaults.cs`, `WorkspaceEntity.cs`, `WorkspaceSnapshot.cs` in place. Split `WorkspaceEntityKind` out of `WorkspaceRelationships.cs` into `Workspace/WorkspaceEntityKind.cs`. Remove the now-empty `WorkspaceRelationships.cs`. Update consumers.
2. **`Stores/`** — move `Store.cs`. Update consumers.
3. **`Niches/`** — move `Niche.cs`. Update consumers.
4. **`Groups/`** — move `TopicGroup.cs` and `GroupHierarchy.cs`. Update consumers.
5. **`Items/`** — move `Item.cs`, `ItemHierarchy.cs`, `ItemDisplayNameFormatter.cs`. Split `ItemWorkflowPolicy.cs` into `ItemOperationKind.cs`, `ItemEditDecision.cs`, `ItemStatusTransitionDecision.cs`, `ItemWorkflowPolicy.cs`. Split `ItemStatus.cs` + `ItemStatuses.cs` out of `WorkspaceRelationships.cs`. Update consumers.
6. **`Tags/`** — move `Tag.cs`. Split `ItemTag.cs` out of `WorkspaceRelationships.cs`. Update consumers.
7. **`Assets/`** — move `Asset.cs`, `WorkspaceFileReference.cs`. Split `AssetKind.cs` and `AssetLink.cs` out of `WorkspaceRelationships.cs`. Update consumers.
8. **`Prompts/`** — move `Prompt.cs`. Update consumers.
9. **`Workflow/`** — split `WorkflowStage.cs` into `WorkflowStage.cs` + `WorkflowStages.cs` and move both. Update consumers.
10. **`Navigation/`** — split `NavigationTree.cs` into `NavigationNodeRole.cs`, `NavigationNode.cs`, `NavigationTopicReference.cs`, `NavigationTreeSnapshot.cs`, `WorkspaceNavigation.cs` and move all. Update consumers.
11. **Test mirror** — move test files under `tests/FusionCanvas.Domain.Tests/` into matching capability folders (`Items/`, `Groups/`, `Navigation/`, `Workflow/`, `Assets/`, `Workspace/`) and update their `using` directives. Run the full baseline once more.

### Decisions not to reopen

- The capability folder mapping (Decision 2) is final for this change; a type that seems misplaced gets corrected in a follow-up, not relitigated mid-implementation.
- No spec deltas are written for this change. If review concludes a spec-level requirement is affected, the change is paused and rerouted through the behavior-change workflow.
- The follow-up `Application`/`Integration` folder reorganization is a separate change; do not expand scope here.

### Verification approach

- **Build:** `dotnet build .\FusionCanvas.sln` after each step.
- **Tests:** `dotnet test .\FusionCanvas.sln` after each step. All 482 tests must remain green; no test logic changes, only `using` directives and file locations.
- **OpenSpec validation:** `openspec validate` after the final step to confirm no spec drift was introduced.
- **Coding-standard spot check:** confirm no `Workspace/` folder contains more than the agreed shared-root types, and no Domain file contains more than one top-level type (excluding the permitted exceptions in §2: private nested types).
- **Acceptance scenarios:** the existing `NavigationTreeTests`, `ItemWorkflowPolicyTests`, `ItemHierarchyTests`, `ItemLifecycleStatusTests`, `WorkflowStageTests`, `WorkspaceFileStorageModelTests`, and `DomainPersistenceBoundaryTests` continue to pass unchanged — they are the behavioral verification that the refactor preserved behavior. No new desktop or manual verification is warranted for a pure namespace move; UX preflight is not applicable.

## Open Questions

None. The mapping, sequencing, and verification approach are decided. Any unresolved placement discovered during implementation is returned for review rather than guessed.
