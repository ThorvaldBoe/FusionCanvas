## 1. Domain: UpdatedAt and within-scope uniqueness

- [x] 1.1 Add failing `IdeationRejectionUpdatedAtTests` covering null default `UpdatedAt`, explicit non-null set, and rejection of `UpdatedAt < CreatedAt`.
- [x] 1.2 Add failing `RejectionPhraseComparisonTests` covering normalize key (trim, collapse whitespace, case-insensitive), within-scope collision, and across-scope allow.
- [x] 1.3 Extend `IdeationRejection` with an optional `DateTimeOffset? UpdatedAt` constructor parameter and init-only property; validate that a non-null value is `>= CreatedAt`. Keep the record otherwise unchanged.
- [x] 1.4 Add the pure `RejectionPhraseComparison` helper with `NormalizeKey` and same-scope semantics. No persistence or UI dependencies.

## 2. Integration: SQLite schema version 8

- [x] 2.1 Add failing `SqliteWorkspaceRepositoryUpdatedAtTests` covering: never-edited round-trips null `UpdatedAt`; edited round-trips a non-null value; pre-v8 database migrates with null `updated_at` and intact unrelated tables (stores, niches, groups, items, tags); new DB created at v8; migration failure rolls back.
- [x] 2.2 Bump `SqliteDatabaseSchema.CurrentVersion` from 7 to 8.
- [x] 2.3 Add transactional `MigrateToVersion8Async` performing `ALTER TABLE ideation_rejections ADD COLUMN updated_at TEXT NULL;` and wire it into `EnsureSchemaCoreAsync` under `if (!isFreshDatabase && schemaVersion < 8)`.
- [x] 2.4 Add `updated_at TEXT NULL` to the fresh `CREATE TABLE ideation_rejections` DDL.
- [x] 2.5 Update `InsertIdeationRejectionAsync` to write `updated_at` (null when `UpdatedAt` is null).
- [x] 2.6 Update `LoadIdeationRejectionsAsync` to read `updated_at` and pass it into the reconstructed `IdeationRejection`.
- [x] 2.7 Verify the v5→v7 migration path needs no `updated_at` literal (table created at v7; column added only for pre-v8 databases) and that `WorkspaceSnapshot` save/load round-trips `UpdatedAt` end-to-end.

## 3. Application: Rejected phrase management service

- [x] 3.1 Add failing `RejectedPhraseManagementServiceTests` with a deterministic in-memory `IWorkspaceRepository`/`IClock`/`IIdGenerator`, covering: initialize, load, search, scope filter (whole/niche/group), create at active scope, refuse create at whole-workspace view, within-scope create collision, within-scope edit collision, across-scope allow, edit preserves identity/scope/mode/`CreatedAt` and advances `UpdatedAt`, edit-only-reason, delete success, delete of last row, atomic-failure recoverable, concurrent-operation serialization, and that manual `Basic` records flow into the same `IdeationRejections` collection.
- [x] 3.2 Add the `RejectedPhraseScope`, `RejectedPhraseSummary`, `RejectedPhraseManagementState`, `RejectedPhraseCreateRequest`, `RejectedPhraseUpdateRequest`, and `RejectedPhraseManagementResult` records in `src/FusionCanvas.Application/RejectedPhrases/`.
- [x] 3.3 Add the `IRejectedPhraseManagementService` port in `src/FusionCanvas.Application/RejectedPhrases/` with `InitializeAsync`, `LoadAsync`, `CreateAsync`, `UpdateAsync`, and `DeleteAsync`.
- [x] 3.4 Add `RejectedPhraseManagementService` over `IWorkspaceRepository`/`IClock`/`IIdGenerator`: reload snapshot, validate scope (store/niche/group exist), enforce within-scope normalized uniqueness on create/update, apply, save atomically, and return the refreshed state for the same scope+search. Manual creation uses `IdeationMode.Basic`.

## 4. App: View model and dialog

- [x] 4.1 Add failing `RejectedPhrasesViewModelTests` (framework-free) covering list/search/scope-filter/selection, dirty tracking and `CanSave`/`CanDelete`, new-draft focus intent, blank-draft cancel without prompt, create-at-whole-workspace refusal, unsaved-prompt Save/Discard/Cancel, delete confirmation confirm/cancel, sensible next selection after delete, empty/no-results states, busy serialization, and recoverable error preservation.
- [x] 4.2 Add `RejectedPhrasesViewModel` in `src/FusionCanvas.App/RejectedPhrases/` mirroring `SnowcloneLibraryViewModel` structure: `OpenAsync`, `WhenIdleAsync`, `RequestClose`, search/scope-filter/list/editor state, and New/Save/RequestDelete/ConfirmDelete/CancelDelete/SaveAndContinue/DiscardAndContinue/CancelPending/Close/ClearSearch/ClearScope commands.
- [x] 4.3 Add `RejectedPhrasesWindow.axaml` + `.axaml.cs` in `src/FusionCanvas.App/RejectedPhrases/`: focused dialog with scope filter, search, list, side editor, and action row; theme-aware; OS-close routed through `RequestClose`.
- [x] 4.4 Add failing Avalonia headless `RejectedPhrasesWindowTests` covering construction, bindings, scope filter, search, single-instance nested open over the Ideation dialog, OS-close routing, keyboard reachability, and theme switching.

## 5. App: Ideation launcher wiring

- [x] 5.1 Add failing `IdeationViewModelTests` coverage for the new `ManageRejectedPhrasesCommand`/`IsRejectedPhrasesOpen` gating (disabled while busy or already open) and that opening the manager does not disturb Ideation candidates, progress, selection, mode, guidance, count, or rejection draft.
- [x] 5.2 Extend `IdeationViewModel` with an optional `IRejectedPhraseManagementService` dependency, `ManageRejectedPhrasesCommand`, `IsRejectedPhrasesOpen`, and `OpenRejectedPhrases()` mirroring the existing `OpenSnowcloneLibrary()`/`ManageSnowclonesCommand` pattern.
- [x] 5.3 Wire `RejectedPhrasesWindow` ownership and single-instance focus behavior nested over the Ideation dialog window, exactly like the Snowclone Library opener.
- [x] 5.4 Raise `WorkspaceChanged` on the Ideation view model after a successful create/edit/delete so the navigation tree and other open representations refresh from authoritative workspace state.
- [x] 5.5 Register `IRejectedPhraseManagementService` and inject it into `IdeationViewModel` in the App's composition/locator wiring.

## 6. Verification and baseline

- [x] 6.1 Run `dotnet test .\FusionCanvas.sln` and ensure the full solution baseline is green.
- [x] 6.2 Run `openspec validate` and resolve any spec or change-artifact issues until validation is clean.
- [x] 6.3 Complete `verification.md` mapping every acceptance scenario in `specs/rejected-phrase-management/spec.md` and `specs/local-sqlite-persistence/spec.md` to final evidence (test names, commands, and any optional supplemental desktop notes).
- [x] 6.4 Confirm the module introduced no main-window/settings launcher, no Ideation generation/context-assembly changes, no CSV/archive/sync behavior, and no workspace-transfer semantic changes (contributor scope-review note in `verification.md`).
