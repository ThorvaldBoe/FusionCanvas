## 1. Marketplace Metadata Model

- [ ] 1.1 Document listing metadata keys for price (with optional currency), marketplace notes, product type, provider product reference, shipping profile notes, and draft keywords.
- [ ] 1.2 Add domain validation for non-negative decimal price with optional ISO 4217 currency code.
- [ ] 1.3 Preserve unknown metadata keys during edits, moves, archive/restore, and duplication.
- [ ] 1.4 Add domain tests for field validation, unknown-key preservation, and omission tolerance.

## 2. Listing Metadata Editor Application Layer

- [ ] 2.1 Define `IListingMetadataEditorService` requests and results for load and save of marketplace-preparation metadata, with recoverable error reporting.
- [ ] 2.2 Implement save that persists marketplace-metadata keys atomically, preserves identity/topic/archive state/status/tags/assets/prompts/concepts/designs/mockups, and clears dirty state for the publishing-metadata section only.
- [ ] 2.3 Implement context inheritance that resolves store, niche, topic path, selected concept, selected final designs, mockups, and inherited tags as read-only reference without auto-populating marketplace fields.
- [ ] 2.4 Add application tests for save, validation rejection, context inheritance, missing-context readiness, and repository-failure atomicity.

## 3. Persistence Verification

- [ ] 3.1 Confirm the existing schema and snapshot validation support the documented marketplace-metadata keys without a database migration.
- [ ] 3.2 Add SQLite integration tests for save, move, archive/restore, duplication, complete reload, and unknown-key preservation.
- [ ] 3.3 Verify failed or cancelled metadata operations persist no partial marketplace-metadata changes.

## 4. Editor Surface and Cross-Capability Integration

- [ ] 4.1 Add a focused Listing Metadata Editor surface or section that preselects the active listing, loads existing metadata, and uses explicit save with dirty tracking and unsaved-change prompts.
- [ ] 4.2 Keep creative-notes (listing-management) and publishing-metadata (this capability) sections visibly separated with independent save actions and labels.
- [ ] 4.3 Apply compact action sizing, icon tooltips, predictable tab order, busy states, duplicate-submission prevention, inline errors, and shared desktop control guidance.
- [ ] 4.4 Add view-model and UI tests for load/save, dirty tracking, unsaved-change prompts, section independence, missing-context readiness, validation errors, rollback, and shared control guidance.

## 5. Verification

- [ ] 5.1 Run domain, application, integration, app, and UI automation test suites and resolve listing-management, listing-lifecycle-status, tag-management, concept-versions, design-records, and mockup-records regressions.
- [ ] 5.2 Manually verify metadata editing, context inheritance, creative-notes separation, preservation through moves and lifecycle, unknown-key preservation, validation errors, and application reload.
- [ ] 5.3 Run strict OpenSpec validation and confirm every listing-metadata-editor scenario is covered by implementation or automated tests.
