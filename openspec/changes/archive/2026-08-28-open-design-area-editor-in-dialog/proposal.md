## Why

The Manage Design Areas surface permanently reserves a large editor column even when no draft is active, reducing list width and weakening the collection-first hierarchy. Add and edit are occasional multi-field tasks that fit the repository's focused-dialog pattern and should no longer consume the default management surface.

## Origin

- Primary issue: [#199](https://github.com/ThorvaldBoe/FusionCanvas/issues/199)

## What Changes

- Make Manage Design Areas a full-width, collection-focused surface with no inline editor column.
- Open the complete existing form in one modal dialog for both Add and Edit, with mode-specific title and sensible initial focus.
- Preserve current identity, maximum-size, artwork-guidance, compatibility, advanced provider data, validation, persistence, and referenced-record behavior.
- Close only on successful save or confirmed cancellation/dismissal; keep failed drafts and validation visible.
- Protect meaningful unsaved changes on Cancel, Escape, and window close, and return focus to the invoking Add/Edit action.
- Close stale dialogs safely when the Offering or workspace context changes.
- Reconcile the accepted OpenSpec behavior, UI description, and focused ViewModel/headless tests.

This is one cohesive UI-delivery module: the Design Area collection remains the stable parent context while the existing form moves into a focused modal lifetime. It has no open dependencies and introduces no catalog-model or persistence behavior.

The primary workflow is reviewing Design Areas; Add/Edit is less frequent and belongs in a focused surface. The parent list may use the full supported width. Initial/add, populated/edit, validation failure, save success, cancel, unsaved-close, archived/read-only, and stale-context states are explicitly covered.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `product-supplier-setup`: Move Design Area Add/Edit from a permanent inline master-detail editor into one focused, guarded modal dialog while preserving catalog behavior.

## Impact

- `CatalogSetupViewModel` presentation state/events and existing Design Area draft lifecycle.
- `StoreEditorWindow` dialog ownership and focus restoration.
- New App-only `DesignAreaEditorWindow` plus removal of the inline XAML region.
- `docs/Visuals/ui-descriptions/manage-design-areas.ui.yaml`.
- Focused App ViewModel/headless tests; no Domain, Application, Integration, database, migration, or package changes.
