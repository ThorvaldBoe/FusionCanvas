## 1. Domain source-image model and resolution policy

- [x] 1.1 Add current and revision source-image entities, option-value condition entities, and one-primary-type-per-file namespaces under `FusionCanvas.Domain.Mockups`.
- [x] 1.2 Implement domain validation for stable identities, non-empty unique condition sets, Offering ownership, active values, mapping bounds, Placeholder-compatible variants, and immutable revision snapshots.
- [x] 1.3 Implement deterministic exact-one source-image resolution with stable missing and ambiguity diagnostics per compatible concrete Variant.
- [x] 1.4 Add focused Domain tests for color-only coverage, arbitrary option-value conjunctions, cross-offering/archived values, missing/overlapping matches, mapping validation, and revision immutability.

## 2. Application source configuration and asset lifecycle

- [x] 2.1 Define source-image draft/request/result/readiness contracts and a raster-metadata inspection port at the Application boundary without exposing Avalonia or decoder types.
- [x] 2.2 Implement an explicit-save local source-image use case that stages source changes, validates them, imports new files through `IWorkspaceFileStore`, creates Store-owned Assets and links, and saves the complete Template/source/revision graph once.
- [x] 2.3 Ensure cancelled, invalid, unreadable, unsupported, decoder-failed, and persistence-failed input preserves the confirmed configuration and best-effort cleans only a newly copied managed file after a failed save.
- [ ] 2.4 Replace the provider-candidate-dependent Template creation/edit behavior with source-entry-specific mappings, condition selections, revision snapshots, and readiness summaries.
- [x] 2.5 Extend Asset removal dependency checks to block deletion of Assets used by current or historical Template source entries and expose actionable dependency details.
- [ ] 2.6 Add deterministic Application tests with repository, file-store, and image-inspection fakes for create, replace, cancel, failures, readiness, source provenance, and removal blockers.

## 3. Local image inspection and SQLite/workspace persistence

- [x] 3.1 Implement the bounded Integration raster-metadata adapter and source-specific supported-format policy; reject untrusted or non-decodable files before persistence.
- [x] 3.2 Add the ordered transactional SQLite migration, new-table mappings, indexes, referential validation, and new-database schema for current/revision source images and conditions.
- [ ] 3.3 Preserve prior Template/color/revision records during migration; convert only valid non-null legacy local source assets and never fabricate local data from provider-reference-only history.
- [ ] 3.4 Add isolated Integration tests for SQLite save/load, migration compatibility and rollback, relationship validation, managed-file cleanup, and workspace-package round trips of source Assets and source snapshots.

## 4. Focused Mockup Template dialog

- [ ] 4.1 Remove the production unavailable provider-catalog composition and update Template terminology, state messages, and accessible names to local **Mockup source images**.
- [ ] 4.2 Add an injected raster-file picker and draft source-image collection with a keyboard-accessible Browse action, selected-entry behavior, busy protection, and explicit cancellation handling.
- [ ] 4.3 Add Color-first applicability selection plus progressively disclosed active non-color Option Value conditions; preserve per-entry draft state while selection changes.
- [ ] 4.4 Render the selected staged or managed source image safely in the placement editor, initialize and persist a source-specific in-bounds mapping, and expose missing/ambiguous/readiness feedback.
- [ ] 4.5 Preserve existing meaningful-draft discard confirmation and focus-return behavior for source, condition, and mapping edits; keep archived Store configuration visibly read-only.
- [ ] 4.6 Add focused framework-free ViewModel tests and meaningful Avalonia headless dialog tests for bindings, command state, selected source/conditions, preview readiness, error/empty/read-only states, focus, and discard behavior.

## 5. Documentation and completion verification

- [ ] 5.1 Reconcile directly affected UI/product guidance from the empty future-source state to local managed source images with flexible Option Value applicability; do not document external-provider behavior as implemented.
- [x] 5.2 Create `verification.md` mapping every `mockup-template-source-images` and modified `product-supplier-setup` acceptance scenario to focused evidence, including any not-applicable rationale.
- [x] 5.3 Run focused Domain, Application, Integration, and App test suites; correct failed criteria and rerun their focused checks.
- [x] 5.4 Run `openspec validate add-local-mockup-template-sources --strict` and correct every validation error or warning.
- [x] 5.5 Run `dotnet test .\FusionCanvas.sln` and record the deterministic baseline result in `verification.md`.
- [ ] 5.6 Perform changed-scope architecture, security, persistence, UI, and specification-drift review; record results and keep Printify/API, credentials, drag-and-drop, rendering, and marketplace scope excluded.
