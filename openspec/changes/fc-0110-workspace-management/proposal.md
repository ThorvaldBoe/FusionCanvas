## Why

FusionCanvas currently treats stores as the highest-level working context, which mixes private, client, and multi-brand work in one store list. Users need a higher-level workspace boundary so separate settings such as personal projects, client work, and large client portfolios can stay visually and logically isolated.

## What Changes

- Introduce user-facing workspaces as the top-level organizational scope above stores.
- Allow users to create, select, rename, archive, restore, and delete workspaces.
- Associate every store with exactly one workspace, while preserving existing store, niche, group, listing, asset, prompt, tag, and navigation behavior below the selected workspace.
- Add an automatic default workspace for existing data so current users keep their stores after migration.
- Scope active store selection, store management, navigation, and context-aware work to the selected active workspace.
- Update the main workspace shell to show a compact active workspace indicator and move workspace switching/management into a dedicated management window.
- Update local SQLite persistence to store workspaces, persist store workspace ownership, and migrate existing databases safely.

## Capabilities

### New Capabilities

- `workspace-management`: Defines creating, selecting, editing, archiving, restoring, and deleting user-facing workspaces as the top-level organizational scope.

### Modified Capabilities

- `core-domain-model`: Adds Workspace to the core model and changes Store from the highest-level context to a workspace-scoped business context.
- `store-management`: Scopes store creation, selection, uniqueness, editor lists, and first-store prompting to the active workspace.
- `local-sqlite-persistence`: Persists workspaces, store workspace ownership, and the migration from pre-workspace databases.
- `desktop-application-foundation`: Updates the main shell expectations so the regular workspace UI includes lightweight workspace selection above store selection.

## Impact

- Domain: add a Workspace entity and WorkspaceId ownership on Store.
- Application: add workspace management service/contracts and pass active workspace context into store and niche flows.
- Integration: bump SQLite schema version, add workspaces table, add store workspace ownership, and backfill a default workspace for existing stores.
- UI: add a compact workspace management entry point in the main window and coordinate workspace changes with active store, niche, navigation, document, and tool context state.
- Tests: add domain, application, integration, and app coverage for workspace lifecycle, migration, scoping, and selector behavior.
