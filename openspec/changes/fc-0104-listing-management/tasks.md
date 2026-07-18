## 1. Listing Location and Navigation Foundation

- [ ] 1.1 Add an explicit listing location reference that validates store, niche, and group destinations without weakening topic-only group movement contracts.
- [ ] 1.2 Update workspace navigation tree construction to include active store-level listings directly under their store and exclude archived listings from normal navigation.
- [ ] 1.3 Extend listing movement and creation-scope resolution for store-level listings while retaining same-store and active-ancestry validation.
- [ ] 1.4 Add domain and application navigation tests for store-level build, ordering, selection, reveal, creation scope, topic moves, store moves, and rejected cross-store or unavailable destinations.

## 2. Listing Management Application Layer

- [ ] 2.1 Define listing-management location, context, create, update, move, duplicate, delete, summary, state, and result contracts with testable error reporting.
- [ ] 2.2 Implement loading and active listing selection scoped to the active workspace/store, separating active and archived listings and clearing invalid selection deterministically.
- [ ] 2.3 Implement one-line listing creation with normalization, active location validation, draft defaults, optional core details, stable identity, and timestamps.
- [ ] 2.4 Integrate resolved context-aware tags and metadata into listing creation, preserving explicit overrides, inherited provenance, unknown metadata, and reusable tag records.
- [ ] 2.5 Implement core-detail editing that preserves identity, placement, status, archive state, relationships, and unknown metadata while rejecting invalid titles.
- [ ] 2.6 Implement same-store moves among store, niche, and group locations without recalculating inherited context or changing existing tag, prompt, and asset relationships.
- [ ] 2.7 Implement duplication with a new identity, collision-safe copy title, draft status, copied core metadata/tag links, optional same-store destination, and excluded prompt/asset relationships.
- [ ] 2.8 Implement reversible archive and restore with preserved placement/context and blocked restoration for unavailable parent locations.
- [ ] 2.9 Implement confirmed permanent deletion, block listings with prompt/asset/future dependent records, and atomically remove listing-specific tag links without deleting tags.
- [ ] 2.10 Add application service tests covering successful operations, invalid/missing/archived/cross-workspace contexts, inherited values and overrides, preservation rules, duplication exclusions, restore blocking, deletion guards, state, and repository failures.

## 3. Persistence Integration

- [ ] 3.1 Verify and, only if required, adjust snapshot validation and SQLite persistence so store-level listings and listing-management mutations round-trip without a schema migration.
- [ ] 3.2 Add integration tests for create, edit, store/topic move, duplicate metadata/tag links, archive/restore, guarded delete, and full reload behavior.
- [ ] 3.3 Verify failed or cancelled operations do not persist partial listings, tag links, placement changes, or relationship removals.

## 4. Compact Listing Capture

- [ ] 4.1 Add a compact New Listing command beside the active navigation context for active store, niche, and group selections, with clear blocked behavior when no active store exists.
- [ ] 4.2 Implement the one-line capture draft with initial focus, optional progressive details, create/cancel keyboard flow, retained input on validation or save failure, and duplicate-submission prevention.
- [ ] 4.3 Refresh, expand, reveal, and select a successfully created listing and hand it to the existing document-tab workflow without permanently consuming document workspace area.
- [ ] 4.4 Add app tests for capture scope, empty and blocked states, focus/cancellation, validation and persistence errors, loading state, navigation refresh, selection, and document availability.

## 5. Focused Listing Management Surface

- [ ] 5.1 Add a focused listing-management view model and Avalonia surface opened from the selected listing or compact item action menu, preselecting the invoking listing while preserving main-window context.
- [ ] 5.2 Implement active/archived listing browsing plus explicit Save, Move, Duplicate, Archive, Restore, and Delete commands with state-coherent enablement and progressive disclosure.
- [ ] 5.3 Add unsaved-change detection and discard confirmation for listing selection changes, destination changes, and close, preserving draft and focus when discard is declined.
- [ ] 5.4 Add explicit permanent-deletion confirmation and cancellation, blocked-deletion guidance, and archived-parent restore guidance.
- [ ] 5.5 Implement deterministic post-archive/delete replacement selection, parent empty states, and focus return to the invoking or replacement control.
- [ ] 5.6 Apply shared compact button sizing, icon tooltips, predictable tab order, keyboard-accessible save/cancel/confirm behavior, inline errors, and in-progress state presentation.
- [ ] 5.7 Add app tests for action enablement, editing and move drafts, duplication, active/archived transitions, unsaved changes, confirmations, errors, replacement selection, focus behavior, and compact UI rules.

## 6. Verification

- [ ] 6.1 Run the domain, application, integration, and app test suites and resolve listing-management regressions.
- [ ] 6.2 Build the full solution and manually smoke-test one-line capture, store/niche/group placement, focused editing, move, duplicate, archive, restore, delete blocking, and application reload.
- [ ] 6.3 Run strict OpenSpec validation and confirm every listing-management and modified navigation scenario is covered by implementation or automated tests.
