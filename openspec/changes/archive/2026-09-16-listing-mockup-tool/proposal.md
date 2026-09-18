## Why

Issue [#137](https://github.com/ThorvaldBoe/FusionCanvas/issues/137) asks creators to turn the PNG design files already prepared in the Design stage into product mockups for each offered color. FusionCanvas already has the approved local template setup and image-space placement model, but the Listing stage currently stops at a read-only placeholder, so creators cannot apply a configured template or retain the resulting images.

## What Changes

- Add a Listing-stage mockup tool that discovers the active Item's selected Offering, Design files, offered colors, and ready Mockup Templates.
- Let the creator select a template and apply it to the applicable design/color combinations.
- Compose each design PNG over the selected color-specific template source image using the saved image-space mapping, preserving the design aspect ratio while fitting the mapped rectangle.
- Import successful composites into managed workspace storage as Item-linked `MockupImage` assets, retaining template and revision metadata for traceability.
- Show generated mockups in the Listing stage, allow applying a different template later, and report missing color templates, missing design files, invalid configuration, and persistence failures without losing existing outputs.
- Keep marketplace publishing, external synchronization, image editing, drag-and-drop template authoring, and a new store-global template entity out of this module.

The existing Offering-scoped Mockup Template library is the store-wide library in the current catalog model: it is reachable across the Store's Offerings, while a template remains owned by one Offering so its Design Area and Variant compatibility remain valid.

## Capabilities

### New Capabilities

- `listing-mockup-generation`: Apply ready local Mockup Templates to Listing-stage Design files and store the resulting mockup assets.

### Modified Capabilities

- None. Existing Mockup Template management and Listing Inspector requirements remain intact; this adds the previously absent Listing-stage generation surface.

## Impact

- Domain/Application: mockup-generation request/result contracts and output metadata, using existing Item, Offering, Design Area, Asset, Mockup Template, revision, and color models.
- Integration: a local raster compositor and managed-file output path behind application ports; no network service is required.
- App: Listing stage view model and AXAML surface, including template selection, apply/busy/error states, and generated-output gallery.
- Tests: framework-free composition and selection tests, application orchestration tests with deterministic file/compositor collaborators, integration raster/file tests, and Avalonia headless Listing-stage tests.
- Persistence: no schema migration is required if generated files use existing `Asset`/`AssetLink` records and metadata JSON.
