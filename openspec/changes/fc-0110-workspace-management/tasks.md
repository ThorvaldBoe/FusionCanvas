## 1. Domain Model

- [x] 1.1 Add a user-facing Workspace domain record with identity, name, optional description, archive state, timestamps, and metadata JSON.
- [x] 1.2 Add WorkspaceId ownership to Store and update constructors, sample data, and domain tests.
- [x] 1.3 Update WorkspaceSnapshot to include workspaces and ensure empty/default snapshot behavior remains explicit.
- [x] 1.4 Add or update relationship tests that prove the hierarchy is Workspace -> Store -> Niche/Group/Listing.

## 2. Application Services

- [x] 2.1 Add workspace management request, summary, state, result, and service contract types.
- [x] 2.2 Implement WorkspaceManagementService create, update, archive, restore, typed-confirm delete, load, and select behavior.
- [x] 2.3 Update StoreManagementService to require or receive active workspace context and filter all store lists to that workspace.
- [x] 2.4 Update store creation, rename validation, restore validation, and first-store state to operate per workspace.
- [x] 2.5 Update NicheManagementService and context resolution paths to validate that selected stores belong to the active workspace.
- [x] 2.6 Add application tests for workspace lifecycle, duplicate names, delete restrictions, workspace-scoped store uniqueness, and active child reset behavior.

## 3. SQLite Persistence

- [x] 3.1 Bump the SQLite schema version and add a workspaces table.
- [x] 3.2 Add workspace_id persistence for stores and ensure store load/save round-trips workspace ownership.
- [x] 3.3 Implement migration from pre-workspace databases by creating a default workspace and assigning existing stores to it.
- [x] 3.4 Preserve transaction safety for workspace and store saves, including invalid store ownership failures.
- [x] 3.5 Add integration tests for new database initialization, workspace round-trip, pre-workspace migration with stores, and migration with no stores.

## 4. App Shell and View Models

- [x] 4.1 Add workspace management view model state and commands for selecting, creating, editing, archiving, restoring, and deleting workspaces.
- [x] 4.2 Wire MainWindowViewModel so active workspace changes refresh store selection, niche state, navigation contexts, document context, and tool context safely.
- [x] 4.3 Add a compact workspace indicator and management entry point above the store selector in the main window sidebar.
- [x] 4.4 Update empty states so no-workspace, empty-workspace, and empty-store situations are distinct.
- [x] 4.5 Add app tests for workspace management, switching, first-store prompting per workspace, and hiding stores from other workspaces.

## 5. Documentation and Verification

- [x] 5.1 Update data model and architecture documentation to describe Workspace as the user-facing top-level scope.
- [x] 5.2 Run domain, application, integration, and app tests.
- [x] 5.3 Run the solution build.
- [x] 5.4 Review the OpenSpec change status and confirm all required artifacts are implementation-ready.
