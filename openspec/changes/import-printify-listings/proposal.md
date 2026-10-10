## Why

Creators who already sell through Printify need a practical way to bring those existing listings into FusionCanvas when they first set up a workspace or reconnect a Store. A focused import lets them continue managing those Items locally while new designs can originate in FusionCanvas and move through its Printify listing workflow.

## What Changes

- Add a `Import from Printify…` action to a Niche's navigation context menu.
- Add a focused preview and selection dialog for products in the Store's selected Printify shop, with linked products hidden by default and available through a show/hide control.
- Add an explicit title-and-description duplicate check for selected products, with choices to import as new or connect an unlinked local Item while preserving its title and description.
- Import selected Printify listing data and original print-area artwork into Listing-stage Items, attach durable Printify identity and imported state, and group new Items under a dated Printify group.
- Keep initial import lightweight: offer an explicit Listing-stage action to download the listing's variant setup into the local catalog and Design workflow when the user is ready to edit it.
- Use FusionCanvas's standard image placement and scale for imported artwork; Printify-specific positioning adjustments are not imported or retained in v1.
- Show per-product import failures and allow the user to retry the import later; avoid duplicate Items when successful products are encountered again.
- Keep the import Printify-specific. Direct Shopify import and image-based duplicate detection are out of scope.
- Reuse the existing Store catalog import and Design workflows when the user explicitly downloads variant setup; do not reconstruct catalog or Design state during initial listing import.

## Capabilities

### New Capabilities
- `printify-listing-import`: Import existing products from the selected Printify shop into a Niche as linked Listing-stage Items, including preview, selection, duplicate review, artwork, recoverable per-product failures, and optional on-demand variant setup reconstruction.

### Modified Capabilities

None.

## Impact

- Application: Printify listing discovery, preview, duplicate comparison, adoption/linking, import orchestration, and on-demand variant-setup reconstruction.
- Integration: paginated shop product retrieval, product details, and safe download of original print-area artwork using the Store's existing Printify connection.
- Persistence and workspace files: reuse the Store-scoped external listing mapping for stable shop/product identity and snapshot data; save artwork as managed local assets; create a dated group and Listing-stage Items.
- App: Niche context-menu entry, preview/selection/duplicate-resolution dialog, progress and per-product outcome presentation.
- Verification: focused application and integration tests, persistence coverage for mappings/assets/grouping and retry idempotency, and Avalonia headless coverage for dialog state and selection behavior. No new package or external dependency is expected.
