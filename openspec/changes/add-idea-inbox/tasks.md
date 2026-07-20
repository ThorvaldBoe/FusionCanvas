## 1. Idea Inbox Application Layer

- [x] 1.1 Add an `IdeaInbox` application service that loads non-archived, non-rejected idea-stage listings scoped to an active store, niche, group, or topic context using the shared archive-aware projection.
- [x] 1.2 Define idea-stage metadata keys for audience, phrase fragments, and visual direction and document them as preserved listing metadata.
- [x] 1.3 Implement inbox capture that delegates topic resolution and atomic persistence to the existing listing-management service, returning stable identity and selection state.
- [x] 1.4 Implement optional idea-stage metadata edits that preserve identity, topic, archive state, status, tags, prompts, assets, and unknown metadata.
- [x] 1.5 Implement promote-to-Concept/Design/Listing by requesting the workflow-stage-navigator, surfacing accepted or rejected transitions without setting the stage field directly.
- [x] 1.6 Implement archive and reject (archive-with-`idea.rejected` marker plus optional reason) by delegating archive to listing-management and recording the marker atomically.
- [x] 1.7 Implement restore-clears-marker behavior through the existing restore flow so restored ideas return to the active inbox when their topic path is active.
- [x] 1.8 Add application tests for context scoping, archived/rejected exclusion, capture resolution, metadata preservation, promotion success and rejection, archive, reject, restore, and repository-failure atomicity.

## 2. Persistence Verification

- [x] 2.1 Confirm the existing schema and snapshot validation support idea-stage metadata keys and the `idea.rejected` marker without a database migration.
- [x] 2.2 Add SQLite integration tests for capture, metadata edit, promotion, archive, reject, restore, and complete reload behavior.
- [x] 2.3 Verify failed or cancelled inbox operations persist no partial listings, metadata changes, stage changes, archive flags, or reject markers.

## 3. Focused Inbox Surface

- [x] 3.1 Add an `IdeaInboxViewModel` and Avalonia focused surface opened from store/niche/group/topic context or the active idea-stage listing.
- [x] 3.2 Implement capture, save, promote, archive, reject, and cancel actions with dirty tracking, unsaved-change prompts, and keyboard-accessible confirmation.
- [x] 3.3 Implement optimistic drafts, rollback on validation or persistence failure, busy states, duplicate-submission prevention, and inline error retention.
- [x] 3.4 Coordinate normal inbox selection with canonical tree selection and the reusable working tab through the existing normal-selection path.
- [x] 3.5 Implement deterministic post-promote/archive/reject replacement selection and focus return to an adjacent idea-stage listing or the invoking context.
- [x] 3.6 Apply compact action sizing, icon tooltips, predictable tab order, empty/blocked/error states, and shared desktop control guidance.
- [x] 3.7 Add view-model and UI tests for triage flows, blocked context, dirty tracking, rollback, replacement selection, focus behavior, and shared control guidance.

## 4. Verification

- [x] 4.1 Run domain, application, integration, app, and UI automation test suites and resolve listing-management and workflow-stage-navigator regressions.
- [x] 4.2 Manually verify keyboard-only capture, triage, promotion, archive, reject, restore, replacement selection, filtering, rollback, and application reload.
- [x] 4.3 Run strict OpenSpec validation and confirm every idea-inbox scenario is covered by implementation or automated tests.
