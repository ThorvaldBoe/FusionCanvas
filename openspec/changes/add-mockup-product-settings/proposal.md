## Why

The Basic Listing Tool can generate mockups, but it has no store-level configuration model that defines which supplier products a store sells, which mockup templates exist for those products, how the design area maps onto each template, and which exact color variants are offered. FC-0213 adds Mockup Product Settings as the store-scoped configuration capability that lets a creator define products, templates, and color variants once and reuse them across many listings, while preserving exact provider color names for manual Printify or Shopify setup.

## What Changes

- Add a three-level store-scoped configuration model: Store → MockupProduct → MockupTemplate → MockupColorVariant.
- Add MockupProduct fields: vendor name, product name, provider product name, product type, design area width/height, notes, active flag, metadata.
- Add MockupTemplate fields: name, template asset reference, view name, default color name, placement X/Y/width/height/scale/rotation, active flag, metadata.
- Add MockupColorVariant fields: provider color name, display color name, template asset reference (color-specific when needed), swatch hex, sort order, active flag, metadata.
- Add a focused store settings surface to add, edit, archive, restore, and mark active products, templates, and color variants.
- Preserve exact provider color names as entered, because those names are needed when preparing Printify or another provider manually.
- Define design-area-to-template mapping: scale the final design from the supplier design area to the placement width and place it at the configured coordinates.
- Make active products, templates, and color variants available to the Basic Listing Tool.
- Keep initial configuration manual and simple with numeric entry plus a visual display of the placement rectangle on the template image; defer provider catalog import, visual drag-and-drop placement editing, perspective warp, fabric simulation, and advanced rendering to later integrations or plugins.

## Capabilities

### New Capabilities

- `mockup-product-settings`: Defines the store-scoped MockupProduct, MockupTemplate, and MockupColorVariant configuration entities, their fields and active/archive lifecycle, the design-area-to-template mapping model, the focused store settings surface, and the active-set exposure to the Basic Listing Tool.

### Modified Capabilities

None. FC-0213 reuses `store-management` for the store shell and secondary-surface pattern, `asset-management` for template and color-specific template image assets, and `listing-management`'s archive-aware projection patterns without changing their accepted requirements. The Basic Listing Tool (FC-0212, in-flight) consumes the active configuration through the contract this capability exposes.

## Impact

- Adds MockupProduct, MockupTemplate, and MockupColorVariant domain entities owned by a store, with stable identity, timestamps, active/archive flags, and documented fields.
- Adds a `MockupProductSettingsService` application service for create/edit/archive/restore/reorder and active-set queries, and SQLite persistence mappings through the existing snapshot boundary with a forward-only schema migration.
- Adds a focused store settings surface with progressive disclosure across the three levels.
- Adds domain, application, integration, app, and UI tests for the three-level model, archive/restore, active-set exposure, placement mapping validation, exact provider color name preservation, and atomic persistence.
