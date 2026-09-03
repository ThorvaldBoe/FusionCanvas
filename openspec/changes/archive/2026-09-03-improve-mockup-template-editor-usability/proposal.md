## Why

The mockup-template editor now supports independent source images, but placement is blind without seeing the selected image and repetitive mappings make the common all-colors workflow unnecessarily slow. The current option grouping and image list also obscure the intended metadata and selection model, while dialog ownership can fail during StoreEditor lifecycle transitions.

## What Changes

- Display the selected source image in the placement editor with an overlay for the design-area mapping.
- Add one-click reuse of a non-default mapping from another configured source image.
- Filter the Color metadata section to Color option values only; keep Size and other options in the secondary section.
- Replace the repeated-label image list with a selectable table including headings, row striping, and selected-row highlighting.
- Guard modal dialog opening when StoreEditor has no valid owner.
- Preserve independent per-image mappings and existing incomplete-row behavior.

## Capabilities

### New Capabilities

- `mockup-template-placement-preview`: visual placement preview and mapping reuse within the editor.

### Modified Capabilities

- `mockup-template-source-images`: refine editor presentation, mapping reuse, option filtering, and safe dialog lifecycle behavior.
- `product-supplier-setup`: update the focused Mockup Template editor interaction contract.

## Impact

Affected Avalonia editor AXAML/code-behind, CatalogSetupViewModel placement state, option-choice construction, StoreEditorWindow dialog launching, focused UI tests, UI-language evidence, and OpenSpec capability guidance. No persistence schema or external provider integration changes are expected.
