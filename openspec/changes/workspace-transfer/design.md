# Design: workspace-transfer

## Context

A FusionCanvas installation's entire portable state is one `WorkspaceSnapshot` (structured data, one SQLite database shared by all workspaces) plus the managed `workspace-files` root (binaries referenced by workspace-relative paths). Existing constraints make a transfer feature cheap to add correctly:

- `IWorkspaceRepository` is a two-method full-snapshot contract (`SaveAsync`/`LoadAsync`); saves are a single transaction (delete-all + reinsert), and schema versioning with stepwise migrations and refuse-newer already exists (`PRAGMA user_version`, currently v5).
- Workspace file references are workspace-relative and machine-independent by spec; every managed file (including per-item design PNGs, which are ordinary `Asset` rows) is reachable from an `Asset` record.
- The workspace-delete cascade in `WorkspaceManagementService` already implements the "everything owned by workspace X" filter — the export filter is its inverse.
- Workspace deletion removes records only; managed files remain on disk as orphans. Restore flows must tolerate files already present at packaged paths.
- The workspace-management dialog (`WorkspaceManagementWindow` + shared `WorkspaceManagementViewModel`) is the accepted focused surface for workspace administration; the main window's no-workspace overlay binds the same view model.

Stakeholders: creators migrating machines, restoring backups, or sharing a self-contained workspace; future contributors changing the schema, who inherit the documented compatibility obligation.

## Goals / Non-Goals

**Goals:**

- Export any single workspace (active or archived) to one portable `.fcworkspace` package: manifest + filtered SQLite DB + referenced managed files.
- Import a valid package as a restored workspace with preserved identities, relationships, metadata, and file references.
- One-shot import: refuse duplicate identities; never merge/update/sync.
- Safe pre-flight (manifest), hardened extraction (traversal + extension allowlist, streaming), progress + cancellation, no partial state after failure or cancel.
- Entry points confined to the workspace-management dialog plus a secondary import action on the no-workspace overlay; zero persistent main-window surface.
- Documented backward-compatibility policy covering both local databases and packages.

**Non-Goals:**

- Whole-app backup (all workspaces in one package) — the manifest must not preclude it later, but nothing is built for it.
- Merge, update, diff, or sync of an existing workspace from a package.
- Per-store/per-entity export, cloud transfer, OS file-type association, marketplace export packaging.
- Content hashing of packaged files; repairing missing files; deduplication.

## Decisions

### D1 — Structured payload is an embedded SQLite database, not JSON DTOs

The package contains a filtered SQLite DB written by the **existing** `SqliteWorkspaceRepository` against a temporary path (schema auto-created at the current version). Import opens it with the same repository class, so the existing `user_version` migration chain handles old packages exactly like old local databases.

*Alternatives considered:* JSON DTO dump (rejected: a permanent parallel mapping layer for 10 entity types, its own versioning discipline, and per-entity-change maintenance, for a format that is app-to-app only). The embedded-DB choice means the documented compatibility policy (D10) is one rule covering databases and packages alike.

### D2 — Identities are preserved; import is one-shot

Import keeps every entity's original `Guid`. If any packaged entity identity already exists in the live snapshot, the import is refused before any file is copied. No ID remapping machinery is built.

*Rationale:* restore and transfer are the real workflows; remap-on-import exists mainly to support importing the same package twice as a template, which was explicitly deprioritized. *Consequence (spec'd, not to reopen):* re-importing a newer export of the same workspace is refused — import is transfer/restore, never update or sync.

### D3 — Packaged files restore at their exact workspace-relative paths, skip-if-exists

Asset rows pass through untouched; files copy to their original relative paths. A destination that already exists is kept (not overwritten, not an error) and counted in the summary — this makes "delete workspace (files orphaned) → re-import package" work, and is safe because paths embed the GUID minted at original import, so an existing path implies the same file.

*Alternatives:* fail on collision (breaks the legitimate restore flow); overwrite (trusts package bytes over local bytes for no benefit).

### D4 — Name conflicts auto-suffix

A restored workspace whose normalized name collides with an **active** workspace gets an automatic unique suffix (e.g. `My Brand (2)`), reported in the summary. Conflict with an archived-only name imports under the original name, matching existing workspace-management uniqueness rules.

### D5 — Missing files never block a transfer

Export of an asset whose file is gone: record included, noted in manifest and summary; the asset shows as missing after import. Import of a file violating the extension allowlist: file skipped, asset imports as missing, summary warns. Missing-state presentation already exists and needs no changes.

### D6 — Extraction is hardened and streamed

ZIP handling uses `System.IO.Compression` (no new dependency). Every entry path is validated with the same normalization rules as `WorkspaceFileReference` (reject rooted paths and `.`/`..`) before writing; writes stay inside the managed root boundary; extracted files must match the **existing** creative-asset extension allowlist (images plus fonts/design sources/PDF — an image-only list would make font and design-source assets non-round-trippable, violating full-fidelity export); all copies stream with cancellation checks.

### D7 — Atomicity follows existing idioms

- Export writes the package to a temporary file and moves it onto the destination only when complete (the `JsonApplicationSettingsStore` temp+move idiom); cancel/failure deletes the temp file.
- Import copies files first, then merges snapshots and saves once (single transaction). Save failure or cancellation removes the files copied for that import on a best-effort basis (the `AssetManagementService` copy-then-save-then-cleanup idiom). Import never modifies or deletes pre-existing records or files.

### D8 — Entry points: workspace dialog + shared-command overlay action

Export lives in the workspace-management edit panel for the selected workspace; import lives in the dialog's list rail. The main window's no-workspace overlay gains one secondary import button bound to the **same** `ImportWorkspaceCommand` on the shared view model — failure or refusal opens the workspace-management surface with the error; success flips `ShouldShowNoWorkspaceState` and the overlay dismisses itself. File picking uses an `IWorkspacePackagePicker` abstraction over Avalonia `StorageProvider` (the `IAssetFilePicker` pattern) so view-model tests stay headless.

### D9 — Progress and cancellation are first-class

Both operations accept `IProgress<WorkspaceTransferProgress>` (phase, completed units, total units; file bytes or file count as the unit) and a `CancellationToken`. While a transfer runs, other transfer and workspace-mutation commands in the dialog are disabled; a cancel command is offered. Cancellation is cooperative and always ends in a clean state per D7.

### D10 — One documented compatibility obligation

`docs/data-model.md` gains a "Workspace package and database compatibility" section: every shipped schema version must remain migratable by future versions (the rule already honored for local DBs, now covering packages); a deliberately breaking schema or container change must be stated explicitly and ship with a migration path. The manifest carries `formatVersion` (container; refuse newer), `schemaVersion` (informational pre-flight so refusal happens before opening the DB; the DB's own `user_version` remains authoritative), and `appVersion` (diagnostics).

## Package format (v1)

```
<WorkspaceName>-<yyyyMMdd>.fcworkspace   (ZIP)
 ├─ manifest.json      formatVersion=1, schemaVersion, appVersion,
 │                     workspaceId, workspaceName, exportedAtUtc,
 │                     entityCounts, files[{path,size}], missingFiles[],
 │                     droppedLinkCount
 ├─ workspace.db       filtered snapshot, SQLite, current schema version
 └─ files/<workspace-relative paths…>
```

## Risks / Trade-offs

- **Hostile or corrupt package** → manifest pre-flight, traversal and allowlist guards, streaming, refusal before any state change; corrupt-DB errors mapped to recoverable failures.
- **`user_version` refuse-newer throws from the repository** → pre-check `schemaVersion` in the manifest and translate repository version errors into the friendly "requires a newer FusionCanvas" refusal instead of an exception.
- **Large workspaces** → streaming everywhere; progress in file units; no whole-package memory residency. Accept that very large exports are bounded by disk speed only.
- **Skip-if-exists could mask a genuinely different file at the same path** → accepted: the path's embedded GUID makes a different-file collision practically impossible outside tampering; skips are counted in the summary.
- **Concurrent mutation between import's load and save** → accepted: same load-mutate-save pattern as every existing service; single-writer UI orchestration.
- **In-flight `reorganize-*-capability-folders` changes** → new code follows the post-reorganization capability-folder conventions; expect path churn in Domain/Application/Integration and place new types accordingly at implementation time.
- **Format coupling to internal schema** → mitigated by D10's documented policy; a future breaking schema release must provide the migration path for packages and databases together.

## Implementation Plan

### Domain (`FusionCanvas.Domain`, new `Workspace/Transfer` capability area)

- `WorkspaceSnapshotFilter.ForWorkspace(WorkspaceSnapshot, Guid workspaceId)` → filtered `WorkspaceSnapshot` plus a `DroppedAssetLinks` report. Ownership rule: entities belong via `Store.WorkspaceId`; an `AssetLink` is included only when both its asset and its target entity are included (defends against cross-workspace links, which the UI never creates but the model permits). Pure, framework-free.
- `WorkspaceImportPreflight` — pure policy functions: `FindIdentityCollisions(live, package)` (any shared entity ID across all lists) and `ResolveImportName(packageName, activeWorkspaceNames)` (original or suffixed unique name; archived names ignored).
- Tests: `tests/FusionCanvas.Domain.Tests` — filter completeness vs. the delete-cascade behavior, link dropping, collision detection, suffix rules.

### Application (`FusionCanvas.Application`, new `Workspaces/Transfer` area)

- `IWorkspaceTransferService` with `ExportWorkspaceAsync(WorkspaceExportRequest, IProgress<WorkspaceTransferProgress>, CancellationToken)` and `ImportWorkspaceAsync(WorkspaceImportRequest, …)` returning result records (`WorkspaceTransferResult` with `WorkspaceTransferSummary`: counts, warnings, final name; failure carries a recoverable message).
- Ports: `IWorkspacePackageWriter` (filtered snapshot + file source + manifest → destination path) and `IWorkspacePackageReader` (package path → manifest, snapshot, validated file entries to restore). Manifest DTO `WorkspacePackageManifest` and `WorkspaceTransferProgress` live here as contracts.
- Service orchestration (import): load live snapshot → pre-flight identity collision → resolve name → restore files via file store (skip-if-exists, progress, cancel) → merge lists → `SaveAsync` once → cleanup-on-failure. Export: filter → writer (which owns temp DB + zip + move) → summary.
- Extend `IWorkspaceFileStore` additively with a restore operation, e.g. `Task<WorkspaceFileRestoreOutcome> RestoreAsync(string workspaceRelativePath, Stream content, CancellationToken)` that validates the path, writes only when absent, and reports `Created`/`SkippedExisting`. Update `InMemoryWorkspaceFileStore` and contract tests accordingly.
- Tests: `tests/FusionCanvas.Application.Tests` — service tests with fake package ports and the in-memory file store; file-store contract tests for the restore operation.

### Integration (`FusionCanvas.Integration`, new `Packages` area)

- `ZipWorkspacePackageWriter` / `ZipWorkspacePackageReader` (`System.IO.Compression`): temp-dir workspace, filtered DB via `SqliteWorkspaceRepository` at a temp path, manifest (de)serialization, streamed entries, temp+move destination write, traversal/allowlist enforcement on read, migration-on-open for older embedded schemas, friendly refusal for newer versions or corrupt packages.
- `LocalWorkspaceFileStore.RestoreAsync` implementing the new contract with the existing root-boundary guard.
- App version from assembly informational version.
- Tests: `tests/FusionCanvas.Integration.Tests` — round-trip (build snapshot with real files in a temp root → export → import into an empty repository+root → structural equality incl. metadata and paths), missing-file export, skip-if-exists, traversal entry refusal, allowlist skip, newer-version refusal, older-schema migration (construct a package whose embedded DB is downgraded via direct SQL in the test), corrupt package handling, cancel mid-copy.

### App (`FusionCanvas.App`)

- `WorkspaceManagementViewModel`: `ExportSelectedWorkspaceCommand`, `ImportWorkspaceCommand`, `CancelTransferCommand`; `IsTransferRunning`, `TransferProgress`, summary/error surface; disable conflicting commands while running; import failure from any entry point surfaces `ErrorMessage` and opens the management window.
- `IWorkspacePackagePicker` + Avalonia implementation (save dialog for export with `.fcworkspace` filter and default `<name>-<yyyyMMdd>` file name; open dialog for import).
- `WorkspaceManagementWindow.axaml`: Export button in the selected-workspace action row; Import button in the list rail; compact progress bar + cancel + summary area (progressive disclosure, fixed-width buttons per UI guidelines).
- `MainWindow.axaml` no-workspace overlay: one secondary "Import workspace…" button bound to the shared import command.
- `MainWindowViewModel`: on successful import, refresh the snapshot and select the restored workspace (existing `SwitchWorkspace`/refresh pattern); the overlay dismisses itself via `ShouldShowNoWorkspaceState`.
- Tests: `tests/FusionCanvas.App.Tests` — view-model command/state tests (fake picker + service) and headless view tests: dialog exposes export/import, progress/cancel state disables actions, overlay button exists only in the no-workspace state and triggers the same command, failure routes to the management surface.

### Docs

- `docs/data-model.md`: new "Workspace package and database compatibility" section (D10).

### Sequencing

1. Domain filter + preflight policy + tests.
2. Application contracts/DTOs + file-store restore contract (+ in-memory update) + tests.
3. Integration zip writer/reader + file-store restore + round-trip tests.
4. Application transfer service orchestration + tests.
5. App view-model, dialog UI, overlay action, picker + headless view tests.
6. Docs policy section; full-suite run; verification mapping.

### Verification mapping

Every acceptance scenario in `specs/workspace-transfer/spec.md` maps to a deterministic `dotnet test` case at the lowest reliable layer (domain rule tests, application use-case tests, integration round-trip/boundary tests, headless view tests for UI placement and state). Live desktop checks are optional supplemental evidence only; no acceptance scenario requires an interactive display. The `local-sqlite-persistence` delta is documentation-level and is verified by review plus the existing persistence test suite remaining green.

### Decisions not to reopen during implementation

Embedded-SQLite payload (D1), identity preservation and one-shot import (D2), skip-if-exists (D3), auto-suffix (D4), missing-file philosophy (D5), allowlist scope (D6), entry-point placement (D8), documented-policy approach to constraint 3 (D10).

## Open Questions

None. All discovery decisions were resolved with the requester before this design was written.
