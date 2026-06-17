## ADDED Requirements

### Requirement: Basic Listing Tool Is Available From The Listing Stage For An Existing Item
FusionCanvas SHALL ensure that the Basic Listing Tool is available from the Listing stage for an existing item.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The Basic Listing Tool is available from the Listing stage for an existing item

### Requirement: Tool Is Not Available As A Free-floating Listing Workspace Without An Item
FusionCanvas SHALL ensure that the tool is not available as a free-floating listing workspace without an item.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The tool is not available as a free-floating listing workspace without an item

### Requirement: If No Item Is Selected, The Stage Tool Host Should Show An Appropriate Empty State Or Creation Path Instead Of Opening The Tool
FusionCanvas SHALL ensure that if no item is selected, the Stage Tool Host should show an appropriate empty state or creation path instead of opening the tool.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** If no item is selected, the Stage Tool Host should show an appropriate empty state or creation path instead of opening the tool

### Requirement: Selected Item Must Have A Valid Navigation Context, Including Store, Niche, And Topic Path Where Applicable
FusionCanvas SHALL ensure that the selected item must have a valid navigation context, including store, niche, and topic path where applicable.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The selected item must have a valid navigation context, including store, niche, and topic path where applicable

### Requirement: Tool Receives Context From The Stage Tool Host Instead Of Scraping UI State
FusionCanvas SHALL ensure that the tool receives context from the Stage Tool Host instead of scraping UI state.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The tool receives context from the Stage Tool Host instead of scraping UI state

### Requirement: Tool Displays The Current Item Context, Including Store, Niche, Topic Path, Item Title, Stage, Selected Concept, Selected Final Design Variants, Inherited Tags, And Relevant Metadata
FusionCanvas SHALL ensure that the tool displays the current item context, including store, niche, topic path, item title, stage, selected concept, selected final design variants, inherited tags, and relevant metadata.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The tool displays the current item context, including store, niche, topic path, item title, stage, selected concept, selected final design variants, inherited tags, and relevant metadata

### Requirement: Tool Should Require At Least One Selected Final Design Variant Before Generating Mockups
FusionCanvas SHALL ensure that the tool should require at least one selected final design variant before generating mockups.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The tool should require at least one selected final design variant before generating mockups

### Requirement: Tool Can Still Edit Listing Metadata When No Final Design Exists
FusionCanvas SHALL ensure that the tool can still edit listing metadata when no final design exists.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The tool can still edit listing metadata when no final design exists

### Requirement: Tool Can Show A Readiness Warning When Required Design Or Mockup Information Is Missing
FusionCanvas SHALL ensure that the tool can show a readiness warning when required design or mockup information is missing.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The tool can show a readiness warning when required design or mockup information is missing

### Requirement: Tool Supports Editing Title, Description, Price, Status, Notes, And Marketplace Preparation Fields
FusionCanvas SHALL ensure that the tool supports editing title, description, price, status, notes, and marketplace preparation fields.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The tool supports editing title, description, price, status, notes, and marketplace preparation fields

### Requirement: Tool Supports One Or More Mockups Per Item
FusionCanvas SHALL ensure that the tool supports one or more mockups per item.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The tool supports one or more mockups per item

### Requirement: Tool Supports Generating Mockups From Configured Product Mockup Settings
FusionCanvas SHALL ensure that the tool supports generating mockups from configured product mockup settings.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The tool supports generating mockups from configured product mockup settings

### Requirement: Tool Supports Manually Attaching Existing Mockup Images
FusionCanvas SHALL ensure that the tool supports manually attaching existing mockup images.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The tool supports manually attaching existing mockup images

### Requirement: Generated Mockups Are Stored As Assets And Linked Through Mockup Records
FusionCanvas SHALL ensure that generated mockups are stored as assets and linked through mockup records.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Generated mockups are stored as assets and linked through mockup records

### Requirement: Mockup Records Should Identify Product, Vendor, Template, Color Variant, Design Variant, View, Output File, And Intended Marketplace Use Where Known
FusionCanvas SHALL ensure that mockup records should identify product, vendor, template, color variant, design variant, view, output file, and intended marketplace use where known.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Mockup records should identify product, vendor, template, color variant, design variant, view, output file, and intended marketplace use where known

### Requirement: Tool Should Not Directly Create, Update, Or Publish Printify, Shopify, Etsy, Or Other Marketplace Products
FusionCanvas SHALL ensure that the tool should not directly create, update, or publish Printify, Shopify, Etsy, or other marketplace products.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The tool should not directly create, update, or publish Printify, Shopify, Etsy, or other marketplace products

### Requirement: Marketplace-specific Values May Be Stored Locally As Draft Preparation Data
FusionCanvas SHALL ensure that marketplace-specific values may be stored locally as draft preparation data.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Marketplace-specific values may be stored locally as draft preparation data

### Requirement: Publishing Actions Must Be Delegated To Later Integration Specs Or Plugins
FusionCanvas SHALL ensure that publishing actions must be delegated to later integration specs or plugins.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Publishing actions must be delegated to later integration specs or plugins

### Requirement: Acceptance Criteria Remain Satisfied
FusionCanvas SHALL satisfy the acceptance criteria captured in the source PRD.

#### Scenario: User Can Open The Basic Listing Tool For An Existing Item In The Listing Stage
- **WHEN** the corresponding capability is delivered
- **THEN** A user can open the Basic Listing Tool for an existing item in the Listing stage.

#### Scenario: Tool Does Not Allow Listing Work Without An Existing Item
- **WHEN** the corresponding capability is delivered
- **THEN** The tool does not allow listing work without an existing item.

#### Scenario: Tool Receives Store, Niche, Topic Path, Item, Stage, Selected Concept, Selected Final Design Variants, Tags, Metadata, Nearby Work, And Available Mockup Product Settings From The Stage Tool Host
- **WHEN** the corresponding capability is delivered
- **THEN** The tool receives store, niche, topic path, item, stage, selected concept, selected final design variants, tags, metadata, nearby work, and available mockup product settings from the Stage Tool Host.

#### Scenario: User Can Edit Title, Description, Price, Status, Notes, Marketplace Notes, Draft Tags, And Basic Preparation Fields
- **WHEN** the corresponding capability is delivered
- **THEN** The user can edit title, description, price, status, notes, marketplace notes, draft tags, and basic preparation fields.

#### Scenario: User Can Manually Attach Existing Mockup Images
- **WHEN** the corresponding capability is delivered
- **THEN** The user can manually attach existing mockup images.

#### Scenario: User Can Generate Mockups From Configured Product Templates And Color Variants
- **WHEN** the corresponding capability is delivered
- **THEN** The user can generate mockups from configured product templates and color variants.

#### Scenario: Generated Mockups Are Saved As Assets And Linked To Mockup Records
- **WHEN** the corresponding capability is delivered
- **THEN** Generated mockups are saved as assets and linked to mockup records.

#### Scenario: Mockup Records Preserve Product, Vendor, Template, Color Variant, Design Variant, View, Output Asset, And Intended Use Where Known
- **WHEN** the corresponding capability is delivered
- **THEN** Mockup records preserve product, vendor, template, color variant, design variant, view, output asset, and intended use where known.

#### Scenario: Tool Shows Readiness Information For Missing Design, Mockup, Metadata, Price, And Provider Preparation Data
- **WHEN** the corresponding capability is delivered
- **THEN** The tool shows readiness information for missing design, mockup, metadata, price, and provider preparation data.

#### Scenario: Tool Does Not Create Or Update Printify, Shopify, Etsy, Or Other External Marketplace Products Directly
- **WHEN** the corresponding capability is delivered
- **THEN** The tool does not create or update Printify, Shopify, Etsy, or other external marketplace products directly.

#### Scenario: Default Listing Tool Can Coexist With Future Plugin-provided Listing-stage Tools
- **WHEN** the corresponding capability is delivered
- **THEN** The default listing tool can coexist with future plugin-provided Listing-stage tools.

## Source PRD

# FC-0212 - Basic Listing Tool

## Summary

The Basic Listing Tool is the default built-in tool for the Listing stage.

It helps the creator prepare a selected item for marketplace presentation after final design artwork exists. The basic tool focuses on local mockup generation, listing metadata, price, status, and manual marketplace preparation. It does not create products in Shopify or Printify directly.

The Listing stage is where the product becomes sellable: the selected design is placed on product templates, product images are prepared, listing text is written or edited, and basic commercial information is captured.

## User Need

As a creator, I need a focused place to turn approved artwork into a marketplace-ready product listing.

As a creator using Shopify or Printify, I need FusionCanvas to keep enough structured information to prepare a product manually, even before direct marketplace integrations exist.

As a plugin author, I need the default Listing-stage experience to leave clear extension points for paid or specialized Printify, Shopify, marketplace validation, and advanced mockup plugins.

## Goals

- Provide the default Listing-stage tool for a single selected item.
- Require item context and at least one final selected design variant before mockup generation.
- Generate basic local mockups from configured product templates.
- Support multiple product colors and multiple mockup templates per product.
- Let users edit title, description, price, status, notes, and basic marketplace fields.
- Keep Printify and Shopify product creation out of the basic tool.
- Preserve generated mockups as assets and mockup records.
- Track basic listing readiness without requiring publishing integrations.
- Keep the tool extensible through the Stage Tool Host.

## Core Principle

The Listing stage prepares the selling presentation for an item.

Earlier stages answer different questions:

- Idea: what seed is worth pursuing?
- Concept: what is the product about?
- Design: what is the final artwork?
- Listing: how is this product presented for sale?

The basic Listing tool should be useful without external marketplace APIs. It should support the work a creator must do before publishing, especially creating product images and organizing listing information.

## Requirements

- The Basic Listing Tool is available from the Listing stage for an existing item.
- The tool is not available as a free-floating listing workspace without an item.
- If no item is selected, the Stage Tool Host should show an appropriate empty state or creation path instead of opening the tool.
- The selected item must have a valid navigation context, including store, niche, and topic path where applicable.
- The tool receives context from the Stage Tool Host instead of scraping UI state.
- The tool displays the current item context, including store, niche, topic path, item title, stage, selected concept, selected final design variants, inherited tags, and relevant metadata.
- The tool should require at least one selected final design variant before generating mockups.
- The tool can still edit listing metadata when no final design exists.
- The tool can show a readiness warning when required design or mockup information is missing.
- The tool supports editing title, description, price, status, notes, and marketplace preparation fields.
- The tool supports one or more mockups per item.
- The tool supports generating mockups from configured product mockup settings.
- The tool supports manually attaching existing mockup images.
- Generated mockups are stored as assets and linked through mockup records.
- Mockup records should identify product, vendor, template, color variant, design variant, view, output file, and intended marketplace use where known.
- The tool should not directly create, update, or publish Printify, Shopify, Etsy, or other marketplace products.
- Marketplace-specific values may be stored locally as draft preparation data.
- Publishing actions must be delegated to later integration specs or plugins.

## Basic Tool Layout

The default tool should use a focused Listing-stage layout:

- context summary near the top
- stage tool selector provided by the Stage Tool Host when multiple Listing tools are available
- selected final design panel
- listing metadata editor for title, description, price, status, notes, and marketplace preparation fields
- mockup generation panel
- generated and attached mockup gallery
- readiness checklist for missing design, mockup, metadata, price, and marketplace preparation data
- activity or history area showing generated mockups, attached mockups, metadata changes, and status changes

The mockup gallery and metadata editor are the primary work areas. The user should be able to prepare the listing without thinking about external API details.

## Listing Metadata

The basic tool should support simple, editable fields:

```text
Listing metadata
- title
- description
- price
- status
- notes
- marketplace notes
- product type
- provider product reference
- shipping profile notes
- draft tags or keywords
```

These fields are preparation data. They may later be mapped to Shopify, Printify, Etsy, Creative Fabrica, or other marketplace fields by integrations.

The basic tool should not try to model every marketplace requirement up front. Marketplace-specific details should live in JSON metadata until a direct integration proves which fields need structure.

## Mockup Generation

Mockup generation combines:

- a selected final design variant
- a configured vendor product
- a design area definition
- one or more mockup templates
- one or more color variants for each template
- a design-area-to-template mapping

The user chooses which configured product, template, color variants, and final design variant should be used.

For each selected color/template combination, the tool should:

1. Load the final design image.
2. Treat the final design as matching the product design area dimensions or scale it relative to that design area.
3. Resize the design according to the template mapping.
4. Place the resized design at the configured template coordinates.
5. Composite the design onto the blank product template.
6. Save the final mockup image as an asset.
7. Create or update a mockup record linked to the listing, design, template, color variant, and output asset.

The MVP may use simple flat compositing. Perspective warping, fabric effects, displacement maps, shadows, and realistic print blending are advanced plugin territory.

## Product and Color Selection

Mockups are tied to specific supplier products and color variants.

The basic tool should let a creator choose from store-configured mockup products such as:

```text
SwiftPOD / Gildan 64000 T-Shirt
```

For that product, the user should choose:

- one or more mockup templates
- one or more color variants available for each template
- one or more final design variants when different artwork is needed for different shirt colors

The exact provider color names should be preserved as entered, because those names are needed when preparing Printify or another provider manually.

## Workflow

The user opens an item in the Listing stage. The item has one selected final design variant:

```text
Design: Dark shirt export
Asset: does-a-17-hit-dark.png
```

The user selects:

```text
Product: SwiftPOD / Gildan 64000 T-Shirt
Template: Flat front shirt mockup
Colors: Black, Navy, Dark Heather
```

FusionCanvas generates one mockup for each selected color, stores each output as an asset, and creates mockup records connected to the listing and design.

The user edits:

```text
Title: Does a 17 Hit? DnD Shirt
Description: ...
Price: 24.99
Status: Ready for manual Printify setup
```

The item is now prepared for manual listing work or for a future Printify/Shopify plugin.

## Readiness

The Basic Listing Tool should provide a lightweight readiness checklist.

Useful readiness checks include:

- final design selected
- product mockup settings selected
- at least one mockup generated or attached
- title exists
- description exists
- price exists
- status reflects the current action needed
- provider product and color names captured when relevant

Readiness should help the creator see what is missing. It should not block manual work unless a specific command cannot run without required data.

## Extensibility

The Basic Listing Tool is the default implementation for the Listing stage.

Future plugins may provide alternative or companion listing tools, such as:

- Printify product creation
- Shopify product creation or sync
- Etsy listing preparation
- marketplace validation
- advanced mockup rendering
- provider catalog import
- shipping profile mapping
- pricing calculators
- SEO scoring
- bulk mockup generation
- bulk listing publishing

These tools should be selectable through the Stage Tool Host when they support the current item and stage context.

## Acceptance Criteria

- A user can open the Basic Listing Tool for an existing item in the Listing stage.
- The tool does not allow listing work without an existing item.
- The tool receives store, niche, topic path, item, stage, selected concept, selected final design variants, tags, metadata, nearby work, and available mockup product settings from the Stage Tool Host.
- The user can edit title, description, price, status, notes, marketplace notes, draft tags, and basic preparation fields.
- The user can manually attach existing mockup images.
- The user can generate mockups from configured product templates and color variants.
- Generated mockups are saved as assets and linked to mockup records.
- Mockup records preserve product, vendor, template, color variant, design variant, view, output asset, and intended use where known.
- The tool shows readiness information for missing design, mockup, metadata, price, and provider preparation data.
- The tool does not create or update Printify, Shopify, Etsy, or other external marketplace products directly.
- The default listing tool can coexist with future plugin-provided Listing-stage tools.

## Out of Scope

- Direct Printify product creation
- Direct Shopify product creation
- Marketplace publishing or sync
- Provider catalog import
- Automated shipping profile configuration
- Automated pricing optimization
- Advanced realistic mockup rendering
- Bulk mockup generation
- Full marketplace validation
- SEO scoring

## Related Notes

- [[Roadmap]]
- [[Product]]
- [[Data Model]]
- [[FC-0008 - Workflow Stage Navigator]]
- [[FC-0010 - Context-Aware Tools]]
- [[FC-0011 - Stage Tool Host]]
- [[FC-0203 - Design Records]]
- [[FC-0206 - Manual Mockup Records]]
- [[FC-0207 - Listing Metadata Editor]]
- [[FC-0211 - Basic Design Tool]]
- [[FC-0213 - Mockup Product Settings]]
- [[FC-0408 - Listing Text Generation]]
- [[FC-0603 - Printify Integration]]
- [[FC-0604 - Shopify Integration]]
