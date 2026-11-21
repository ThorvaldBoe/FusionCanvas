## 1. Workspace style model and persistence

- [x] 1.1 Add Domain artwork-style and ordered reference records; extend workspace snapshots, validation, and workspace-scoped queries.
- [x] 1.2 Add an additive SQLite migration, repository mapping/load/save support, title uniqueness validation, and disposable round-trip/migration tests.
- [x] 1.3 Add managed reference-image import/read/removal orchestration with traversal-safe paths, stable ordering, shared-file protection, and staged-file cleanup on save failure.
- [x] 1.4 Add Application style list/create/update/delete use cases, including required-field validation, case-insensitive workspace-local title uniqueness, and atomic Item-to-General fallback on deletion.
- [ ] 1.5 Add focused Application and Integration tests for isolation, CRUD, invalid drafts, delete confirmation inputs, affected Items, shared/missing references, and failure cleanup. (Service and repository behavior is covered; dedicated Integration tests for missing-file and workspace isolation cases remain.)

## 2. Workspace package compatibility

- [x] 2.1 Extend workspace snapshot filtering and package serialization to preserve style identities, descriptions, ordered references, Item selections, and available managed files.
- [x] 2.2 Validate style ownership, duplicate titles/identities, reference ordering, and safe paths during package preflight and atomic import.
- [ ] 2.3 Add package tests for export/import round trips, older-schema migrations, missing-reference reporting, and rejection without partial import. (Style record/reference/file round trip is covered; style-specific missing-file and malformed-package cases remain.)

## 3. Design style selection and generation

- [x] 3.1 Extend Design state and Item preference persistence with the selected custom style ID and General fallback.
- [x] 3.2 Update default prompt compilation so custom style title/description replaces Niche visual-style guidance while other applicable Niche context remains; preserve exact prompt overrides and make Reset compile the current style.
- [x] 3.3 Resolve and validate the complete ordered reference set before generation, gate on endpoint maximum capacity, submit all references through the existing request contract, and recheck stale context before saving.
- [ ] 3.4 Add prompt compiler, preference, and generation tests for General, custom style, Niche-field replacement, explicit overrides, reference order/content, endpoint capacity, missing/unsupported files, cancellation, and stale results. (Prompt/generation coverage passes; a few style-specific preference/unsupported-file cases remain.)

## 4. Workspace and Design user surfaces

- [x] 4.1 Add workspace-scoped style management using a focused list/editor surface with create, update, reference-image handling, deletion impact confirmation, validation, save-failure, and unsaved-draft behavior.
- [x] 4.2 Add Manage artwork styles in Workspace settings and ensure management is scoped to the active workspace.
- [x] 4.3 Add the Design style selector and Add new shortcut; preserve the originating Item, select a newly created style on return, and expose reference-capacity or missing-image guidance.
- [ ] 4.4 Add view-model and Avalonia headless tests for settings access, selection persistence/re-entry, Add new round trip, focus and Item preservation, draft discard, delete confirmation/cancel, and blocked generation states. (Settings entry is covered; style manager and selector journeys need dedicated headless coverage.)

## 5. Acceptance verification and completion gates

- [ ] 5.1 Map every scenario in the style-management, Design-style-selection, workspace-management, workspace-file-storage, and workspace-transfer delta specs to focused test evidence in the module verification record. (The matrix records evidence and explicitly identifies scenarios without dedicated tests.)
- [x] 5.2 Review migration, transfer, managed-file cleanup, prompt-injection boundaries, accessibility, and changed-scope behavior against the proposal and design; correct artifacts if evidence reveals a mismatch.
- [x] 5.3 Run focused tests and `dotnet test .\\FusionCanvas.sln`.
- [x] 5.4 Run strict OpenSpec validation and resolve all errors and relevant warnings before marking the module complete.
