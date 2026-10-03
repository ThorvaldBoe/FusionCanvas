## Why

Creators commonly prepare one mockup template with a set of color-specific source images. Requiring a separate native file-picker interaction for every image adds repetitive friction to an editor that already models each source image independently. Extending the existing upload action to support multi-select makes the common setup path faster without introducing a second workflow or shared metadata semantics.

## What Changes

- Allow the existing Mockup Template source-image upload action to select multiple supported raster files in one native picker interaction.
- Add one independent draft row per selected file, preserving the existing requirement that applicability and image-space mapping are configured separately for each row.
- Rename the action to the discoverable plural **Upload images…** and provide accessible guidance that multiple files may be selected.
- Read preview dimensions independently and retain per-file warnings when metadata cannot be read.
- Keep valid selected files staged when another selected file fails, with existing partial-save diagnostics covering persistence failures.
- Use an image-specific picker filter for this route; do not broaden the existing Store Assets import behavior.
- Select the first newly added row for continued metadata configuration and preserve unsaved edits on other rows.
- Do not add a separate bulk-upload button, batch metadata-copy behavior, folder import, or schema migration.

The primary workflow remains the focused Mockup Template editor. This is a frequent setup action for creators configuring several source images for one offering, while the lower master-detail editor remains responsible for per-image configuration.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `mockup-template-source-images`: Extend the focused source-image upload route from one selected file to one or more selected files while preserving independent entries, incomplete drafts, and per-image configuration.

## Impact

The App picker boundary and Avalonia picker implementation will gain a multi-file source-image path while retaining the existing single-file asset import contract. `CatalogSetupViewModel` will stage multiple local source drafts and continue using the existing source-image application service for sequential Save processing. `MockupTemplateEditorWindow` and its headless tests will reflect the plural action and multi-row behavior. No domain or persistence schema changes are expected because each selected file already becomes its own managed source Asset and source-image entry.

Risks are limited to native picker behavior, per-file metadata failures, and partial persistence if one source import fails after earlier files have been saved. Verification will cover the view-model collection behavior, focused picker contract, rendered headless editor workflow with a deterministic picker, the existing source-image persistence tests, and the solution test baseline.
