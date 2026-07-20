## Why

Asset management already imports files, attaches each asset to one context, labels purpose, detects missing files, and preserves relationships across listing and group moves. Phase 2 adds Concept and Design entities that also need asset support, and creators need a single asset to support more than one context when one source file, export, or generated image serves multiple creative records. FC-0204 extends asset relationships to concept and design contexts, allows additional context links after import, and keeps purpose visible per relationship without changing the import, missing-file, or removal behavior.

## What Changes

- Allow an asset to be linked to more than one context (listing, niche, group, store, concept, or design) through explicit additional-link operations.
- Add Concept and Design as valid asset context kinds, with the same active-ancestry validation as existing contexts.
- Surface concept and design asset context views alongside listing, niche, group, and store views.
- Preserve all context links when listings, concepts, or designs are edited or superseded; preserve asset records when a linked concept or design is removed.
- Keep asset purpose visible per context link and allow per-context purpose labels where useful.
- Keep removal behavior unchanged: removing an asset removes the asset record and all its context links atomically; removing a concept or design removes that context link without deleting the asset.

## Capabilities

### New Capabilities

- `asset-relationships`: Defines multi-context asset linking, per-context purpose visibility, concept and design asset context views, and relationship preservation across concept/design lifecycle and listing reorganization.

### Modified Capabilities

- `asset-management`: Relaxes the "exactly one context" import rule to "at least one context" and adds Concept and Design as valid asset context kinds alongside listing, niche, group, and store.

## Impact

- Extends the asset context-link model to support multiple links per asset and adds concept and design as valid context kinds.
- Adds an `AssetRelationshipService` for additional-link, relink, per-context purpose, and relationship-preserving removal operations.
- Updates the asset surface to expose concept and design context views.
- Reuses `concept-versions` and `design-records` for context validation and lifecycle.
- Adds domain, application, integration, app, and UI tests for multi-context linking, concept/design context views, per-context purpose, preservation across edits and removals, and shared desktop control guidance.
- No schema migration is expected beyond the context-link kind extension; existing single-link assets remain valid.
