# Snowclone Library Verification

## Status

Complete. All 35 acceptance scenarios have passing deterministic evidence, the full solution baseline passes, and strict change and repository OpenSpec validation pass.

## Acceptance evidence

All test names below are exact `Class.Method` names. Parameterized tests passed for every listed case.

| # | Acceptance scenario | Passing automated evidence |
|---:|---|---|
| 1 | Snowclone survives application data reload | `SqliteSnowcloneRepositoryTests.SaveAndLoadAsync_RoundTripsSnowclonesAndStarterMarker` |
| 2 | Workspace lifecycle does not affect snowclones | `SqliteSnowcloneRepositoryTests.WorkspaceSave_PreservesSnowcloneLibrary`; `SqliteSnowcloneRepositoryTests.SnowcloneSave_PreservesWorkspaceContent` |
| 3 | Snowclone operation fails during persistence | `SnowcloneLibraryServiceTests.SaveFailure_ReturnsPreviousConfirmedState`; `SnowcloneLibraryViewModelTests.SaveFailure_PreservesDraftAndConfirmedLibrary` |
| 4 | Phrase contains one valid placeholder | `SnowcloneTemplatePolicyTests.Validate_AcceptsSupportedPlaceholderForms` |
| 5 | Phrase contains named, repeated, or multiple placeholders | `SnowcloneTemplatePolicyTests.Validate_AcceptsSupportedPlaceholderForms` |
| 6 | Phrase has invalid placeholder structure | `SnowcloneTemplatePolicyTests.Validate_RejectsInvalidPhraseStructure` |
| 7 | Guidance is missing | `SnowcloneTemplatePolicyTests.Validate_RejectsBlankGuidance` |
| 8 | Create duplicates an existing phrase | `SnowcloneLibraryServiceTests.CreateAsync_RejectsNormalizedDuplicateWithoutSaving`; `SnowcloneTemplatePolicyTests.CreateDuplicateKey_CollapsesWhitespaceAndFoldsCase` |
| 9 | Edit collides with another phrase | `SnowcloneLibraryServiceTests.UpdateAsync_CollisionLeavesConfirmedRecord` |
| 10 | Creator saves a new snowclone | `SnowcloneLibraryServiceTests.CreateAsync_UsesDeterministicIdentityAndTimestamp`; `SnowcloneLibraryViewModelTests.NewAndSave_CreatesDraftThenSelectsPersistedRecord` |
| 11 | Creator updates an existing snowclone | `SnowcloneLibraryServiceTests.UpdateAsync_PreservesIdentityAndCreatedAtAndAdvancesUpdatedAt`; `SnowcloneLibraryViewModelTests.SaveFailure_PreservesDraftAndConfirmedLibrary` |
| 12 | Creator cancels or abandons a blank draft | `SnowcloneLibraryViewModelTests.BlankNewDraft_SelectionChangeDiscardsWithoutPrompt` |
| 13 | Creator confirms snowclone deletion | `SnowcloneLibraryServiceTests.DeleteAsync_RemovesOnlyRequestedSnowclone`; `SnowcloneLibraryViewModelTests.Delete_CancelKeepsRecordAndConfirmSelectsRemainingRecord` |
| 14 | Creator cancels snowclone deletion | `SnowcloneLibraryViewModelTests.Delete_CancelKeepsRecordAndConfirmSelectsRemainingRecord` |
| 15 | Search matches phrase | `SnowcloneLibraryServiceTests.LoadAsync_SortsAndSearchesPhraseAndGuidance` |
| 16 | Search matches guidance | `SnowcloneLibraryServiceTests.LoadAsync_SortsAndSearchesPhraseAndGuidance` |
| 17 | Search has no matches | `SnowcloneLibraryWindowTests.SearchAndUnsavedConfirmation_RenderCompleteInteractionStates` |
| 18 | Snowclone library initializes for the first time | `SnowcloneLibraryServiceTests.InitializeAsync_ImportsOnceAndPersistsMarker`; `SnowcloneCsvCodecTests.EmbeddedStarterResource_UsesTheNormalCsvContract` |
| 19 | Creator deletes the initial starter record | `SnowcloneLibraryServiceTests.InitializeAsync_AfterStarterDeletionDoesNotResurrectIt` |
| 20 | Creator imports the bundled library explicitly | `SnowcloneLibraryServiceTests.ImportBundledAsync_AddsUniqueAndPreservesExistingGuidance`; `SnowcloneLibraryViewModelTests.BundledImportWithDraft_SaveAndContinuePersistsBothChanges` |
| 21 | Bundled starter data is invalid | `SnowcloneLibraryServiceTests.InitializeAsync_InvalidBundleDoesNotSaveOrSetMarker` |
| 22 | Creator exports the library | `SnowcloneLibraryServiceTests.ExportAsync_WritesAlphabeticalRowsWithoutMutation`; `SnowcloneCsvCodecTests.WriteAsync_UsesExactHeaderCrLfAndRoundTrips` |
| 23 | Creator imports a valid CSV | `SnowcloneCsvCodecTests.ReadAsync_ParsesQuotedCommaQuoteAndMultilineGuidance`; `SnowcloneLibraryServiceTests.ImportAsync_DuplicatesWithinDocumentAreSkippedAtomically`; `SnowcloneLibraryViewModelTests.Import_ReportsCountsAndRefreshesList` |
| 24 | CSV header or row is invalid | `SnowcloneCsvCodecTests.ReadAsync_RejectsNonExactHeader`; `SnowcloneCsvCodecTests.ReadAsync_RejectsMalformedQuotedRow`; `SnowcloneLibraryServiceTests.ImportAsync_InvalidSemanticRowRejectsEntireDocument` |
| 25 | CSV contains only duplicates | `SnowcloneLibraryServiceTests.ImportAsync_AllRowsDuplicateExistingLibraryDoesNotSave` |
| 26 | Creator cancels a CSV picker | `SnowcloneLibraryViewModelTests.CancelledPicker_PreservesSelectionSearchAndDraft` |
| 27 | Future owner opens the Snowclone Library dialog | `SnowcloneLibraryWindowTests.Window_ConstructsWithRequiredControlsAndPreselectedRecord`; `SnowcloneLibraryViewModelTests.OpenAsync_PreselectsFirstAlphabeticalSnowclone` |
| 28 | Dialog has an active search | `SnowcloneLibraryViewModelTests.Search_WhenSelectionIsFilteredOut_PreservesEditorDraft`; `SnowcloneLibraryWindowTests.SearchAndUnsavedConfirmation_RenderCompleteInteractionStates` |
| 29 | Library operation is running | `SnowcloneLibraryViewModelTests.ImportBusy_DisablesConflictingActionsUntilCodecCompletes` |
| 30 | Dialog operation fails | `SnowcloneLibraryViewModelTests.SaveFailure_PreservesDraftAndConfirmedLibrary`; `SnowcloneLibraryWindowTests.DeleteConfirmationAndRecoverableError_AreVisible` |
| 31 | Contributor reviews current entry points | `SnowcloneLibraryWindowTests.CurrentMainWindowAndSettingsExposeNoSnowcloneLauncher`; `SnowcloneLibraryWindowTests.Window_ConstructsWithRequiredControlsAndPreselectedRecord` |
| 32 | Creator leaves meaningful unsaved edits | `SnowcloneLibraryViewModelTests.MeaningfulDraft_SelectionTransitionSupportsCancelAndDiscard`; `SnowcloneLibraryViewModelTests.ImportWithDraft_CancelPreservesDraftAndDiscardContinues`; `SnowcloneLibraryViewModelTests.BundledImportWithDraft_SaveAndContinuePersistsBothChanges`; `SnowcloneLibraryViewModelTests.CloseWithDraft_SaveAndContinuePersistsThenRequestsClose` |
| 33 | Creator starts a new draft | `SnowcloneLibraryViewModelTests.NewAndSave_CreatesDraftThenSelectsPersistedRecord`; `SnowcloneLibraryWindowTests.NewCommand_FocusesPhraseAndDisablesDelete` |
| 34 | Creator completes or cancels a confirmation | `SnowcloneLibraryWindowTests.SearchAndUnsavedConfirmation_RenderCompleteInteractionStates`; `SnowcloneLibraryWindowTests.DeleteConfirmationAndRecoverableError_AreVisible` |
| 35 | Contributor reviews module scope | `SnowcloneTemplatePolicyTests.Snowclone_HasOnlyApprovedDataShape`; `SnowcloneLibraryWindowTests.CurrentMainWindowAndSettingsExposeNoSnowcloneLauncher` |

## Workspace-transfer coordination

The `workspace-transfer` change has planning artifacts but no implementation under `src/` or `tests/` in this worktree. Snowclones remain outside `WorkspaceSnapshot`; `SqliteWorkspaceRepository.SaveAsync` never reads, deletes, or inserts snowclone rows. `SqliteSnowcloneRepositoryTests.WorkspaceSave_PreservesSnowcloneLibrary` and `SnowcloneSave_PreservesWorkspaceContent` prove that the persistence scopes coexist. The future workspace-transfer implementation must retain its approved snapshot-only filtering and entity counts.

## Commands and results

- `dotnet test .\FusionCanvas.sln --no-restore --filter "FullyQualifiedName~Snowclone" -v minimal` — passed: Domain 21, Application 16, Integration 21, App/headless 18.
- `dotnet test .\tests\FusionCanvas.Integration.Tests\FusionCanvas.Integration.Tests.csproj --no-restore --filter "FullyQualifiedName~Snowclone|FullyQualifiedName~SqliteWorkspaceRepositoryTests" -v minimal` — passed, 40 tests.
- `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-restore -v minimal` — passed, 202 tests before the two additional focused draft-transition tests; the final solution baseline includes 204.
- `dotnet test .\FusionCanvas.sln --no-restore -v minimal` — passed, 558 tests total: Domain 117, Application 166, Integration 71, App/headless 204.
- `openspec validate snowclone-library --strict` — passed.
- `openspec validate --all --strict` — passed, 30 items and 0 failures.

## Changed-scope review

- Architecture: domain policy is pure; Application owns use cases and ports; Integration owns SQLite/CSV/resource adapters; App owns Avalonia state, storage-provider adaptation, and the dialog.
- Persistence: schema version 6 has one shared migration authority. Fresh databases, v5 upgrades, newer-version refusal, rollback, and workspace/snowclone coexistence are covered. The library is global and outside workspace snapshots.
- Product scope: only reusable library CRUD, search, starter initialization, import/export, and management UI were added. No substitution, ideation generation, AI, categorization, tagging, archive, cloud sync, or backup behavior was introduced.
- UI: the dialog is constructible for the future ideation owner, uses compiled bindings, protects drafts, exposes busy/error/empty/confirmation states, and adds no temporary launcher.
- CSV and starter data: the embedded resource ships with exact `Phrase,Guidance` headers and the single approved brace-placeholder row. Strict UTF-8 parsing, exact headers, quoting, multiline fields, deterministic export, atomic import, and duplicate handling are covered.
- Security and dependencies: no package dependency was added; CSV is treated only as inert text and never executed; picker-selected streams avoid application-owned path construction; validation and parameterized SQLite commands handle untrusted content; no secrets or credentials are present.
- Drift and hygiene: no unrelated refactor entered the change. Existing analyzer warnings elsewhere in the test suite remain pre-existing; the changed snowclone production and test files introduce no compiler or analyzer warnings. `git diff --check` passed apart from Git's informational line-ending notices.
- Optional live desktop/file-picker observation was not needed; deterministic headless UI and adapter tests are the acceptance gate.
