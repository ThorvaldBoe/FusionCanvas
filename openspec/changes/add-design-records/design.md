## Context

The accepted core-domain-model defines the Phase 0 entities and defers Design, Mockup, and similar advanced entities. The in-flight concept-versions change adds the Concept entity as a Phase 2 concern and narrows the premature-advanced-entities exclusion to accept concept versioning. FC-0104 made the listing the primary item entity with topic-resolved creation, archive-aware projection, atomic snapshot persistence, and a permanent-deletion guard that blocks listings referenced by dependent creative records. FC-0008 established workflow stage navigation; FC-0010 established context-aware inherited context; FC-0109 established asset import and asset context links with purpose labels. The Phase 2 PRD requires Design-stage work to be item-bound, to preserve variants and history, to track approval and cleanup state, and to expose one or more promoted final designs for downstream Listing, mockup, and export work.

What is missing is the Design entity itself, the multi-variant model, the approval and cleanup state machines, and the final-selection invariant that downstream Listing and mockup tools depend on.

## Goals / Non-Goals

**Goals:**

- Add an item-bound Design entity with stable identity, timestamps, name, version, notes, approval state, source method, intended use, cleanup state, and optional implemented-concept reference.
- Allow multiple design variants per listing and preserve draft, rejected, needs-revision, and superseded variants without deletion.
- Add approval states: draft, needs revision, approved, rejected, exported, ready for export.
- Allow one or more designs to be promoted as final selected artwork at the listing level without deleting other variants.
- Associate designs with assets through the existing asset-management capability and reference prompt history, provider metadata, and generation settings for AI-assisted designs.
- Track intended use and cleanup state as documented metadata so later Listing, mockup, and export tools can use them.
- Persist design operations through the existing atomic snapshot model with a forward-only schema migration.

**Non-Goals:**

- Do not implement image editing, automated export, mockup generation, or marketplace publishing.
- Do not implement the Basic Design Tool UI; that is FC-0211.
- Do not implement Mockup, Marketplace Product, or Performance entities.
- Do not implement a free-floating design outside an item.
- Do not implement bulk multi-item design generation.

## Decisions

### 1. Add a Design domain entity owned by a listing

A Design is a first-class domain entity that belongs to exactly one listing and is scoped to the Design stage. It carries stable identity, created/modified timestamps, the documented creative and production fields, and an optional reference to the concept it implements. The implemented-concept reference is optional because the PRD allows creating a design directly from a strong phrase and graphic direction when the creator has enough direction, without forcing a concept record first. Designs are not nested; a listing owns a flat set of design variants.

Alternative considered: require a selected concept before any design can exist. Rejected because the PRD explicitly allows skipping the concept record when the phrase and graphic direction are sufficient.

### 2. Final selection is a listing-level collection with an invariant

A listing exposes a final-selected-designs collection of design ids. The invariant: any design variant may be promoted to final regardless of its approval state; final membership is marked by a `final` tag on the design and by membership in the listing-level collection; promoting does not delete or alter other variants; at least one final design is required before the workflow-stage-navigator allows advancing the listing to the Listing stage. Demoting a final design removes it from the collection and clears the `final` tag without deleting it. The approval state machine (draft, needs revision, approved, rejected, exported, ready for export) remains independent of final selection and is used for tracking and review, not for gating final promotion. The collection is stored at the listing level so downstream Listing, mockup, and export tools can read it without scanning all designs.

Alternative considered: require approved or ready-for-export state before final promotion. Rejected because the product owner decided final selection should use a tag rather than a status requirement, so a creator can mark any variant as final without first advancing its approval state.

### 3. Approval state is a small explicit state machine

Designs move through draft, needs revision, approved, rejected, exported, and ready for export. Transitions are validated (e.g., only approved or ready-for-export designs can be promoted to final; only exported or ready-for-export designs can advance through export targets). Rejected and superseded designs remain available for review without deletion.

Alternative considered: free-form status text. Rejected because downstream tools need to branch on approval state.

### 4. Intended use and cleanup are structured metadata, not tags

Intended use (dark shirts, light shirts, specific colors, product types, marketplaces, export targets) is stored as structured design metadata fields, not as design-scoped tags, so downstream Listing, mockup, and export tools can read typed fields. Cleanup state (needs upscale, needs transparent cleanup, cropped, print-ready) is stored as documented design metadata. The structured MockupProduct, MockupTemplate, and MockupColorVariant entities arrive with FC-0213 (Mockup Product Settings) and are referenced by id from the mockup's regeneration-metadata block when relevant. FC-0203 does not require those entities to exist; the structured intended-use fields hold the values directly.

Alternative considered: use design-scoped tags for intended use. Rejected because the product owner decided shirt color mappings should be structured metadata, not tags, so downstream tools get typed fields rather than free-form tag strings.

### 5. Designs reference assets and prompts, not own them

A design associates with assets through the existing asset-management context-link model (the design becomes a valid asset context). AI-assisted designs reference prompt records through the existing Prompt entity and store provider metadata and generation settings as documented design metadata. Designs never own asset files or prompt records; removal of a design does not delete its referenced assets or prompts.

Alternative considered: embed assets inside the design. Rejected because asset-management already owns import, deduplication-avoidance, missing-file detection, and removal.

### 6. Forward-only schema migration

A new design table and a listing-level final-selected-designs collection are added through the existing snapshot and migration path. Existing listings load with zero designs and an empty final-selected-designs collection. No data backfill is required. No down-migration is provided.

Alternative considered: store designs inside listing JSON. Rejected because multiple designs per listing, final-selection queries, and downstream tool joins benefit from a typed table.

## Risks / Trade-offs

- [Stacked core-domain-model modification] -> FC-0203 modifies the same "Core model avoids premature advanced entities" requirement that the in-flight concept-versions change modifies. Archive concept-versions first, then re-base this change's MODIFIED block onto the post-concept-versions spec before archiving.
- [Final-selection invariant can break under partial saves] -> Enforce the invariant inside the atomic snapshot and add invariant tests.
- [Variant accumulation may clutter downstream tools] -> The Basic Design Tool shows final and draft variants distinctly; downstream Listing tools see final variants by default.
- [Intended-use metadata can become an untyped catch-all] -> Limit FC-0203 keys to documented fields and preserve unknown keys during edits.

## Migration Plan

Increment the workspace snapshot version, add the design table and listing-level final-selected-designs collection, and migrate existing snapshots by assigning zero designs and an empty collection. No data backfill is required. Down-migration is unsupported.

## Open Questions

- Should "exported" and "ready for export" be distinct states, or a single state with an export-targets metadata block? (Default: distinct states, because the PRD lists both and downstream export work benefits from the distinction.)
- Should a design be allowed to reference more than one concept (e.g., a merge of two directions)? (Default: no — one implemented-concept reference per design, to keep the concept-to-design chain traceable.)
