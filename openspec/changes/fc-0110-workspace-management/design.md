## Context

FusionCanvas currently uses "workspace" to describe the local database-backed creative data universe. Stores are the accepted top-level business context, and niches, groups, listings, assets, prompts, and tags are scoped through stores.

The new user need introduces a second, product-facing meaning of workspace: a top-level partition for different operating settings such as personal projects, client work, or a large client with several stores. This change must therefore add a user-facing Workspace concept while preserving the existing local-first repository and snapshot style.

## Goals / Non-Goals

**Goals:**

- Make Workspace the user-facing top-level organizational scope above stores.
- Keep existing stores and child records intact during migration by placing them in a default workspace.
- Scope normal store selection, store management, navigation, and context-aware work to the active workspace.
- Preserve the current local SQLite and snapshot-based persistence approach.
- Keep the first workspace UI lightweight and consistent with the existing store selector/editor model.

**Non-Goals:**

- Do not add multi-user permissions, sharing, cloud sync, workspace billing, or account identity.
- Do not split each workspace into a separate database file in this change.
- Do not add cross-workspace search or reporting.
- Do not change niche, group, listing, asset, prompt, or tag ownership unless needed to preserve store-scoped behavior.
- Do not perform a broad rename of existing `WorkspaceSnapshot`, repository, or namespace symbols solely for terminology cleanup.

## Decisions

### Decision: Store WorkspaceId is the isolation boundary

Add a `Workspace` domain entity and add `WorkspaceId` to `Store`. Keep niches, groups, listings, assets, prompts, and tags scoped to stores as they are today.

Rationale: most existing records already use `StoreId`, so the full containment chain can be derived as `Workspace -> Store -> child records`. This avoids stamping every table with `WorkspaceId` before there is a query or integrity need for it.

Alternative considered: add `WorkspaceId` to every domain entity. That would make some queries direct, but it increases migration and validation surface and risks divergent workspace ownership between a store and its children.

### Decision: One local database can contain multiple user-facing workspaces

Keep a single SQLite database and add a `workspaces` table. Existing databases migrate by creating one default workspace and assigning all existing stores to it.

Rationale: workspaces are an organizational boundary, not yet a physical storage or security boundary. A single database keeps backup, migration, repository contracts, and application startup simple.

Alternative considered: one database per workspace. That could simplify hard isolation later, but would complicate switching, global settings, migration, and existing repository assumptions now.

### Decision: Introduce workspace management as a sibling to store management

Create workspace application contracts and a `WorkspaceManagementService` that mirrors the lifecycle pattern used by store management: load, create, update, archive, restore, typed-confirm delete, and select active.

Rationale: workspace lifecycle is similar to store lifecycle but has different scoping rules. Keeping it separate avoids overloading `StoreManagementService` with two levels of responsibility.

Alternative considered: fold workspace behavior into store management. That would save files initially, but it would make active workspace and active store coordination harder to test and reason about.

### Decision: Active workspace controls active store validity

When the active workspace changes, active store and active niche state must be cleared or reselected only from stores and niches inside the selected workspace. Normal store lists, first-store prompts, and store editor lists are workspace-local.

Rationale: the core promise is that private stores do not appear while working in a client workspace. Any retained active child selection must be validated against the selected workspace.

Alternative considered: allow a global active store independent of workspace. That would preserve fewer state transitions, but it creates accidental cross-workspace leakage.

### Decision: Avoid broad terminology cleanup in the first change

The implementation may keep names such as `WorkspaceSnapshot`, `IWorkspaceRepository`, and `FusionCanvas.Domain.Workspace` even though the new product entity is also called Workspace.

Rationale: users should get the right product concept now. Internal cleanup can happen later if naming becomes painful, but a broad rename would expand the blast radius without adding behavior.

Alternative considered: rename the existing internal workspace terminology first. That would clarify code, but it would turn this feature into a large refactor plus behavior change.

## Risks / Trade-offs

- Terminology collision between user-facing Workspace and existing internal workspace names -> Mitigate with explicit design notes, focused naming in new service/contracts, and tests that describe user-facing behavior.
- Cross-workspace data leakage in UI lists or tool context -> Mitigate by filtering stores at the service boundary and validating active child selections after workspace changes.
- SQLite migration complexity for adding a non-null foreign key to existing stores -> Mitigate by creating a default workspace, backfilling existing stores, and using a safe schema version migration path.
- Deleting a workspace with hidden connected data could destroy substantial work -> Mitigate by requiring explicit permanent deletion confirmation and an exact typed workspace-name confirmation before deleting a non-empty workspace and its owned stores and child records.
- Snapshot-wide load/save may become less efficient with many workspaces -> Accept for now; this matches the current repository pattern and can be revisited when query-based repositories are introduced.

## Migration Plan

1. Bump SQLite schema version.
2. Create `workspaces` table if missing.
3. Create a default workspace for databases that predate workspace support.
4. Add or rebuild store persistence so every store has a `workspace_id`.
5. Backfill all existing stores to the default workspace.
6. Load and save snapshots with workspace records and store workspace ownership.
7. Reject databases with a newer schema version as before.

Rollback is not automatic. Once a database is migrated, older FusionCanvas versions without workspace support should refuse it because the schema version is newer.

## Open Questions

- What should the default migrated workspace be named: "Personal", "Default workspace", or something user-editable on first launch?
- Should archived workspaces appear in a dedicated workspace manager immediately, or is restore support enough through a simple management list?
- Should the main window close active tabs when switching workspaces, or keep them open but mark cross-workspace tabs inactive/unavailable?
