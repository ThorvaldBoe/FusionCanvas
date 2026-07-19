## Context

The accepted foundation already provides everything FC-0109 builds on: the domain defines `Asset` (store-owned record with kind, workspace-relative path, original source path, missing flag), `AssetKind`, and `AssetLink` (asset + `WorkspaceEntityKind` + entity id). `IWorkspaceFileStore.ImportAsync` copies a source file into `assets/yyyy/MM` under the workspace root with collision-safe naming and extension allow-listing, and `SqliteWorkspaceRepository` persists `assets` and `asset_links` inside the atomic `WorkspaceSnapshot`. Deletion guards in listing, niche, and store management already recognize asset links, and group permanent deletion removes links to deleted entities while preserving asset records. What does not exist anywhere is the user-facing workflow: no application service, no import command, no purpose labeling, no asset list.

UX preflight (per `docs/ux-guidelines.md`): the performer is a creator preserving existing files against their work. Import is occasional; reviewing a listing's assets is a regular read; relabeling is rare; removal is rare and destructive. Per the "protect the primary workspace" guideline, import/relabel/remove belong in a focused surface rather than permanently consuming tree or document area, and the main window keeps its context while that surface is open. Review must be reachable from the work it supports with one gesture. The FC-0106 Listing Inspector (parallel, unmerged) will eventually own in-document asset display; FC-0109 does not depend on it and uses the same focused-surface pattern as the store, group, and listing editors already on main.

## Goals / Non-Goals

**Goals:**

- Provide an application-owned asset management service over `WorkspaceSnapshot` for context asset listing, import, purpose relabeling, and confirmed removal.
- Import copies the file into managed workspace storage through the existing `IWorkspaceFileStore` port; the managed copy is authoritative and the original path is preserved only as metadata.
- Attach an imported asset to exactly one context at import: listing (primary), niche, group, or store.
- Show a context's assets with purpose labels and missing-file state in one focused Assets window reachable from tree context menus; the store context lists every store-owned asset with its context label so orphaned assets remain reachable.
- Reuse the existing domain records, `AssetKind` enum, file-storage port, and SQLite tables without schema changes.
- Persist every mutation atomically through one `IWorkspaceRepository.SaveAsync` call, and never leave an asset record pointing at a file that import failed to copy.
- Preserve asset relationships across listing/group movement structurally (links target stable identities).

**Non-Goals:**

- No batch import, automatic file matching, image processing, mockup generation, deduplication, version history, asset validation, or cloud sync (FC-0109 PRD out-of-scope).
- No multi-context linking or asset sharing UI; an asset has exactly one context link in this phase (asset relationships are FC-0204).
- No missing-file repair, relinking, or automatic cleanup of orphaned managed files.
- No asset rename, notes editing, preview/thumbnails, or in-document inspector panel.
- No changes to accepted deletion-guard or group-cascade semantics in listing/niche/store/group management.
- No tree or document-window footprint: no asset nodes in the navigation tree and no permanent panel.

## Decisions

### 1. Add an application-owned `IAssetManagementService`

The service exposes context asset state loading, import, purpose relabeling, and removal over `WorkspaceSnapshot`, following the established listing/group management pattern: reload the latest snapshot, validate the full operation, build one replacement snapshot, call `IWorkspaceRepository.SaveAsync` once. The view model owns presentation state and file-picker interaction only; it never mutates snapshots or touches SQLite.

Alternative considered: let the Assets window view model compose snapshots directly. Rejected because it would duplicate validation, bypass atomic persistence, and break the layer rule that application services own use cases.

### 2. Attach each asset to exactly one context at import

Import takes a context reference (`WorkspaceEntityKind` + id covering Listing, Niche, Group, or Store), validates that the target exists and is effectively active (for listings and groups, including ancestor activity via `GroupHierarchy`), then creates the store-owned `Asset` plus one `AssetLink` in the same snapshot. The asset's `StoreId` always matches the target's store. Multi-link sharing stays a future asset-relationships feature.

Alternative considered: allow attaching to several contexts during import. Rejected as premature complexity; the PRD requires association, not sharing, and FC-0204 owns relationships.

### 3. One focused Assets window for all four context kinds

A single non-modal Assets window, opened from a tree context-menu "Assets…" command on a listing, niche, group, or store, owns the whole feature surface: asset list (name, purpose, file name, missing state), Import… with a system file picker and purpose selector, a purpose drop-down per asset for relabeling, and a Remove… action behind inline confirmation. Topic and listing views show only assets linked to that context; the store view lists every store-owned asset with a derived context label ("Listing X", "Niche Y", "Store", or "—" for unlinked records left by group deletion cascades) so every asset stays reachable and removable. Empty states explain the feature and offer Import; busy states disable duplicate submission; errors are actionable and preserve the window's selection.

Alternative considered: embedding an asset panel into the listing properties window only. Rejected because niches, groups, and stores need the same attachment workflow, and one parameterized surface is smaller than four embedded panels.

### 4. Purpose labeling reuses `AssetKind` with extension-based pre-selection

The import flow asks for purpose explicitly and pre-selects a kind mapped deterministically from the file extension (`.afdesign/.psd/.ai/.eps` → SourceDesign, `.png/.jpg/.jpeg/.webp/.tif/.tiff` → ExportedImage, `.svg` → Svg, `.ttf/.otf` → Font, `.brush` → Brush, otherwise Unknown). The mapping is a small pure policy in the Application layer; the user can change it before confirming and relabel afterwards via the row's purpose selector, which persists immediately as its own atomic mutation. No new enum or purpose vocabulary is introduced; friendly labels are presentation concerns.

Alternative considered: inferring purpose silently without asking. Rejected because the PRD requires the user to identify purpose, and silent inference hides a decision the user should own.

### 5. Import is copy-then-save with cleanup on failure

Import calls `IWorkspaceFileStore.ImportAsync` first (which validates the source exists and the extension is supported, then copies), then reloads the snapshot, appends asset + link, and saves once. If the save fails, the service best-effort deletes the just-copied managed file so no orphan accumulates, and returns a recoverable error. Unsupported extensions and missing sources are blocked before any copy with clear messages. Re-importing the same source file creates a second independent asset; deduplication is out of scope.

Alternative considered: saving the record before copying. Rejected because a failed copy would leave a record pointing at nothing; copy-first keeps the persisted snapshot truthful.

### 6. Removal deletes record and managed file, save-first

Removal requires explicit confirmation, then removes the asset record and all its links in one snapshot save; only after a successful save does the service best-effort delete the managed file. This order guarantees the file still exists whenever a save fails, so no user file is lost to a persistence error; a failed file deletion leaves benign disk residue consistent with the no-automatic-cleanup non-goal. Removal unblocks the accepted permanent-deletion guards for listings, niches, and stores.

Alternative considered: detach-only removal that keeps the record. Rejected because Phase 1 has no other surface to reach an unlinked non-store asset, which would strand invisible records.

### 7. Missing files are detected at view time, never repaired

When loading context assets, the service checks each reference through `IWorkspaceFileStore.Exists` and reports the missing flag on the summary; the UI shows a distinct Missing state and keeps Remove available. The persisted `is_missing` column is left untouched (no background rewriting), and no repair/relink flow exists in this phase.

Alternative considered: persisting missing state on every load. Rejected as unnecessary writes for a purely derived view concern.

## Risks / Trade-offs

- [File copy happens outside the SQLite transaction] → Copy first, save once, delete the copied file on save failure; accept that a crash mid-copy can leave an unreferenced file until a future cleanup feature.
- [Group deletion cascades unlink assets into store-level orphans] → Store view lists all store-owned assets with a "—" context label and working Remove, matching the accepted group-management semantics without changing them.
- [AssetKind vocabulary is broader than the PRD's purpose list] → Present friendly labels for all kinds but default sensibly; extra kinds (PromptOutput, ExternalLink, Unknown, Other) remain valid for future features.
- [Snapshot replacement can overwrite concurrent mutations] → Reload immediately before mutation and serialize through application services, same as listing/group management; optimistic concurrency remains future work.
- [One-link-per-asset may frustrate sharing across listings] → Explicit non-goal deferred to FC-0204 asset relationships; the link model already supports many links so the future change is additive.
- [FC-0106 Listing Inspector lands later in the document window] → Keep FC-0109 surface-independent in a focused window; the inspector can reuse `IAssetManagementService` read paths without rework.

## Migration Plan

1. Add asset-management application contracts and the service, reusing existing domain records, file store port, and repository tables; no schema migration.
2. Cover service behavior with application tests (fake file store + in-memory repository) and integration tests (temporary SQLite database + temporary workspace root).
3. Add the Assets window, view model, and tree context-menu wiring in the App layer with view-model tests.
4. Run the full baseline (`dotnet test .\FusionCanvas.sln`), then a real desktop UI verification pass against a disposable workspace per the testing baseline, and validate the OpenSpec change strictly.

Rollback removes the service and UI wiring; persisted `assets`/`asset_links` rows remain readable by the pre-FC-0109 model because no schema or identity formats change.

## Open Questions

None for FC-0109. The PRD's two open questions resolve from existing material: assets attach to listings and to niche/group/store contexts (PRD "Attach Reference Material" workflow), and missing files are detected but not repaired (accepted workspace-file-storage spec). Multi-context linking awaits FC-0204.
