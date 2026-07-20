## 1. Concept Domain Entity

- [ ] 1.1 Add a `Concept` domain entity owned by exactly one listing, with stable identity, created/modified timestamps, Concept-stage context, and the documented creative fields (idea, phrase, graphic direction, audience reaction, risks, quality notes, score metadata).
- [ ] 1.2 Add a listing-level selected-concept reference and enforce the "at most one selected concept per listing" invariant inside the atomic snapshot.
- [ ] 1.3 Add concept lifecycle states (active, superseded, rejected) with explicit supersede and reject transitions that preserve versions without deletion.
- [ ] 1.4 Add domain tests for item-binding, multi-version preservation, single-selection invariant, supersede/reject transitions, and zero-concept listings.

## 2. Concept Application Layer

- [ ] 2.1 Define `IConceptService` requests and results for create, edit, supersede, reject, select, and metadata update, with recoverable error reporting.
- [ ] 2.2 Implement concept creation from an existing idea or directly from a phrase, graphic direction, or later-stage starting point, with stable identity, timestamps, and applicable inherited context through the context-aware boundary.
- [ ] 2.3 Implement concept edit that preserves identity, listing, lifecycle state, selection, prompts, asset links, and unknown metadata.
- [ ] 2.4 Implement supersede (new version selected, prior demoted), reject (selection cleared, marker recorded), and select (set selected reference) with single-selection enforcement.
- [ ] 2.5 Implement advisory score metadata storage with absent, overall, weak element, confidence, and critique hint states, without stage-advancement gating.
- [ ] 2.6 Add application tests for inherited values, overrides, preservation rules, supersede/reject/select flows, score absence, and repository-failure atomicity.

## 3. Persistence and Migration

- [ ] 3.1 Add a forward-only schema migration that creates the concept table and the listing-level selected-concept reference and increments the snapshot version.
- [ ] 3.2 Migrate existing snapshots by assigning zero concepts and no selection without data backfill.
- [ ] 3.3 Add SQLite integration tests for create, edit, supersede, reject, select, metadata, score, complete reload, and migration of pre-existing snapshots.
- [ ] 3.4 Verify failed or cancelled concept operations persist no partial concepts, selection changes, lifecycle changes, or metadata changes.

## 4. Deletion Guard and Cross-Capability Integration

- [ ] 4.1 Treat concepts as dependent creative records in the listing-management permanent-deletion guard and block connected listing deletion with actionable guidance.
- [ ] 4.2 Expose the selected concept's idea, phrase, and graphic direction as the current values for downstream Concept and Design tools, and report "no concept selected" when none exists.
- [ ] 4.3 Add integration tests for deletion blocking, downstream tool exposure, and zero-selection reporting.

## 5. Verification

- [ ] 5.1 Run domain, application, integration, app, and UI automation test suites and resolve core-domain-model, listing-management, and workflow-stage-navigator regressions.
- [ ] 5.2 Manually verify concept creation from idea and from later-stage starting points, supersede, reject, select, inherited context, score absence, persistence, and deletion-guard blocking.
- [ ] 5.3 Run strict OpenSpec validation and confirm every concept-versions and modified core-domain-model scenario is covered by implementation or automated tests.
