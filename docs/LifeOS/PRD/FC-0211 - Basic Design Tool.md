# FC-0211 - Basic Design Tool

## Summary

The Basic Design Tool is the default built-in tool for the Design stage.

It helps the creator turn a selected concept for a specific item into one or more concrete design variants that can eventually be used on a shirt.

The tool supports three valid production paths:

- manual design work in an external tool such as Affinity Designer or Photoshop
- design generation in an external AI tool such as ChatGPT, Midjourney, or another image system, followed by import
- design generation inside FusionCanvas through a configured AI provider

There is no single correct production path. FusionCanvas should preserve context, variants, assets, prompts, and approval decisions regardless of how the design was created.

## User Need

As a creator, I need a focused place to collect, compare, clean up, and select final artwork for a specific item after the concept is clear.

As an AI-assisted creator, I need FusionCanvas to use the selected concept, niche context, topic path, style guidance, constraints, and prior creative history when generating or preparing design prompts.

As a plugin author, I need the default Design-stage experience to use the same Stage Tool Host and context model as ideation and concept tools, so specialized design tools can be added later.

## Goals

- Provide the default Design-stage tool for a single selected item.
- Require item context and a selected concept or equivalent design-ready creative brief.
- Support manual design import without forcing AI use.
- Support importing externally generated AI artwork.
- Support in-app AI design generation when a provider with image-generation capability is configured.
- Allow many experimental variants to exist without making all of them final.
- Allow one or more variants to be promoted as selected final designs.
- Track variations for dark shirts, light shirts, color groups, product types, or other future variant mappings.
- Preserve source files, exported files, generated images, prompts, and cleanup operations as related assets and history.
- Keep FusionCanvas lightweight: useful for organization, prompt context, import, basic cleanup, and selection, but not a full graphic design application.
- Keep the tool extensible through the Stage Tool Host.

## Core Principle

The Design stage creates the final visual implementation of a concept.

The Concept stage decides what the design should be about:

- idea
- phrase
- graphic direction
- audience reaction
- creative risks
- style or marketplace constraints

The Design stage decides what the final shirt artwork actually is:

- source artwork
- generated or imported artwork
- exported print files
- variants for different shirt colors or product uses
- selected final version or versions

The design work is always attached to a specific item. The tool can create related design variants and assets, but it should not operate as a free-floating image generator without item context.

## Requirements

- The Basic Design Tool is available from the Design stage for an existing item.
- The tool is not available as a free-floating design workspace without an item.
- If no item is selected, the Stage Tool Host should show an appropriate empty state or creation path instead of opening the tool.
- The selected item must have a valid navigation context, including store, niche, and topic path where applicable.
- The tool should use the selected concept version by default.
- If no concept version is selected, the tool can use listing-level idea, phrase, and graphic direction only when they are sufficient to form a design brief.
- The tool receives context from the Stage Tool Host instead of scraping UI state.
- The tool displays the current item context, including store, niche, topic path, item title, stage, selected concept, inherited tags, and relevant metadata.
- The tool displays a design brief derived from the selected concept.
- The user can edit Design-stage notes and constraints without modifying the underlying concept unless they explicitly choose to update the concept.
- The tool supports importing source files and exported artwork.
- The tool supports importing externally generated AI images.
- The tool supports creating multiple design records or design variants for the same item.
- Each design variant can have a name, status, notes, source, prompt, related assets, and tags.
- A design variant can be marked for dark shirts, light shirts, all shirts, specific colors, product types, marketplaces, or other later product-variant mappings.
- A design variant can be promoted as selected final artwork.
- More than one design variant can be selected when the item needs separate final artwork for different shirt colors or uses.
- Promotion to final artwork should not delete rejected, draft, or superseded variants.
- The user can reject, archive, or mark variants as needs revision.
- The tool can advance the item toward Listing once at least one final design variant is selected.

## Design Variant Metadata

Each design variant should preserve enough information to understand how it was produced and how it should be used later.

Useful metadata includes:

```text
Design variant
- source method: manual, external AI, in-app AI, imported asset, plugin
- selected concept version
- visual style
- constraints
- intended shirt colors
- intended product types
- marketplace or brand notes
- prompt or generation settings when AI was used
- source asset
- export asset
- cleanup steps
- approval state
```

Variant tags should support practical downstream use, such as:

```text
dark-shirts
light-shirts
black-shirt
white-shirt
text-only
badge
distressed
needs-upscale
needs-transparent-cleanup
final
```

Tags and metadata should later be usable by listing, mockup, export, and product-variant tools.

## Basic Tool Layout

The default tool should use a focused Design-stage layout:

- context summary near the top
- stage tool selector provided by the Stage Tool Host when multiple Design tools are available
- selected concept or design brief panel
- variant workspace showing imported and generated variants
- actions for import, generate, duplicate, compare, reject, mark needs revision, promote final, and proceed
- variant detail panel for status, tags, notes, source, intended use, and related assets
- optional basic cleanup actions when supported
- history or activity area showing imports, generations, cleanup operations, and final selection changes

The variant workspace is the primary interaction model. The creator should be able to experiment freely, then choose the final design or designs deliberately.

## Workflow 1: Manual Design in External Tool

The user opens an item in the Design stage. The item has a selected concept:

```text
Phrase: Does a 17 hit?
Graphic: A tiny adventurer facing only the huge clawed foot of a monster
Style: dark fantasy, distressed shirt graphic, readable at thumbnail size
```

The user creates the artwork manually in Affinity Designer.

The user imports:

- the Affinity source file
- a transparent PNG export for dark shirts
- a second transparent PNG export adjusted for light shirts

FusionCanvas creates or updates design variants, attaches the assets, and lets the user tag the variants:

```text
Variant A: Dark shirt export
Tags: dark-shirts, final

Variant B: Light shirt export
Tags: light-shirts, final
```

The item can proceed to Listing because final design assets are selected.

## Workflow 2: External AI Generation and Import

The user generates artwork in an external AI tool, then imports the result into FusionCanvas.

FusionCanvas treats this similarly to manual import, with additional AI-related metadata where the user provides it:

- tool or provider name
- prompt text
- seed or settings when known
- imported image
- cleanup notes
- required follow-up tasks

Because AI images often need cleanup, the Basic Design Tool may support lightweight adjustment actions such as:

- crop to artwork bounds
- remove near-transparent edge pixels
- inspect transparency
- record upscale requirement
- attach an upscaled replacement
- mark background cleanup needed

FusionCanvas should not try to become Photoshop, Affinity Designer, or a full image editor.

## Workflow 3: Generate Inside FusionCanvas

When an AI provider with image-generation capability is configured, the user can generate design variants inside FusionCanvas.

The tool should build generation context from:

- store context
- niche context
- topic path
- item title
- selected concept version
- idea
- phrase
- graphic direction
- visual style notes
- constraints
- inherited tags and metadata
- relevant sibling items
- accepted, rejected, or superseded concept/design history

The user should be able to describe or adjust Design-stage generation instructions, such as:

```text
Visual style: gritty woodcut-inspired fantasy shirt graphic
Composition: centered, high contrast, transparent background
Text: include the exact phrase "Does a 17 hit?"
Constraints: no copyrighted characters, no mockup background, readable at thumbnail size
Output intent: print-ready t-shirt artwork
```

The tool can generate multiple variants in a session.

Generated variants are drafts until the user accepts, rejects, edits metadata, imports a replacement, or promotes one as final.

AI generation should never overwrite final selected artwork without explicit approval.

## Basic Cleanup Support

The Basic Design Tool may offer rudimentary cleanup and preparation support.

Examples:

- crop to visible artwork
- identify transparent background status
- remove fully transparent border pixels
- flag semi-transparent edge artifacts
- record target size or export dimensions
- attach replacement assets after external cleanup
- mark a variant as needing upscale, cleanup, vectorization, or manual revision

Cleanup should be implemented through application services or plugin-provided image processors where practical.

Outcomes should be recorded as asset or design metadata so later tools know whether the artwork is ready.

## Final Selection

The key Design-stage decision is final selection.

A selected final design is the artwork FusionCanvas should use by default for listing work, mockups, export, and marketplace preparation.

The tool should support:

- one final variant for simple designs
- separate final variants for dark and light shirts
- selected variants for specific product or color groups
- changing the selected final variant later
- preserving earlier final selections in history

Final selection should be explicit. Importing or generating an image does not automatically mean it is final.

## Extensibility

The Basic Design Tool is the default implementation for the Design stage.

Future plugins may provide alternative or companion design tools, such as:

- Affinity Designer round-trip integration
- Photoshop round-trip integration
- OpenAI image generation
- Midjourney import assistant
- local image model generation
- transparent background cleanup
- upscaling
- vectorization
- trademark or policy review
- print-readiness validation
- colorway generation
- bulk variant comparison

These tools should be selectable through the Stage Tool Host when they support the current item and stage context.

## Acceptance Criteria

- A user can open the Basic Design Tool for an existing item in the Design stage.
- The tool does not allow design work without an existing item.
- The tool receives store, niche, topic path, item, stage, selected concept, tags, metadata, nearby work, and AI capabilities from the Stage Tool Host.
- The tool shows a design brief derived from the selected concept or sufficient listing-level creative fields.
- The user can import manually created source files and exported artwork.
- The user can import externally generated AI artwork and optionally record the prompt or source tool.
- The user can create and preserve multiple design variants for the same item.
- The user can tag or classify variants for dark shirts, light shirts, specific colors, product types, or other later variant mappings.
- The user can mark variants draft, needs revision, approved, rejected, selected final, ready for export, or exported.
- The user can promote one or more variants as final selected designs.
- Final selection does not delete rejected, draft, or superseded variants.
- If image-generation AI is configured, the user can generate design variants using the current item and concept context.
- Generated variants remain drafts until the user approves or promotes them.
- The tool may offer rudimentary cleanup actions, but it does not become a full graphic design tool.
- The item can advance toward Listing when at least one final design variant is selected.
- The default design tool can coexist with future plugin-provided Design-stage tools.

## Out of Scope

- Full raster image editing
- Full vector editing
- Layer-based design authoring
- Guaranteed print quality
- Guaranteed originality
- Full legal validation
- Automatic marketplace product creation
- Mockup generation
- Listing metadata generation
- Bulk multi-item design generation

## Related Notes

- [[Roadmap]]
- [[Product]]
- [[Data Model]]
- [[FC-0008 - Workflow Stage Navigator]]
- [[FC-0010 - Context-Aware Tools]]
- [[FC-0011 - Stage Tool Host]]
- [[FC-0109 - Import Existing Assets]]
- [[FC-0202 - Concept Versions]]
- [[FC-0203 - Design Records]]
- [[FC-0204 - Asset Relationships]]
- [[FC-0205 - Prompt History]]
- [[FC-0209 - Creative History Timeline]]
- [[FC-0210 - Basic Concept Tool]]
- [[FC-0401 - AI Provider Abstraction]]
- [[FC-0402 - AI Settings]]
- [[FC-0407 - Image Prompt Generation]]
