## ADDED Requirements

### Requirement: Documented Scope And Acceptance Expectations Are Preserved For Future Implementation
FusionCanvas SHALL ensure that the documented scope and acceptance expectations are preserved for future implementation.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The documented scope and acceptance expectations are preserved for future implementation

### Requirement: Acceptance Criteria Remain Satisfied
FusionCanvas SHALL satisfy the acceptance criteria captured in the source PRD.

#### Scenario: Store Can Define One Or More Mockup Products
- **WHEN** the corresponding capability is delivered
- **THEN** A store can define one or more mockup products.

#### Scenario: Mockup Product Can Define Vendor, Product Name, Product Type, And Design Area Dimensions
- **WHEN** the corresponding capability is delivered
- **THEN** A mockup product can define vendor, product name, product type, and design area dimensions.

#### Scenario: Mockup Product Can Contain Multiple Templates
- **WHEN** the corresponding capability is delivered
- **THEN** A mockup product can contain multiple templates.

#### Scenario: Template Can Reference A Template Image Asset
- **WHEN** the corresponding capability is delivered
- **THEN** A template can reference a template image asset.

#### Scenario: Template Can Define Design Placement Using X, Y, Width Or Scale, And Optional Height Or Rotation
- **WHEN** the corresponding capability is delivered
- **THEN** A template can define design placement using X, Y, width or scale, and optional height or rotation.

#### Scenario: Template Can Contain Multiple Color Variants
- **WHEN** the corresponding capability is delivered
- **THEN** A template can contain multiple color variants.

#### Scenario: Color Variant Can Preserve Exact Provider Color Name
- **WHEN** the corresponding capability is delivered
- **THEN** A color variant can preserve exact provider color name.

#### Scenario: Color Variant Can Reference Its Own Template Image When Needed
- **WHEN** the corresponding capability is delivered
- **THEN** A color variant can reference its own template image when needed.

#### Scenario: Active Products, Templates, And Color Variants Are Available To The Basic Listing Tool
- **WHEN** the corresponding capability is delivered
- **THEN** Active products, templates, and color variants are available to the Basic Listing Tool.

#### Scenario: Manual Configuration Is Enough To Generate Basic Flat Mockups
- **WHEN** the corresponding capability is delivered
- **THEN** Manual configuration is enough to generate basic flat mockups.

## Source PRD

# FC-0213 - Mockup Product Settings

## Summary

Mockup Product Settings define the store-level configuration needed for basic local mockup generation.

These settings describe which supplier products a store sells, which mockup templates exist for those products, how the design area maps onto each template, and which exact color variants are offered.

The settings are tied to a store because different stores may sell different products, use different suppliers, or offer different color selections.

## User Need

As a creator, I need to configure product mockup data once so I can generate consistent mockups for many listings.

As a creator preparing Printify products manually, I need FusionCanvas to preserve exact supplier, product, and color names so I can match them later.

As a plugin author, I need a clear basic configuration model that advanced mockup, Printify, or Shopify plugins can build on without replacing the core app's local data.

## Goals

- Let each store define the products it intends to sell.
- Support supplier-specific product data such as vendor, product name, and design area.
- Support multiple mockup templates per product.
- Support multiple color variants per template.
- Preserve exact provider color names.
- Define how a full-size design area maps to each template image.
- Keep the initial configuration manual and simple.
- Leave provider catalog import and advanced rendering to later integrations or plugins.

## Configuration Structure

Mockup settings should use a three-level structure:

```text
Store
Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬ Mockup Product
    Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬ Mockup Template
        Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬ Color Variant
```

Example:

```text
Tomes of Virtue
Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬ SwiftPOD / Gildan 64000 T-Shirt
    Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬ Flat front shirt mockup
    Ã¢â€â€š   Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬ Black
    Ã¢â€â€š   Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬ Navy
    Ã¢â€â€š   Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬ Dark Heather
    Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬ Lifestyle front mockup
        Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬ Black
        Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬ Navy
```

## Mockup Product

A mockup product represents a supplier-specific product that the store may sell.

Example:

```text
SwiftPOD / Gildan 64000 T-Shirt
```

Suggested fields:

```text
MockupProduct
- Id
- StoreId
- VendorName
- ProductName
- ProviderProductName
- ProductType
- DesignAreaWidthPx
- DesignAreaHeightPx
- Notes
- IsActive
- MetadataJson
```

Example:

```json
{
  "vendorName": "SwiftPOD",
  "productName": "Gildan 64000 T-Shirt",
  "productType": "T-Shirt",
  "designArea": {
    "widthPx": 3692,
    "heightPx": 4800
  },
  "provider": "Printify",
  "providerProductName": "Unisex Softstyle T-Shirt"
}
```

## Mockup Template

A mockup template is an image containing a blank product view.

Each product can have multiple templates. A template may represent a flat front image, lifestyle image, back view, close-up, or any other reusable visual presentation.

Suggested fields:

```text
MockupTemplate
- Id
- MockupProductId
- Name
- TemplateAssetId
- ViewName
- DefaultColorName
- PlacementX
- PlacementY
- PlacementWidth
- PlacementHeight
- PlacementScale
- RotationDegrees
- IsActive
- MetadataJson
```

The template mapping defines how the supplier design area is scaled and placed on the template image.

For the basic version, the mapping should support:

- upper-left X coordinate
- upper-left Y coordinate
- placement width or scale factor
- optional placement height when needed
- optional rotation, defaulting to 0

If both placement width and scale factor exist, placement width should be preferred because it is easier to reason about visually.

Advanced mapping such as perspective transform, warping, masks, displacement maps, fabric shadows, or print blending should be plugin territory.

## Color Variant

A color variant represents one sellable product color for a specific template.

The exact color name should match the supplier or print provider where practical.

Suggested fields:

```text
MockupColorVariant
- Id
- MockupTemplateId
- ProviderColorName
- DisplayColorName
- TemplateAssetId
- SwatchHex
- SortOrder
- IsActive
- MetadataJson
```

A color variant may use:

- the parent template asset, when the image is reusable across colors
- a color-specific template asset, when each shirt color has its own image

For most shirt mockups, each color will likely need its own template image.

## Design Area Mapping

The design area describes the source print area for the supplier product.

Example:

```text
Gildan 64000 from SwiftPOD
Design area: 3692x4800px
```

The template mapping describes how that design area appears on a template image.

Example:

```text
Template image: 1500x1500px
Design placement:
- x: 536
- y: 365
- width: 428
```

When generating a mockup, FusionCanvas scales the final design from the supplier design area to the placement width and places it at the configured coordinates.

The source design should be expected to fit the supplier design area. If the source design has a different size, FusionCanvas should either scale it to the supplier design area first or warn the user that the design dimensions do not match the product configuration.

## Store Settings UI

The settings section for a store should allow the user to:

- add, edit, archive, and restore mockup products
- set vendor, product name, product type, and design area dimensions
- add, edit, archive, and restore templates for a product
- attach template images
- define template placement coordinates and size
- add, edit, archive, and restore color variants for a template
- attach color-specific template images
- set exact provider color names
- mark active products, templates, and colors that should appear in the Basic Listing Tool

The first version may use manual numeric entry for placement coordinates. A later visual mapping editor can make this easier.

## Acceptance Criteria

- A store can define one or more mockup products.
- A mockup product can define vendor, product name, product type, and design area dimensions.
- A mockup product can contain multiple templates.
- A template can reference a template image asset.
- A template can define design placement using X, Y, width or scale, and optional height or rotation.
- A template can contain multiple color variants.
- A color variant can preserve exact provider color name.
- A color variant can reference its own template image when needed.
- Active products, templates, and color variants are available to the Basic Listing Tool.
- Manual configuration is enough to generate basic flat mockups.

## Out of Scope

- Provider catalog import
- Printify API synchronization
- Shopify product variant synchronization
- Automatic discovery of design area dimensions
- Visual drag-and-drop placement editor
- Perspective warp mapping
- Fabric simulation
- Advanced shadows, masks, displacement maps, or blending
- Bulk provider color import

## Related Notes

- [[Roadmap]]
- [[Product]]
- [[Data Model]]
- [[FC-0101 - Store Management]]
- [[FC-0206 - Manual Mockup Records]]
- [[FC-0212 - Basic Listing Tool]]
- [[FC-0603 - Printify Integration]]
- [[FC-0604 - Shopify Integration]]
