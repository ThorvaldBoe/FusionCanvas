## Context

The accepted store-management capability owns the store shell and the secondary-surface pattern used by niche-management and listing-management. The accepted asset-management capability owns template image assets. The in-flight mockup-records change adds the Mockup creative record that references a showcased design and asset outputs. The in-flight basic-listing-tool change (FC-0212) generates flat-composited mockups from a configured product, template, and color selection. The Phase 2 PRD requires a store-level configuration model so a creator configures products, templates, and color variants once and reuses them across many listings, preserving exact provider color names for manual Printify or Shopify setup.

What is missing is the configuration model itself, the store settings surface, and the active-set contract the Basic Listing Tool consumes.

## Goals / Non-Goals

**Goals:**

- Add a three-level store-scoped configuration: Store → MockupProduct → MockupTemplate → MockupColorVariant.
- Add documented fields for each level, including design area dimensions and placement mapping.
- Preserve exact provider color names.
- Add a focused store settings surface to add, edit, archive, restore, and mark active items at each level.
- Expose the active set to the Basic Listing Tool.
- Persist operations through the existing atomic snapshot model with a forward-only schema migration.

**Non-Goals:**

- Do not implement provider catalog import, Printify or Shopify API synchronization, automatic design-area discovery, visual drag-and-drop placement, perspective warp, fabric simulation, advanced shadows/masks/displacement/blending, or bulk provider color import.
- Do not implement mockup generation; that belongs to FC-0212.
- Do not implement mockup creative records; that belongs to FC-0206.
- Do not implement cross-store configuration sharing.

## Decisions

### 1. Three-level configuration owned by the store

A store owns zero or more MockupProducts. A MockupProduct owns zero or more MockupTemplates. A MockupTemplate owns zero or more MockupColorVariants. Each level has stable identity, timestamps, an active flag, and an archive flag. The store settings surface uses progressive disclosure: products first, then templates per product, then color variants per template.

Alternative considered: flatten products and templates into one list. Rejected because the PRD explicitly defines the three-level structure and downstream generation needs the hierarchy.

### 2. Placement mapping uses width-preferred coordinates

Each MockupTemplate stores placement X, placement Y, placement width, optional placement height, optional placement scale, and optional rotation (default 0). When both placement width and scale are present, placement width is preferred because it is easier to reason about visually. The design area dimensions (width and height in pixels) live on the MockupProduct. Generation scales the final design from the supplier design area to the placement width and places it at the configured coordinates.

Alternative considered: scale-only mapping. Rejected because width-preferred mapping is more intuitive for manual numeric entry.

### 3. Color variants preserve exact provider color names

Each MockupColorVariant stores `ProviderColorName` (exact, as entered) and `DisplayColorName` (optional, for in-app display). The provider color name is preserved verbatim because creators need it when preparing Printify or another provider manually. A color variant may use the parent template's asset (when the image is reusable across colors) or its own color-specific template asset (when each shirt color has its own image); for most shirt mockups each color needs its own template image.

Alternative considered: normalize color names to a palette. Rejected because exact provider names matter for manual marketplace setup.

### 4. Active set is the contract the Basic Listing Tool consumes

Only active, non-archived products, templates, and color variants are exposed to the Basic Listing Tool. Archiving a product, template, or color variant removes it from the active set without deleting it; existing mockup records that reference an archived configuration remain valid and retain their regeneration metadata. The active-set query is the only contract the Basic Listing Tool uses; it never reads archived configuration for generation.

Alternative considered: let the Basic Listing Tool read archived configuration too. Rejected because the active set should drive generation choices.

### 5. Focused store settings surface with progressive disclosure

The store settings surface is a focused secondary surface opened from the store, following the store-management secondary-surface pattern. It supports add, edit, archive, restore, and active-flag toggle at each level, with explicit save, dirty tracking, unsaved-change prompts, and shared desktop control guidance. The first version uses manual numeric entry for placement coordinates; a later visual mapping editor can make this easier.

Alternative considered: dock configuration beside the tree permanently. Rejected because configuration is occasional setup work that should not consume primary workspace.

### 6. Template and color-specific assets go through asset-management

Template images and color-specific template images are imported and managed as assets through asset-management, with MockupTemplate as a valid asset context kind (added by this change). A template references its template asset by id. This reuses asset-management's import, missing-file detection, and removal behavior.

Alternative considered: store template images outside asset-management. Rejected because asset-management already owns file lifecycle.

### 7. Forward-only schema migration

New tables for MockupProduct, MockupTemplate, and MockupColorVariant are added through the existing snapshot and migration path. Existing stores migrate with zero configuration. The asset context-link kind enum is extended to include MockupTemplate (and MockupColorVariant when color-specific assets are used). No down-migration is provided.

Alternative considered: store configuration as store JSON. Rejected because the three-level hierarchy and active-set queries benefit from typed tables.

## Risks / Trade-offs

- [Manual numeric entry is error-prone] -> Accept for MVP and document a visual mapping editor as a future enhancement.
- [Archived configuration referenced by existing mockups] -> Keep regeneration metadata on mockup records so they remain valid even after the referenced configuration is archived.
- [Stacked asset-management modification] -> FC-0213 extends the asset context-kind set that asset-relationships and mockup-records also extend. Archive in dependency order and rebase MODIFIED blocks as needed.

## Migration Plan

Increment the workspace snapshot version, add the MockupProduct, MockupTemplate, and MockupColorVariant tables and the MockupTemplate asset context kind, and migrate existing stores by assigning zero configuration. No data backfill is required. Down-migration is unsupported.

## Open Questions

- Should the store settings surface support duplicating a MockupProduct to another store? (Default: no, defer cross-store duplication to a later change.)
- Should placement scale be exposed in the UI alongside placement width, or hidden when width is present? (Default: hide scale when width is present, to reduce confusion.)
