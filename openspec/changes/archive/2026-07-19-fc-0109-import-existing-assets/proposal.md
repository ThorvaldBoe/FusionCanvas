## Why

Creators already have files that support their products — source designs, exports, mockups, reference images, textures, brushes, and fonts — scattered across disconnected folders. FusionCanvas defines `Asset`, `AssetLink`, and managed workspace file storage, and its deletion guards already recognize asset links, but there is no user-facing way to import a file, attach it to the work it supports, or see it again later. FC-0109 closes that gap so existing creative work becomes part of the managed workspace instead of being lost outside it.

## What Changes

- Add an application-owned asset management service that imports an existing local file into managed workspace storage (reusing `IWorkspaceFileStore`), creates the asset record and one context link atomically, lists assets for a context, relabels asset purpose, and removes assets with confirmation.
- Support attaching imported assets to a listing (primary workflow) and to a niche, group, or store when the asset provides broader context, per the FC-0109 PRD.
- Add one focused Assets window, opened from a tree context-menu command on listings, niches, groups, and stores, that shows the context's assets with purpose labels and file state and hosts import, purpose relabeling, and removal. The store-level view lists every store-owned asset, including assets whose links were removed by an accepted group-deletion cascade, so no asset becomes unreachable.
- Label asset purpose with the existing domain `AssetKind` (source design, exported image, SVG, mockup, reference, texture, brush, font, and other kinds), pre-selecting a sensible purpose from the file extension at import while keeping the user's choice explicit.
- Detect and display missing managed files when listing assets, reusing the accepted workspace-file-storage missing-file boundary; no repair or relinking in this phase.
- Preserve asset relationships automatically when listings or groups move, because links target stable entity identities rather than tree placement.
- Keep batch import, automatic file matching, image processing, deduplication, version history, validation, and multi-context linking out of scope, per the FC-0109 PRD.

## Capabilities

### New Capabilities

- `asset-management`: Defines user-facing import of existing files into managed workspace storage, attachment to listing/niche/group/store context, purpose labeling, per-context asset visibility, missing-file presentation, confirmed removal, and atomic persistence for asset operations.

### Modified Capabilities

None. The workspace-file-storage spec already accepts managed-copy import, workspace-relative references, asset categories, context association, and missing-file detection. The listing-management, niche-management, store-management, and group-management specs already guard or cascade asset links on deletion. FC-0109 builds the user workflow on those accepted contracts without changing them.

## Impact

- Adds `IAssetManagementService` and related request/result/state contracts in `FusionCanvas.Application.Workspace`, following the established listing/group management service pattern: reload snapshot, validate, produce one replacement snapshot, save once.
- Reuses the existing domain `Asset`, `AssetLink`, `AssetKind`, `WorkspaceFileReference`, the `IWorkspaceFileStore` port, and the existing SQLite `assets`/`asset_links` persistence; no schema migration is expected.
- Adds an `Assets` focused window and view model in `FusionCanvas.App`, with a tree context-menu entry point for listings, niches, groups, and stores; the main workspace layout, document tabs, and stage tool host are unchanged.
- Adds application tests with deterministic collaborators, integration tests over temporary SQLite databases and workspace roots, view-model tests, and a real desktop UI verification pass per the testing baseline.
- Requires no changes to FC-0105/FC-0106/FC-0107 parallel work; the future Listing Inspector may later surface the same asset data in the document window.
