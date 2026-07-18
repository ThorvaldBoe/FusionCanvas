# FusionCanvas Roadmap

First draft. This roadmap is intentionally feature-oriented rather than specification-oriented. Each item should be expanded through OpenSpec before implementation.

---

# Development Sequence

## Phase 0: Foundation

Goal: establish the application shell, architecture, persistence, and development workflow needed for every later feature.

1. [[FC-0001 - Application Shell|`FC-0001` Application Shell]]
2. [[FC-0002 - Core Domain Model|`FC-0002` Core Domain Model]]
3. [[FC-0003 - Local SQLite Persistence|`FC-0003` Local SQLite Persistence]]
4. [[FC-0004 - Workspace File Storage|`FC-0004` Workspace File Storage]]
5. [[FC-0005 - Navigation Tree|`FC-0005` Navigation Tree]]
6. [[FC-0006 - OpenSpec Project Workflow|`FC-0006` OpenSpec Project Workflow]]
7. [[FC-0007 - Testing Baseline|`FC-0007` Testing Baseline]]
8. [[FC-0008 - Workflow Stage Navigator|`FC-0008` Workflow Stage Navigator]]
9. [[FC-0009 - Tabbed Document Window|`FC-0009` Tabbed Document Window]]
10. [[FC-0010 - Context-Aware Tools|`FC-0010` Context-Aware Tools]]
11. [[FC-0011 - Stage Tool Host|`FC-0011` Stage Tool Host]]

## Phase 1: MVP Creative Workspace

Goal: make FusionCanvas useful as a local-first workspace for organizing Print on Demand work.

1. [[FC-0101 - Store Management|`FC-0101` Store Management]]
2. [[FC-0102 - Niche Management|`FC-0102` Niche Management]]
3. [[FC-0103 - Group Management|`FC-0103` Group Management]]
4. [[FC-0104 - Listing Management|`FC-0104` Listing Management]]
5. [[FC-0105 - Listing Lifecycle Status|`FC-0105` Listing Lifecycle Status]]
6. [[FC-0106 - Listing Inspector|`FC-0106` Listing Inspector]]
7. [[FC-0107 - Basic Search and Filtering|`FC-0107` Basic Search and Filtering]]
8. [[FC-0108 - Tags|`FC-0108` Tags]]
9. [[FC-0109 - Import Existing Assets|`FC-0109` Import Existing Assets]]

## Phase 2: Product Creation Workflow

Goal: support the full idea-to-listing flow, even before advanced automation exists.

1. [[FC-0201 - Idea Inbox|`FC-0201` Idea Inbox]]
2. [[FC-0202 - Concept Versions|`FC-0202` Concept Versions]]
3. [[FC-0203 - Design Records|`FC-0203` Design Records]]
4. [[FC-0204 - Asset Relationships|`FC-0204` Asset Relationships]]
5. [[FC-0205 - Prompt History|`FC-0205` Prompt History]]
6. [[FC-0206 - Manual Mockup Records|`FC-0206` Manual Mockup Records]]
7. [[FC-0207 - Listing Metadata Editor|`FC-0207` Listing Metadata Editor]]
8. [[FC-0208 - Design Triangle View|`FC-0208` Design Triangle View]]
9. [[FC-0209 - Creative History Timeline|`FC-0209` Creative History Timeline]]
10. [[FC-0210 - Basic Concept Tool|`FC-0210` Basic Concept Tool]]
11. [[FC-0211 - Basic Design Tool|`FC-0211` Basic Design Tool]]
12. [[FC-0212 - Basic Listing Tool|`FC-0212` Basic Listing Tool]]
13. [[FC-0213 - Mockup Product Settings|`FC-0213` Mockup Product Settings]]

## Phase 3: Batch Workflow

Goal: optimize FusionCanvas for creators working on many listings at once.

1. [[FC-0301 - Multi-Select Operations|`FC-0301` Multi-Select Operations]]
2. [[FC-0302 - Bulk Status Changes|`FC-0302` Bulk Status Changes]]
3. [[FC-0303 - Bulk Metadata Editing|`FC-0303` Bulk Metadata Editing]]
4. [[FC-0304 - Batch Asset Import|`FC-0304` Batch Asset Import]]
5. [[FC-0305 - Batch Export Preparation|`FC-0305` Batch Export Preparation]]
6. [[FC-0306 - Work Queues|`FC-0306` Work Queues]]
7. [[FC-0307 - Saved Views|`FC-0307` Saved Views]]

## Phase 4: AI Assistance

Goal: make AI contextual and useful without making it the source of truth.

1. [[FC-0401 - AI Provider Abstraction|`FC-0401` AI Provider Abstraction]]
2. [[FC-0402 - AI Settings|`FC-0402` AI Settings]]
3. [[FC-0403 - Niche AI Context|`FC-0403` Niche AI Context]]
4. [[FC-0404 - Idea Generation|`FC-0404` Idea Generation]]
5. [[FC-0405 - Concept Refinement|`FC-0405` Concept Refinement]]
6. [[FC-0406 - Phrase Generation|`FC-0406` Phrase Generation]]
7. [[FC-0407 - Image Prompt Generation|`FC-0407` Image Prompt Generation]]
8. [[FC-0408 - Listing Text Generation|`FC-0408` Listing Text Generation]]
9. [[FC-0409 - Critique and Scoring|`FC-0409` Critique and Scoring]]
10. [[FC-0410 - Prompt Reuse Library|`FC-0410` Prompt Reuse Library]]

## Phase 5: Plugin Platform

Goal: expose stable extension points so specialized capabilities can live outside the core application.

1. [[FC-0501 - Plugin Manifest|`FC-0501` Plugin Manifest]]
2. [[FC-0502 - Plugin Discovery|`FC-0502` Plugin Discovery]]
3. [[FC-0503 - Plugin Dependency Registration|`FC-0503` Plugin Dependency Registration]]
4. [[FC-0504 - Plugin Command Contributions|`FC-0504` Plugin Command Contributions]]
5. [[FC-0505 - Plugin Data Storage|`FC-0505` Plugin Data Storage]]
6. [[FC-0506 - Plugin Event Hooks|`FC-0506` Plugin Event Hooks]]
7. [[FC-0507 - Plugin Settings UI|`FC-0507` Plugin Settings UI]]
8. [[FC-0508 - Sample Plugin|`FC-0508` Sample Plugin]]

## Phase 6: Publishing and Integrations

Goal: connect FusionCanvas to external tools while keeping local data authoritative.

1. [[FC-0601 - Marketplace Product Records|`FC-0601` Marketplace Product Records]]
2. [[FC-0602 - Export Package Builder|`FC-0602` Export Package Builder]]
3. [[FC-0603 - Printify Integration|`FC-0603` Printify Integration]]
4. [[FC-0604 - Shopify Integration|`FC-0604` Shopify Integration]]
5. [[FC-0605 - Etsy Integration|`FC-0605` Etsy Integration]]
6. [[FC-0606 - Creative Fabrica Export|`FC-0606` Creative Fabrica Export]]
7. [[FC-0607 - Publishing Status Sync|`FC-0607` Publishing Status Sync]]
8. [[FC-0608 - Marketplace Validation|`FC-0608` Marketplace Validation]]

## Phase 7: Analytics and Learning

Goal: help creators learn from performance and improve future work.

1. [[FC-0701 - Manual Performance Import|`FC-0701` Manual Performance Import]]
2. [[FC-0702 - Performance Data Model|`FC-0702` Performance Data Model]]
3. [[FC-0703 - Listing Performance View|`FC-0703` Listing Performance View]]
4. [[FC-0704 - Niche Performance View|`FC-0704` Niche Performance View]]
5. [[FC-0705 - Phrase and Style Pattern Tracking|`FC-0705` Phrase and Style Pattern Tracking]]
6. [[FC-0706 - Experiment Tracking|`FC-0706` Experiment Tracking]]
7. [[FC-0707 - Improvement Suggestions|`FC-0707` Improvement Suggestions]]

## Phase 8: Advanced Automation

Goal: automate repetitive workflows after the underlying structure has proven itself.

1. [[FC-0801 - Command History|`FC-0801` Command History]]
2. [[FC-0802 - Undo and Redo|`FC-0802` Undo and Redo]]
3. [[FC-0803 - Workflow Templates|`FC-0803` Workflow Templates]]
4. [[FC-0804 - Automation Recipes|`FC-0804` Automation Recipes]]
5. [[FC-0805 - Event-Driven Automation|`FC-0805` Event-Driven Automation]]
6. [[FC-0806 - Scheduled Tasks|`FC-0806` Scheduled Tasks]]
7. [[FC-0807 - End-to-End Production Runs|`FC-0807` End-to-End Production Runs]]

---

# Feature Hierarchy

## `FC-0000` Foundation

Core technical and architectural capabilities required before product features can mature.

### `FC-0001` Application Shell

Avalonia desktop shell with main window, dependency injection, basic layout, and startup flow.

### `FC-0002` Core Domain Model

Initial domain entities for Store, Niche, Group, Listing, Asset, Prompt, and Tag.

### `FC-0003` Local SQLite Persistence

Local database storage for stable structured data, with migrations and repository abstractions.

### `FC-0004` Workspace File Storage

Filesystem structure for large assets such as source files, PNG exports, references, and mockups.

### `FC-0005` Navigation Tree

Hierarchical browsing for Store, Topic, and Item objects. Niches are the default top-level topics inside a store, groups are nested topics, and listings are the initial item type.

The tree should support arbitrary topic depth, move operations, item-to-topic conversion, empty-topic-to-item conversion, expand all, collapse all, and default visibility for all top-level topics.

### `FC-0006` OpenSpec Project Workflow

Standard process for moving ideas into specifications, review, implementation, testing, and archive.

### `FC-0007` Testing Baseline

xUnit coverage for domain logic, application services, and persistence boundaries, plus isolated real desktop UI verification for every user-facing feature and an all-features UI regression matrix in each full QA review.

### `FC-0008` Workflow Stage Navigator

Visible Idea, Concept, Design, and Listing stage navigation for the current item, with completed stages enabled and future stages disabled.

### `FC-0009` Tabbed Document Window

Obsidian-inspired document window with multiple tabs, where the active tab drives navigation context, workflow stage display, and detail content.

### `FC-0010` Context-Aware Tools

Creation and generation tools inherit the active store, niche, topic path, item, workflow stage, metadata, tags, and nearby work so results are targeted to the current context.

### `FC-0011` Stage Tool Host

Host built-in and plugin-provided tools in the lower document-window detail area, with a selector for switching tools when multiple tools support the current stage and context.

## `FC-0100` MVP Creative Workspace

The first usable version of FusionCanvas as a local organizer for Print on Demand work.

### `FC-0101` Store Management

Create, edit, archive, and configure stores as top-level business contexts.

### `FC-0102` Niche Management

Create and maintain niches with audience notes, style guidance, constraints, and AI context.

### `FC-0103` Group Management

Organize listings into flexible groups, collections, campaigns, experiments, and subgroups.

### `FC-0104` Listing Management

Create, edit, move, duplicate, archive, and delete listings as the primary working objects.

### `FC-0105` Listing Lifecycle Status

Track each listing's workflow stage separately from its operational status, so the app can distinguish creative stage from the next action needed.

### `FC-0106` Listing Inspector

Right-side or detail-panel editor for listing title, idea, phrase, graphic direction, notes, status, and metadata.

### `FC-0107` Basic Search and Filtering

Filter the navigation tree by text, tags, item status, store, niche, topic/subtree, and basic searchable fields while preserving enough parent context to understand where each result lives.

### `FC-0108` Tags

Flexible tagging for topics, listings, assets, ideas, phrases, and future entities. Tags should be usable as navigation filters for both topics and items.

### `FC-0109` Import Existing Assets

Attach existing image, design, reference, and source files to listings or stores.

## `FC-0200` Product Creation Workflow

Features that preserve creative context from idea through product listing.

### `FC-0201` Idea Inbox

Capture raw ideas before they become listings.

### `FC-0202` Concept Versions

Preserve multiple concept directions for a listing, including rejected or superseded versions.

### `FC-0203` Design Records

Track concrete design implementations, versions, approval state, and export readiness.

### `FC-0204` Asset Relationships

Connect files to listings, concepts, designs, mockups, stores, or niches.

### `FC-0205` Prompt History

Store AI prompts, provider information, inputs, outputs, and related entities.

### `FC-0206` Manual Mockup Records

Track manually attached and generated mockup images as product previews connected to listings, designs, products, templates, and color variants.

### `FC-0207` Listing Metadata Editor

Edit title, description, keywords, tags, marketplace notes, and platform-specific fields.

### `FC-0208` Design Triangle View

Show and evaluate the relationship between idea, phrase, and graphic for a listing.

### `FC-0209` Creative History Timeline

Timeline of important listing events, concept changes, prompt runs, asset imports, and status changes.

### `FC-0210` Basic Concept Tool

Default Concept-stage tool for an existing item, centered on the design triangle with manual editing, optional AI-assisted node refinement, concept history, quality indication, and save-as-new-item branching.

### `FC-0211` Basic Design Tool

Default Design-stage tool for an existing item, supporting manual design import, external AI image import, variant comparison, basic cleanup metadata, and final design selection. In-app generative image creation is intentionally outside the first MVP slice.

### `FC-0212` Basic Listing Tool

Default Listing-stage tool for an existing item, supporting local mockup generation from configured product templates, manual mockup attachment, title, description, price, status, marketplace notes, and readiness tracking without direct marketplace publishing.

### `FC-0213` Mockup Product Settings

Store-level configuration for supplier products, design areas, mockup templates, template placement mapping, and exact color variants used by the Basic Listing Tool.

## `FC-0300` Batch Workflow

Tools for creators producing many designs and listings at once.

### `FC-0301` Multi-Select Operations

Allow users to select multiple listings, assets, or groups and apply commands together.

### `FC-0302` Bulk Status Changes

Move many listings through workflow stages in one operation.

### `FC-0303` Bulk Metadata Editing

Apply shared metadata, tags, marketplace settings, or notes to multiple listings.

### `FC-0304` Batch Asset Import

Import many files and map them to listings using filenames, folders, or manual matching.

### `FC-0305` Batch Export Preparation

Prepare export-ready files and metadata packages for downstream tools.

### `FC-0306` Work Queues

Focused queues for items needing design, mockups, listing text, publishing, review, or revision.

### `FC-0307` Saved Views

Reusable filters for common workflows such as "ready for mockups" or "needs listing metadata".

## `FC-0400` AI Assistance

Contextual AI tools that assist the creator while preserving human approval.

### `FC-0401` AI Provider Abstraction

Core interface for AI providers so OpenAI, Anthropic, local models, or future providers can be plugins.

### `FC-0402` AI Settings

Manage provider keys, preferred models, default behavior, and privacy controls.

### `FC-0403` Niche AI Context

Use niche knowledge, audience, humor, style, constraints, and successful patterns as AI context.

### `FC-0404` Idea Generation

Basic ideation tool for manual capture, optional AI generation, one-click listing creation, accepted/rejected idea feedback, and future plugin ideation tools.

### `FC-0405` Concept Refinement

Improve raw ideas into clearer concepts with audience reaction, risks, and visual direction.

### `FC-0406` Phrase Generation

Suggest phrases, hooks, variations, and reusable phrase templates.

### `FC-0407` Image Prompt Generation

Generate image prompts from concept, phrase, niche, style, and marketplace constraints.

### `FC-0408` Listing Text Generation

Draft product titles, descriptions, keywords, and tags for marketplace use.

### `FC-0409` Critique and Scoring

Review idea, phrase, and graphic alignment and provide improvement suggestions.

### `FC-0410` Prompt Reuse Library

Store reusable prompt templates for idea, concept, phrase, image, listing, critique, and research workflows.

## `FC-0500` Plugin Platform

Extensibility layer for optional capabilities.

### `FC-0501` Plugin Manifest

Define plugin identity, version, capabilities, dependencies, and settings.

### `FC-0502` Plugin Discovery

Load plugins from known folders and expose them to the application.

### `FC-0503` Plugin Dependency Registration

Allow plugins to register services through dependency injection.

### `FC-0504` Plugin Command Contributions

Allow plugins to add commands to menus, context actions, queues, or entity views.

### `FC-0505` Plugin Data Storage

Let plugins attach structured JSON data to core entities without changing the core schema.

### `FC-0506` Plugin Event Hooks

Publish events such as ListingCreated, AssetImported, MockupGenerated, and ProductPublished.

### `FC-0507` Plugin Settings UI

Provide a standard way for plugins to expose configuration screens.

### `FC-0508` Sample Plugin

Reference plugin demonstrating manifest, commands, settings, data storage, and events.

## `FC-0600` Publishing and Integrations

External publishing and marketplace coordination.

### `FC-0601` Marketplace Product Records

Track external marketplace products linked to FusionCanvas listings.

### `FC-0602` Export Package Builder

Create local export packages containing artwork, mockups, and listing metadata.

### `FC-0603` Printify Integration

Create or update Printify products from listing assets and metadata.

### `FC-0604` Shopify Integration

Sync product information and publishing status with Shopify.

### `FC-0605` Etsy Integration

Prepare or sync listing information for Etsy.

### `FC-0606` Creative Fabrica Export

Package design files and metadata for Creative Fabrica submission.

### `FC-0607` Publishing Status Sync

Refresh external status, URLs, and IDs while preserving FusionCanvas as the source of truth.

### `FC-0608` Marketplace Validation

Check listing data, image requirements, tags, and metadata before export or publishing.

## `FC-0700` Analytics and Learning

Features that turn historical results into future creative guidance.

### `FC-0701` Manual Performance Import

Import performance data from CSV or manual entry before direct marketplace analytics integrations exist.

### `FC-0702` Performance Data Model

Store impressions, clicks, favorites, carts, sales, revenue, ad spend, and related source data.

### `FC-0703` Listing Performance View

Show performance history and derived metrics for individual listings.

### `FC-0704` Niche Performance View

Aggregate performance by niche, group, tag, phrase type, style, or campaign.

### `FC-0705` Phrase and Style Pattern Tracking

Identify recurring patterns in successful and unsuccessful designs.

### `FC-0706` Experiment Tracking

Track experiments across groups, campaigns, prompt strategies, styles, or marketplaces.

### `FC-0707` Improvement Suggestions

Suggest candidates for revision, retirement, re-export, metadata improvement, or new variations.

## `FC-0800` Advanced Automation

Automation built on top of structured data, commands, events, and plugin extension points.

### `FC-0801` Command History

Record user actions in a form that supports auditing, automation, and eventual undo.

### `FC-0802` Undo and Redo

Undo and redo supported commands where practical.

### `FC-0803` Workflow Templates

Reusable workflow structures for common production styles, stores, niches, and campaigns.

### `FC-0804` Automation Recipes

User-defined sequences for repetitive workflows such as import, metadata generation, export, and validation.

### `FC-0805` Event-Driven Automation

Trigger actions when listings, assets, mockups, prompts, or marketplace products change.

### `FC-0806` Scheduled Tasks

Run selected sync, import, validation, or reporting tasks on a schedule.

### `FC-0807` End-to-End Production Runs

Coordinate larger batch workflows while requiring human approval at important creative and publishing points.

---

# MVP Boundary

The first MVP should probably stop after a useful subset of the foundation, Phase 1, Phase 2, and the limited ideation support needed to make idea capture feel alive:

- `FC-0001` Application Shell
- `FC-0002` Core Domain Model
- `FC-0003` Local SQLite Persistence
- `FC-0004` Workspace File Storage
- `FC-0005` Navigation Tree
- `FC-0008` Workflow Stage Navigator
- `FC-0009` Tabbed Document Window
- `FC-0010` Context-Aware Tools
- `FC-0011` Stage Tool Host
- `FC-0101` Store Management
- `FC-0102` Niche Management
- `FC-0103` Group Management
- `FC-0104` Listing Management
- `FC-0105` Listing Lifecycle Status
- `FC-0106` Listing Inspector
- `FC-0107` Basic Search and Filtering
- `FC-0108` Tags
- `FC-0109` Import Existing Assets
- `FC-0202` Concept Versions
- `FC-0203` Design Records
- `FC-0204` Asset Relationships
- `FC-0206` Manual Mockup Records
- `FC-0207` Listing Metadata Editor
- `FC-0208` Design Triangle View
- `FC-0209` Creative History Timeline, limited to important status, concept, design, asset, prompt, mockup, and metadata events
- `FC-0401` AI Provider Abstraction, minimal provider contract needed for MVP idea generation and concept improvement
- `FC-0402` AI Settings, minimal key/model/privacy configuration needed for MVP AI workflows
- `FC-0404` Idea Generation, limited to basic manual capture and AI-assisted idea suggestions
- `FC-0405` Concept Refinement, limited to AI-assisted improvement of concept triangle elements
- `FC-0210` Basic Concept Tool, limited to manual triangle editing, concept history, and AI-assisted concept improvement
- `FC-0211` Basic Design Tool, limited to importing manually created or externally generated assets, creating design variants, tagging variants for shirt colors, and selecting final artwork
- `FC-0212` Basic Listing Tool, limited to local mockup generation, manual mockup attachment, listing metadata, price, status, and readiness tracking
- `FC-0213` Mockup Product Settings, limited to manual product, design area, template, placement, and color setup
- `FC-0205` Prompt History

This MVP would validate the core claim: FusionCanvas can preserve creative context better than folders, spreadsheets, and scattered documents, while carrying one item through the practical Idea -> Concept -> Design -> Listing flow.

AI is included in the first MVP as a minimal slice for idea generation and concept improvement. The Idea and Concept tools should still remain usable in manual mode when no provider is configured, but the provider abstraction and settings work are part of the MVP boundary.

---

# Early Open Questions

- Should Idea and Phrase begin as independent entities, or remain listing metadata until workflows prove the need?
- Should prompts always be stored in the database, or should large outputs become external files?
- Which marketplace integration should be first: Printify, Shopify, Etsy, or Creative Fabrica?
- How much plugin infrastructure is needed before the first real integration, and how much can be proven through built-in stage tools first?
- Should Printify and Shopify integrations be official paid plugins, community plugins, or optional built-in modules?

---

# Related Notes

- [[README|FusionCanvas]]
- [[Vision]]
- [[Product]]
- [[Architecture]]
- [[Data Model]]
- [[Principles]]
- [[Phase 0 - Foundation]]
- [[Phase 1 - MVP Creative Workspace]]
- [[Phase 2 - Product Creation Workflow]]
- [[Strategic Decisions]]
