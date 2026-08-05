# Verification: Add SLL generation

Criterion-level map from every acceptance scenario to its verification method and result. All deterministic gates pass: full solution test baseline (`dotnet test .\FusionCanvas.sln`) green; strict `openspec validate add-sll-generation` passes; strict `openspec validate --all` passes.

## sll-generation / concept-refinement scenarios

| # | Scenario | Verification | Result |
|---|----------|--------------|--------|
| 1a | Complete triangle enables generation | `SllGenerationSessionViewModelTests.Generate_EnabledWhenTriangleCompleteAndAvailableAndEditable`; surface-load refresh wired in `RefreshStageToolState` (`SllGeneration.RefreshAvailabilityAsync()` on Concept-surface load) | PASS |
| 1b | Incomplete triangle disables generation | `Generate_DisabledWhenTriangleIncomplete` (score < 100 → CanGenerate false + "Complete" guidance) | PASS |
| 1c | SLL AI unavailable disables generation | `Generate_DisabledWhenAiUnavailable` + `SllSectionHeadlessTests.ActionsDisabled_WhenSllAiUnavailable` (disabled + tooltip reason) | PASS |
| 1d | Read-only earlier-stage review disables generation | `ReadOnlyReview_DisablesGenerateWithStageReason` (item at Design stage reviewed at Concept → CanGenerate false, GenerateDisabledReason == StageReadOnlyReason) | PASS |
| 1e | Completeness gate refreshes live | `StaleMarker_ShowsWhenScoreDropsAfterGeneration` (CanGenerate flips false on corner edit, no AI call) | PASS |
| 2a | Generate derives a full minimal SLL | `SllGenerationServiceTests.GenerateAsync_WhenAiSucceeds_ParsesBlocksAndReturnsSuccess` + `GenerateSuccess_AppliesSllToInspectorAndCommits` (all six blocks surfaced) | PASS |
| 2b | No implicit generation on stage entry | Inspection: session VM performs no AI call on construction/`ResetSession`/`LoadAsync`; `ItemSwitch_CancelsInFlightAndDoesNotApplyLateResult` confirms no apply on switch | PASS (inspection) |
| 2c | Unlabeled phrase mutation is rejected | `SllDocumentTests.Validate_UnlabeledPhraseMutation_ReturnsFalse` + `GenerateAsync_WhenPhraseMutatedUnlabeled_ReturnsFailed` | PASS |
| 3a | Regenerate replaces the current SLL | `Regenerate_WhenCurrentSllExists_ReplacesIt` | PASS |
| 3b | Regeneration failure preserves existing SLL | `GenerateFailure_KeepsExistingSllAndShowsError` | PASS |
| 3c | Stale SLL after a triangle edit | `StaleMarker_ShowsWhenScoreDropsAfterGeneration` (stale + regenerate disabled) | PASS |
| 4a | SLL survives reopen | `ItemInspectorPersistenceTests.InspectorSave_StageAwareConceptSave_RoundTripsSll` (Integration, SQLite) + `SaveStageAsync_PersistsSllAndReloadsIt` | PASS |
| 4b | Persistence failure is recoverable | `CommitFailure_RetainsSllDraftAndSurfacesRecoverableError` (FailSaves → SLL draft retained, inspector error surfaced) | PASS |
| 5a | Actions disabled while running | `Busy_DisablesActions` (IsBusy disables Generate/Regenerate) | PASS |
| 5b | Item switch cancels in-flight operation | `ItemSwitch_CancelsInFlightAndDoesNotApplyLateResult` | PASS |
| 5c | Operation fails | `GenerateFailure_KeepsExistingSllAndShowsError` (existing display unchanged, inline error) | PASS |
| 6a | Request includes framework and creative context | `GenerateAsync_CapturedRequest_UsesSllPurposeAndContainsFrameworkAndContextAndNoOperationalFields` | PASS |
| 6b | Operational and secret data is excluded | `GenerateAsync_AdversarialMetadata_ExcludesOperationalKeys` + captured-request `DoesNotContain` assertions | PASS |
| 7a | Keyboard operation | `SllSectionHeadlessTests.SectionVisible_ForConceptStage` (Generate + Regenerate names present; Generate declared before Regenerate in tab order, after refinement actions) | PASS |
| 7b | Theme coherence | `SllSectionHeadlessTests.BusyIndicator_VisibleWhenBusy` + `ErrorMessage_VisibleWhenSet` + `ActionsDisabled_WhenSllAiUnavailable` (IsEnabled) + `StaleMarker_HiddenWhenNoCurrentSll`; `WarningTextBrush` defined in both Light and Dark theme dictionaries | PASS |
| P1 | Section appears with the Concept stage surface | `SllSectionHeadlessTests.SectionVisible_ForConceptStage` / `SectionNotVisible_ForNonConceptStage` | PASS |
| P2 | Earlier-stage review disables the SLL section | `ReadOnlyReview_DisablesGenerateWithStageReason` (beyond-Concept review disables actions with stage reason) | PASS |
| C1 | Existing settings file loads with `Sll == InheritGeneral` | `JsonApplicationSettingsStoreTests.LoadAsync_PreSllSettingsJsonDefaultsSllToInheritGeneral` | PASS |

## SR-001 regression guard

| # | Scenario | Verification | Result |
|---|----------|--------------|--------|
| R1 | SLL-only change is detected as dirty and commits | `ItemInspectorViewModelTests.SllOnlyChange_MarksDirtyAndCommitsThroughStagePayload` (load Concept state, set only `Sll`, assert `HasUnsavedChanges` true and `CommitEditsAsync` persists `sll` metadata) | PASS |

## Gates

- **Baseline:** `dotnet test .\FusionCanvas.sln` — PASS.
- **Strict validation:** `openspec validate add-sll-generation --strict` — PASS; `openspec validate --all` — PASS.
- **Optional live desktop:** task 7.4 deferred (no requirement to run; visual judgment of the rendered ASCII sketch may be recorded as supplemental evidence later).
