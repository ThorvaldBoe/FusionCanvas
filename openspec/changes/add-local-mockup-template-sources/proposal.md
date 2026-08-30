## Why

Creators can configure a Mockup Template but cannot add the local source images that make it usable. The editor currently presents a deliberately unavailable provider-catalog selector, even though the intended near-term workflow is offline-first: one named template contains locally managed source images, each applicable to one or more offering option values.

## Origin

- GitHub issue: [#208 Supply selectable provider mockup images in the production editor](https://github.com/ThorvaldBoe/FusionCanvas/issues/208)

## What Changes

- Replace the unavailable, provider-catalog-only Mockup Template image flow with a local **Mockup source image** workflow in the focused Store Editor dialog; no Printify credential, API, network, or synchronization behavior is introduced.
- Separate file collection from configuration: creators can repeatedly upload local raster images without automatically assigning option metadata, then select any image and configure its applicability and image-space mapping independently.
- Present the dialog as a master-detail editor: the upper image table owns upload, selection, archive, applicability/design-area summaries, and complete/incomplete status; the lower editor owns metadata and placement for the selected image. The Template name remains the single shared field above both regions.
- Let a creator assign each image to grouped option values from the template's Blueprint Offering. Color is prominent and optimized for the normal one-color/all-sizes case, while Size and other active Options remain available without hardcoding the applicability model.
- Define deterministic grouped applicability: selected values are OR alternatives within one Option and configured Option groups are AND conditions across Options. An active template is ready only when every compatible Variant resolves to exactly one image. Missing or overlapping matches remain visible per-Variant outcomes, but do not prevent saving an incomplete Template or resolving unaffected Variants.
- Preserve managed source-asset identity in current template state and immutable revision-color snapshots. A source-image or applicability change creates a template revision; past revisions continue to identify their source assets.
- Import the selected local file into managed workspace storage and preserve confirmed configuration if import, validation, preview loading, or persistence fails. Source assets remain local and offline-capable.
- Keep one authoritative target Design Area on the Template while every image retains its own placement rectangle within its own pixel dimensions. Show the selected managed image in the lower placement editor and make unconfigured, complete, invalid, missing-coverage, ambiguous-coverage, import-error, cancellation, focus, archive, and archived-store read-only states explicit.
- Keep drag-and-drop, image editing, rendering/composition, generated mockups, external image selection, Printify synchronization, and marketplace credentials out of scope. A later integration may provide another source-selection route through the same source-asset and applicability model.

The primary workflow is occasional Store setup, so it remains in the existing focused Mockup Template dialog rather than the daily workspace. Upload and metadata editing are frequent repeated actions within that focused session, so the image collection stays visible while metadata is progressively disclosed for the selected row. This remains one coherent delivery module under issue #208: local file import, grouped applicability, draft completeness, source identity, revision provenance, preview placement, validation, persistence, and dialog behavior must agree before a creator can safely configure a reusable template.

## Capabilities

### New Capabilities

- `mockup-template-source-images`: Local managed source-image import, independently editable grouped option applicability, per-Variant resolution, incomplete-source lifecycle, revision provenance, and focused master-detail editor interaction states for Mockup Templates.

### Modified Capabilities

- `product-supplier-setup`: Replace the unavailable provider-catalog image-selection requirement with local source-image setup and clarify its relationship to Blueprint Offering option values and templates.

## Impact

- **Domain:** Mockup Template source completeness, grouped Option/Option Value applicability, and per-Variant exact-one-match policy; existing `SourceAssetId` fields become active behavior.
- **Application:** A focused local-source import/configuration use case, image-dimension inspection boundary, validation summaries, and atomic coordination with workspace-file storage and repository persistence.
- **Integration:** Managed workspace-file import/cleanup and existing SQLite mappings; no HTTP client, provider SDK, secret, or network dependency.
- **App:** `CatalogSetupViewModel`, Mockup Template master-detail dialog, independent upload and selection, completion indicators, archive flow, managed-image preview, grouped option-value selectors, and focused Avalonia headless coverage.
- **Tests:** Domain policy, deterministic application/file-store fakes, SQLite round-trip and rollback coverage, view-model state coverage, and meaningful dialog binding/focus/selection headless tests.
