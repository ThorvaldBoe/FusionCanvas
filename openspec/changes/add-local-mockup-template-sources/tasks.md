## 1. Domain source-image model and resolution policy

- [x] 1.1 Add current and revision source-image entities, option-value condition entities, and one-primary-type-per-file namespaces under `FusionCanvas.Domain.Mockups`.
- [ ] 1.2 Revise current and revision source entities so absent applicability and mapping are valid persisted incomplete state while assigned grouped values and mappings retain identity, ownership, uniqueness, and bounds validation.
- [ ] 1.3 Implement OR-within-one-Option and AND-across-Options matching, excluding incomplete entries and retaining stable resolved, missing, and ambiguous outcomes independently for every compatible Variant.
- [ ] 1.4 Add focused Domain tests for one-Color/all-Sizes coverage, alternatives within an Option, conditions across Options, incomplete entries, cross-offering/archived values, unaffected successful resolutions, mapping validation, and revision immutability.

## 2. Application source configuration and asset lifecycle

- [ ] 2.1 Extend source-image draft/request/result/readiness contracts with optional metadata, derived per-entry completeness, grouped applicability presentation, archive state, and per-Variant resolution without exposing Avalonia or decoder types.
- [ ] 2.2 Implement an explicit-save local source-image use case that imports new files through `IWorkspaceFileStore`, persists complete and incomplete entries plus archives, creates Store-owned Assets and links, and saves the Template/source/revision graph once.
- [x] 2.3 Ensure cancelled, invalid, unreadable, unsupported, decoder-failed, and persistence-failed input preserves the confirmed configuration and best-effort cleans only a newly copied managed file after a failed save.
- [ ] 2.4 Replace the provider-candidate-dependent Template creation/edit behavior with one atomic master-detail draft save containing Template name, shared Design Area, independently uploaded entries, grouped conditions, optional source mappings, archives, revision snapshots, and readiness summaries.
- [x] 2.5 Extend Asset removal dependency checks to block deletion of Assets used by current or historical Template source entries and expose actionable dependency details.
- [ ] 2.6 Add deterministic Application tests with repository, file-store, and image-inspection fakes for incomplete save/reload, later metadata completion, grouped matching, archive, replace, cancel, failures, per-Variant outcomes, source provenance, and removal blockers.

## 3. Local image inspection and SQLite/workspace persistence

- [x] 3.1 Implement the bounded Integration raster-metadata adapter and source-specific supported-format policy; reject untrusted or non-decodable files before persistence.
- [ ] 3.2 Add the next ordered transactional SQLite migration and mappings for optional current/revision image mappings and persisted zero-condition entries; preserve existing complete source graphs and update new-database schema, indexes, and referential validation.
- [ ] 3.3 Preserve prior Template/color/revision records during migration; convert only valid non-null legacy local source assets and never fabricate local data from provider-reference-only history.
- [ ] 3.4 Add isolated Integration tests for complete and incomplete SQLite save/load, new and legacy migration compatibility and rollback, grouped relationship reconstruction, archive state, managed-file cleanup, and workspace-package round trips of source Assets and source snapshots.

## 4. Focused Mockup Template dialog

- [ ] 4.1 Remove the production unavailable provider-catalog composition and update Template terminology, state messages, and accessible names to local **Mockup source images** and an **Upload image...** action.
- [ ] 4.2 Recompose the dialog from the approved UI-language artifact: Template name/shared Design Area, upper image table with upload/archive/select/status, and lower selected-image metadata/placement editor.
- [ ] 4.3 Make upload independent of metadata; select a newly uploaded row without copying applicability or mapping, preserve every row's draft across selection changes, and select a sensible remaining row or empty state after confirmed archive.
- [ ] 4.4 Implement Color-first grouped applicability with one Color/all-Sizes as the shortest path, progressively disclosed Size/other Options, OR-within/AND-between summaries, and stable identity-based selection.
- [ ] 4.5 Render the selected staged or managed source image safely, keep mapping unset until explicitly configured, persist independent in-bounds mappings, and expose per-row completeness separately from Template-level Variant readiness.
- [ ] 4.6 Preserve meaningful-draft discard confirmation and focus behavior for source, condition, mapping, and archive edits; keep incomplete saves allowed and archived Store configuration visibly read-only.
- [ ] 4.7 Add focused framework-free ViewModel tests and meaningful Avalonia headless dialog tests for master-detail bindings, upload independence, table selection, archive aftermath, grouped conditions, mapping, completion/readiness states, keyboard focus, errors, empty/read-only states, and discard behavior.

## 5. Documentation and completion verification

- [ ] 5.1 Reconcile directly affected UI/product guidance from the empty future-source state to local managed source images with flexible Option Value applicability; do not document external-provider behavior as implemented.
- [ ] 5.2 Validate and retain the UI-language source plus incomplete, complete, and no-selection SVG states as review evidence; reconcile it with the final AXAML behavior.
- [ ] 5.3 Replace prior partial evidence in `verification.md` with criterion-level mappings for every revised `mockup-template-source-images` and `product-supplier-setup` scenario.
- [ ] 5.4 Run focused Domain, Application, Integration, UI-description, and App test suites; correct failed criteria and rerun their focused checks.
- [ ] 5.5 Run `openspec validate add-local-mockup-template-sources --strict` and correct every validation error or warning.
- [ ] 5.6 Run `dotnet test .\FusionCanvas.sln` and record the deterministic baseline result in `verification.md`.
- [ ] 5.7 Perform changed-scope architecture, security, persistence, UI, and specification-drift review; record results and keep Printify/API, credentials, drag-and-drop, rendering, and marketplace scope excluded.
