## Why

The Manage Mockup Templates surface permanently splits space between its collection and a large placement editor, constraining both review and image mapping even when no draft is active. Reviewing templates is the primary workflow; occasional Add/Edit work needs the existing preview-first editor in a focused surface with explicit draft safety.

## Origin

- Primary issue: [#201](https://github.com/ThorvaldBoe/FusionCanvas/issues/201)

## What Changes

- Make Manage Mockup Templates a collection-focused surface with one clear **Add Mockup Template** action and no inline editor region.
- Open the complete preview-first template form in one Store Editor-owned modal for both Add and Edit, with mode-specific title and sensible initial focus.
- Preserve provider image selection, Design Area targeting, Color applicability, image-space placement, advanced provider data, validation, revision, persistence, and archive/read-only behavior.
- Close only after successful save or confirmed cancellation; keep failed drafts and validation guidance visible.
- Protect meaningful unsaved changes on Cancel, Escape, and window close, close stale dialogs on Offering/workspace changes, and restore focus to the invoking action when practical.
- Reconcile the Mockup Template UI description and add focused ViewModel, Avalonia headless, and fixture tests.

This is one cohesive UI delivery module with no open implementation dependency: the Offering-scoped collection remains the stable parent while its existing editor receives a modal lifecycle. It does not require provider synchronization, new catalog data, or persistence changes.

The frequent workflow is scanning templates; Add/Edit is an occasional, complex mapping task that belongs in a focused, resizable surface. Empty, Add, Edit, validation failure, save success, cancel, meaningful-close, archived/read-only, and stale-context states are explicitly covered.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `product-supplier-setup`: Move Mockup Template Add/Edit from a permanent inline master-detail editor into one focused, guarded modal while preserving existing catalog and placement behavior.

## Impact

- `CatalogSetupViewModel` presentation state/events and existing Mockup Template draft lifecycle.
- `StoreEditorWindow` modal ownership and focus restoration.
- New App-only `MockupTemplateEditorWindow` plus removal of the inline XAML region.
- `docs/Visuals/ui-descriptions/manage-mockup-templates.ui.yaml` and its deterministic test expectations.
- Focused App ViewModel/headless tests; no Domain, Application, Integration, database, migration, package, or external-provider changes.
