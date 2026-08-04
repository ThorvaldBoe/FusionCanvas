# Tasks: Add SLL generation

## 1. SLL document model (Domain)

- [ ] 1.1 Add `SllDocument` with sub-records (`SllCommunication`, `SllTriangle`, `SllNotes`, `SllValidation`) and framework-free JSON `Serialize`/`TryDeserialize` in `src/FusionCanvas.Domain/Concepts/`. The model covers the six parsed blocks only (assumptions, communication, triangle, ascii sketch, notes, validation).
- [ ] 1.2 Add `SllDocument.Validate(suppliedPhrase)` rule set: all six blocks present, ASCII sketch non-empty, and the `TRIANGLE` phrase equals the supplied phrase unless preceded by an explicit `REVISED PHRASE:` marker (D5) — an unlabelled mutation is a hard failure.
- [ ] 1.3 Add tests `tests/FusionCanvas.Domain.Tests/Concepts/SllDocumentTests.cs` (round-trip; validation pass/fail; empty-sketch rejected; unlabelled phrase mutation rejected; labelled revision accepted).

## 2. AI purpose and profile plumbing

- [ ] 2.1 Add `Sll` to `AiRequestPurpose` in `src/FusionCanvas.Application/AI/AiRequestPurpose.cs`.
- [ ] 2.2 Add `Sll` profile to `AiConfigurationSettings`/`Default` in `AiProfileSettings.cs`; extend `JsonApplicationSettingsStore.Normalize(AiConfigurationSettings)` to null-coalesce `settings.Sll is null → InheritGeneral` so existing settings files load without NRE (D3).
- [ ] 2.3 Add `AiRequestPurpose.Sll => settings.Sll` branch in `src/FusionCanvas.Application/AI/AiConfigurationResolver.cs`.
- [ ] 2.4 Update/extend AI resolver + settings tests in `tests/FusionCanvas.Application.Tests/AI/` for the new purpose; add an Integration load test round-tripping a pre-`Sll` settings JSON through the store yielding `Sll == InheritGeneral`.

## 3. SLL service and access status (Application)

- [ ] 3.1 Add `ISllGenerationService` (`GenerateAsync(Guid itemId, ConceptRefinementTriangle triangle, string originalIdea, CancellationToken)`) and `SllGenerationService` resolving creative context, embedding framework excerpts + §8 block contract, calling `IAiTextGenerationService.GenerateAsync` with `AiRequestPurpose.Sll`, and parsing to `SllDocument`.
- [ ] 3.2 Add `ISllAccessStatus` + `ConfiguredSllAccessStatus` querying `GetAvailabilityAsync(AiRequestPurpose.Sll)` with `AvailabilityChanged`.
- [ ] 3.3 Add `SllGenerationServiceTests.cs`: prompt assembly includes framework, triangle, original idea, creative context; operational/secret data excluded; parse success and invalid-response failure.
- [ ] 3.4 Add `SllAccessStatusTests.cs`: ready → Available; missing credential/model/config → Unavailable; refresh raises event on change.

## 4. Inspector persistence of the SLL

- [ ] 4.1 Add `Sll` to `ItemStageSavePayload` and an `sll` metadata key to `ItemMetadataCodec`.
- [ ] 4.2 Apply `sll` in `ApplyStagePayload` (Concept case) and read it back into `ItemInspectorState.Sll` in `ItemInspectorService`.
- [ ] 4.3 Add `Sll` property to `ItemInspectorViewModel` (local + loaded); extend `HasCurrentStageDraftChanges` Concept arm to include `Sll != _originalSll`; add `_originalSll` baseline tracking in `ResetBaselines`/`ApplyState`/`ApplySavedStatePreservingEdits`; thread `Sll` through `CaptureCommitSnapshot`/`CreateStagePayload`; raise `PropertyChanged`.
- [ ] 4.4 Extend inspector + persistence tests: `sll` save/load round-trip, stage guard blocks read-only, an **SLL-only** change commits and round-trips, Concept-stage persistence covered in Integration.

## 5. Session view model and UI (App)

- [ ] 5.1 Add `SllGenerationSessionViewModel` mirroring `ConceptRefinementSessionViewModel`: Generate + Regenerate commands, availability + completeness (score == 100) gating, `IsBusy`, `ErrorMessage`, single-operation concurrency + cancellation on item switch, persistence via the inspector. Implement D8 stale-marker (`IsStale`) when an SLL exists and score < 100.
- [ ] 5.2 Render the SLL section in `MainWindow.axaml` below the refinement section, gated on `ShowsConceptStageTool`, with Generate/Regenerate buttons, disabled-reason + inline-error lines, busy indicator, and monospace read-only ASCII-sketch rendering with `AutomationProperties.Name`.
- [ ] 5.3 Wire the service/access/session VM in `AppWorkspaceFactory.cs` + `AppWorkspaceRuntime` and construct the session VM in `MainWindowViewModel` beside `ConceptRefinement`; provide `DisabledSllGenerationService`/`DisabledSllAccessStatus` fallbacks and subscribe `Settings.Ai.SettingsChanged`/`AvailabilityChanged → SllGeneration.RefreshAvailabilityAsync`.
- [ ] 5.4 Add `SllGenerationSessionViewModelTests.cs` (gate on score, regenerate replaces, failure preserves existing SLL, busy disables, item-switch cancels, stale-marker when score drops after generation).
- [ ] 5.5 Add a focused Avalonia headless view test `SllSectionHeadlessTests.cs` for section construction, bindings, and enabled/disabled/busy states.

## 6. AI settings editing surface

- [ ] 6.1 Add the SLL purpose to `AiSettingsViewModel` mirroring the `Concept` block: `SllUseGeneral`, `Sll` (`AiProfileEditorViewModel`), `SllReadiness`, the `ApplyProfiles` Sll arm, `ApplyModelFilter` `Sll.Models` assignment, and `NotifyReadiness` `SllReadiness`.
- [ ] 6.2 Add the SLL purpose editor/readiness to the settings AXAML mirroring Concept.
- [ ] 6.3 Extend `AiSettingsViewModelTests.cs` for Sll defaults and round-trip.

## 7. Verification

- [ ] 7.1 Run `dotnet test .\FusionCanvas.sln` and confirm the full suite passes.
- [ ] 7.2 Run strict `openspec validate` for the change and fix any violations.
- [ ] 7.3 Complete `verification.md` mapping every acceptance scenario to its evidence and result.
- [ ] 7.4 Optional live desktop check (disposable DB/workspace) recording visual judgment of the rendered ASCII sketch as supplemental evidence only.
