# Phase 2 - Product Creation Workflow

## Purpose

Phase 2 turns FusionCanvas from a creative organizer into a usable idea-to-listing production workspace.

The goal is to preserve the full creative chain for each item:

```text
Idea -> Concept -> Design -> Listing
```

Phase 2 should support creators who move between manual work, external tools, AI assistance, and FusionCanvas-native tools without losing context.

## Phase Goal

A creator should be able to capture an idea, refine it into a clear concept, create or import final design artwork, preserve variants and history, and prepare the item for listing work.

The phase does not need to automate every step. It should make each step explicit, context-aware, and connected to the current item.

## Target User

Phase 2 is for creators who already have stores, niches, groups, and listings organized in FusionCanvas and now want to move real items through production.

The primary user is managing:

- raw ideas
- concept versions
- phrases and graphic directions
- design variants
- generated or imported assets
- prompt history
- mockups or preview records
- listing metadata drafts

## Scope

Phase 2 includes:

- [[FC-0201 - Idea Inbox|idea inbox]]
- [[FC-0202 - Concept Versions|concept versions]]
- [[FC-0203 - Design Records|design records]]
- [[FC-0204 - Asset Relationships|asset relationships]]
- [[FC-0205 - Prompt History|prompt history]]
- [[FC-0206 - Manual Mockup Records|manual mockup records]]
- [[FC-0207 - Listing Metadata Editor|listing metadata editor]]
- [[FC-0208 - Design Triangle View|design triangle view]]
- [[FC-0209 - Creative History Timeline|creative history timeline]]
- [[FC-0210 - Basic Concept Tool|basic concept tool]]
- [[FC-0211 - Basic Design Tool|basic design tool]]
- [[FC-0212 - Basic Listing Tool|basic listing tool]]
- [[FC-0213 - Mockup Product Settings|mockup product settings]]

Phase 2 does not include:

- marketplace publishing
- marketplace sync
- full plugin marketplace behavior
- full raster or vector image editing
- advanced automated mockup generation beyond the basic configured-template mockup workflow
- analytics
- batch workflow automation
- guaranteed legal validation or originality checks

## Product Principles

Phase 2 should follow these principles:

- Keep the item as the center of the workflow.
- Let users skip ahead when they already have enough information.
- Preserve useful alternatives instead of overwriting them.
- Treat AI as optional assistance, not the source of truth.
- Make manual, external AI, and in-app AI workflows equally valid.
- Keep final approval explicit.
- Use the same context-aware tool model across Idea, Concept, Design, and Listing.

---

# Stage Definitions

## Idea

The Idea stage captures the initial product seed.

Idea-stage work may begin from a selected topic and create new items. The output should be concrete enough to become an item in the navigation structure.

Examples:

- rough joke
- audience observation
- phrase fragment
- visual seed
- product angle

Relevant specs:

- [[FC-0201 - Idea Inbox]]
- [[FC-0404 - Idea Generation]]

## Concept

The Concept stage clarifies the creative direction for a specific existing item.

Concept-stage work is item-bound. The Basic Concept Tool uses the design triangle:

```text
        Idea

Phrase        Graphic
```

The output should be a selected concept version that explains what the design is about, what phrase it uses if any, what graphic direction it needs if any, and what constraints or risks matter.

Relevant specs:

- [[FC-0202 - Concept Versions]]
- [[FC-0208 - Design Triangle View]]
- [[FC-0210 - Basic Concept Tool]]
- [[FC-0405 - Concept Refinement]]
- [[FC-0406 - Phrase Generation]]
- [[FC-0409 - Critique and Scoring]]

## Design

The Design stage turns the selected concept into concrete shirt artwork for the same item.

Design-stage work is item-bound. The Basic Design Tool supports:

- manual design import from tools such as Affinity Designer or Photoshop
- external AI image import from tools such as ChatGPT or Midjourney
- in-app AI image generation when configured
- multiple draft variants
- tags and metadata for dark shirts, light shirts, product colors, product types, or marketplace uses
- basic cleanup metadata and lightweight preparation support
- explicit promotion of one or more variants as final selected artwork

The output should be at least one selected final design variant connected to its assets, source context, and production notes.

Relevant specs:

- [[FC-0203 - Design Records]]
- [[FC-0204 - Asset Relationships]]
- [[FC-0205 - Prompt History]]
- [[FC-0209 - Creative History Timeline]]
- [[FC-0211 - Basic Design Tool]]
- [[FC-0407 - Image Prompt Generation]]

## Listing

The Listing stage prepares the item for marketplace presentation.

Listing-stage work is item-bound. The Basic Listing Tool supports:

- editing title, description, price, status, notes, and marketplace preparation fields
- generating basic local mockups from configured product templates
- attaching manually created mockups
- preserving provider product and color names for manual Printify or Shopify setup
- tracking readiness for manual publishing or future integration plugins
- keeping direct Printify and Shopify product creation out of the basic tool

The Listing stage should remain context-aware and use the same Stage Tool Host pattern as Concept and Design. The output should be a prepared listing with enough mockups, metadata, price, and status information to support manual marketplace setup or future publishing plugins.

Relevant specs:

- [[FC-0206 - Manual Mockup Records]]
- [[FC-0207 - Listing Metadata Editor]]
- [[FC-0212 - Basic Listing Tool]]
- [[FC-0213 - Mockup Product Settings]]
- [[FC-0408 - Listing Text Generation]]

# Phase-Level Acceptance Criteria

Phase 2 is successful when:

- A creator can capture or import ideas and turn them into items.
- A creator can refine a selected item into one or more concept versions.
- A creator can select the concept version that should drive design work.
- A creator can create, import, or generate multiple design variants for the selected item.
- A creator can preserve source files, generated images, exports, prompts, and related assets.
- A creator can explicitly select final artwork for downstream listing work.
- A creator can configure basic product mockup settings for a store.
- A creator can generate or attach mockups for selected final artwork.
- A creator can prepare title, description, price, status, and basic marketplace notes without direct publishing integration.
- A creator can review useful creative history without digging through folders or chat logs.
- The workflow from Idea to Concept to Design to Listing feels connected even when external tools are used.

# Open Questions

- Should Design-stage final selection require an approved status, or can any variant be selected as final?
- Should shirt color mappings start as tags, structured metadata, or both?
- Should basic cleanup operations be built in, plugin-provided, or both?
- Should the first mockup placement UI use manual numeric entry only, or include a simple visual placement editor?

# Related Notes

- [[Roadmap]]
- [[Product]]
- [[Data Model]]
- [[Principles]]
- [[FC-0008 - Workflow Stage Navigator]]
- [[FC-0010 - Context-Aware Tools]]
- [[FC-0011 - Stage Tool Host]]
