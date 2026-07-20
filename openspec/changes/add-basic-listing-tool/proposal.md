## Why

Design Records, Manual Mockup Records, the Listing Metadata Editor, and Mockup Product Settings together preserve the components of a listing, but creators have no default Listing-stage tool that generates basic local mockups from configured product templates, edits marketplace preparation metadata, tracks readiness, and keeps Printify and Shopify product creation out of the basic tool. FC-0212 adds the Basic Listing Tool as the default built-in Listing-stage tool, hosted through the Stage Tool Host, that combines final artwork with configured mockup products to produce mockup images and records, edits listing metadata, and tracks readiness without publishing.

## What Changes

- Add the Basic Listing Tool as the default built-in Listing-stage tool, available from the Listing stage for an existing item and unavailable without one.
- Require item context and at least one selected final design variant before generating mockups; allow metadata editing when no final design exists.
- Generate basic local mockups from configured mockup product settings (FC-0213): for each selected color/template combination, load the final design, scale to the supplier design area, resize and place per the template mapping, composite onto the blank product template, save the output as an asset, and create a mockup record linked to the listing, design, template, color variant, and output asset.
- Support manually attaching existing mockup images as mockup records.
- Support editing title, description, price, status, notes, and marketplace preparation fields through the listing-metadata-editor capability.
- Provide a readiness checklist for missing design, mockup, metadata, price, and marketplace preparation data.
- Do not directly create, update, or publish Printify, Shopify, Etsy, or other marketplace products; store marketplace-specific values locally as draft preparation data and delegate publishing to future integration plugins.
- Feed important Listing-stage events into the Creative History Timeline.
- Keep the tool extensible through the Stage Tool Host.

## Capabilities

### New Capabilities

- `basic-listing-tool`: Defines the default built-in Listing-stage tool that generates basic local mockups from configured mockup product settings, supports manual mockup attachment, edits listing metadata through the listing-metadata-editor capability, tracks readiness, refuses direct marketplace publishing, feeds the timeline, and advances listing preparation without publishing.

### Modified Capabilities

None. FC-0212 reuses `mockup-records` for mockup storage and lifecycle, `mockup-product-settings` (FC-0213, in-flight) for product/template/color configuration, `listing-metadata-editor` for marketplace metadata, `design-records` for final selected artwork, `asset-management` for output asset storage, `creative-history-timeline` for cross-stage history, `stage-tool-host` for hosting and context, and `workflow-stage-navigator` for stage ownership without changing their accepted requirements. Mockup compositing uses a flat compositing image processor; perspective warping, fabric effects, displacement maps, shadows, and realistic print blending remain plugin territory.

## Impact

- Adds a `BasicListingTool` view model and Avalonia tool registered as the default Listing-stage tool through the Stage Tool Host registry.
- Adds a mockup generation engine that consumes mockup product settings and final designs, composites flat mockups, stores outputs as assets, and creates mockup records through the mockup-records service.
- Adds a readiness checklist view model that reads design, mockup, metadata, price, and marketplace preparation state.
- Reuses the listing-metadata-editor surface for metadata editing.
- Adds app and UI tests for mockup generation against deterministic fake product settings and designs, manual attachment, metadata editing, readiness checks, timeline feeding, and marketplace-publishing refusal.
- No schema migration is expected; the tool reads and writes through existing and in-flight services.
