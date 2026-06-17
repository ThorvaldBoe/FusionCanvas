## ADDED Requirements

### Requirement: Documented Scope And Acceptance Expectations Are Preserved For Future Implementation
FusionCanvas SHALL ensure that the documented scope and acceptance expectations are preserved for future implementation.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The documented scope and acceptance expectations are preserved for future implementation

## Source PRD

# Phase 1 - MVP Creative Workspace

## Purpose

Phase 1 makes FusionCanvas useful as a local-first creative workspace for Print on Demand work.

The goal is not to automate the full production pipeline yet. The goal is to give creators a reliable place to organize stores, niches, groups, listings, statuses, tags, notes, and existing assets so creative context stops getting scattered across folders, spreadsheets, documents, and chat history.

Phase 1 should validate the core product claim:

> FusionCanvas preserves Print on Demand creative context better than folders, spreadsheets, and scattered notes.

## Phase Goal

A creator should be able to create a store, organize work into niches and groups, create listings, track listing status, import existing assets, tag work, search/filter the workspace, and inspect or update the most important details of a listing.

## Target User

Phase 1 is for creators who already produce or plan to produce multiple Print on Demand listings and need a better system for organizing their creative work.

The primary user is managing:

- one or more stores or brands
- multiple niches
- groups, campaigns, experiments, or collections
- many listing ideas and product concepts
- image files, source files, references, and notes

## Scope

Phase 1 includes:

- [[FC-0101 - Store Management|store management]]
- [[FC-0102 - Niche Management|niche management]]
- [[FC-0103 - Group Management|group management]]
- [[FC-0104 - Listing Management|listing management]]
- [[FC-0105 - Listing Lifecycle Status|listing lifecycle status]]
- [[FC-0106 - Listing Inspector|listing inspector]]
- [[FC-0107 - Basic Search and Filtering|basic search and filtering]]
- [[FC-0108 - Tags|tags]]
- [[FC-0109 - Import Existing Assets|importing existing assets]]

Phase 1 does not include:

- broad AI generation workflows beyond the limited basic ideation path, if that is included in the first MVP
- marketplace publishing
- marketplace sync
- analytics
- batch editing
- automated mockup generation
- plugin platform behavior
- general third-party plugin loading
- full product metadata optimization
- performance tracking

## User Outcomes

By the end of Phase 1, a creator can:

- set up a basic workspace for a store or brand
- model their creative work using stores, niches, groups, and listings
- reshape the workspace as ideas evolve
- create listings before final artwork exists
- capture one-line ideas quickly without filling out a full form
- work from the current navigation context so new items land where expected
- see the core Idea -> Concept -> Design -> Listing stage for the current item
- track where each listing is in the creative lifecycle
- add enough detail to understand the idea, phrase, graphic direction, and notes
- import existing files into the managed workspace and attach them to relevant work
- find work by text, status, tags, and location
- distinguish active work from rejected or archive-stage work

## Product Principles

Phase 1 should follow these principles:

- Keep the workspace practical and lightweight.
- Prioritize fast capture and organization over detailed publishing workflows.
- Preserve creative context without forcing rigid process.
- Avoid choking early ideation with mandatory fields.
- Make navigation easy to browse, filter, and reshape.
- Make creation and editing context-aware.
- Keep the core workflow visible while editing an item.
- Treat listings as product concepts, not just image files.
- Make status and tags useful for action, not decoration.
- Avoid adding future-facing complexity before the core workspace is useful.

---

# Requirements

## FC-0101 - Store Management

Detailed specification: [[FC-0101 - Store Management]]

### Goal

Creators can create and maintain stores as top-level business, brand, client, or publishing contexts.

### Requirements

- The user can create a store.
- The user can rename and edit basic store information.
- The user can distinguish active stores from archived stores.
- The user can browse into a store and see its niches, groups, and listings.
- The user can use a store as the primary scope for organization, search, tags, and assets.
- The user can maintain notes or context that apply to the store as a whole.

### High-Level Acceptance Criteria

- A new creator can create their first store without needing to understand advanced configuration.
- A creator with multiple brands can keep work separated by store.
- Archiving a store removes it from normal active work without destroying its context.

### Out of Scope

- Marketplace account connection
- Store-level publishing configuration
- Store analytics
- Multi-user permissions

---

## FC-0102 - Niche Management

Detailed specification: [[FC-0102 - Niche Management]]

### Goal

Creators can organize store work into niches that represent audiences, themes, markets, or creative directions.

### Requirements

- The user can create niches inside a store.
- The user can rename, edit, archive, and restore niches.
- The user can add notes that describe audience, style, humor, constraints, risks, or creative guidance.
- The user can use niches as top-level topic areas inside a store.
- A niche can contain groups and listings.
- A niche should be useful as a knowledge container, not merely a folder.

### High-Level Acceptance Criteria

- A creator can capture the difference between separate target audiences or product themes.
- A creator can store useful niche context that will still matter later.
- A creator can browse a store by niche and understand where work belongs.

### Out of Scope

- AI niche research
- Trend tracking
- Niche scoring
- Marketplace demand analysis

---

## FC-0103 - Group Management

Detailed specification: [[FC-0103 - Group Management]]

### Goal

Creators can organize listings into flexible groups such as collections, campaigns, experiments, batches, or temporary work areas.

### Requirements

- The user can create groups inside niches.
- The user can create nested groups where needed.
- The user can rename, edit, move, archive, and restore groups.
- The user can use groups to organize listings around practical working contexts.
- The user can move groups without losing their child listings or subgroups.
- Empty groups can be treated as flexible planning containers.

### High-Level Acceptance Criteria

- A creator can create a campaign, collection, or experiment group and place listings inside it.
- A creator can restructure groups as the project evolves.
- Moving a group preserves the work contained inside it.

### Out of Scope

- Batch operations
- Saved views
- Automated campaign planning
- Group-level analytics

---

## FC-0104 - Listing Management

Detailed specification: [[FC-0104 - Listing Management]]

### Goal

Creators can create and manage listings as the primary working objects in FusionCanvas.

### Requirements

- The user can create a listing inside a valid store, niche, or group context.
- The user can rename, edit, duplicate, move, archive, restore, and delete listings.
- A listing can exist before final design assets exist.
- A listing can capture the core creative idea behind a potential product.
- Moving a listing preserves its status, notes, tags, and attached assets.
- A listing should be treated as a product concept rather than a single file.

### High-Level Acceptance Criteria

- A creator can capture a product idea quickly as a listing.
- A creator can move a listing between groups or niches as its fit becomes clearer.
- A creator can preserve listing context even when no artwork has been made yet.

### Out of Scope

- Marketplace product creation
- Full listing metadata optimization
- Design version management
- Concept version history
- Performance history

---

## FC-0105 - Listing Lifecycle Status

Detailed specification: [[FC-0105 - Listing Lifecycle Status]]

### Goal

Creators can track where each listing is in the creative workflow and what kind of action it needs next.

### Requirements

- A listing can have a workflow stage.
- A listing can have an operational lifecycle status.
- The workflow stage should support the early creative workflow from capture through archive.
- The user can change a listing's status where appropriate.
- Status should make it easier to distinguish draft work from published, paused, and rejected work.
- Rejected listings should remain available for reference without cluttering active work.

### Initial Workflow Stages

- Idea
- Concept
- Design
- Listing
- Archive

### Initial Operational Statuses

- Draft
- Published
- Paused
- Rejected

### High-Level Acceptance Criteria

- A creator can tell which listings are only ideas, which need design work, and which are ready for listing work.
- A creator can filter by workflow stage or status to focus on the next kind of work.
- A creator can preserve rejected or archive-stage work without treating it as active.

### Out of Scope

- Custom workflow builder
- Required status transition rules
- Automation based on status
- Marketplace publishing status sync

---

## FC-0106 - Listing Inspector

Detailed specification: [[FC-0106 - Listing Inspector]]

### Goal

Creators can inspect and edit the most important details of a listing without losing navigation context.

### Requirements

- The user can view and edit core listing details.
- The inspector should support the main creative context for a listing.
- The inspector should make status, notes, tags, and attached assets visible or accessible.
- The inspector should support fast updates while browsing the workspace.
- The inspector should help the creator understand the listing as a product concept.

### Core Listing Details

- title or working title
- lifecycle status
- idea
- phrase
- graphic direction
- notes
- tags
- related assets

### High-Level Acceptance Criteria

- A creator can select a listing and quickly understand what it is.
- A creator can update the idea, phrase, graphic direction, notes, and status from one focused place.
- A creator can move through listings while preserving enough context to compare work.

### Out of Scope

- Full marketplace metadata editor
- AI critique
- Design triangle scoring
- Rich text document editing
- Version comparison

---

## FC-0107 - Basic Search and Filtering

Detailed specification: [[FC-0107 - Basic Search and Filtering]]

### Goal

Creators can quickly find relevant work without losing the structure around it.

### Requirements

- The user can search by text across important visible fields.
- The user can filter by store, niche, group or subtree, status, and tag.
- Search and filters should work across topics and listings where relevant.
- Filtered results should preserve enough parent context to show where matching work lives.
- The user can clear filters and return to normal browsing.
- Rejected work should not clutter normal results unless included intentionally.

### High-Level Acceptance Criteria

- A creator can find a listing by remembered words from its title, phrase, idea, or notes.
- A creator can filter to see all listings in a specific status.
- A creator can filter by tag and still understand each result's location in the workspace.
- A creator can narrow work without turning the navigation into a disconnected flat list.

### Out of Scope

- Advanced query language
- Ranking or relevance scoring
- Semantic search
- AI search
- Cross-workspace search

---

## FC-0108 - Tags

Detailed specification: [[FC-0108 - Tags]]

### Goal

Creators can classify work flexibly across stores, niches, groups, listings, and assets.

### Requirements

- The user can create, rename, and remove tags.
- The user can apply tags to listings.
- The user can apply tags to topics where useful.
- The user can use tags as filters.
- Tags should help identify workflow state, theme, style, product type, risk, opportunity, or other practical categories.
- Removing a tag from one item should not destroy unrelated work.

### High-Level Acceptance Criteria

- A creator can tag listings with useful labels such as "tavern", "needs mockup", "evergreen", or "risky".
- A creator can filter by tag to find related work across groups.
- A creator can use tags without being forced into a rigid taxonomy.

### Out of Scope

- Tag hierarchy
- Tag automation
- Tag analytics
- Marketplace keyword optimization

---

## FC-0109 - Import Existing Assets

Detailed specification: [[FC-0109 - Import Existing Assets]]

### Goal

Creators can import existing files into the managed FusionCanvas workspace and attach them to stores, niches, groups, or listings so assets remain connected to creative context.

### Requirements

- The user can import existing file assets.
- Imported files are copied into the managed FusionCanvas workspace.
- The user can associate assets with a listing.
- The user can associate assets with broader contexts where useful.
- The user can see which assets belong to a listing.
- The user can identify basic asset purpose, such as source file, export, mockup, reference, texture, or font.
- Imported assets should remain connected to their related work when listings or groups move.

### High-Level Acceptance Criteria

- A creator can import an existing design file to a listing.
- A creator can import reference material or exported artwork without losing where it belongs.
- A creator can open a listing later and see the files that support it.

### Out of Scope

- Batch asset import
- Automatic file matching
- Image processing
- Mockup generation
- Cloud storage sync
- Asset deduplication

---

# Phase-Level Acceptance Criteria

Phase 1 is successful when:

- A creator can set up at least one store with multiple niches.
- A creator can organize listings into groups and nested groups.
- A creator can capture product concepts before artwork exists.
- A creator can track workflow stage and lifecycle status across listings.
- A creator can inspect and update listing details from a focused place.
- A creator can import existing assets to listings.
- A creator can use tags and filters to find work.
- A creator can archive or reject work without deleting useful context.
- The workspace feels more useful than folders and spreadsheets for managing early Print on Demand work.

# Open Questions

- Should Phase 1 include prompt history from the MVP boundary, or should that remain Phase 2/AI-adjacent?
- Should deleting a listing be supported immediately, or should archive/reject be the preferred early behavior?
- Should tags be store-specific from the user's perspective, or available across the whole workspace?
- How much niche context should be editable in Phase 1 before it becomes too detailed?
- Should imported assets be attached only to listings at first, or also to stores, niches, and groups?
- Should listing statuses be fixed in Phase 1, or should users be able to rename/hide them?

# Related Notes

- [[Roadmap]]
- [[Product]]
- [[Data Model]]
- [[Principles]]
- [[FC-0008 - Workflow Stage Navigator]]
- [[FC-0009 - Tabbed Document Window]]
- [[FC-0010 - Context-Aware Tools]]
