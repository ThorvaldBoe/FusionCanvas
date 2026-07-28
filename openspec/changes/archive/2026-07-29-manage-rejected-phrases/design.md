## Context

The active `add-ideation-tool` change introduced a durable `IdeationRejection` record (`src/FusionCanvas.Domain/Ideation/IdeationRejection.cs`) capturing a rejected idea's text, optional reason, store/niche/optional-group scope, `IdeationMode`, and `CreatedAt`. Rejections are persisted in the `ideation_rejections` SQLite table (schema version 7), added to `WorkspaceSnapshot.IdeationRejections`, and read back by `IdeationService.AssembleContext` as negative-guidance context for later generation. Creators currently have no way to review, correct, curate, or seed this guidance outside the reject-from-Ideation flow.

The Snowclone Library (`src/FusionCanvas.App/Snowclones/SnowcloneLibraryViewModel.cs` + `SnowcloneLibraryWindow.axaml`) is the established FusionCanvas pattern for a focused management dialog opened from another owning surface: in-memory drafts, explicit Save, unsaved-change protection on selection/search/close, confirmed destructive deletion, live search, single-instance modal ownership, and headless view tests. This module mirrors that pattern for rejected phrases.

UX preflight (per `docs/ui-guidelines.md` and `docs/ux-guidelines.md`):

- **Workflow and frequency:** reviewing and curating rejected phrases is occasional maintenance tied to ideation, not persistent main-workspace work. The dialog belongs as a focused auxiliary surface opened from the Ideation dialog, co-located with where rejections are created and where negative guidance is consumed.
- **Workspace footprint:** zero persistent main-window footprint. One `Manage rejected phrases…` action in the Ideation dialog; the dialog is owned by the Ideation dialog as a single-instance nested modal.
- **Progressive disclosure:** workspace-wide list by default filtered to the active Ideation scope; a scope filter narrows to store/niche/optional-group; live text search narrows further.
- **States:** empty (no rejections), no-results (search/scope yields nothing), busy (operation in progress, mutations disabled), error (recoverable, last confirmed state plus draft preserved), unsaved-prompt (Save/Discard/Cancel on selection/scope/search/close), delete-confirmation.
- **Selection and focus:** first visible row selected on open; New focuses the phrase field; Save/Delete/Cancel return focus to the next meaningful editor or list control; keyboard order: search, scope filter, list, phrase, reason, New, Save, Delete, Close.
- **Drafts and unsaved changes:** editor input is an in-memory draft until Save; selection, scope, search, and close protect meaningful unsaved edits with Save/Discard/Cancel; Cancel preserves draft, selection, and focus.
- **Destructive actions:** permanent deletion is explicit, confirmed, and selects a sensible remaining visible row.
- **Theming:** shared semantic theme resources apply; the nested modal adopts the active appearance.

## Goals / Non-Goals

**Goals:**

- Let a creator view, filter, edit, create, and delete durable ideation rejections from one focused dialog opened from the Ideation dialog.
- Add an optional `UpdatedAt` audit timestamp to rejections so edited records are distinguishable from never-edited captures, without changing captured-rejection shape or Ideation context assembly.
- Enforce within-scope normalized phrase uniqueness so curation cannot produce duplicate guidance.
- Keep the change small: reuse the existing `IdeationRejection` model, `WorkspaceSnapshot` save path, and Snowclone-Library-style dialog patterns.

**Non-Goals:**

- Changing the Ideation candidate generation flow or context assembly.
- Adding CSV import/export, archive/restore, cloud sync, or whole-application backup for rejections.
- Changing workspace-transfer semantics (rejections remain workspace-scoped; manual/edited records are ordinary `IdeationRejection` rows and transfer unchanged).
- Allowing scope or mode of an existing record to be changed through the manager (only phrase and reason are mutable).
- Adding a main-window or settings launcher.

## Decisions

### D1. Reuse the existing `IdeationRejection` model; add an optional `UpdatedAt`
Extend `IdeationRejection` with an optional `DateTimeOffset? UpdatedAt` constructor parameter (default null) and a corresponding init-only property. Validation: `UpdatedAt`, when non-null, MUST be greater than or equal to `CreatedAt`. The record is otherwise unchanged.
*Why:* Reusing the model keeps manual and captured rejections indistinguishable to Ideation context assembly and workspace transfer, and avoids a parallel persistence path. An optional `UpdatedAt` is the smallest audit addition that marks edited records.
*Alternatives considered:* (a) a separate `RejectedPhrase` global model like Snowclones — rejected because the user wants whole-workspace filtering by scope, which requires the existing scope columns; (b) updating `CreatedAt` on edit — rejected because it destroys capture-time audit and breaks Ideation context ordering by `created_at`.

### D2. Within-scope normalized uniqueness
Define a pure domain helper `RejectionPhraseComparison.NormalizeKey(string)` that trims outer whitespace, collapses internal whitespace runs, and case-insensitively compares, mirroring the Snowclone library's duplicate policy. Uniqueness is enforced within the same `(storeId, nicheId, groupId?)` scope only; the same phrase is allowed in a different scope.
*Why:* The user's intent is guidance per creative scope (a niche's brand direction); a phrase that is bad guidance in one niche may be fine elsewhere. Matching Snowclone normalization keeps the rule consistent and testable in pure domain tests.
*Alternatives considered:* (a) workspace-wide uniqueness — rejected because it would prevent legitimately scoped reuse; (b) no uniqueness — rejected because curation would accumulate duplicate guidance that pollutes negative-guidance context.

### D3. Manual creation defaults to the active scope filter; reuses `Basic` mode
A new manual record takes its store, niche, and optional group from the active scope filter when that filter identifies exactly one store, one niche, and an optional group; creation at whole-workspace view is refused with a clear message. Mode is `IdeationMode.Basic` (no new enum value), so manual records are ordinary rejections from the model's perspective.
*Why:* The user chose to avoid extending `IdeationMode`; reusing `Basic` keeps the change minimal and the records immediately usable by context assembly. Defaulting to the active scope filter matches the creator's mental model when curating guidance for a specific niche/group.
*Alternatives considered:* (a) a separate `Manual` mode enum value — rejected per user decision to keep the mode set unchanged; (b) forcing a scope picker — rejected as unnecessary friction for the common case where the creator is already viewing the target scope.

### D4. Editing mutates only phrase and reason; `UpdatedAt` advances on save
The update path produces a new `IdeationRejection` with the same `Id`, `StoreId`, `NicheId`, `GroupId`, `Mode`, and `CreatedAt`, the edited `Text`/`Reason`, and `UpdatedAt` set to the save clock value (overwriting any prior `UpdatedAt`). Scope/mode are not editable through the manager.
*Why:* The user explicitly limited editing to phrase and reason. Preserving identity/scope/mode/`CreatedAt` matches the Snowclone editing contract and keeps transfer and context-assembly stable.
*Alternatives considered:* allowing scope changes — rejected per user decision and because it complicates within-scope uniqueness, transfer-package membership, and context assembly.

### D5. Application service over the existing workspace snapshot
Add `IRejectedPhraseManagementService` (Application port) and a default `RejectedPhraseManagementService` operating over `IWorkspaceRepository.LoadAsync`/`SaveAsync`. Each `ListAsync`/`CreateAsync`/`UpdateAsync`/`DeleteAsync` reloads the current snapshot, validates, applies, and saves atomically through the existing transactional save path, returning a `RejectedPhraseManagementResult` carrying the refreshed `RejectedPhraseManagementState` (visible list, all-rejections index, affected record, added/skipped/error/summary fields). The service exposes a `RejectedPhraseScope` filter record (`StoreId?`, `NicheId?`, `GroupId?`, plus a `WholeWorkspace` flag) and a `RejectedPhraseSummary` view record.
*Why:* Mirrors `SnowcloneLibraryService` and `IdeationService` shape, keeps UI free of repository access, and ensures every mutation goes through the atomic workspace save that transfer and other services already consume.
*Alternatives considered:* a dedicated rejection repository — rejected as speculative indirection; the existing `IWorkspaceRepository` is the accepted boundary.

### D6. SQLite migration to version 8 adds `ideation_rejections.updated_at`
Bump `SqliteDatabaseSchema.CurrentVersion` to 8 and add `MigrateToVersion8Async`: a single transactional `ALTER TABLE ideation_rejections ADD COLUMN updated_at TEXT NULL;` followed by the version bump. Update `EnsureSchemaCoreAsync` to call the v8 migration when `schemaVersion < 8`. Update the fresh-`CREATE TABLE ideation_rejections` DDL to include `updated_at TEXT NULL`. Update `InsertIdeationRejectionAsync` and `LoadIdeationRejectionsAsync` to read and write `updated_at`. New databases are created at version 8.
*Why:* `ALTER TABLE … ADD COLUMN … NULL` is SQLite-safe, preserves all existing rows, and matches the established migration pattern (`MigrateToVersion4Async` for `tags.color`).
*Alternatives considered:* rebuilding the table with a copy-and-rename migration — rejected as unnecessary for a nullable column add.

### D7. Dialog ownership and single instance
Add `RejectedPhrasesWindow.axaml`/`.axaml.cs` and a `RejectedPhrasesViewModel` mirroring `SnowcloneLibraryViewModel`. The Ideation view model gains `ManageRejectedPhrasesCommand`, `IsRejectedPhrasesOpen`, an `OpenRejectedPhrases()` method, and an injected `IRejectedPhraseManagementService`. Opening while already open focuses the existing window. The nested modal is owned by the Ideation dialog window, exactly like the Snowclone Library opener (`ManageSnowclonesCommand`/`IsSnowcloneLibraryOpen` in `src/FusionCanvas.App/Ideation/IdeationViewModel.cs`).
*Why:* Reuses the established single-instance nested-modal pattern; keeps Ideation candidate state untouched.
*Alternatives considered:* a non-modal detached window — rejected because curation is tied to an Ideation session and a detached window risks stale scope and orphaned state.

### D8. Workspace refresh after mutation
A successful create/edit/delete through `IRejectedPhraseManagementService` returns the refreshed snapshot state; the view model applies it locally and raises `WorkspaceChanged` on the Ideation view model so the navigation tree and other open representations refresh from authoritative workspace state (same event already used by `CreateCandidateAsync`/`RejectAsync`).
*Why:* Ensures the main workspace reflects curated guidance and prevents the manager and other surfaces from diverging.
*Alternatives considered:* a global event bus — rejected as speculative; the existing `WorkspaceChanged` event is sufficient.

## Risks / Trade-offs

- **Stale Ideation context after a curation change:** edited or deleted rejections are not reflected in an already-running Ideation generation batch. → The manager prevents opening during an active Ideation batch (the `Manage rejected phrases…` action follows the same `IsBusy` gating as `ManageSnowclonesCommand`), and context assembly reloads the snapshot on the next generation, so the next batch sees curated guidance. This is acceptable and matches Snowclone's relationship to Ideation.
- **Nested modal complexity over an already-modal Ideation dialog:** focus trapping, theme application, and OS-close routing need care. → Mirror the Snowclone Library opener exactly, which already solves nested-modal ownership, single instance, and theme switching; add headless dialog tests for nested open/close and focus return.
- **Accidental loss of captured rejections through deletion:** destructive. → Permanent deletion is explicit and confirmed; the confirmation warning names the phrase; a sensible remaining row is selected afterward; headless tests cover confirm and cancel.
- **Within-scope uniqueness edge cases:** whitespace/casing collisions across manual and captured records. → Pure domain parameterized tests cover normalization; application tests cover create-collision and edit-collision within scope and allow-across-scope.
- **Migration safety on existing databases:** a failed migration could leave the schema partially advanced. → Single-transaction migration that rolls back on failure; the `updated_at` column is nullable so existing rows backfill to null; migration tests assert pre-existing rejection rows and unrelated tables remain intact.
- **Whole-workspace list performance:** the workspace-wide list loads all rejections. → The list is bounded by the active workspace's rejection set (expected small), filtered client-side in the view model after a single `ListAsync`, mirroring Snowclone's `LoadAsync`+search.
- **Mode reuse ambiguity:** manual records use `Basic` mode, so they are indistinguishable from captured `Basic` rejections in the data. → Accepted per user decision; the `UpdatedAt` audit field and the management surface itself provide the necessary curation signal. A future module may add a `Manual` mode if distinguishing them in context assembly becomes valuable.

## Migration Plan

1. Bump `SqliteDatabaseSchema.CurrentVersion` 7 → 8.
2. Add `MigrateToVersion8Async` (transactional `ALTER TABLE ideation_rejections ADD COLUMN updated_at TEXT NULL;`) and wire it into `EnsureSchemaCoreAsync` for `schemaVersion < 8`.
3. Update the fresh-DB `CREATE TABLE ideation_rejections` DDL to include `updated_at TEXT NULL`.
4. Update `InsertIdeationRejectionAsync` to write `updated_at` and `LoadIdeationRejectionsAsync` to read it.
5. Extend `IdeationRejection` with `UpdatedAt` and update `WorkspaceSnapshot` save/load mappings.
6. Rollback: if the migration is reverted, older application builds refuse to open a version-8 database per the existing "newer than supported" rule; no data loss occurs because `updated_at` is an additive nullable column. Re-applying the migration is idempotent because `ALTER TABLE ADD COLUMN` is only run when `schemaVersion < 8`.

No data backfill is required: existing rejections receive null `updated_at` and remain valid negative-guidance context.

## Implementation Plan

### Affected layers and likely files/types

**Domain (`src/FusionCanvas.Domain/Ideation/`)**

- Extend `IdeationRejection` with an optional `DateTimeOffset? UpdatedAt` constructor parameter and init-only property; add validation that a non-null `UpdatedAt` is `>= CreatedAt`. Keep the record otherwise unchanged.
- Add `RejectionPhraseComparison` (pure helper) with `NormalizeKey(string)` and `SameScope(IdeationRejection, other)` semantics. No persistence/UI dependencies.

**Application (`src/FusionCanvas.Application/Ideation/` and a new `RejectedPhrases/` subfolder)**

- Add `IRejectedPhraseManagementService` port and `RejectedPhraseManagementService` default implementation in `src/FusionCanvas.Application/RejectedPhrases/` (new capability folder, mirroring the per-capability folder convention).
- Records: `RejectedPhraseScope` (filter: `StoreId?`, `NicheId?`, `GroupId?`, `bool WholeWorkspace`), `RejectedPhraseSummary` (view of a rejection for the list/editor), `RejectedPhraseManagementState` (visible list, all-rejections index), `RejectedPhraseCreateRequest` (`Text`, `Reason`, `Scope`), `RejectedPhraseUpdateRequest` (`Id`, `Text`, `Reason`, `Scope`), `RejectedPhraseManagementResult` (`Succeeded`, `State`, `AffectedSummary?`, `Error?`).
- Service methods: `InitializeAsync(scope, search, ct)`, `LoadAsync(scope, search, ct)`, `CreateAsync(request, ct)`, `UpdateAsync(request, ct)`, `DeleteAsync(id, scope, search, ct)`. Each reloads the snapshot, validates scope (store/niche/group exist), enforces within-scope uniqueness on create/update, applies the change, saves atomically, and returns the refreshed state for the same scope+search.
- The service depends on `IWorkspaceRepository`, `IClock`, `IIdGenerator` (reuse the same abstractions `IdeationService` uses). Manual creation uses `IdeationMode.Basic`.

**Integration (`src/FusionCanvas.Integration/Persistence/`)**

- `SqliteDatabaseSchema.CurrentVersion` → 8.
- Add `MigrateToVersion8Async` (transactional `ALTER TABLE ideation_rejections ADD COLUMN updated_at TEXT NULL;`); wire into `EnsureSchemaCoreAsync` under `if (!isFreshDatabase && schemaVersion < 8)`.
- Add `updated_at TEXT NULL` to the fresh `CREATE TABLE ideation_rejections` DDL.
- Update `InsertIdeationRejectionAsync` to write `updated_at` (null when `UpdatedAt` is null).
- Update `LoadIdeationRejectionsAsync` to read `updated_at` and pass it into the reconstructed `IdeationRejection`.
- Update the v5→v7 migration path only if it reconstructs `IdeationRejection` records (it creates the table; verify no `updated_at` literal is required there — the table is created at v7 and the v8 migration adds the column for pre-v8 databases).

**App (`src/FusionCanvas.App/RejectedPhrases/` new folder; `src/FusionCanvas.App/Ideation/`)**

- `RejectedPhrasesViewModel` mirroring `SnowcloneLibraryViewModel`: `OpenAsync`, `WhenIdleAsync`, `RequestClose`, search/scope-filter/list/editor state, `New`/`Save`/`RequestDelete`/`ConfirmDelete`/`CancelDelete`/`SaveAndContinue`/`DiscardAndContinue`/`CancelPending`/`Close`/`ClearSearch`/`ClearScope` commands, draft+unsaved+confirmation+busy+error+empty/no-results state, `IsRejectedPhrasesOpen`-style single-instance guard handled by the owning Ideation view model.
- `RejectedPhrasesWindow.axaml` + `.axaml.cs`: focused dialog, list + side editor + scope filter + search + action row; theme-aware; OS-close routed through `RequestClose`.
- `IdeationViewModel`: add `IRejectedPhraseManagementService?` optional constructor dependency, `ManageRejectedPhrasesCommand`, `IsRejectedPhrasesOpen`, `OpenRejectedPhrases()` (mirrors `OpenSnowcloneLibrary()`), gating on `!IsBusy && !IsRejectedPhrasesOpen && _rejectedPhrases is not null && _scope is not null`. The manager is unavailable when the Ideation scope cannot resolve a store+niche (whole-niche root is still a valid scope filter default; the manager itself can still open at niche-root scope).
- Composition wiring in the App's service/locator registration (where `IdeationViewModel` is constructed) to inject `IRejectedPhraseManagementService`.

**Tests**

- `tests/FusionCanvas.Domain.Tests`: `IdeationRejectionUpdatedAtTests` (null default, non-null set, `UpdatedAt < CreatedAt` rejected), `RejectionPhraseComparisonTests` (normalize key, same-scope and across-scope collisions).
- `tests/FusionCanvas.Application.Tests`: `RejectedPhraseManagementServiceTests` with a deterministic in-memory `IWorkspaceRepository`/`IClock`/`IIdGenerator` covering initialize, load, search, scope filter (whole/niche/group), create at scope, create at whole-workspace refusal, within-scope create/edit collisions, across-scope allow, edit preserves identity/scope/mode/`CreatedAt` and advances `UpdatedAt`, edit-only-reason, delete confirm/cancel, sensible next-selection, atomic-failure recoverable, concurrent-operation serialization, and that manual `Basic` records flow into the same `IdeationRejections` collection.
- `tests/FusionCanvas.Integration.Tests`: `SqliteWorkspaceRepositoryUpdatedAtTests` — never-edited round-trips null, edited round-trips value, pre-v8 database migrates with null `updated_at` and intact unrelated tables, new DB at v8, migration-failure rollback, and that `LoadIdeationRejectionsAsync` reconstructs `UpdatedAt`.
- `tests/FusionCanvas.App.Tests`: `RejectedPhrasesViewModelTests` (framework-free) for list/search/scope-filter/selection/draft/IsDirty/CanSave/CanDelete/unsaved-prompt/delete-confirmation/error/empty/no-results/sensible-next-selection; `RejectedPhrasesWindowTests` (Avalonia headless) for construction, bindings, scope filter, search, single-instance nested open over the Ideation dialog, OS-close routing, keyboard reachability, and theme switching.
- `tests/FusionCanvas.App.Tests`: extend `IdeationViewModelTests` for the new `ManageRejectedPhrasesCommand`/`IsRejectedPhrasesOpen` gating and that opening the manager does not disturb Ideation candidate/progress/selection state.

### Sequencing

1. Domain: `IdeationRejection.UpdatedAt` + `RejectionPhraseComparison` + tests (fail-first).
2. Integration: schema v8 migration, DDL, insert/load mapping + migration/round-trip tests.
3. Application: `IRejectedPhraseManagementService` + default service + result/state records + tests (deterministic in-memory collaborators).
4. App: `RejectedPhrasesViewModel` + view-model tests; `RejectedPhrasesWindow.axaml` + headless dialog tests; `IdeationViewModel` launcher wiring + tests; composition wiring.
5. Baseline: `dotnet test .\FusionCanvas.sln` green; `openspec validate` clean.

### Edge cases

- New draft with blank phrase: Save unavailable; Discard/Cancel of a blank draft does not prompt.
- New draft at whole-workspace scope: Save refuses with a clear single-store-and-niche-required message; draft preserved.
- Edit producing a within-scope collision: refused; selected record and recoverable draft preserved.
- Edit with no effective change: Save is unavailable (not dirty) or no-ops without advancing `UpdatedAt` (treat as not dirty → `CanSave` false).
- Delete of the last visible row: shows empty/no-results state appropriate to the active search and scope.
- Active scope filter excluding the selected row: editor does not silently discard unsaved input.
- Opening the manager while an Ideation batch is running: action disabled.
- Concurrent manager operation: duplicate submission prevented; mutation actions disabled while busy.
- Persistence failure mid-create/edit/delete: no partial rejection row or partial snapshot committed; recoverable error; last confirmed state plus recoverable draft preserved.

### Decisions not to reopen

- The manager is launched only from the Ideation dialog (no main-window/settings launcher).
- Editing is limited to phrase and reason (scope/mode/identity/`CreatedAt` immutable through the manager).
- Manual records reuse `IdeationMode.Basic` (no new enum value).
- Rejections remain workspace-scoped; the manager shows the active workspace only and writes through `IWorkspaceRepository` so workspace transfer is unaffected.
- `UpdatedAt` is optional and null for never-edited records.

## Acceptance-to-verification mapping

| Acceptance scenario (delta spec) | Planned verification |
| --- | --- |
| Manager launched from Ideation; single instance; absent from main workspace; does not disturb Ideation state | Avalonia headless `RejectedPhrasesWindowTests` (nested open, single instance, OS-close, no Ideation-state change) + `IdeationViewModelTests` for command gating |
| Lists workspace rejections; preselects first; empty state; live phrase/reason search; no-results | `RejectedPhrasesViewModelTests` + headless dialog tests |
| Scope filter defaults to active Ideation scope; narrows to niche; returns to whole-workspace; active-excludes-selected coherence | `RejectedPhraseManagementServiceTests` (scope filtering) + view-model/headless tests |
| Selecting loads editor; dirty tracking; Save availability | `RejectedPhrasesViewModelTests` |
| Edit preserves identity/scope/mode/`CreatedAt`, advances `UpdatedAt`; edit-only-reason; cancel restores | `RejectedPhraseManagementServiceTests` + `SqliteWorkspaceRepositoryUpdatedAtTests` (round-trip) |
| Within-scope uniqueness on create and edit; allow across scope | `RejectionPhraseComparisonTests` (domain) + `RejectedPhraseManagementServiceTests` (service) |
| Manual create at active scope; refused at whole-workspace view; blank-draft cancel | `RejectedPhraseManagementServiceTests` + `RejectedPhrasesViewModelTests` |
| Delete confirmed/cancelled; new draft not deletable; sensible next selection | `RejectedPhrasesViewModelTests` + service tests |
| Draft protection on selection/scope/search/close; keyboard reachability; focus return | `RejectedPhrasesViewModelTests` + headless dialog tests |
| Durable, atomic, recoverable; concurrent serialization; workspace refresh | `RejectedPhraseManagementServiceTests` (atomic failure, concurrency) + `SqliteWorkspaceRepositoryUpdatedAtTests`; view-model `WorkspaceChanged` assertion |
| Module scope (no Ideation/transfer/CSV/archive/sync changes) | Contributor review in `verification.md` |
| Never-edited null `UpdatedAt` round-trips | `SqliteWorkspaceRepositoryUpdatedAtTests` |
| Edited `UpdatedAt` round-trips | `SqliteWorkspaceRepositoryUpdatedAtTests` |
| Pre-v8 DB migrates with null `updated_at`, unrelated tables intact | `SqliteWorkspaceRepositoryUpdatedAtTests` migration case |
| New DB at v8; migration failure rollback | `SqliteWorkspaceRepositoryUpdatedAtTests` |

Optional live desktop check (not a gate): a single manual open-from-Ideation smoke to confirm nested-modal window stacking and theme coherence on Windows. Recorded as supplemental evidence only.
