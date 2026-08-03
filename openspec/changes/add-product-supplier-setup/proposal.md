## Why

Creators currently have to research a product's provider-specific printable areas outside FusionCanvas before beginning a design. That repeats manual work and makes it easy to prepare artwork for the wrong size or position.

Issue [#94](https://github.com/ThorvaldBoe/FusionCanvas/issues/94) needs a durable, store-scoped product catalog so a creator can maintain the products and fulfillment choices they actually use, then deliberately select the relevant design targets while working on an Item.

## What Changes

- Add a store-management Product and fulfillment catalog where a creator manually maintains Printify product blueprints, fixed-provider offerings, and Printify Choice network offerings.
- Capture provider-offering variants and their option values, plus design areas containing Printify-compatible position, decoration method, pixel width, pixel height, and applicable variants.
- Let an Item's editable Design stage use no configured target or select one or more configured design areas from its Store; retain the selected targets with the Item.
- Present Printify Choice as a variable fulfillment network rather than a fixed provider, including a clear consistency warning.
- Keep catalog setup in the focused store-management surface; do not add it to the application Settings window.

## Capabilities

### New Capabilities

- `product-supplier-setup`: Store-scoped manual management of product blueprints, fulfillment offerings, variants, and design areas.
- `design-area-target-selection`: Optional multi-selection of configured, store-compatible design-area targets for an Item at the Design stage.

### Modified Capabilities

- `basic-product-workflow`: The Design Stage Tool gains optional configured design-area targets while retaining its existing design-file behavior.

## Impact

- Domain and application layers gain store catalog and Item design-target concepts, validation, and use cases.
- SQLite workspace persistence gains catalog and Item-target tables, snapshot integration, validation, and schema migration coverage.
- The store-management and Design-stage Avalonia surfaces gain focused controls and headless view tests for meaningful bindings and selection behavior.
- The module is manual and local-first: Printify credentials, catalog import/synchronization, publishing, pricing/shipping management, mockups, and the future consolidation of store management with Settings are non-goals.
- The module is coherent because one outcome—selecting accurate design areas from the Store's maintained catalog—requires both catalog setup and Design-stage consumption, but excludes all network synchronization complexity.
