## Why

FusionCanvas already tracks a record through Idea, Concept, Design, and Listing, but its generic inspector does not provide a usable minimum workflow for completing those stages, and the stage-agnostic record is confusingly named `Listing`. This module makes the application useful for real product tracking now while establishing explicit Item, stage-content, design-file, publication-protection, and verification boundaries for later advanced tools.

**Module outcome:** A user can create an otherwise empty Item, carry it intentionally from Idea through Listing, manage the minimum content and PNG Design files for each stage, and control publication-oriented lifecycle states without losing data or accidentally modifying protected content.

**Scope rationale:** The terminology migration, stage tools, workflow/edit policy, Design-file handling, lifecycle protection, synchronization, and persistence proof form one coherent outcome because they operate on the same Item record and must work together in one end-to-end acceptance journey. Splitting them would leave an unusable intermediate workflow or duplicate migration and verification work. Advanced creative tools, mockups, marketplace publishing, and versioning remain separate outcomes and are excluded.

## What Changes

- **BREAKING:** Rename the universal stage-agnostic `Listing` concept to `Item` across domain and application contracts, user-facing language, serialized contracts, relationships, and physical SQLite structures, with an identity- and relationship-preserving migration.
- Allow creation of an Item with only its automatically assigned stable ID; show titleless Items as `Untitled item · <short ID>` without persisting the fallback.
- Replace the all-sections inspector with a scrollable Item document surface containing shared Overview, Notes, Tags, related assets, and lifecycle areas around exactly one active built-in Stage Tool.
- Add basic built-in Stage Tools:
  - Idea: optional multi-line original Idea;
  - Concept: optional Concept idea, single-line Phrase, and multi-line Graphics description;
  - Design: zero or more managed PNG Design files with import, in-app preview, Export copy, missing-state handling, and confirmed removal;
  - Listing: the Item's local lifecycle-status workflow without marketplace publishing.
- Make earlier available stages review-only. Editing upstream content requires explicit adjacent regression; stage movement has no content-completion gates and preserves downstream data.
- Enforce the approved status graph, stage preconditions, confirmations, and Published/Rejected/archive edit protections while keeping approved shared metadata available.
- Use explicit Save and save/discard/cancel guards for working title, current-stage text, and Notes; retain immediate independent Tag persistence.
- Keep mockups, marketplace integration, external image opening, design editing/versioning, AI, batch operations, and custom workflows out of scope.
- **Verification:** Add criterion-level deterministic evidence plus a targeted real-desktop pass for migration, the end-to-end workflow, file safety, publication protection, synchronization, restart, scrolling, focus, and recovery.

## Capabilities

### New Capabilities

- `basic-product-workflow`: Defines the minimum stage-specific Item content, current-stage edit boundary, built-in stage surfaces, and end-to-end progression outcome.

### Modified Capabilities

- `core-domain-model`: Replace the universal Listing concept with Item, allow ID-only Items, and preserve identity and relationships through migration.
- `listing-management`: Rename management behavior to Item management and allow titleless Item creation with a stable presentation fallback.
- `listing-inspector`: Replace the generic all-sections Listing inspector with shared Item chrome plus one stage-specific built-in tool and approved save/edit boundaries.
- `listing-lifecycle-status`: Rename lifecycle ownership to Item, define the complete status graph and confirmations, and protect Published and Rejected content.
- `workflow-stage-navigator`: Make earlier-stage navigation review-only and require regression before upstream editing while preserving ungated adjacent movement.
- `asset-management`: Treat Item-linked exported PNGs as Design files and add workflow-scoped preview/export/remove behavior without versioning.
- `workspace-file-storage`: Add safe managed-file preview/read and Export copy boundaries.
- `stage-tool-host`: Register and render the four basic built-in Item stage tools while shared Item metadata remains outside the tool.
- `navigation-tree`: Use Item terminology and stable fallback labels while preserving canonical selection and inactive treatments.
- `tabbed-document-window`: Use Item terminology and the same stable title fallback in tabs and active document context.
- `search-filtering`: Apply Item terminology and keep stage/status/archive filtering synchronized with the new transition rules.
- `local-sqlite-persistence`: Migrate physical Listing tables, columns, and relationships to Item terminology without identity or data loss.
- `tag-management`: Rename universal links to Item tags and preserve immediate Tag persistence for otherwise guarded Item drafts.
- `context-aware-tools`: Supply selected Item context to built-in and future contributed tools under the renamed universal concept.

## Impact

- **Domain:** `Listing`, status helpers, tag/prompt relationships, entity-kind naming, snapshot collections, hierarchy helpers, and related tests become Item-named contracts while persisted enum values remain compatible where required.
- **Application:** Listing management/inspector/metadata contracts become Item services; workflow and edit policy becomes centralized; stage saves become stage-aware; Design-file use cases extend existing asset/file boundaries.
- **Integration:** SQLite advances from schema version 4 through a transactional Item rename/data-copy migration; managed storage gains traversal-safe preview/read and export-copy behavior.
- **App:** document context, navigation, tabs, filters, status controls, inspector bindings, and tests adopt Item language; primary stage content moves through built-in Stage Tool registration; the outer Item surface becomes vertically scrollable.
- **Compatibility:** existing IDs, stage/status/archive values, text, hidden Audience/generic Description data, unknown metadata, provenance, tags, prompts, asset links, topic placement, and managed files must survive upgrade.
- **Documentation:** accepted specs and current UI/UX guidance must be reconciled with the approved explicit-save exception for Item workflow fields.
- **Dependencies:** no new third-party package or external service is required; all behavior remains local-first.
- **Primary risks:** the cross-cutting Item rename and SQLite migration could lose compatibility; managed-file operations could escape the workspace or leave partial state; stale tabs could overwrite newer workflow state; and the combined UI surface could become difficult to navigate. The design addresses these through transactional migration fixtures, centralized policy, traversal-safe file boundaries, authoritative mutation results, focused layer gates, and targeted desktop verification.
