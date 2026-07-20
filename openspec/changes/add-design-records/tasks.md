## 1. Design Domain Entity

- [ ] 1.1 Add a `Design` domain entity owned by exactly one listing, with stable identity, created/modified timestamps, Design-stage context, name, version, notes, source method, intended use, cleanup state, approval state, and an optional implemented-concept reference.
- [ ] 1.2 Add a listing-level final-selected-designs collection and enforce the invariant (only approved or ready-for-export designs may be members; at least one final design required before advancing to Listing).
- [ ] 1.3 Add the approval state machine (draft, needs revision, approved, rejected, exported, ready for export) with validated transitions and final-selection eligibility.
- [ ] 1.4 Add domain tests for item-binding, multi-variant preservation, final-selection invariant, approval-state transitions, optional implemented-concept reference, and zero-design listings.

## 2. Design Application Layer

- [ ] 2.1 Define `IDesignService` requests and results for create, edit, state transition, promote-final, demote-final, cleanup metadata, intended-use tagging, and removal, with recoverable error reporting.
- [ ] 2.2 Implement design creation from a selected concept or directly from a strong phrase and graphic direction, with stable identity, timestamps, and applicable inherited context through the context-aware boundary.
- [ ] 2.3 Implement design edit that preserves identity, listing, approval state, final-selection membership, implemented-concept reference, asset links, prompt references, and unknown metadata.
- [ ] 2.4 Implement approval-state transitions, final-selection promotion and demotion, and the "at least one final design before Listing advancement" guard through the workflow-stage-navigator.
- [ ] 2.5 Implement intended-use and cleanup-state metadata storage with documented keys and unknown-key preservation.
- [ ] 2.6 Add application tests for inherited values, overrides, preservation rules, state transitions, final-selection invariant, metadata preservation, and repository-failure atomicity.

## 3. Persistence and Migration

- [ ] 3.1 Add a forward-only schema migration that creates the design table and the listing-level final-selected-designs collection and increments the snapshot version.
- [ ] 3.2 Migrate existing snapshots by assigning zero designs and an empty final-selected collection without data backfill.
- [ ] 3.3 Add SQLite integration tests for create, edit, state transitions, promote/demote final, cleanup metadata, intended-use tags, prompt and asset references, complete reload, and migration of pre-existing snapshots.
- [ ] 3.4 Verify failed or cancelled design operations persist no partial designs, approval-state changes, final-selection changes, or metadata changes.

## 4. Asset, Prompt, and Cross-Capability Integration

- [ ] 4.1 Treat designs as a valid asset context through asset-management and preserve referenced assets and prompts when a design is removed.
- [ ] 4.2 Allow AI-assisted designs to reference prompt records and store provider metadata and generation settings as documented design metadata.
- [ ] 4.3 Treat designs as dependent creative records in the listing-management permanent-deletion guard and block connected listing deletion with actionable guidance.
- [ ] 4.4 Expose the final-selected-designs collection to downstream Listing, mockup, and export tools.
- [ ] 4.5 Add integration tests for asset context links, prompt references, deletion blocking, and downstream tool exposure.

## 5. Verification

- [ ] 5.1 Run domain, application, integration, app, and UI automation test suites and resolve core-domain-model, concept-versions, listing-management, asset-management, and workflow-stage-navigator regressions.
- [ ] 5.2 Manually verify design creation from concept and from phrase/graphic, state transitions, final-selection promotion and demotion, intended-use and cleanup metadata, persistence, migration, and deletion-guard blocking.
- [ ] 5.3 Run strict OpenSpec validation and confirm every design-records and modified core-domain-model scenario is covered by implementation or automated tests.
