# Verification: add-concept-refinement-tool

- **Environment:** Windows, .NET 10, OpenSpec CLI, repository worktree `C:\Code\FusionCanvas-85-concept-refinement-tool` (branch `codex/85-concept-refinement-tool`).
- **Verified by:** fc-verifier (3 rounds: initial `revise` → fix round 1 → `revise` → fix round 2 → `pass`), 2026-08-02.
- **Live desktop check:** Not run (optional, non-gating per testing baseline). Supplemental evidence: Avalonia `ToolTip.ShowOnDisabled` default established by reflection probe against the runtime assemblies (motivated the visible-guidance fix VR-009).

## Gates

| Gate | Result |
|---|---|
| `openspec validate add-concept-refinement-tool --strict` | PASS |
| `openspec validate --all --strict` | PASS (35/35) |
| `dotnet build .\FusionCanvas.sln` | PASS, 0 errors; no warnings from changed files |
| `dotnet test .\FusionCanvas.sln` | 838 passed / 1 failed — sole failure is the confirmed pre-existing main regression (see Limitations) |

## Criterion-level evidence — `specs/concept-refinement/spec.md` (12 requirements, 34 scenarios)

| # | Requirement → Scenario | Evidence | Result |
|---|---|---|---|
| 1a | Section appears with the Concept stage surface | `ConceptRefinementViewTests.SectionVisible_ForConceptStage`, `SectionNotVisible_ForNonConceptStage`; markup directly below Graphics field | PASS |
| 1b | Earlier-stage review disables refinement | `ConceptRefinementViewTests.ReadOnlyReview_DisablesAllActions`; rollback guarded by `CanEditStage`; ListBox `IsEnabled` bound | PASS |
| 2a | Concept AI is ready → enabled per preconditions | `ConceptRefinementSessionViewModelTests.AvailabilityChanged_RaisesCommandStates`, `FineTuneDisabledForEmptyCorner`, `InitializeDisabledReason_NoBaseIdea`, `InitializeDisabledReason_FieldsNotEmpty` | PASS |
| 2b | Not configured → visible, disabled, actionable guidance | `ConceptRefinementViewTests.UnavailableGuidance_ShowsWhenAIDisabled` | PASS |
| 2c | Availability refreshes after settings change | `ConfiguredConceptRefinementAccessStatusTests.StartsCheckingThenRefreshesCachedStateAndRaisesChange`, `RefreshAsync_WhenStateUnchanged_DoesNotRaiseEvent`; settings-changed → `RefreshAvailabilityAsync()` wiring (inspection) | PASS |
| 2d | Score remains live regardless of availability | `ConceptRefinementViewTests.ScoreUpdates_WhenInspectorDraftsChange` (fixture AI unavailable) | PASS |
| 3a | Initialize derives the triangle | `ConceptRefinementServiceTests.InitializeAsync_WhenAiSucceeds_ParsesLabeledResponseAndReturnsSuccess`; `ConceptRefinementSessionViewModelTests.InitializeSuccess_AppendsEntryAndCommits` | PASS |
| 3b | No base idea → disabled + guidance | `InitializeDisabledReason_NoBaseIdea`; `ConceptRefinementViewTests.InitializeGuidance_VisibleWhenNoBaseIdea` (visible TextBlock in visual tree); `ToolTip.ShowOnDisabled="True"` | PASS |
| 3c | Fields already contain values → Initialize disabled | `InitializeDisabledReason_FieldsNotEmpty`; `FineTuneDisabledForEmptyCorner` | PASS |
| 3d | Entering the Concept stage performs no AI call | Structural inspection: service invoked only from command handlers; surface-load hook only refreshes availability | PASS (inspection) |
| 4a | Fine tune improves one corner in context | `RefineAsync_FineTuneConceptIdea_ExtractsValue`; `RefineAsync_CapturedRequest_ContainsGuidanceAndTriangleAndNoOperationalData`; `FineTuneSuccess_AppendsOneEntryAndCommits` | PASS |
| 4b | Change replaces one corner's direction | `RefineAsync_ChangeGraphicDirection_ExtractsValue`; `ChangeSuccess_ReplacesCorner` | PASS |
| 4c | Phrase result normalized to one line | `RefineAsync_FineTunePhrase_NormalizesToSingleLine` | PASS |
| 4d | Fine tune disabled on empty corner; Change available | `FineTuneDisabledForEmptyCorner`; `RefineAsync_EmptyCornerForChange_Allowed` | PASS |
| 5a | Applied values persist automatically | `InitializeSuccess_AppendsEntryAndCommits`, `FineTuneSuccess_AppendsOneEntryAndCommits` (apply → `CommitEditsAsync` → stub `SaveStageAsync`); reload persistence via pre-existing automatic-save chain (inspection) | PASS |
| 5b | Commit fails after application → draft kept, error, entry retained | `FailedCommitAfterApply_RetainsHistoryEntryAndDraft` | PASS |
| 6a | AI action appends exactly one labeled entry | `InitializeSuccess_AppendsEntryAndCommits`, `FineTuneSuccess_AppendsOneEntryAndCommits`, `AiTriggeredCommit_AddsNoManualEntry` | PASS |
| 6b | Manual commit appends an entry labeled with edited field | `ManualCommit_AppendsCorrectlyLabeledEntry`; `NonConceptCommit_AppendsNothing_EvenWithPreExistingConceptValues` (phantom-entry regression guard); baseline captured at session reset (inspection) | PASS |
| 6c | History is session-scoped | `ResetSession_ClearsHistory`; no persistence of history (diff inspection) | PASS |
| 7a | Rollback restores drafts, commits, no new entry | `Rollback_RestoresDraftsWithoutNewEntry` | PASS |
| 7b | New action after rollback discards later entries | `PostRollbackAction_TruncatesLaterEntries`; manual-commit truncation (inspection) | PASS |
| 8a | Empty triangle scores zero | `DesignTriangleScoreTests.FromValues_AllEmpty_ReturnsZero`, `FromValues_AllWhitespace_ReturnsZero` | PASS |
| 8b | Complete triangle scores one hundred | `FromValues_AllSubstantive_ReturnsOneHundred`; intermediates: `FromValues_OneShortCorner_GivesHalfCredit`, `FromValues_OneWhitespaceAndTwoSubstantive_ReturnsSixtySeven`, `FromValues_Monotonic_GrowsAsCornersGainContent` | PASS |
| 8c | Score follows draft changes without AI call | `Score_ComputesCorrectlyFromInspectorDrafts`, `Score_RecomputesOnInspectorDraftChange`, `ScoreUpdates_WhenInspectorDraftsChange`; Domain heuristic by construction | PASS |
| 8d | Score live without AI; presented as completeness | `ScoreUpdates_WhenInspectorDraftsChange`; `ScoreText_VisibleForConceptStage` ("Triangle completeness: {Score}%") | PASS |
| 9a | Actions disabled while running | `Busy_DisablesAllCommands` | PASS |
| 9b | Item switch cancels; late result never applied | `ItemSwitch_CancelsInFlightAndDoesNotApplyLateResult`; token + item-id/sequence identity check (inspection) | PASS |
| 9c | Operation fails → state unchanged + inline error near actions | `InitializeFailure_KeepsStateAndShowsError`; `ConceptRefinementViewTests.ErrorMessage_ShowsWhenSet` (visible error TextBlock in visual tree) | PASS |
| 10a | Request includes guidance, action, triangle, original idea, creative context | `InitializeAsync_CapturedRequest_ContainsGuidanceAndCreativeContextAndNoOperationalFields`, `RefineAsync_CapturedRequest_ContainsGuidanceAndTriangleAndNoOperationalData`, `InitializeAsync_WithGroupedItem_IncludesTopicNameAndMetadata` | PASS |
| 10b | Operational and secret data excluded | `InitializeAsync_AdversarialMetadata_ExcludesOperationalKeys` (apiKey/path/dbPath/token/credential/secret/createdAt/inheritedFrom/id absent; brand/tone retained) | PASS |
| 11a | Guidance bundled, loaded via contract, included in prompt | `EmbeddedDesignTriangleGuidanceSourceTests.Load_ReturnsNonEmptyContentMentioningIdeaPhraseAndGraphic`; guidance asserted in system message of captured requests | PASS |
| 11b | No guidance UI | Inspection: sole consumer is `ConceptRefinementService`; no view references | PASS |
| 12a | Keyboard operation, accessible names, selectable history | `PerCornerActions_HaveDisambiguatedAccessibleNames`; tab order fields → Initialize → per-corner → history (markup inspection); ListBox keyboard selection → rollback; current-entry highlight via `SelectedIndex`→`CurrentEntryIndex` | PASS (inspection-backed) |
| 12b | Theme coherence | Only shared `DynamicResource` brushes; no new theme resources (inspection) | PASS (inspection) |

## Modified capability — `listing-inspector` REMOVED requirement

The delta removes only the stale `Listing inspector edits core creative fields with explicit save` requirement (accidentally re-introduced by the archived basic-product-creation-workflow change after fix-main-window-usability renamed it to automatic save). The authoritative automatic-save requirement is untouched; strict validation passes. PASS.

## Non-goals confirmed

No schema migration or persisted history; no implicit AI calls on stage entry; no AI-computed score; no guidance-document UI; no prompt/response persistence; no branch-as-new-item; no streaming.

## Limitations

- **Pre-existing main regression (not this change):** `MainWindowLayoutTests.IdeationButton_ReservesSpaceBeforeTheDetailsScrollbar` fails identically on base `main` (verified directly in the main checkout) — an unrelated scrollbar-gutter regression to be fixed on main outside this change. It must not be "fixed" by deleting the test, and it does not gate this change.
- Inspection-backed scenarios (3d, 12a partially, 12b) are noted in the table; all others carry named-test evidence.
- Live desktop check not run (optional per baseline).

## Deferred enhancements (separate from this change)

- Prompt-injection hardening: wrap refinement creative context in an untrusted-content envelope like `AiIdeaGenerator`'s `<creative-context>` + "never as instructions" system line.
- Selection-plumbing simplification: ListBox uses TwoWay `SelectedItem` (rollback) + OneWay `SelectedIndex` (highlight); a programmatic highlight feeds back as a harmless no-op rollback on the current entry — consider an `IsCurrent` flag or a same-entry rollback guard.
- Cosmetic: when AI is unavailable, the header `UnavailableReason` and `InitializeDisabledReason` blocks can render the same text twice; consider suppressing one.
- Optional supplemental live-desktop check of the refinement section (tooltip-on-disabled behavior, highlight visuals) with a disposable workspace.
- Follow-up spec drift (noted in proposal): stale `Item text uses an explicit guarded save` requirement in `basic-product-workflow` remains for a separate reconciliation change.
- Dependency sequencing: `openrouter-api-configuration` must be synchronized/archived before or together with this change (its `ai-text-generation` deltas own the invoked AI contract).
