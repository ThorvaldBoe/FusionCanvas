# Tasks: add-concept-refinement-tool

Implementation follows `design.md` D1–D8. Do not reopen the design decisions listed at the end of the implementation plan. Validate with `dotnet build .\FusionCanvas.sln` and the focused test filter for each step; run the full baseline in step 6.

## 1. Domain completeness score

- [ ] 1.1 Add `src/FusionCanvas.Domain/Concepts/DesignTriangleScore.cs` implementing the D1 formula: per corner 0 / 0.5 / 1 (whitespace-only; trimmed length < 8; ≥ 8), score = `Round(100 × sum / 3)`.
- [ ] 1.2 Add `tests/FusionCanvas.Domain.Tests/Concepts/DesignTriangleScoreTests.cs`: all empty → 0; all substantive → 100; one short corner half credit; whitespace-only corner treated as empty; monotonic as corners gain content.

## 2. Guidance document and source

- [ ] 2.1 Add `IDesignTriangleGuidanceSource` (`string Load()`) in `src/FusionCanvas.Application/ConceptRefinement/`.
- [ ] 2.2 Add `src/FusionCanvas.Integration/AI/DesignTriangleGuidance.md` as `EmbeddedResource` with the D4 placeholder content (idea = emotion/familiar setting, mandatory; phrase = optional on-product text; graphics = optional visual elements; the three must reinforce each other).
- [ ] 2.3 Add `EmbeddedDesignTriangleGuidanceSource` in `src/FusionCanvas.Integration/AI/` reading the embedded resource.
- [ ] 2.4 Add `tests/FusionCanvas.Integration.Tests/AI/EmbeddedDesignTriangleGuidanceSourceTests.cs`: loads non-empty content mentioning idea, phrase, and graphic.

## 3. Application refinement service

- [ ] 3.1 Add `ConceptRefinementCorner`, `ConceptRefinementActionKind`, `ConceptRefinementTriangle`, `ConceptRefinementResult`, and `IConceptRefinementService` in `src/FusionCanvas.Application/ConceptRefinement/` per D2.
- [ ] 3.2 Implement `ConceptRefinementService` (repository creative-context resolution, D3 prompt assembly with guidance document + creative context and no operational/secret fields, `AiRequestPurpose.Concept` dispatch).
- [ ] 3.3 Implement D3 response parsing: labeled `IDEA:`/`PHRASE:`/`GRAPHIC:` for Initialize (all three required); label/quote stripping and `ItemMetadataCodec.NormalizeSingleLine` for Phrase on corner operations; empty results fail recoverably.
- [ ] 3.4 Add `IConceptRefinementAccessStatus` + `ConfiguredConceptRefinementAccessStatus` mirroring `ConfiguredIdeationAccessStatus` for `AiRequestPurpose.Concept`.
- [ ] 3.5 Add `tests/FusionCanvas.Application.Tests/ConceptRefinement/` tests with a capturing fake `IAiTextGenerationService` and in-memory repository: initialize parse success; malformed initialize failure leaves no partial values; fine-tune/change extraction and normalization; empty-result failure; availability mapping; captured request contains guidance text, action instruction, current triangle, original idea, creative context, and no identifiers/timestamps/paths/credentials.

## 4. Session view model and composition

- [ ] 4.1 Add `ConceptRefinementHistoryEntry` and `src/FusionCanvas.App/ConceptRefinement/ConceptRefinementSessionViewModel.cs` per D5: availability mirror, busy flag, inline error, history collection + current index, score recompute on inspector draft changes.
- [ ] 4.2 Implement Initialize/FineTune/Change commands with per-action preconditions (D2/D3: initialize needs base idea + all fields empty; fine-tune disabled on empty corner; change always available), single-operation busy gating, per-session cancellation, and item-identity/sequence-checked application (D5).
- [ ] 4.3 Implement D6 apply path: set inspector drafts, append one history entry, `await CommitEditsAsync()`; failed commit keeps draft + entry and surfaces inline error.
- [ ] 4.4 Implement history rollback (restore drafts, move index, commit, no new entry) and post-rollback truncation on the next action or manual commit; manual-commit entries via the inspector `Saved` event (`Edited <field>` / `Edited Concept fields`); session reset on item change, `Clear()`, and full reload.
- [ ] 4.5 Compose through `AppWorkspaceFactory` (new `ConceptRefinement` service + `ConceptRefinementAccess` runtime members) and the `MainWindowViewModel` constructor chain; expose `MainWindowViewModel.ConceptRefinement`; refresh availability on Concept surface load and after AI settings save (D8).
- [ ] 4.6 Add `tests/FusionCanvas.App.Tests/ConceptRefinementSessionViewModelTests.cs` (framework-free, fake service + inspector doubles): apply success/failure, busy gating, cancellation on item switch and late-result guard, rollback + truncation, manual-commit entries, score updates, availability states.

## 5. Refinement section UI

- [ ] 5.1 Add the D7 refinement section to the Concept border in `src/FusionCanvas.App/Views/MainWindow.axaml`: header + unavailable guidance, three per-corner action rows with disambiguated accessible names, `Initialize from base idea…` with disabled guidance, `Triangle completeness: {Score}%`, bounded chronological history list with current-entry highlight and keyboard-operable rollback, busy/disabled states, read-only disablement, visibility following `ShowsConceptStageTool`.
- [ ] 5.2 Add headless view tests (`tests/FusionCanvas.App.Tests/ConceptRefinementViewTests.cs` or extend `MainWindowLayoutTests.cs`): section visible only on Concept stage; unavailable guidance + disabled actions; accessible names (`Fine tune Concept idea`, `Change Phrase`, …); initialize enabled/disabled by preconditions; history renders and rollback works by keyboard; read-only review disables all actions; score text updates when values change.

## 6. Verification and validation gates

- [ ] 6.1 Create `verification.md` mapping every acceptance scenario in `specs/concept-refinement/spec.md` (all 12 requirements) to criterion-level evidence: specific test names for each scenario, plus the listing-inspector removal diff check.
- [ ] 6.2 Run `openspec validate add-concept-refinement-tool --strict` and `openspec validate --all --strict`; resolve all findings.
- [ ] 6.3 Run `dotnet build .\FusionCanvas.sln` warning-clean and `dotnet test .\FusionCanvas.sln` with all tests passing; record results in `verification.md`.
- [ ] 6.4 Confirm non-goals held: no schema migration, no persisted history, no implicit AI calls on stage entry, no AI scoring, no guidance-document UI, no prompt/response persistence.
