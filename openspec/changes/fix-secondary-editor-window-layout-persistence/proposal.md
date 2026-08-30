## Why

Issue #230 reports that window placement is not remembered. The existing window-layout capability only names the original set of non-transient windows; reusable focused editor windows added later from Store Management reopen at their defaults because they have no stable geometry identity or persistence wiring. Remembering those editor placements removes repeated setup friction in a workflow that creators revisit frequently.

## What Changes

- Extend the non-transient window boundary to include the reusable Store Management editor windows: Option Value Management, Add Variant, Bulk Add Variants, Design Area Editor, and Mockup Template Editor.
- Give each included editor a stable local geometry key and restore its last valid normal position and size when it opens.
- Capture each included editor's latest valid normal geometry when it closes through the existing application-settings path.
- Add deterministic coverage for geometry capture and restoration, including the position as well as size, and for the Store Management wiring.
- Keep short-lived confirmation dialogs at their default placement; no settings UI, per-workspace layout, window-state persistence, or new storage format is introduced.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `window-layout-persistence`: Expand the defined non-transient-window set to cover reusable Store Management editor windows and require their normal geometry to persist and restore.

## Impact

- Affected App-layer window identity constants, shared geometry-persistence wiring, and Store Management editor-window creation paths.
- Affected deterministic Avalonia headless tests for geometry restoration/capture and Store Management dialogs.
- Reuses the existing optional, local `windowGeometry` settings section and settings save path; no Application/Integration contract, settings-version, workspace schema, external dependency, or main-window behavior change is expected.
- UX preflight: this is a frequent convenience behavior on focused management surfaces. It leaves the main workspace unchanged, does not add controls or alter draft, confirmation, cancellation, focus-return, loading, empty, blocked, or error behavior; each editor retains its existing interaction flow while reopening in a safe normalized position.
