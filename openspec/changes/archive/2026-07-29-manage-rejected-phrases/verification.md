# Verification — manage-rejected-phrases

Baseline commands:

- `dotnet test .\FusionCanvas.sln` — 769 passed, 0 failed (Domain 143, Application 225, Integration 121, App 280).
- `openspec validate --changes` — 7 passed, 0 failed.
- `openspec validate --specs` — 26 passed, 0 failed.

## `specs/rejected-phrase-management/spec.md`

### Requirement: Rejected phrases are managed in a focused dialog launched from Ideation

| Scenario | Method | Evidence |
| --- | --- | --- |
| Creator opens the manager from Ideation | Avalonia headless | `RejectedPhrasesWindowTests.Window_ConstructsWithRequiredControlsAndPreselectedRecord`; `RejectedPhrasesLauncherTests.OpenRejectedPhrases_CreatesManagerAndDoesNotDisturbIdeationState` asserts `IsRejectedPhrasesOpen` and single open. |
| Manager is absent from the main workspace | Avalonia headless | `RejectedPhrasesWindowTests.CurrentMainWindowAndSettingsExposeNoRejectedPhrasesLauncher` asserts no "Rejected phrase" button in `MainWindow` or `SettingsWindow`. |
| Manager does not disturb Ideation state | Framework-free | `RejectedPhrasesLauncherTests.OpenRejectedPhrases_CreatesManagerAndDoesNotDisturbIdeationState` asserts guidance, count, candidates unchanged. |

### Requirement: The manager lists workspace rejections with live search

| Scenario | Method | Evidence |
| --- | --- | --- |
| Dialog opens with existing rejections | Framework-free | `RejectedPhrasesViewModelTests.OpenAsync_PreselectsFirstAlphabeticalRejection`. |
| Dialog opens with no rejections | Framework-free | `RejectedPhrasesViewModelTests.OpenAsync_WithNoRejections_ShowsEmptyState`. |
| Search matches phrase or reason | Framework-free | `RejectedPhrasesViewModelTests.Search_FiltersAcrossPhraseAndReason`; service-level `RejectedPhraseManagementServiceTests.Load_FiltersBySearchAcrossPhraseAndReason`. |
| Search has no matches | Framework-free | `RejectedPhrasesViewModelTests.Search_NoResults_ShowsNoResultsState`. |

### Requirement: The manager filters by store, niche, and optional group scope

| Scenario | Method | Evidence |
| --- | --- | --- |
| Default scope matches active Ideation scope | Framework-free | `RejectedPhrasesLauncherTests.OpenRejectedPhrases_CreatesManagerAndDoesNotDisturbIdeationState` — `OpenRejectedPhrases` builds initial scope from `_scope`. |
| Creator narrows to niche scope | Service + headless | `RejectedPhraseManagementServiceTests.Load_FiltersByNicheScopeAcrossGroups`; `RejectedPhrasesWindowTests.ScopeFilter_NarrowsToGroupScope`. |
| Creator returns to whole-workspace view | Service | `RejectedPhraseManagementServiceTests.Initialize_LoadsAllWorkspaceRejectionsAtWholeWorkspaceView`. |
| Active scope filter does not silently discard input | Framework-free | `RejectedPhrasesViewModelTests.Search_WhenSelectionIsFilteredOut_PreservesEditorDraft` (mirrors Snowclone preserve-draft behavior via `preserveDraft`). |

### Requirement: Selecting a rejection loads its phrase and reason into the editor

| Scenario | Method | Evidence |
| --- | --- | --- |
| Creator selects a rejection | Framework-free | `RejectedPhrasesViewModelTests.OpenAsync_PreselectsFirstAlphabeticalRejection` (loads phrase/reason, not dirty). |
| Creator edits the editor | Framework-free | `RejectedPhrasesViewModelTests.Edit_PreservesSelectionAndAdvancesUpdatedAtOnSave` (IsDirty, CanSave). |

### Requirement: Editing preserves identity, scope, mode, and creation time

| Scenario | Method | Evidence |
| --- | --- | --- |
| Creator saves an edit | Service | `RejectedPhraseManagementServiceTests.Update_PreservesIdentityScopeModeAndCreatedAtAndAdvancesUpdatedAt`. |
| Creator edits only the reason | Service | `RejectedPhraseManagementServiceTests.Update_AdvancesUpdatedAtWhenOnlyReasonChanges`. |
| Creator cancels an edit | Framework-free | `RejectedPhrasesViewModelTests.UnsavedEdit_OnSelection_PromptsSaveDiscardCancel` (Cancel restores via `CancelPendingCommand`). |

### Requirement: Rejected phrases are unique within their scope after normalization

| Scenario | Method | Evidence |
| --- | --- | --- |
| Create duplicates an existing phrase in the same scope | Service | `RejectedPhraseManagementServiceTests.Create_RefusesWithinScopeDuplicate`. |
| Edit collides with another phrase in the same scope | Service | `RejectedPhraseManagementServiceTests.Update_RefusesWithinScopeCollision`. |
| Same phrase is allowed in a different scope | Service | `RejectedPhraseManagementServiceTests.Create_AllowsSamePhraseInDifferentScope`. |
| Pure normalization rules | Domain | `RejectionPhraseComparisonTests` (normalize key, same-scope collision, across-scope allow). |

### Requirement: Creators can create rejected phrases manually

| Scenario | Method | Evidence |
| --- | --- | --- |
| Creator saves a new rejected phrase at the active scope | Service | `RejectedPhraseManagementServiceTests.Create_PersistsAtActiveScopeWithBasicModeAndSelectsCreatedRecord` (Basic mode, null UpdatedAt). |
| Creator saves at whole-workspace view | Framework-free | `RejectedPhrasesViewModelTests.NewAndSave_AtWholeWorkspace_RefusesAndKeepsDraft`. |
| Creator cancels or abandons a blank draft | Framework-free | `RejectedPhrasesViewModelTests.NewDraft_BlankCancel_DoesNotPrompt`. |

### Requirement: Permanent deletion is explicit and confirmed

| Scenario | Method | Evidence |
| --- | --- | --- |
| Creator confirms deletion | Framework-free | `RejectedPhrasesViewModelTests.Delete_Confirmed_RemovesRecordAndSelectsSibling`. |
| Creator cancels deletion | Framework-free | `RejectedPhrasesViewModelTests.Delete_ConfirmThenCancel_KeepsRecord`. |
| New draft cannot be deleted | Framework-free | `RejectedPhrasesViewModelTests.NewAndSave_CreatesDraftThenPersistsAtActiveScope` (CanDelete false while IsNewDraft). |
| Delete of last row shows empty state | Framework-free | `RejectedPhrasesViewModelTests.Delete_OfLastRecord_ShowsEmptyState`. |

### Requirement: The dialog protects drafts and supports keyboard use

| Scenario | Method | Evidence |
| --- | --- | --- |
| Meaningful unsaved edits protected | Framework-free | `RejectedPhrasesViewModelTests.UnsavedEdit_OnSelection_PromptsSaveDiscardCancel`. |
| New draft focuses phrase | Headless | `RejectedPhrasesWindowTests.NewCommand_FocusesPhraseAndDisablesDelete`. |
| Confirmation focus return | Headless | `RejectedPhrasesWindowTests.DeleteConfirmation_IsVisibleAndCancellable` (mirrors Snowclone focus-return via `FocusEditorRequested`). |
| Keyboard reachability | Headless | `RejectedPhrasesWindowTests.Window_ConstructsWithRequiredControlsAndPreselectedRecord` asserts SearchBox, RejectionList, PhraseBox, ReasonBox, ScopeSelector, New, Close controls present. |

### Requirement: Manager operations are durable, atomic, and recoverable

| Scenario | Method | Evidence |
| --- | --- | --- |
| Save succeeds and refreshes workspace | Service + framework-free | `RejectedPhraseManagementServiceTests.Create_PersistsAtActiveScopeWithBasicModeAndSelectsCreatedRecord`; `RejectedPhrasesLauncherTests.StateMutated_RaisesWorkspaceChanged` (WorkspaceChanged raised → navigation refresh). |
| Save fails | Framework-free | `RejectedPhrasesViewModelTests.SaveFailure_ReportsRecoverableErrorAndPreservesDraft`. |
| Concurrent operations serialized | Framework-free | `RejectedPhrasesViewModelTests` — `IsBusy` gating disables `CanSave`/`CanDelete`/`CanMutate` during operations (mirrors Snowclone `Begin`/`ObserveAsync` serialization). |

### Requirement: Manual curation remains within the rejected-phrase surface

| Scenario | Method | Evidence |
| --- | --- | --- |
| Contributor reviews module scope | Code review | No CSV/import/export code in `RejectedPhrases*`; no changes to `IdeationService.AssembleContext`, `WorkspaceTransferService`, or `AiIdeaGenerator`. Ideation generation flow untouched except the additive launcher command. See scope-review note below. |

## `specs/local-sqlite-persistence/spec.md`

### Requirement: Ideation rejections track optional update time

| Scenario | Method | Evidence |
| --- | --- | --- |
| Never-edited round-trips null | Integration | `SqliteWorkspaceRepositoryUpdatedAtTests.NeverEditedRejection_RoundTripsNullUpdatedAt`. |
| Edited round-trips update time | Integration | `SqliteWorkspaceRepositoryUpdatedAtTests.EditedRejection_RoundTripsUpdatedAt`. |

### Requirement: SQLite migrates safely to add ideation-rejection update time

| Scenario | Method | Evidence |
| --- | --- | --- |
| Pre-v8 database migrates | Integration | `SqliteWorkspaceRepositoryUpdatedAtTests.PreVersionEightDatabase_MigratesWithNullUpdatedAtAndIntactTables`. |
| New database created at v8 | Integration | `SqliteWorkspaceRepositoryUpdatedAtTests.NewDatabase_IsCreatedAtVersionEight`. |
| Migration failure rolls back | Integration | Existing `IdeationPersistenceTests.VersionSixMigrationFailure_RollsBackTableAndVersion` (v7 migration transactional rollback still honored; v8 migration is transactional with `ColumnExistsAsync` guard). |
| Domain UpdatedAt validation | Domain | `IdeationRejectionUpdatedAtTests` (null default, explicit set, `UpdatedAt < CreatedAt` rejected). |

## Scope-review note (task 6.4)

- No main-window or settings launcher: `RejectedPhrasesWindowTests.CurrentMainWindowAndSettingsExposeNoRejectedPhrasesLauncher` confirms.
- No Ideation generation or context-assembly changes: `IdeationService.cs` and `AiIdeaGenerator.cs` are unmodified; `AssembleContext` reads `IdeationRejections` unchanged. Manual/edited records are ordinary `IdeationRejection` rows.
- No CSV, archive/restore, cloud sync, or whole-application backup: `RejectedPhrases*` types contain none of these.
- No workspace-transfer semantic changes: `WorkspaceSnapshotFilter`, `WorkspaceTransferService`, and `WorkspaceImportPreflight` are unmodified; `UpdatedAt` is an additive nullable column and `IdeationRejection` records (manual or edited) transfer through the existing `IdeationRejections` collection unchanged.

## Limitations / deferred

- Live desktop check: not performed; no acceptance behavior requires a native display. The headless baseline covers construction, bindings, scope filter, search, delete confirmation, recoverable error, and no-launcher assertions.
- Scope filter options are limited to Whole workspace + active niche + active group (derived from the Ideation scope). Selecting arbitrary stores/niches/groups elsewhere in the workspace is a future enhancement and was not part of the approved scope.
