## Context

The accepted core-domain-model defers Mockup as an advanced entity. The in-flight concept-versions, design-records, and asset-relationships changes add Concept, Design, and multi-context asset linking. FC-0104 made the listing the primary item entity with a permanent-deletion guard that blocks listings referenced by dependent creative records. FC-0109 established asset import and asset context links. The Phase 2 PRD requires mockup records to connect mockup images to listings and designs, identify manual versus generated mockups, preserve product/template/color/view metadata, and store enough metadata to regenerate generated mockups when the source design or template changes.

What is missing is the Mockup entity itself, the manual-attach and metadata-tracking behavior, and the asset context extension. Generation behavior itself arrives with FC-0212 Basic Listing Tool; FC-0206 owns the record model and the manual-attach path.

## Goals / Non-Goals

**Goals:**

- Add an item-bound Mockup entity with stable identity, timestamps, optional showcased-design reference, and documented mockup metadata.
- Allow a mockup to reference asset files through asset-management, with Mockup as a valid asset context kind.
- Track product type, vendor product, template, color variant, view, notes, and intended marketplace use.
- Distinguish manually attached mockups from generated mockups.
- Preserve superseded mockups without deletion.
- Store regeneration metadata on generated mockups so FC-0212 can regenerate them when the source design or template changes.
- Persist mockup operations through the existing atomic snapshot model with a forward-only schema migration.

**Non-Goals:**

- Do not implement advanced realistic mockup rendering, print provider catalog import, or publishing sync.
- Do not implement mockup generation itself; that arrives with FC-0212.
- Do not implement Marketplace Product or Performance entities.
- Do not implement a free-floating mockup outside an item.

## Decisions

### 1. Add a Mockup domain entity owned by a listing

A Mockup is a first-class domain entity that belongs to exactly one listing and is scoped to the Listing stage. It carries stable identity, created/modified timestamps, an optional reference to the design it showcases, the documented mockup metadata, a source flag (manual or generated), and an optional regeneration-metadata block for generated mockups. Mockups are not nested; a listing owns a flat set of mockup records. A mockup may reference a design from the same listing; cross-listing design references are rejected.

Alternative considered: store mockups as listing metadata. Rejected because multiple mockups per listing, asset references, and downstream tool joins benefit from a typed table.

### 2. Mockups reference assets through asset-management

A mockup associates with asset files through the existing asset-management context-link model, with Mockup added as a valid context kind. A mockup may reference one or more assets (e.g., a primary mockup image and alternate color variants). Removing a mockup removes its asset context links without deleting the assets. Removing an asset removes the asset context link from the mockup without deleting the mockup record.

Alternative considered: embed asset ids in mockup metadata. Rejected because asset-management already owns import, missing-file detection, and removal.

### 3. Manual and generated mockups share one entity with a source flag

A mockup record has a source flag: `manual` or `generated`. Manual mockups are created by the user attaching an existing image. Generated mockups are created by FC-0212's Basic Listing Tool generation flow and carry a regeneration-metadata block (source design id, mockup product id, template id, color variant id, placement parameters, and generation timestamp) so they can be regenerated when the source design or template changes. FC-0206 owns the record model and the manual-attach path; FC-0212 owns the generation path and regeneration behavior.

Alternative considered: separate ManualMockup and GeneratedMockup entities. Rejected because both share the same listing ownership, asset references, and metadata, and downstream tools read them uniformly.

### 4. Product/template/color metadata is documented metadata, not separate entities

Product type, vendor product, template, color variant, view, and intended marketplace use are stored as documented mockup metadata. The structured MockupProduct, MockupTemplate, and MockupColorVariant entities arrive with FC-0213 (Mockup Product Settings) and are referenced by id from the mockup's regeneration-metadata block when relevant. FC-0206 does not require those entities to exist; manual mockups can store the values as plain metadata strings.

Alternative considered: add structured product/template/color entities now. Rejected because FC-0213 owns that model and FC-0206 should remain decoupled.

### 5. Superseded mockups are preserved

A mockup has a lifecycle marker: active, superseded, or rejected. Superseding a mockup (e.g., regenerating with an updated design) creates or promotes a new mockup and demotes the prior one to superseded without deleting it. Rejected mockups remain available for review. The active mockup gallery shows active mockups by default.

Alternative considered: overwrite mockup records on regeneration. Rejected because the PRD wants to preserve context and history.

### 6. Forward-only schema migration

A new mockup table with metadata, source flag, lifecycle marker, showcased-design reference, and regeneration-metadata block is added through the existing snapshot and migration path. Existing listings migrate with zero mockups. The asset context-link kind enum is extended to include Mockup. No down-migration is provided.

Alternative considered: store mockups inside listing JSON. Rejected because asset references and downstream tool joins benefit from a typed table.

## Risks / Trade-offs

- [Stacked core-domain-model and asset-management modifications] -> FC-0206 modifies the same core-domain-model requirement that concept-versions and design-records modify, and the same asset-management context-kind set that asset-relationships extends. Archive in dependency order: concept-versions, design-records, asset-relationships, then FC-0206, rebasing MODIFIED blocks as needed.
- [Regeneration metadata can drift from FC-0213's structured model] -> Keep the regeneration-metadata block documented and let FC-0212 and FC-0213 own its structured shape.
- [Manual mockups without structured product data can be hard to filter] -> Accept plain-metadata manual mockups for FC-0206 and let later listing readiness checks warn when structured data is missing.

## Migration Plan

Increment the workspace snapshot version, add the mockup table and Mockup asset context kind, and migrate existing snapshots by assigning zero mockups. No data backfill is required. Down-migration is unsupported.

## Open Questions

- Should a mockup be allowed to showcase more than one design (e.g., a comparison mockup)? (Default: no — one showcased-design reference per mockup, to keep the design-to-mockup chain traceable.)
- Should manual mockups be required to reference a MockupProduct from FC-0213, or is plain metadata sufficient for FC-0206? (Default: plain metadata is sufficient for FC-0206; FC-0213 may add structured references later.)
