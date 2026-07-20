## Context

The in-flight mockup-records change adds the Mockup entity with manual/generated source flag, lifecycle markers, showcased-design reference, asset references, and regeneration metadata. The in-flight listing-metadata-editor change adds marketplace-preparation metadata and its focused editor. The in-flight mockup-product-settings change (FC-0213) will add store-level MockupProduct, MockupTemplate, and MockupColorVariant configuration. The in-flight design-records change adds the final-selected-designs collection. The accepted stage-tool-host, context-aware-tools, and workflow-stage-navigator capabilities define hosting, context, and stage ownership. The Phase 2 PRD makes the Basic Listing Tool the default Listing-stage tool: it generates basic local mockups from configured product templates, edits listing metadata, tracks readiness, and keeps Printify and Shopify product creation out of the basic tool.

What is missing is the tool itself: the hosting registration, the mockup generation engine, the manual attachment flow, the readiness checklist, the metadata editor integration, the timeline feeding, and the marketplace-publishing refusal.

## Goals / Non-Goals

**Goals:**

- Provide the default Listing-stage tool for a single selected item, hosted through the Stage Tool Host.
- Require item context and at least one final-selected design variant before generating mockups; allow metadata editing without final artwork.
- Generate basic local flat-composited mockups from configured mockup product settings, storing outputs as assets and mockup records.
- Support manually attaching existing mockup images.
- Edit listing metadata through the listing-metadata-editor capability.
- Provide a readiness checklist for missing design, mockup, metadata, price, and marketplace preparation data.
- Refuse direct marketplace publishing and keep marketplace-specific values as draft preparation data.
- Feed important Listing-stage events into the Creative History Timeline.

**Non-Goals:**

- Do not implement direct Printify, Shopify, or Etsy product creation, publishing, or sync.
- Do not implement provider catalog import, automated shipping profile configuration, automated pricing optimization, advanced realistic mockup rendering, bulk mockup generation, full marketplace validation, or SEO scoring.
- Do not implement mockup storage; that belongs to FC-0206.
- Do not implement mockup product configuration; that belongs to FC-0213.
- Do not implement listing metadata storage; that belongs to FC-0207.

## Decisions

### 1. The tool is a Stage Tool Host built-in default

The Basic Listing Tool is registered as the default Listing-stage tool through the host-facing tool registry. It declares support for the Listing stage and requires a selected-item context. When no item is selected, the host shows an empty state or creation path. Future plugin-provided Listing-stage tools (Printify, Shopify, advanced mockup, etc.) can coexist and remain selectable.

Alternative considered: open the tool without an item. Rejected because the PRD and stage-tool-host require item context for Listing work.

### 2. Mockup generation consumes mockup product settings and final designs

The generation flow takes a selected final design variant, a configured mockup product (vendor, product, design area), one or more mockup templates (template image, placement X/Y/width/scale/rotation), and one or more color variants per template. For each color/template combination it: loads the final design image, scales the design to the supplier design area, resizes per the template placement width, places it at the configured coordinates, composites onto the blank product template, saves the output as an asset through asset-management, and creates a generated mockup record through the mockup-records service with the regeneration-metadata block. When the source design dimensions do not match the product design area, the tool scales to the design area first or warns the user.

Alternative considered: delegate generation to a plugin. Rejected because the PRD wants basic local mockup generation in the default tool.

### 3. Flat compositing only; advanced rendering is plugin territory

The MVP uses simple flat compositing: scale, place, and alpha-composite the design onto the blank template. Perspective warping, fabric effects, displacement maps, shadows, and realistic print blending are deferred to plugins through the image-processor port introduced by FC-0211 or a dedicated mockup-rendering port.

Alternative considered: implement realistic rendering now. Rejected because the PRD explicitly defers it.

### 4. Manual attachment creates manual mockup records

The user can attach an existing mockup image (imported or already in workspace storage) as a manual mockup record through the mockup-records service. Manual mockups carry the manual source flag and do not require a mockup product configuration.

Alternative considered: require mockup product configuration for all mockups. Rejected because creators often have mockups from external tools.

### 5. Metadata editing reuses the listing-metadata-editor surface

The tool hosts the listing-metadata-editor surface (or section) for title, description, price, status, notes, marketplace notes, draft tags, and basic preparation fields. It does not re-implement metadata editing. Operational status editing belongs to listing-lifecycle-status.

Alternative considered: duplicate metadata fields in the listing tool. Rejected because listing-metadata-editor already owns them.

### 6. Readiness is advisory, not blocking, except for generation

The readiness checklist shows missing final design, mockup product settings, at least one mockup, title, description, price, status, and provider product/color names. Missing readiness items do not block manual work except that mockup generation requires at least one final-selected design variant and a selected mockup product configuration. The checklist helps the creator see what is missing; it is not a gatekeeper for stage advancement.

Alternative considered: block all work until readiness is complete. Rejected because the PRD says readiness should help, not block, unless a specific command cannot run without required data.

### 7. Marketplace publishing is refused and delegated

The tool does not create, update, or publish Printify, Shopify, Etsy, or other marketplace products directly. Marketplace-specific values are stored locally as draft preparation data through listing-metadata-editor. Publishing actions are delegated to future integration specs or plugins that will be selectable through the Stage Tool Host.

Alternative considered: add a "publish" button that calls a future API. Rejected because the PRD explicitly keeps direct marketplace creation out of the basic tool.

### 8. Timeline feeding and stage behavior

The tool records mockup generation, manual attachment, metadata changes, and status changes into the Creative History Timeline. The Listing stage is terminal for the basic workflow; the tool does not advance the item to a further stage. A future marketplace-publishing plugin may advance the item once publishing is implemented.

Alternative considered: add a "published" stage. Rejected because marketplace publishing is out of scope for Phase 2.

## Risks / Trade-offs

- [Generation depends on FC-0213 configuration that is not yet accepted] -> Keep the generation flow decoupled through the mockup-product-settings contract and let FC-0213 provide the configuration model.
- [Flat compositing may produce unrealistic mockups] -> Accept flat compositing for MVP and document advanced rendering as plugin territory.
- [Readiness checklist can become noisy] -> Keep checks advisory and clearly distinct from blocking generation requirements.

## Migration Plan

No database migration is expected. The tool reads and writes through existing and in-flight services. Existing listings load with the tool available when their stage is Listing.

## Open Questions

- Should mockup generation batch all color/template combinations in one operation, or one at a time? (Default: batch in one operation, with per-combination progress and failure reporting.)
- Should the readiness checklist support per-marketplace readiness profiles? (Default: no, defer to integrations; the basic checklist covers generic readiness.)
