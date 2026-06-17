# FusionCanvas Strategic Decisions

This document captures product and architecture choices that materially affect FusionCanvas.

These are not implementation tasks. They are decisions that should be made deliberately before the related feature becomes expensive to change.

## 2026-06-17 - MVP AI Boundary

### Decision Needed

Should the first MVP include AI-assisted workflows, or should it ship as a fully manual local-first workflow with AI added immediately after?

### Context

The PRDs describe optional AI assistance in the Idea, Concept, Design, and Listing tools. The roadmap MVP boundary includes limited AI ideation and optional AI support, but those features depend on provider abstraction and settings.

### Options

#### Option A - Manual MVP First

Ship the first MVP with manual Idea, Concept, Design, and Listing tools only.

Pros:

- Fastest route to a working product.
- Validates the core workflow without API keys, privacy concerns, or provider churn.
- Keeps the first implementation focused on local data, navigation, assets, and stage tools.

Cons:

- The app may feel less differentiated at first.
- Prompt history and AI-context design may be harder to validate early.

#### Option B - Minimal AI Slice in MVP

Include `FC-0401` AI Provider Abstraction and `FC-0402` AI Settings at a minimal level, then enable optional AI in `FC-0404`, `FC-0210`, and selected Design/Listing workflows.

Pros:

- Validates the AI-native product claim earlier.
- Tests provider settings, privacy messaging, prompt history, and context-aware generation before patterns harden.
- Makes the Basic Concept Tool feel closer to the intended product.

Cons:

- Increases MVP complexity.
- Requires careful provider error handling and privacy communication.
- May slow down the local-first organizer if AI work dominates attention.

### Decision

FusionCanvas selects Option B: Minimal AI Slice in MVP.

This means:

- Support generating ideas in the ideation tool.
- Support improving concepts in the concept tool.

Out of scope for this slice:

- generative image generation in the design tool
- other advanced AI support

## 2026-06-17 - Workflow Stage vs Lifecycle Status

### Decision Needed

Should FusionCanvas store workflow stage and operational status separately?

### Context

Several docs use Idea, Concept, Design, and Listing as both workflow stages and lifecycle statuses. That is understandable in early product language, but it can create duplicate state once the stage navigator, filters, and readiness workflows exist.

### Options

#### Option A - Single Status Field

Use one status field for everything.

Pros:

- Simple UI and storage.
- Easy to explain in the earliest version.

Cons:

- Harder to represent a listing that is in one creative stage but has a separate operational state, such as Paused or Rejected.
- The workflow navigator may conflict with operational status.

#### Option B - Separate WorkflowStage and Status

Use `WorkflowStage` for Idea, Concept, Design, Listing, and Archive. Use `Status` for operational states such as Draft, Published, Paused, and Rejected.

Pros:

- Cleaner source of truth for the stage navigator.
- Better filtering and readiness workflows.
- Scales naturally into batch queues and automation.

Cons:

- Slightly more complexity.
- Requires UI language that does not overwhelm the user.

### Decision

FusionCanvas selects Option B: separate `WorkflowStage` from `Status`.

- `WorkflowStage`: Idea, Concept, Design, Listing, Archive
- `Status`: Draft, Published, Paused, Rejected

Statuses are operational. Everything starts as Draft and may later move into one of the other statuses. Published is the regular final status, Paused is for maybe-later ideas, and Rejected is for work that should definitely not continue.

## 2026-06-17 - Navigation Data Model

### Decision Needed

Should Store, Niche, Group, and Listing remain separate tables, or should all navigable objects use a shared node table?

### Context

The product wants a flexible topic/item tree with conversion between topics and items. The domain language also benefits from explicit concepts such as Store, Niche, Group, and Listing.

### Options

#### Option A - Separate Domain Tables

Keep Store, Niche, Group, and Listing as separate tables.

Pros:

- Clear domain model.
- Easier to reason about business rules.
- Fits the current PRDs and diagrams.

Cons:

- Topic/item conversion requires careful application logic.
- Generic tree operations may need adapters or query models.

#### Option B - Shared Navigation Node Table

Use a shared node table for all navigable objects, with type-specific tables or JSON metadata for details.

Pros:

- Tree operations, filtering, ordering, and moving become uniform.
- Topic/item conversion may be easier.

Cons:

- Domain concepts can become blurry.
- More abstraction early, before the product has proven the workflow.

### Decision

FusionCanvas selects Option A: use separate domain tables and expose a navigation query model that presents them as topics and items.

Revisit a shared node table only if move, filter, and convert behavior becomes awkward.

## 2026-06-17 - Asset File Ownership

### Decision Needed

When a user imports or attaches an asset, should FusionCanvas copy the file into its workspace, reference the original location, or allow both?

### Context

The product is local-first and depends on durable asset relationships. The PRDs also need to support existing creator folders and external tools.

### Options

#### Option A - Reference Original Files

Store paths to existing files without copying them.

Pros:

- Non-invasive.
- Works well with existing folder structures.
- Avoids duplicate storage.

Cons:

- Files can move or disappear.
- Backup/export is less reliable.

#### Option B - Copy Into Workspace

Copy imported files into a FusionCanvas-managed workspace folder.

Pros:

- More reliable local ownership.
- Easier backup, export, and missing-file handling.
- Clearer relationship between database and assets.

Cons:

- Duplicates files.
- May frustrate users with established asset libraries.

#### Option C - Both, With Per-Asset Mode

Allow users to either reference or copy files, with the chosen mode visible on the asset.

Pros:

- Flexible.
- Supports both existing libraries and managed project storage.

Cons:

- More UI and edge cases.
- Requires clear missing-file behavior.

### Decision

FusionCanvas selects Option B: copy imported files into a FusionCanvas-managed workspace.

The copied file becomes the authoritative FusionCanvas asset. Original filename and import source may be preserved as metadata, but the app should not depend on fragile external file paths.
