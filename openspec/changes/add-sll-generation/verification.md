# Verification: Add SLL generation

Criterion-level map from every acceptance scenario to its planned verification method and result. Populate results during implementation (task 7.3); `TBD` rows are planned, not yet executed.

## sll-generation / concept-refinement scenarios

| # | Scenario | Verification | Result |
|---|----------|--------------|--------|
| 1a | Complete triangle enables generation | `SllGenerationSessionViewModelTests` (score == 100 + SLL AI available + editable) | TBD |
| 1b | Incomplete triangle disables generation | `SllGenerationSessionViewModelTests` (score < 100) | TBD |
| 1c | SLL AI unavailable disables generation | `SllAccessStatusTests` + session VM disable + guidance | TBD |
| 1d | Read-only earlier-stage review disables generation | Session VM + headless view test (CanEditStage false) | TBD |
| 1e | Completeness gate refreshes live | `SllGenerationSessionViewModelTests` (PropertyChanged-driven recompute) | TBD |
| 2a | Generate derives a full minimal SLL | `SllDocumentTests` + `SllGenerationServiceTests` (parse success) + session-VM/headless assertion that all six blocks are rendered | TBD |
| 2b | No implicit generation on stage entry | Session VM (no call on open/switch) | TBD |
| 2c | Unlabeled phrase mutation is rejected | `SllDocumentTests` (mutation rejected) + `SllGenerationServiceTests` (invalid-response result) | TBD |
| 3a | Regenerate replaces the current SLL | `SllGenerationSessionViewModelTests` | TBD |
| 3b | Regeneration failure preserves existing SLL | `SllGenerationSessionViewModelTests` (failure path) | TBD |
| 3c | Stale SLL after a triangle edit | `SllGenerationSessionViewModelTests` (stale-marker + regenerate disabled) | TBD |
| 4a | SLL survives reopen | Inspector round-trip tests + Integration `sll` persistence | TBD |
| 4b | Persistence failure is recoverable | `ItemInspectorServiceTests` (commit failure) + session VM | TBD |
| 5a | Actions disabled while running | Session VM (IsBusy) + headless view test | TBD |
| 5b | Item switch cancels in-flight operation | `SllGenerationSessionViewModelTests` (cancellation / late-result discard) | TBD |
| 5c | Operation fails | `SllGenerationServiceTests` + session VM (no-op, inline error) | TBD |
| 6a | Request includes framework and creative context | `SllGenerationServiceTests` prompt-capture assertions | TBD |
| 6b | Operational and secret data is excluded | `SllGenerationServiceTests` (secret/operational absent) | TBD |
| 7a | Keyboard operation | Headless view test for tab order / automation names | TBD |
| 7b | Theme coherence | Headless view test asserting `IsBusy`/`IsEnabled`/`IsVisible` and that busy/disabled/error controls resolve to named theme resources (no pixel regression) | TBD |
| P1 | Section appears with the Concept stage surface | Headless view test (`ShowsConceptStageTool` visibility) | TBD |
| P2 | Earlier-stage review disables the SLL section | Headless view test (read-only) | TBD |
| C1 | Existing settings file loads with `Sll == InheritGeneral` | Integration load test round-tripping pre-`Sll` settings JSON | TBD |

## Gates

- **Baseline:** `dotnet test .\FusionCanvas.sln` passes.
- **Strict validation:** `openspec validate` passes with no violations.
- **Optional live desktop:** visual judgment of the rendered ASCII sketch on a disposable DB/workspace, recorded as supplemental evidence only.
