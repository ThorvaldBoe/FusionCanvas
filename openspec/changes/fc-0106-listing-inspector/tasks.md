## 1. Inspector Application Layer

- [x] 1.1 Define inspector contracts: inspector state (core details, topic path, status, stage, effective activity), creative-field draft, tag entry with provenance, related-asset entry, save request, and result with recoverable error reporting.
- [x] 1.2 Implement inspector state building over `WorkspaceSnapshot`: listing details, notes, `idea`/`phrase`/`graphicDirection` reserved metadata keys, linked tag names with inherited provenance, related assets with kind and missing state, and display path.
- [x] 1.3 Implement the atomic inspector save reusing the FC-0104 listing update path: title validation (nonblank single-line), field normalization (single-line phrase; trimmed plain-text idea/graphic direction/notes), preserved identity, placement, status, archive state, unknown metadata keys, and inherited provenance.
- [x] 1.4 Implement tag resolve-or-create within the save: normalized nonblank single-line names, case-insensitive store-scoped matching, single reusable tag creation for new names, link additions/removals without deleting reusable tag records.
- [x] 1.5 Add application tests for state building, empty listings, normalization, metadata/provenance preservation, tag matching/creation/link removal, invalid title and tag input, inactive listings, and repository-failure atomicity.

## 2. Persistence Verification

- [x] 2.1 Confirm the existing SQLite schema and snapshot model round-trip inspector saves (creative metadata keys, notes, created tags, listing-tag links) without a migration.
- [x] 2.2 Add SQLite integration tests for inspector save and reload, tag resolve-or-create reuse across listings, and link removal preserving reusable tags.
- [x] 2.3 Verify failed or cancelled saves persist no partial listing, tag, or tag-link changes.

## 3. Inspector Presentation

- [x] 3.1 Add the inspector view model: draft fields distinct from persisted state, dirty tracking, explicit Save/validation errors, busy state, and duplicate-submission prevention.
- [x] 3.2 Implement tag editing in the view model: existing-tag suggestions, inline new-tag entry, link removal, and invalid-name rejection.
- [x] 3.3 Implement stage-relevant emphasis mapping (Idea: idea; Concept: phrase and graphic direction; Design: assets; Listing: status summary) while keeping all sections accessible.
- [x] 3.4 Implement read-only inactive presentation (archived or archived-ancestry listings) with an inactive notice and restore guidance, and empty states for creative fields, tags, and assets.
- [x] 3.5 Build the Avalonia inspector view with compact action sizing, sectioned layout in a single scroll container, tooltips for icon-only commands, predictable tab order, and keyboard-reachable save/cancel.
- [x] 3.6 Add view-model tests for dirty tracking, save/validation flows, tag operations, emphasis mapping, inactive read-only behavior, and error recovery.

## 4. Document Window Coordination

- [x] 4.1 Host the inspector in the document detail area when the active document context is a listing item, keeping existing stage-tool placeholder behavior for store/topic contexts.
- [x] 4.2 Synchronize the inspector with active-tab switches, listing mutations from other surfaces (rename, move, archive, restore, status/stage changes), and the workflow stage navigator without adding a second status selector.
- [x] 4.3 Wire the save/discard/cancel guard into listing-selection changes, tab switches, and tab closes, keeping the current context and draft on cancel and reverting on discard.
- [x] 4.4 Route inspector saves through the application service and refresh canonical context (tree row, tab title, navigator, detail header) after success.
- [x] 4.5 Add view-model and UI automation tests for hosting, cross-surface synchronization, guard prompts on switch/close/cancel/discard, and shared control guidance.

## 5. Verification

- [x] 5.1 Run the full solution test suite and resolve regressions in listing-management, tabbed-document-window, and workflow-stage-navigator behavior.
- [x] 5.2 Perform the real desktop UI verification pass on a disposable workspace: browsing listings, viewing all inspector sections, editing and saving each field, tag link/create/remove, assets empty and populated states, unsaved-change guard across selection/tab switch/close, archived listing read-only behavior, keyboard-only save and cancellation, persistence across restart, and record evidence per the QA baseline.
- [x] 5.3 Run strict OpenSpec validation and confirm every listing-inspector and tabbed-document-window scenario is covered by implementation or automated tests.
