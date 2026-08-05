# Proposal: workspace-transfer

## Why

FusionCanvas workspaces are currently trapped inside one installation: there is no way to move a workspace (with all its stores, niches, groups, items, assets, prompts, tags, and managed image/design files) to another machine, restore it from a backup, or hand a self-contained copy to a collaborator. Because every workspace's structured data lives in one SQLite database and its files live under one managed root with workspace-relative references, the app is already well-prepared for a portable, single-file workspace package — this change turns that latent capability into a user-facing export/import feature.

## What Changes

- **Workspace export**: from the workspace-management dialog, export any workspace (active or archived) to a single portable `.fcworkspace` package (a ZIP archive) containing a manifest, a filtered SQLite database holding exactly that workspace's subgraph at the current schema version, and every managed file referenced by the workspace's assets. Archived entities are included (full fidelity). Assets whose managed file is missing are exported as records and noted in the manifest.
- **Workspace import**: from the workspace-management dialog — and as a secondary action on the no-workspace overlay in the main window — import a `.fcworkspace` package as a restored workspace. The imported top-level workspace is activated so it can become the current scope, while descendant archive states are preserved. Entity identities are preserved; import of a package whose entities already exist is refused (import is one-shot transfer/restore, never a merge or sync). Workspace name conflicts with active workspaces are resolved by automatic suffixing. Files already present at their destination paths are skipped (supports delete-then-restore over orphaned files). Newer package format or schema versions are refused with a clear message; older schemas migrate through the existing SQLite migration chain.
- **Transfer UX**: both operations run asynchronously with progress reporting and cancellation; cancellation and failure leave no partial workspace behind (best-effort file cleanup, single-transaction snapshot save). A completion summary reports imported/exported counts and warnings (missing files, skipped files, dropped cross-workspace links, renamed workspace). After a successful import the imported workspace becomes active.
- **Compatibility policy**: a documented requirement in `docs/data-model.md` stating that workspace packages and local databases share one backward-compatibility obligation — every shipped schema version must remain migratable by future versions, and any deliberately breaking change must ship with an explicit migration path. The package manifest carries `formatVersion`, `schemaVersion`, and `appVersion` so importers can pre-flight and refuse safely.
- **Security handling**: package extraction validates entry paths against traversal (zip-slip), enforces the existing creative-asset extension allowlist for extracted files (violating files are skipped, their assets import as missing, and the summary warns), and streams content rather than loading packages into memory.

Non-goals: whole-app backup (all workspaces in one package — the format must not preclude it later), merge/update/sync of an existing workspace, per-store or per-entity export, cloud transfer, OS file-type association, marketplace export packaging.

## Capabilities

### New Capabilities

- `workspace-transfer`: User-facing export of one workspace to a portable package file and import of a package as a restored workspace, including package format and manifest expectations, identity-preservation and one-shot import rules, name-conflict resolution, missing/skipped file handling, version refusal, progress and cancellation, failure atomicity, and dialog/overlay entry points.

### Modified Capabilities

- `local-sqlite-persistence`: the Phase 0 scope-exclusion requirement currently excludes "full backup/restore, import/export packages". It is narrowed so single-workspace transfer packages are now in scope (whole-app backup/restore remains excluded).

## Impact

- **Domain** (`FusionCanvas.Domain`): new pure, framework-free logic — workspace subgraph filtering (the inverse of the existing workspace-delete cascade filter) and import pre-flight policy (identity-collision detection, name-conflict resolution).
- **Application** (`FusionCanvas.Application`): new `IWorkspaceTransferService` use case plus package reader/writer ports, result/summary types, and progress plumbing; no changes to existing service contracts.
- **Integration** (`FusionCanvas.Integration`): ZIP package writer/reader (`System.IO.Compression`), temporary-database export via the existing `SqliteWorkspaceRepository`, streaming file copies with allowlist and traversal enforcement.
- **App** (`FusionCanvas.App`): export/import commands, busy/progress/cancel and summary states in `WorkspaceManagementWindow` + `WorkspaceManagementViewModel`; one secondary import button on the no-workspace overlay in `MainWindow.axaml` bound to the same shared view-model command. No persistent main-window surface is added.
- **Docs**: new compatibility-policy section in `docs/data-model.md`.
- **Specs**: new `workspace-transfer` capability spec; narrowed exclusion in `local-sqlite-persistence`.
- **Tests**: domain rule tests (filter, policy), application use-case tests with deterministic fakes, integration round-trip tests (export → wipe → import → structural equality) with isolated temporary roots, and headless view tests for the dialog and overlay entry points.
- **Verification approach**: criterion-level mapping of every acceptance scenario to a deterministic `dotnet test` result; round-trip integration tests are the core evidence; no interactive desktop required.

### UX preflight summary

- **Who/how often**: a creator migrating machines, restoring a backup, or sharing a workspace. Both actions are rare, administration-class operations.
- **Placement**: focused surface (workspace-management dialog) per the "protect the primary workspace" rule; zero persistent main-window area. The single main-window addition is a secondary import action on the no-workspace overlay, which renders only when zero workspaces exist — the exact first-run restore moment.
- **States**: initial (buttons enabled), in-progress (progress + cancel, all other transfer actions disabled), success (summary, imported workspace selected, overlay dismisses itself), blocked (duplicate identities, newer format/schema — with explanation), recoverable errors (context preserved, no partial state), cancellation (clean rollback of copied files).
- **Focus/keyboard**: file pickers are keyboard-reachable via the platform storage provider; focus returns to the invoking button when the operation ends.
- **Editing safety**: no drafts or unsaved changes; export is read-only against live state; import is additive only and never overwrites existing records.
