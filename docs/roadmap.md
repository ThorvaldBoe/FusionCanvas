# FusionCanvas Roadmap With Review Gates

Source: `docs/LifeOS/Roadmap.md`

This roadmap preserves the original LifeOS phase grouping from Phase 1 onward. Phase 0 is intentionally omitted because the foundation work is already close to complete.

Review gates are added where later work in the same phase depends on earlier features, especially where data models, workflow contracts, extension points, or cross-feature UX patterns become load-bearing. Each phase ends with a review gate.

## Phase 1: MVP Creative Workspace

Goal: make FusionCanvas useful as a local-first workspace for organizing Print on Demand work.

1. [[FC-0101 - Store Management|`FC-0101` Store Management]]
   - Establishes stores as the top-level business context for later organization, filtering, settings, and import behavior.
2. [[FC-0102 - Niche Management|`FC-0102` Niche Management]]
   - Adds the default top-level topic structure inside stores, including audience, style, constraints, and future AI context.
3. [[FC-0103 - Group Management|`FC-0103` Group Management]]
   - Adds nested organization for collections, campaigns, experiments, and subgroups.
4. **Review Gate: Workspace Hierarchy Review**
   - Confirm the store, niche, group, and navigation relationships are stable enough for listings, filters, tags, and imported assets.
   - Validate create, edit, archive, move, and hierarchy behaviors before listing data begins depending on them.
5. [[FC-0104 - Listing Management|`FC-0104` Listing Management]]
   - Introduces listings as the primary working object inside the established workspace hierarchy.
6. [[FC-0105 - Listing Lifecycle Status|`FC-0105` Listing Lifecycle Status]]
   - Defines workflow and operational status semantics that later inspectors, filters, queues, and batch actions will rely on.
7. [[FC-0106 - Listing Inspector|`FC-0106` Listing Inspector]]
   - Provides the main editing surface for listing title, idea, phrase, graphic direction, notes, status, and metadata.
8. **Review Gate: Listing Core Review**
   - Confirm listing identity, placement, lifecycle status, metadata ownership, and inspector editing flows before search, tags, and imports build on them.
9. [[FC-0107 - Basic Search and Filtering|`FC-0107` Basic Search and Filtering]]
   - Adds text and structured filtering across the workspace while preserving enough hierarchy context to understand results.
10. [[FC-0108 - Tags|`FC-0108` Tags]]
    - Adds flexible classification across topics, listings, assets, ideas, phrases, and future entities.
11. [[FC-0109 - Import Existing Assets|`FC-0109` Import Existing Assets]]
    - Attaches existing image, design, reference, and source files to listings or stores.
12. **Review Gate: Phase 1 Review**
    - Confirm the MVP workspace can organize stores, niches, groups, listings, tags, filters, and imported assets coherently.
    - Check that Phase 2 creative workflow entities can attach to the current listing, hierarchy, tag, status, and asset foundations without rework.

## Phase 2: Product Creation Workflow

Goal: support the full idea-to-listing flow, even before advanced automation exists.

1. [[FC-0201 - Idea Inbox|`FC-0201` Idea Inbox]]
   - Captures raw ideas before they become listings.
2. [[FC-0202 - Concept Versions|`FC-0202` Concept Versions]]
   - Preserves multiple concept directions for a listing, including rejected or superseded versions.
3. [[FC-0203 - Design Records|`FC-0203` Design Records]]
   - Tracks concrete design implementations, versions, approval state, and export readiness.
4. [[FC-0204 - Asset Relationships|`FC-0204` Asset Relationships]]
   - Connects files to listings, concepts, designs, mockups, stores, or niches.
5. **Review Gate: Creative Data Model Review**
   - Confirm idea, concept, design, and asset relationships are stable before prompt history, mockups, metadata, timeline, and stage tools depend on them.
   - Validate ownership rules, versioning semantics, and attachment boundaries across listings and workspace context.
6. [[FC-0205 - Prompt History|`FC-0205` Prompt History]]
   - Stores AI prompts, provider information, inputs, outputs, and related entities.
7. [[FC-0206 - Manual Mockup Records|`FC-0206` Manual Mockup Records]]
   - Tracks manually attached and generated mockup images as product previews.
8. [[FC-0207 - Listing Metadata Editor|`FC-0207` Listing Metadata Editor]]
   - Edits title, description, keywords, tags, marketplace notes, and platform-specific fields.
9. [[FC-0208 - Design Triangle View|`FC-0208` Design Triangle View]]
   - Shows and evaluates the relationship between idea, phrase, and graphic for a listing.
10. [[FC-0209 - Creative History Timeline|`FC-0209` Creative History Timeline]]
    - Shows important listing events, concept changes, prompt runs, asset imports, and status changes.
11. **Review Gate: Workflow Context Review**
    - Confirm prompt, mockup, metadata, design-triangle, and timeline behavior all read from the same creative source of truth.
    - Check that later Concept, Design, and Listing tools can reuse these records instead of inventing parallel state.
12. [[FC-0210 - Basic Concept Tool|`FC-0210` Basic Concept Tool]]
    - Default Concept-stage tool for manual triangle editing, optional AI-assisted refinement, concept history, quality indication, and branching.
13. [[FC-0211 - Basic Design Tool|`FC-0211` Basic Design Tool]]
    - Default Design-stage tool for manual design import, external AI image import, variant comparison, cleanup metadata, and final design selection.
14. [[FC-0212 - Basic Listing Tool|`FC-0212` Basic Listing Tool]]
    - Default Listing-stage tool for mockup generation, manual mockup attachment, listing metadata, price, status, marketplace notes, and readiness tracking.
15. [[FC-0213 - Mockup Product Settings|`FC-0213` Mockup Product Settings]]
    - Store-level configuration for supplier products, design areas, mockup templates, placement mapping, and color variants.
16. **Review Gate: Phase 2 Review**
    - Confirm a complete local Idea -> Concept -> Design -> Listing workflow works end to end.
    - Pay special attention to stage transitions, record reuse, asset relationships, mockup settings, and whether the Basic Listing Tool has all product configuration it needs.

## Phase 3: Batch Workflow

Goal: optimize FusionCanvas for creators working on many listings at once.

1. [[FC-0301 - Multi-Select Operations|`FC-0301` Multi-Select Operations]]
   - Allows users to select multiple listings, assets, or groups and apply commands together.
2. **Review Gate: Multi-Select Foundation Review**
   - Confirm selection semantics, command availability, partial failure handling, and undo/audit expectations before bulk operations build on them.
3. [[FC-0302 - Bulk Status Changes|`FC-0302` Bulk Status Changes]]
   - Moves many listings through workflow stages in one operation.
4. [[FC-0303 - Bulk Metadata Editing|`FC-0303` Bulk Metadata Editing]]
   - Applies shared metadata, tags, marketplace settings, or notes to multiple listings.
5. [[FC-0304 - Batch Asset Import|`FC-0304` Batch Asset Import]]
   - Imports many files and maps them to listings using filenames, folders, or manual matching.
6. [[FC-0305 - Batch Export Preparation|`FC-0305` Batch Export Preparation]]
   - Prepares export-ready files and metadata packages for downstream tools.
7. [[FC-0306 - Work Queues|`FC-0306` Work Queues]]
   - Adds focused queues for items needing design, mockups, listing text, publishing, review, or revision.
8. [[FC-0307 - Saved Views|`FC-0307` Saved Views]]
   - Adds reusable filters for common workflows such as "ready for mockups" or "needs listing metadata".
9. **Review Gate: Phase 3 Review**
   - Confirm batch commands, imports, export preparation, queues, and saved views all use consistent selection, filtering, status, and metadata rules.

## Phase 4: AI Assistance

Goal: make AI contextual and useful without making it the source of truth.

1. [[FC-0401 - AI Provider Abstraction|`FC-0401` AI Provider Abstraction]]
   - Defines the core interface for AI providers so OpenAI, Anthropic, local models, or future providers can be plugins.
2. [[FC-0402 - AI Settings|`FC-0402` AI Settings]]
   - Manages provider keys, preferred models, default behavior, and privacy controls.
3. [[FC-0403 - Niche AI Context|`FC-0403` Niche AI Context]]
   - Uses niche knowledge, audience, humor, style, constraints, and successful patterns as AI context.
4. **Review Gate: AI Foundation Review**
   - Confirm provider contracts, settings, privacy controls, and niche context are stable before user-facing AI tools depend on them.
   - Validate manual fallback behavior for workflows where no provider is configured.
5. [[FC-0404 - Idea Generation|`FC-0404` Idea Generation]]
   - Adds manual capture, optional AI generation, one-click listing creation, accepted/rejected idea feedback, and future plugin ideation tools.
6. [[FC-0405 - Concept Refinement|`FC-0405` Concept Refinement]]
   - Improves raw ideas into clearer concepts with audience reaction, risks, and visual direction.
7. [[FC-0406 - Phrase Generation|`FC-0406` Phrase Generation]]
   - Suggests phrases, hooks, variations, and reusable phrase templates.
8. [[FC-0407 - Image Prompt Generation|`FC-0407` Image Prompt Generation]]
   - Generates image prompts from concept, phrase, niche, style, and marketplace constraints.
9. [[FC-0408 - Listing Text Generation|`FC-0408` Listing Text Generation]]
   - Drafts product titles, descriptions, keywords, and tags for marketplace use.
10. **Review Gate: AI Workflow Review**
    - Confirm generation tools share context assembly, prompt history, approval, rejection, and save-back behavior.
    - Check that generated content attaches to the correct idea, concept, design, listing, niche, and metadata records.
11. [[FC-0409 - Critique and Scoring|`FC-0409` Critique and Scoring]]
    - Reviews idea, phrase, and graphic alignment and provides improvement suggestions.
12. [[FC-0410 - Prompt Reuse Library|`FC-0410` Prompt Reuse Library]]
    - Stores reusable prompt templates for idea, concept, phrase, image, listing, critique, and research workflows.
13. **Review Gate: Phase 4 Review**
    - Confirm AI assistance remains contextual, auditable, optional, and human-approved across idea, concept, phrase, image prompt, listing text, critique, and prompt reuse workflows.

## Phase 5: Plugin Platform

Goal: expose stable extension points so specialized capabilities can live outside the core application.

1. [[FC-0501 - Plugin Manifest|`FC-0501` Plugin Manifest]]
   - Defines plugin identity, version, capabilities, dependencies, and settings.
2. [[FC-0502 - Plugin Discovery|`FC-0502` Plugin Discovery]]
   - Loads plugins from known folders and exposes them to the application.
3. [[FC-0503 - Plugin Dependency Registration|`FC-0503` Plugin Dependency Registration]]
   - Allows plugins to register services through dependency injection.
4. **Review Gate: Plugin Runtime Review**
   - Confirm manifest, discovery, versioning, dependency, and service-registration rules before plugins add commands, storage, events, and UI.
5. [[FC-0504 - Plugin Command Contributions|`FC-0504` Plugin Command Contributions]]
   - Allows plugins to add commands to menus, context actions, queues, or entity views.
6. [[FC-0505 - Plugin Data Storage|`FC-0505` Plugin Data Storage]]
   - Lets plugins attach structured JSON data to core entities without changing the core schema.
7. [[FC-0506 - Plugin Event Hooks|`FC-0506` Plugin Event Hooks]]
   - Publishes events such as ListingCreated, AssetImported, MockupGenerated, and ProductPublished.
8. [[FC-0507 - Plugin Settings UI|`FC-0507` Plugin Settings UI]]
   - Provides a standard way for plugins to expose configuration screens.
9. [[FC-0508 - Sample Plugin|`FC-0508` Sample Plugin]]
   - Demonstrates manifest, commands, settings, data storage, and events.
10. **Review Gate: Phase 5 Review**
    - Confirm the sample plugin proves the platform across runtime loading, commands, storage, events, settings, and dependency registration.

## Phase 6: Publishing and Integrations

Goal: connect FusionCanvas to external tools while keeping local data authoritative.

1. [[FC-0601 - Marketplace Product Records|`FC-0601` Marketplace Product Records]]
   - Tracks external marketplace products linked to FusionCanvas listings.
2. [[FC-0602 - Export Package Builder|`FC-0602` Export Package Builder]]
   - Creates local export packages containing artwork, mockups, and listing metadata.
3. **Review Gate: Publishing Data Review**
   - Confirm marketplace product records, local export packages, IDs, status fields, and source-of-truth rules before direct integrations depend on them.
4. [[FC-0603 - Printify Integration|`FC-0603` Printify Integration]]
   - Creates or updates Printify products from listing assets and metadata.
5. [[FC-0604 - Shopify Integration|`FC-0604` Shopify Integration]]
   - Syncs product information and publishing status with Shopify.
6. [[FC-0605 - Etsy Integration|`FC-0605` Etsy Integration]]
   - Prepares or syncs listing information for Etsy.
7. [[FC-0606 - Creative Fabrica Export|`FC-0606` Creative Fabrica Export]]
   - Packages design files and metadata for Creative Fabrica submission.
8. [[FC-0607 - Publishing Status Sync|`FC-0607` Publishing Status Sync]]
   - Refreshes external status, URLs, and IDs while preserving FusionCanvas as the source of truth.
9. [[FC-0608 - Marketplace Validation|`FC-0608` Marketplace Validation]]
   - Checks listing data, image requirements, tags, and metadata before export or publishing.
10. **Review Gate: Phase 6 Review**
    - Confirm integrations, exports, validation, and sync all preserve local authority while handling external IDs, statuses, errors, and marketplace-specific constraints.

## Phase 7: Analytics and Learning

Goal: help creators learn from performance and improve future work.

1. [[FC-0701 - Manual Performance Import|`FC-0701` Manual Performance Import]]
   - Imports performance data from CSV or manual entry before direct marketplace analytics integrations exist.
2. [[FC-0702 - Performance Data Model|`FC-0702` Performance Data Model]]
   - Stores impressions, clicks, favorites, carts, sales, revenue, ad spend, and related source data.
3. **Review Gate: Performance Data Review**
   - Confirm import mapping, metric definitions, time ranges, attribution, and marketplace source rules before views and learning features depend on the data.
4. [[FC-0703 - Listing Performance View|`FC-0703` Listing Performance View]]
   - Shows performance history and derived metrics for individual listings.
5. [[FC-0704 - Niche Performance View|`FC-0704` Niche Performance View]]
   - Aggregates performance by niche, group, tag, phrase type, style, or campaign.
6. [[FC-0705 - Phrase and Style Pattern Tracking|`FC-0705` Phrase and Style Pattern Tracking]]
   - Identifies recurring patterns in successful and unsuccessful designs.
7. [[FC-0706 - Experiment Tracking|`FC-0706` Experiment Tracking]]
   - Tracks experiments across groups, campaigns, prompt strategies, styles, or marketplaces.
8. [[FC-0707 - Improvement Suggestions|`FC-0707` Improvement Suggestions]]
   - Suggests candidates for revision, retirement, re-export, metadata improvement, or new variations.
9. **Review Gate: Phase 7 Review**
   - Confirm analytics views, pattern tracking, experiments, and improvement suggestions share consistent metrics, attribution, filters, and confidence boundaries.

## Phase 8: Advanced Automation

Goal: automate repetitive workflows after the underlying structure has proven itself.

1. [[FC-0801 - Command History|`FC-0801` Command History]]
   - Records user actions in a form that supports auditing, automation, and eventual undo.
2. [[FC-0802 - Undo and Redo|`FC-0802` Undo and Redo]]
   - Undoes and redoes supported commands where practical.
3. **Review Gate: Command System Review**
   - Confirm command recording, reversibility boundaries, side effects, and audit semantics before templates and automation reuse commands.
4. [[FC-0803 - Workflow Templates|`FC-0803` Workflow Templates]]
   - Adds reusable workflow structures for common production styles, stores, niches, and campaigns.
5. [[FC-0804 - Automation Recipes|`FC-0804` Automation Recipes]]
   - Adds user-defined sequences for repetitive workflows such as import, metadata generation, export, and validation.
6. **Review Gate: Automation Definition Review**
   - Confirm templates and recipes can compose real commands safely before event-driven and scheduled execution run them without direct user initiation.
7. [[FC-0805 - Event-Driven Automation|`FC-0805` Event-Driven Automation]]
   - Triggers actions when listings, assets, mockups, prompts, or marketplace products change.
8. [[FC-0806 - Scheduled Tasks|`FC-0806` Scheduled Tasks]]
   - Runs selected sync, import, validation, or reporting tasks on a schedule.
9. [[FC-0807 - End-to-End Production Runs|`FC-0807` End-to-End Production Runs]]
   - Coordinates larger batch workflows while requiring human approval at important creative and publishing points.
10. **Review Gate: Phase 8 Review**
    - Confirm automation remains observable, reversible where possible, permissioned, and human-approved at creative and publishing boundaries.
