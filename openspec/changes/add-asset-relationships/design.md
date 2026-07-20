## Context

Asset management accepts importing a file into managed workspace storage, attaching it to exactly one active context (listing, niche, group, or store), labeling a purpose, detecting missing managed files, confirming removal atomically, and preserving relationships when listings or groups move. The in-flight concept-versions and design-records changes add Concept and Design as item-bound creative records that need asset support. The Phase 2 PRD requires a single asset to support more than one context, asset purpose to be visible, and missing or unavailable assets to remain understandable.

What is missing is the multi-context link model, the concept and design context kinds, and the per-context purpose visibility that Phase 2 creative work needs.

## Goals / Non-Goals

**Goals:**

- Allow an asset to carry more than one context link, each validated against the active workspace and store.
- Add Concept and Design as valid asset context kinds.
- Surface concept and design asset context views in the focused asset surface.
- Preserve all context links across listing, concept, and design edits, supersession, and lifecycle changes.
- Keep per-context purpose visible and editable.
- Reuse the existing import, missing-file, removal, and atomic-persistence behavior.

**Non-Goals:**

- Do not implement asset deduplication, cloud sync, automatic file matching, or image processing.
- Do not implement Mockup as an asset context kind; that arrives with FC-0206.
- Do not change the import flow's single-context default; import still attaches to one context, with additional links added afterwards.
- Do not implement cross-store asset linking.

## Decisions

### 1. Multi-context links are explicit, validated, and per-asset

An asset record continues to own one store and one managed file. After import, the user may add additional context links, each validated against the active workspace, the asset's store, and the target context's active ancestry. A single asset may be linked to the same context kind multiple times only when the purpose differs; duplicate (context, purpose) links are rejected. Removing a context link is its own atomic operation.

Alternative considered: clone the asset for each context. Rejected because the PRD explicitly wants one asset to support multiple contexts and the managed file should not be duplicated.

### 2. Concept and Design are valid context kinds

The context-link kind enum is extended to include Concept and Design. A concept context link requires an active concept owned by a listing in the asset's store. A design context link requires an active design owned by a listing in the asset's store. Archived, superseded, or rejected concepts and designs remain valid link targets for review but are excluded from active context views through the shared archive-aware projection.

Alternative considered: only allow listing-level asset links and let concepts/designs read listing assets. Rejected because the PRD wants to see which design an asset belongs to and which assets support a concept.

### 3. Per-context purpose is visible and editable

Each context link carries its own purpose label. Import pre-selects the purpose from the file extension as today. Adding a context link pre-selects a purpose derived from the target context kind (e.g., `source` for a design source file, `export` for a design export, `reference` for a concept reference). The user may change the purpose per link.

Alternative considered: one purpose per asset shared across contexts. Rejected because the same file may be a source in one context and an export in another.

### 4. Removal semantics are unchanged but explicitly multi-context

Removing an asset removes the asset record, all context links, and the managed file atomically after confirmation, as today. Removing a concept or design removes that context link through the concept/design service's removal flow without deleting the asset; the asset remains reachable at store level with an unlinked indicator when no context links remain. Removing a listing removes its listing context link; concepts and designs owned by that listing are blocked by their own deletion guards first.

Alternative considered: cascade-remove assets when a concept or design is removed. Rejected because shared assets must survive the removal of one context.

### 5. Reuse the focused asset surface for concept and design views

The existing focused asset surface gains concept and design context views that list assets linked to the selected concept or design with their per-context purposes and file state. The store-level view continues to list every store-owned asset with its linked contexts or an unlinked indicator.

Alternative considered: separate asset panels inside the Concept and Design tools. Rejected because the asset surface is the accepted home for asset review and management.

## Risks / Trade-offs

- [Multi-context links complicate removal guards] -> Keep removal atomic at the asset level and let concept/design removal unlink only their own context.
- [Per-context purpose can confuse users] -> Show purpose inline per link and pre-select sensible defaults per target context kind.
- [Mockup context kind is deferred] -> FC-0206 will extend the context kind enum again; keep the enum open for extension.

## Migration Plan

No data migration is expected. The context-link kind enum is extended; existing links remain valid. The "exactly one context" import invariant is relaxed to "at least one context" in code and spec only.

## Open Questions

- Should adding a context link be allowed during import (multi-target import), or only after import completes? (Default: only after import, to keep the import flow single-context and atomic.)
- Should per-context purpose be required when adding a context link, or defaultable and editable later? (Default: defaultable from the target context kind, editable later.)
