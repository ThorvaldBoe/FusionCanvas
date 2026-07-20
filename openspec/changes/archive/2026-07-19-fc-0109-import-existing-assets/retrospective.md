# fc-0109-import-existing-assets Retrospective

## Outcome

Asset import, purpose labeling, per-context visibility, missing-file display, and confirmed removal are implemented as a new `asset-management` capability backed by a new `IAssetManagementService` and a focused `AssetsWindow`. The service reuses the existing domain `Asset`/`AssetLink`/`AssetKind`, the `IWorkspaceFileStore` port, and the SQLite `assets`/`asset_links` tables with no schema migration. The window is reachable from tree context menus on niches, groups, and listings, and from a "Store assets…" entry in the store actions flyout. The store view lists every store-owned asset (including unlinked records left by group-deletion cascades) with derived context labels. Strict OpenSpec validation passes; the fast deterministic baseline (216 tests) is green.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| The service could delete the managed file directly via `File.Delete` on a computed path. | Application-level tests with a fake file store cannot observe the deletion, so save-failure cleanup and removal file deletion are untestable at that layer. | Add `bool TryDelete(string workspaceRelativePath)` to `IWorkspaceFileStore`, implement it in `LocalWorkspaceFileStore` with the same workspace-boundary guard as `Exists`, and update all fakes. The service calls the port, not the filesystem. | Architecture | Reusable | None — the workspace-file-storage spec describes behavior, not port shape, so no spec delta was needed. |
| Niche context menu did not exist (`HasContextActions` excluded niches). | The FC-0109 spec requires an "Assets…" entry point on niches, but niches had no context menu at all. | Broadened the context menu's visibility to `HasAssetActions` (niche/group/listing) and gated the existing group/listing-only items (Rename/Copy/Cut/Paste/Duplicate/Edit properties) behind `HasContextActions` so niches get only "New listing" + "Assets…". | UX | Change-specific | None |
| Import would pick a kind silently or ask in a modal. | The spec requires the user to confirm or change the purpose before completing the import. | Added an inline pending-import bar (file name + purpose ComboBox pre-selected from extension + Confirm/Cancel) instead of a modal dialog, keeping the focused-surface pattern. | UX | Change-specific | None |
| `Asset` record properties were assumed init-settable for `with` expressions. | `Asset` declares `Kind`/`WorkspaceRelativePath`/etc. as `{ get; }` (not positional init), so `with { Kind = ... }` does not compile. | Relabel reconstructs the `Asset` via its constructor, preserving all fields except `Kind` and `UpdatedAt`. | Implementation defect | Change-specific | None |

## Deferred or Change-Specific Notes

- Real desktop UI verification (task 5.2) is the only remaining task; it requires an interactive Avalonia session and a disposable workspace, which the agent cannot perform. It should be completed by a contributor before archiving.
- Multi-context linking/sharing is intentionally deferred to FC-0204 (asset relationships); an imported asset has exactly one context link in this phase.
- The `IWorkspaceFileStore.TryDelete` addition is an implementation evolution, not a behavior change to the accepted `workspace-file-storage` spec; no spec delta was required.
