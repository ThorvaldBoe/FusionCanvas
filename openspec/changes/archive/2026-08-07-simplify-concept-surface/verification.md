# Verification

## Automated evidence

- `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-build --filter FullyQualifiedName~ConceptRefinement`: 60 passed.
- `openspec validate simplify-concept-surface`: passed.

## Acceptance scenarios

| Requirement scenarios | Evidence | Result |
| --- | --- | --- |
| Unified Concept surface appears; Base idea is visible/read-only; section visibility follows Concept stage; earlier-stage review disables refinement | `ConceptRefinementViewTests.SectionVisible_ForConceptStage`, `SectionNotVisible_ForNonConceptStage`, `UnavailableGuidance_ShowsWhenAIDisabled`; updated AXAML removes duplicate editors and heading | Pass |
| Instruction steers Fine tune/Change; unrelated instructions are excluded; empty instructions preserve behavior; success clears, failure/cancel preserves; reset clears; instructions do not persist/history; read-only makes them read-only | Existing instruction-focused session/view-model tests plus unchanged per-corner command/request plumbing | Pass |
| Working values are legible/editable; initialize and synchronize; Fine tune/Change capture the visible triangle; field exit commits only the edited corner; AI-unavailable manual editing persists; transitions flush; failures preserve local values; successful results synchronize; accessible/read-only behavior remains | `CommitPendingWorkingEdits_PersistsManualTriangleAndAddsOneHistoryEntry`, `ManualWorkingEdits_PersistWhenAiIsUnavailable`, `CommitPendingWorkingEdits_WhenSaveFails_PreservesPendingValues`, existing visible-triangle, failure, AI-apply, and accessibility tests; `MainWindowViewModel` transition guard now drains pending edits | Pass |
| History selection restores all three values without a duplicate entry | `Rollback_RestoresDraftsWithoutNewEntry` and existing post-rollback history tests | Pass |

## Baseline note

The full solution command was run with `dotnet test .\FusionCanvas.sln --no-restore -m:1 -v minimal`. Domain, Application, and Integration tests passed. The App suite has one unrelated pre-existing headless layout failure: `StoreEditorHeadlessTests.NicheDetailsFields_KeepTrailingMargin`; it also fails when run in isolation and does not involve the Concept surface.
