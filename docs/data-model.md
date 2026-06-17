# FusionCanvas Data Model

## Overview

The FusionCanvas data model describes the core entities used by the application and how they relate to each other.

The goal is to support a structured Print on Demand workflow while remaining flexible enough to evolve.

FusionCanvas should use a hybrid model:

- SQLite tables for stable, queryable structure
- JSON metadata fields for flexible, evolving information
- managed workspace file paths for large assets such as images, source files, and mockups

The data model should preserve creative context, workflow state, and relationships between stores, niches, listings, designs, and assets.

## Core Principles

### Structured Where Stable

Information that is central to the application should be stored in structured fields.

Examples:

- ids
- names
- parent relationships
- workflow stage
- created dates
- modified dates
- workflow status
- store ownership

Structured fields allow fast search, filtering, sorting, and validation.

### Flexible Where Evolving

Information that may vary between stores, niches, integrations, or plugins should be stored in JSON metadata.

Examples:

- AI instructions
- niche-specific constraints
- platform-specific listing data
- plugin-specific settings
- prompt history
- experimental scoring data

### Files Stay as Files

Large binary assets should not be embedded directly in the database.

The database should store paths to files managed inside the FusionCanvas workspace.

Imported files are copied into the workspace. The copied file becomes the authoritative asset used by FusionCanvas, while the original filename and import source can be preserved as metadata when useful.

Examples:

- SVG files
- PNG exports
- Affinity Designer files
- mockup images
- source photos
- generated images

## Primary Entities

```text
Store
  Mockup Product
    Mockup Template
      Mockup Color Variant
  Niche
    Group
      Listing
        Concept
        Design
        Asset
        Mockup
        Listing Metadata
        Performance Data
```

## Navigation Topic and Item Model

The navigable workspace should be understood as a topic/item tree:

- A topic is a folder-like grouping node.
- An item is a concrete unit of work.

In the initial model, `Niche` and `Group` behave as topics, and `Listing` behaves as an item. The top-level topics inside a store are niches by default. Groups provide arbitrary-depth subtopics beneath a niche.

The data model should support browsing, filtering, moving, and converting between topic and item roles without requiring users to understand the underlying table structure.

The current navigation context should be derivable from the selected item or topic. This includes store, niche, parent topic path, current topic, current item, tags, workflow stage, and relevant metadata. Creation and generation tools use this context to place new work correctly and produce targeted results.

New work created from a topic should be able to inherit applicable tags and metadata from the selected topic and parent context. Inheritance should reduce manual data entry, but it should remain understandable and editable so inherited context does not become hidden magic.

## Store

A store represents a brand, business, client, or publishing context.

A store is the highest-level business context in FusionCanvas.

Suggested fields:

```text
Store
- Id
- Name
- Description
- CreatedAt
- UpdatedAt
- IsArchived
- MetadataJson
```

Metadata example:

```json
{
  "brandVoice": "Witty, fantasy-inspired, slightly sarcastic",
  "defaultMarketplace": "Shopify",
  "targetRegion": "US",
  "aiInstructions": "Prefer adult tabletop humor. Avoid childish fantasy cliches."
}
```

## Niche

A niche represents a target audience, theme, or market segment inside a store.

A store may have one or more niches. A niche may be narrow or broad, and it may evolve over time.

A niche is more than a folder. It contains knowledge that can guide design and AI assistance.

Suggested fields:

```text
Niche
- Id
- StoreId
- Name
- Description
- CreatedAt
- UpdatedAt
- IsArchived
- MetadataJson
```

Metadata example:

```json
{
  "audience": "Adult tabletop roleplaying players",
  "humorStyle": "Dry, sarcastic, self-aware",
  "visualStyle": "Monochrome, distressed, fantasy illustration",
  "avoid": [
    "childish cartoon style",
    "direct trademark references",
    "overly cute mascots"
  ],
  "successfulPatterns": [
    "relatable table jokes",
    "dice failure humor",
    "party role jokes"
  ],
  "aiInstructions": "Designs should feel like they were made by someone who actually plays tabletop RPGs."
}
```

## Group

A group organizes listings inside a niche.

Groups may represent folders, collections, campaigns, experiments, or temporary work areas. Groups may contain subgroups, allowing an arbitrary number of nested topic levels beneath a niche.

Suggested fields:

```text
Group
- Id
- StoreId
- NicheId
- ParentGroupId
- Name
- Description
- SortOrder
- CreatedAt
- UpdatedAt
- IsArchived
- MetadataJson
```

Groups should support moving within the same store and niche tree where the move does not create a cycle. Moving a group moves its full subtree, including child groups and listings.

An empty group may be converted into a listing or another item type if it has no child groups and no child items. A non-empty group must remain a topic until its children are moved, deleted, archived elsewhere, or otherwise resolved.

## Listing

A listing is the central working object in FusionCanvas.

A listing represents a product concept, not merely an image file. A listing may eventually become one or more marketplace products.

Suggested fields:

```text
Listing
- Id
- StoreId
- NicheId
- GroupId
- Title
- WorkingTitle
- WorkflowStage
- Status
- CreatedAt
- UpdatedAt
- PublishedAt
- ArchivedAt
- MetadataJson
```

Possible status values:

```text
Draft
Published
Paused
Rejected
```

Core workflow stages:

```text
Idea
Concept
Design
Listing
Archive
```

The core workflow stage is used by the visible workflow navigator and stage filtering. Status describes the item's broad operational state: Draft, Published, Paused, or Rejected.

Metadata example:

```json
{
  "idea": "A doomed hero tries to attack a massive monster",
  "phrase": "Does a 17 hit?",
  "graphic": "Tiny adventurer standing beside an enormous monster foot",
  "tone": "Dark humor, absurd tabletop realism",
  "notes": "Avoid making the number 17 look magical or special by itself.",
  "score": {
    "idea": 8,
    "phrase": 9,
    "graphic": 8,
    "overall": 8.5
  }
}
```

A listing behaves as an item in the navigation tree.

A listing should be movable between valid topics without losing status, tags, assets, prompt history, marketplace data, or other creative context.

A listing may be converted into a topic at any time. The resulting topic should preserve the listing's name, relevant metadata, tags, and history where practical, and should be able to contain new child topics or items.

## Concept

A concept captures the creative reasoning behind a listing.

A listing may have multiple concepts over time, especially during ideation. Concepts should preserve abandoned ideas as well as accepted ones.

Concept work is always attached to a listing or other concrete item. There is no standalone concept outside the navigation structure. The selected concept version supplies the design triangle values used by the Concept-stage tool.

Suggested fields:

```text
Concept
- Id
- ListingId
- Version
- Title
- Description
- IsSelected
- CreatedAt
- UpdatedAt
- MetadataJson
```

Metadata example:

```json
{
  "idea": "A last stand against impossible odds",
  "phrase": "Does a 17 hit?",
  "graphicDirection": "Show scale difference using only the monster's foot",
  "phraseUsed": true,
  "graphicUsed": true,
  "designTriangleScore": {
    "idea": 8,
    "phrase": 9,
    "graphic": 8,
    "overall": 8.5,
    "weakestElement": "graphic"
  },
  "audienceReaction": "Players recognize the doomed optimism of asking whether a mediocre roll hits.",
  "risks": [
    "Could be visually unclear without enough fantasy context"
  ],
  "source": "Basic Concept Tool"
}
```

## Design

A design represents a concrete visual implementation of a concept.

A listing may have multiple designs or design versions.

A design may be created manually in an external design tool, imported from an external AI tool, generated inside FusionCanvas, or produced by a plugin. FusionCanvas should preserve the creative and production context regardless of the source.

A design can also represent a variant intended for a specific use, such as dark shirts, light shirts, specific shirt colors, product types, or marketplace exports.

Suggested fields:

```text
Design
- Id
- ListingId
- ConceptId
- Name
- Version
- Status
- SourceMethod
- IsFinalSelected
- CreatedAt
- UpdatedAt
- MetadataJson
```

Possible status values:

```text
Draft
ReadyForExport
Exported
NeedsRevision
Approved
Rejected
SelectedFinal
```

## Asset

An asset is a file or external resource used by a listing, design, concept, or store.

Examples:

- source Affinity file
- exported PNG
- SVG
- mockup image
- texture
- brush
- font
- AI-generated image
- reference image

Suggested fields:

```text
Asset
- Id
- StoreId
- ListingId
- DesignId
- AssetType
- FilePath
- OriginalFileName
- Title
- Description
- CreatedAt
- UpdatedAt
- MetadataJson
```

Possible asset types:

```text
SourceFile
Export
Mockup
Reference
Inspiration
Texture
Brush
Font
PromptOutput
GeneratedImage
CleanedExport
UpscaledExport
```

## Mockup

A mockup represents a visual preview of a design on a product.

A mockup may be generated locally, by a plugin, or by an external service.

Suggested fields:

```text
Mockup
- Id
- ListingId
- DesignId
- AssetId
- MockupProductId
- MockupTemplateId
- MockupColorVariantId
- ProductType
- ColorName
- ViewName
- CreatedAt
- UpdatedAt
- MetadataJson
```

## Mockup Product Settings

Mockup product settings define the store-level products, templates, placement mappings, and color variants used by the Basic Listing Tool.

These records are store-scoped because each store may sell different supplier products and color selections.

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
- CreatedAt
- UpdatedAt
- MetadataJson
```

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
- CreatedAt
- UpdatedAt
- MetadataJson
```

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
- CreatedAt
- UpdatedAt
- MetadataJson
```

For the MVP, these settings may be manually entered and stored in dedicated tables or in store-level JSON metadata. Dedicated tables become more useful once users have multiple products, templates, and color variants per store.

## Phrase

A phrase is a reusable verbal hook.

Phrases may belong to listings, niches, or global phrase libraries.

Suggested fields:

```text
Phrase
- Id
- StoreId
- NicheId
- Text
- PhraseType
- CreatedAt
- UpdatedAt
- MetadataJson
```

Possible phrase types:

```text
Original
Snowclone
Catchphrase
Idiom
QuoteInspired
SongTitleInspired
Template
```

## Idea

An idea captures the emotional, situational, or conceptual seed behind a design.

Ideas may exist before they become listings.

For the MVP, a captured idea may be represented directly as a listing in the Idea stage when that keeps the workflow simpler. The separate Idea entity remains useful for raw inbox capture, rejected generated suggestions, or later workflows that need ideas before listing creation.

Suggested fields:

```text
Idea
- Id
- StoreId
- NicheId
- GroupId
- Title
- Description
- IdeaState
- CreatedAt
- UpdatedAt
- MetadataJson
```

Possible idea states:

```text
Draft
Candidate
Selected
ConvertedToListing
Rejected
```

## Prompt

A prompt stores AI instructions, outputs, and context.

Prompt history should be preserved because old prompts may become valuable later.

Suggested fields:

```text
Prompt
- Id
- StoreId
- NicheId
- ListingId
- ConceptId
- DesignId
- PromptType
- Provider
- Model
- InputText
- OutputText
- CreatedAt
- MetadataJson
```

Possible prompt types:

```text
IdeaGeneration
ConceptRefinement
PhraseGeneration
ImagePrompt
ImageGeneration
ListingText
Critique
MetadataSuggestion
NicheResearch
```

## Marketplace Product

A marketplace product represents an external product created from a listing.

One FusionCanvas listing may create multiple marketplace products.

Suggested fields:

```text
MarketplaceProduct
- Id
- ListingId
- Marketplace
- ExternalId
- ExternalUrl
- Status
- CreatedAt
- UpdatedAt
- MetadataJson
```

Possible marketplaces:

```text
Shopify
Printify
Etsy
AmazonMerch
CreativeFabrica
Other
```

## Performance Data

Performance data stores marketplace or advertising signals.

This data helps determine which designs, niches, phrases, and styles are working.

Suggested fields:

```text
PerformanceData
- Id
- ListingId
- MarketplaceProductId
- Source
- Date
- Impressions
- Clicks
- Favorites
- AddToCarts
- Sales
- Revenue
- AdSpend
- CreatedAt
- MetadataJson
```

Derived metrics should usually be calculated rather than manually stored.

Examples:

```text
CTR
ConversionRate
ROAS
Profit
RevenuePerImpression
```

## Plugin Data

Plugins may need to store their own data.

The core data model should allow plugins to attach metadata without requiring schema changes.

Suggested fields:

```text
PluginData
- Id
- PluginId
- EntityType
- EntityId
- DataJson
- CreatedAt
- UpdatedAt
```

This allows plugin-specific extensions while keeping the core schema stable.

## Tags

Tags provide flexible classification across entities.

Tags may be attached to listings, topics, assets, phrases, ideas, or other entities.

Suggested fields:

```text
Tag
- Id
- StoreId
- Name
- Color
- CreatedAt
- MetadataJson
```

```text
EntityTag
- Id
- TagId
- EntityType
- EntityId
```

Tags should be usable as navigation filters. Both topics and items may have tags, and filtered navigation should be able to show matching tagged topics, matching tagged items, and the parent path needed to understand each match.

## Navigation Query Model

The application should expose a navigation query model that can return a visible tree for a store or subtree.

Supported filters should include:

- text search across topic names, item titles, and selected metadata fields
- workflow stage filters
- tag filters for topics and items
- item status filters
- entity type filters where useful
- archived or active state

Default behavior:

- all top-level topics are visible until a filter is applied
- child topics and items are shown according to normal expansion state
- filtering should preserve parent context for matching descendants
- expand all and collapse all operate on the currently visible tree

Filtering should be implemented from structured fields where possible. JSON metadata can be indexed for selected fields if workflows prove the need.

## Relationships

Store relationships:

```text
Store
- has many Niches
- has many Groups
- has many Listings
- has many Assets
- has many Tags
- has many MockupProducts
```

Niche relationships:

```text
Niche
- belongs to Store
- has many Groups
- has many Listings
- has many Ideas
- has many Phrases
- behaves as a top-level Topic in navigation
```

Group relationships:

```text
Group
- belongs to Store
- belongs to Niche
- may belong to ParentGroup
- has many ChildGroups
- has many Listings
- behaves as a nested Topic in navigation
```

Listing relationships:

```text
Listing
- belongs to Store
- belongs to Niche
- belongs to Group
- has many Concepts
- has many Designs
- has many Assets
- has many Mockups
- has many Prompts
- has many MarketplaceProducts
- has many PerformanceData records
- behaves as an Item in navigation
```

Mockup product relationships:

```text
MockupProduct
- belongs to Store
- has many MockupTemplates

MockupTemplate
- belongs to MockupProduct
- may reference a TemplateAsset
- has many MockupColorVariants

MockupColorVariant
- belongs to MockupTemplate
- may reference a color-specific TemplateAsset
```

## Open Questions

- Should Idea and Phrase be independent entities from the beginning, or should they initially live inside Listing metadata?
- Should prompts be stored in full, or should large prompt outputs be stored as external files?
- Should performance data be imported manually first before building marketplace integrations?
- Should assets be linked to multiple listings?
- Should global phrase libraries exist outside stores?
- Should niches be allowed to exist across multiple stores?
- Should JSON metadata be indexed for selected fields?
- Should archived data remain in the main tables or move to archive tables?
- Should rejected generated ideas be stored as Idea records, Prompt metadata, plugin data, or a dedicated ideation history table?
- Should mockup product settings start as dedicated tables, store metadata JSON, or a hybrid model?

## Initial Recommendation

For the basic working MVP, keep the model simple but complete enough to support the full Idea -> Concept -> Design -> Listing flow:

```text
Store
Niche
Group
Listing
Concept
Design
Asset
Prompt
Tag
Mockup
MockupProduct
MockupTemplate
MockupColorVariant
```

Use JSON metadata for everything else until the need for dedicated tables becomes clear.

Raw `Idea` and reusable `Phrase` records can remain listing metadata in the first implementation unless the Idea Inbox or phrase library workflow proves they need separate tables. Marketplace products, performance data, experiments, automation records, and plugin-specific data can wait until their later phases.

The MVP should keep `WorkflowStage` and `Status` conceptually separate:

- `WorkflowStage` drives the visible stage navigator and should stay close to `Idea`, `Concept`, `Design`, `Listing`, and `Archive`.
- `Status` describes operational state: `Draft`, `Published`, `Paused`, or `Rejected`.

Do not use stage-like statuses such as `Idea`, `Concept`, `Design`, and `Listing` as status values. Those belong to `WorkflowStage`.

Avoid over-normalizing too early.

The model should be allowed to evolve as real workflows emerge.
