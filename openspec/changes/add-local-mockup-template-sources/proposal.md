## Why

Creators can configure a Mockup Template but cannot add the local source images that make it usable. The editor currently presents a deliberately unavailable provider-catalog selector, even though the intended near-term workflow is offline-first: one named template contains locally managed source images, each applicable to one or more offering option values.

## Origin

- GitHub issue: [#208 Supply selectable provider mockup images in the production editor](https://github.com/ThorvaldBoe/FusionCanvas/issues/208)

## What Changes

- Replace the unavailable, provider-catalog-only Mockup Template image flow with a local **Mockup source image** workflow in the focused Store Editor dialog; no Printify credential, API, network, or synchronization behavior is introduced.
- Let a creator add or replace a managed local raster source image and assign it to one or more option values from the template's Blueprint Offering. Color is the common first case, but applicability is not limited to a hardcoded Color dimension.
- Define deterministic applicability: a concrete Variant matches a source image when it contains every value assigned to that image; an active template is usable only when every compatible Variant resolves to exactly one source image. Missing and overlapping matches remain visible setup errors and never select an image implicitly.
- Preserve managed source-asset identity in current template state and immutable revision-color snapshots. A source-image or applicability change creates a template revision; past revisions continue to identify their source assets.
- Import the selected local file into managed workspace storage and preserve confirmed configuration if import, validation, preview loading, or persistence fails. Source assets remain local and offline-capable.
- Show the selected managed image and its real dimensions in the existing placement editor, initialize a valid in-bounds mapping, and make source state, validation gaps, errors, cancellation, focus, and archived-store read-only behavior explicit.
- Keep drag-and-drop, image editing, rendering/composition, generated mockups, external image selection, Printify synchronization, and marketplace credentials out of scope. A later integration may provide another source-selection route through the same source-asset and applicability model.

The primary workflow is occasional Store setup, so it remains in the existing focused Mockup Template dialog rather than the daily workspace. This is one coherent delivery module: local file import, option-value applicability, source identity, revision provenance, preview placement, validation, persistence, and dialog behavior must agree before a creator can safely configure a reusable template.

## Capabilities

### New Capabilities

- `mockup-template-source-images`: Local managed source-image import, option-value applicability, variant resolution, source lifecycle, revision provenance, and focused editor interaction states for Mockup Templates.

### Modified Capabilities

- `product-supplier-setup`: Replace the unavailable provider-catalog image-selection requirement with local source-image setup and clarify its relationship to Blueprint Offering option values and templates.

## Impact

- **Domain:** Mockup Template source applicability and exact-one-match policy; existing `SourceAssetId` fields become active behavior.
- **Application:** A focused local-source import/configuration use case, image-dimension inspection boundary, validation summaries, and atomic coordination with workspace-file storage and repository persistence.
- **Integration:** Managed workspace-file import/cleanup and existing SQLite mappings; no HTTP client, provider SDK, secret, or network dependency.
- **App:** `CatalogSetupViewModel`, Mockup Template dialog, file-picker adaptation, managed-image preview, option-value selectors, and focused Avalonia headless coverage.
- **Tests:** Domain policy, deterministic application/file-store fakes, SQLite round-trip and rollback coverage, view-model state coverage, and meaningful dialog binding/focus/selection headless tests.
