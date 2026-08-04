# Design: Add SLL generation

## Context

The Concept stage today lets a creator refine a Design Triangle (idea, phrase, graphic direction) with AI and tracks a live completeness score, but produces no concrete, inspectable layout of the final design. Issue #108 asks for SLL (Sketch Layout Language) generation: turning a complete triangle into a visual ASCII-art sketch. Issue #107 shipped the canonical Design Triangle framework asset (`src/FusionCanvas.Integration/AI/PoDDesignFramework.md`) which already contains the authoritative "Sketch Layout Language" and "Generating SLL" contracts; the feature reuses that asset as prompt context exactly as Concept refinement does.

The existing Concept refinement feature is the closest precedent and the wiring blueprint: an Application use case (`ConceptRefinementService`) + access-status port (`IConceptRefinementAccessStatus`), a session view model (`ConceptRefinementSessionViewModel`), a UI section in `MainWindow.axaml` inside the Concept stage surface, and composition in `AppWorkspaceFactory`/`MainWindowViewModel`. The design triangle fields are persisted as item metadata keys (`concept.idea`, `phrase`, `graphicDirection`) committed through the Concept-stage-aware automatic-save path (`ItemInspectorService.SaveStageAsync` → `ApplyStagePayload`).

## Goals / Non-Goals

**Goals:**
- Present an SLL generation section in the Concept stage, enabled only when the triangle is complete (score 100), SLL AI is available, and the stage is editable.
- Generate a full minimal SLL (assumptions, intent, triangle, one ASCII sketch, execution notes, validation + largest risk) from the triangle + creative context.
- Persist the most recent SLL with the item via the Concept-stage automatic-save path so it survives reopen.
- Support single generate + regenerate (replace), with single-operation concurrency and cancellation.
- Give SLL its own `AiRequestPurpose.Sll` profile + availability gating and AI-settings editor, mirroring the Concept purpose.

**Non-Goals:**
- In-place editing of the sketch/notes, multiple persisted SLL variants, image generation, print-provider-specific precision, exposing the internal layout model, or a visible UI for the framework document.

## Decisions

### D1. SLL is an opaque document persisted as an item metadata string
The persisted SLL is the **full minimal** output the framework's §8 command returns: six blocks — `Assumptions`, `Communication` (wearer signal, viewer inference, emotion, shared context), `Triangle` (idea, phrase, graphic, relationship), `AsciiSketch`, `Notes`, `Validation` (incl. largest risk). These six blocks are exactly what D5 parses; no title/version/product/composition fields are parsed or persisted in this module (those richer framework §6 fields are deferred to a later variant/editing module). For persistence the parsed `SllDocument` is serialized (JSON) into a single item metadata key `sll`, carried through the inspector as an opaque string field like `Notes`/`ConceptIdea`. Rationale: keeps the inspector generic, no SQLite schema/migration, and survives reopen through the existing automatic-save path. Alternative considered: discrete SLL columns/fields — rejected for migration cost and no current need to query sub-fields.

### D2. SLL is committed through the Concept-stage-aware inspector save path
`ItemStageSavePayload` gains an `Sll` string; `ApplyStagePayload` writes `sll` (and clears any inherited variant) for the Concept stage; `ItemInspectorState` and `ItemInspectorViewModel` gain an `Sll` property; `FindAndBuildState` reads it back. This reuses the stage-aware expected-state guard (no persistence on read-only/advanced-stage review) exactly as the triangle does. Alternative considered: SLL service writing the repository directly — rejected because it would bypass the stage guard and the app's single save model.

### D3. New `AiRequestPurpose.Sll` mirrors the Concept purpose
Add `Sll` to `AiRequestPurpose`; add an `Sll` field to `AiPurposeProfileSettings`/`AiConfigurationSettings` and a switch branch in `AiConfigurationResolver`; add `Sll` to `AiSettingsViewModel` (editor + readiness) and the settings AXAML. Default `InheritGeneral` so existing configs keep working. **Existing settings files have no `Sll` field**, so `JsonApplicationSettingsStore.Normalize` MUST null-coalesce `settings.Sll is null → AiPurposeProfileSettings.InheritGeneral` (mirroring the existing `Ideation`/`Concept` arms) or `AiConfigurationResolver` will NullReferenceException on `settings.Sll.UseGeneral` for every existing user. Rationale: availability/quality tunable independently, symmetric with Concept. Not reopening: whether SLL should share Concept's profile is deliberately rejected in favor of an independent purpose.

### D4. SLL access status reuses the Concept pattern
Add `ISllAccessStatus` + `ConfiguredSllAccessStatus` mirroring `ConfiguredConceptRefinementAccessStatus`, querying `GetAvailabilityAsync(AiRequestPurpose.Sll)` and raising `AvailabilityChanged` for live refresh after settings changes.

### D5. Prompt output uses labelled block markers parsed into the SLL document
The system prompt embeds the relevant "Sketch Layout Language" and "Generating SLL" excerpts (from `IDesignTriangleGuidanceSource`) and instructs the model to return labelled blocks in §8 order — `ASSUMPTIONS`, `INTENT`, `TRIANGLE`, `ASCII_SKETCH`, `NOTES`, `VALIDATION` — with the sketch as a fenced code block to preserve line breaks. Parsing splits on marker headers and strips fences. A missing required block OR an unlabelled phrase mutation (the `TRIANGLE` block's phrase does not equal the supplied phrase and is not preceded by an explicit `REVISED PHRASE:` marker) yields a recoverable invalid-response result, keeping the previous SLL — this honors the spec's SHALL-preserve-phrase as a hard gate, not a soft assertion. Rationale: consistent with the existing labelled-line parsing (`ConceptRefinementService`) and machine-safe.

### D6. The SLL section mirrors the refinement section in the Concept surface
A `SllGenerationSessionViewModel` binds alongside `ConceptRefinement` in `MainWindowViewModel` and is rendered below the refinement section in `MainWindow.axaml`, gated on `ShowsConceptStageTool`, with Generate + Regenerate buttons, an availability/disabled-reason line, an inline error line, a busy indicator, and a read-only `<TextBox>`/monospace block for the rendered ASCII sketch. Regenerate is enabled when a current SLL exists and the same preconditions hold.

### D7. Completeness gate reuses `DesignTriangleScore`
The Generate action is enabled only when `DesignTriangleScore.FromValues(conceptIdea, phrase, graphicDirection) == 100`. The session VM recomputes on every draft change (reusing the inspector `PropertyChanged` subscription pattern) so the gate stays live.

### D8. Stale SLL after a triangle edit remains displayed, Regenerate gated
If a successful SLL exists and the user then edits a triangle corner so the score drops below 100, the existing SLL remains displayed (it is the most recent successful result) and is shown with a visible "stale — complete the triangle to regenerate" marker; Regenerate follows the same completeness gate as Generate and is therefore disabled while incomplete. The SLL is not cleared and is not re-persisted. Rationale: keeps the user's generated artifact visible (no destructive surprise) while making the state coherent, per the UX "keep state and available actions coherent" guideline.

## Risks / Trade-offs

- **Large ASCII/markdown output** → Prompt constrains to one fenced sketch block and labelled sections; parsing is defensive and reports invalid responses rather than crashing.
- **AI may silently rewrite the phrase** → System prompt must preserve the supplied phrase verbatim unless a revision is explicitly labelled; parsing/validation checks the triangle block echoes the phrase (assertion, not hard failure).
- **Inspector payload/state growth** → One opaque `Sll` string; low coupling; covered by existing persistence tests extended for `sll`.
- **New AI purpose touches settings UI** → Mirrors the existing Concept pattern exactly; existing configs remain valid via `InheritGeneral` default.
- **Regeneration replaces rather than versions** → Deliberate scope; variant persistence is a documented non-goal for this module.

## Migration Plan

No SQLite schema or data migration. Rollout = code deployment; existing items simply have no `sll` metadata until one is generated. Rollback = code rollback; any persisted `sll` metadata is inert if the feature is removed. Settings default `Sll = InheritGeneral` so no config migration is required.

## Open Questions

None blocking. (Decisions D1–D7 resolve the module's scope; any unresolved product/UX/data choice would be escalated per the working agreement.)

---

## Implementation Plan

Ordered steps with affected layers and test locations. Decisions D1–D7 are not to be reopened.

1. **SLL document model + serialization (Domain)**
   - Add `SllDocument` (and small sub-records: `SllCommunication`, `SllTriangle`, `SllNotes`, `SllValidation`) with framework-free JSON `Serialize`/`TryDeserialize` and a `Validate` rule set in `src/FusionCanvas.Domain/Concepts/`. `Validate` enforces: all six blocks present, ASCII sketch non-empty, and the `TRIANGLE` block's phrase equals the supplied phrase unless preceded by an explicit `REVISED PHRASE:` marker (D5). Validation is a domain invariant, so it lives in Domain.
   - Tests: `tests/FusionCanvas.Domain.Tests/Concepts/SllDocumentTests.cs` (round-trip; validation pass/fail; empty-sketch rejected; unlabelled phrase mutation rejected; labelled revision accepted).

2. **AI purpose + profile plumbing**
   - `src/FusionCanvas.Application/AI/AiRequestPurpose.cs`: add `Sll`.
   - `src/FusionCanvas.Application/AI/AiProfileSettings.cs`: add `Sll` to `AiConfigurationSettings` and `Default`.
   - `src/FusionCanvas.Application/AI/AiConfigurationResolver.cs`: add `AiRequestPurpose.Sll => settings.Sll` branch.
   - `src/FusionCanvas.Integration/Settings/JsonApplicationSettingsStore.cs`: extend `Normalize(AiConfigurationSettings)` to null-coalesce `settings.Sll is null → AiPurposeProfileSettings.InheritGeneral` (D3) so existing settings files load without NRE.
   - Tests in `tests/FusionCanvas.Application.Tests/AI/` for the new purpose; add an Integration load test round-tripping a pre-`Sll` settings JSON through the store yielding `Sll == InheritGeneral`.

3. **SLL service (Application)**
   - Add `ISllGenerationService` (`GenerateAsync(itemId, triangle, originalIdea, ct)`) and `SllGenerationService` (`SllGenerationService(IWorkspaceRepository, IAiTextGenerationService, IDesignTriangleGuidanceSource)`), resolving creative context from the repository, embedding framework excerpts + §8 block contract, calling `IAiTextGenerationService.GenerateAsync` with `AiRequestPurpose.Sll`, and parsing into `SllDocument`. Failure/empty response → invalid-response result.
   - Add `ISllAccessStatus` + `ConfiguredSllAccessStatus` (Application).
   - Location: `src/FusionCanvas.Application/SllGeneration/` (mirrors `ConceptRefinement/`).
   - Tests: `tests/FusionCanvas.Application.Tests/SllGeneration/SllGenerationServiceTests.cs` (prompt assembly includes framework + triangle + context, secret/operational data excluded; parse success/failure) and `SllAccessStatusTests.cs`.

4. **Inspector persistence of the SLL (Application + App)**
   - `src/FusionCanvas.Application/Items/ItemStageSavePayload.cs`: add `Sll` (string?).
   - `src/FusionCanvas.Application/Items/ItemInspectorService.cs`: apply `sll` in `ApplyStagePayload` (Concept case) via a new key in `ItemMetadataCodec`; read it back in `FindAndBuildState` into `ItemInspectorState.Sll`.
   - `src/FusionCanvas.App/Items/ItemInspectorViewModel.cs`: add `Sll` property (local + loaded). **Critical dirty-check wiring (D2/req 4):** extend `HasCurrentStageDraftChanges` Concept arm to include `Sll != _originalSll`; add `_originalSll` baseline tracking in `ResetBaselines`/`ApplyState`/`ApplySavedStatePreservingEdits`; thread `Sll` through `CaptureCommitSnapshot`/`CreateStagePayload`. Without this, an SLL-only change would be treated as no-op and never persist. Raise `PropertyChanged` for `Sll`.
   - Tests: `tests/FusionCanvas.Application.Tests/Items/ItemInspectorServiceTests.cs` (save/load round-trips `sll`, stage guard, read-only blocked); an inspector test asserting an **SLL-only** change commits and round-trips; extend `tests/FusionCanvas.Integration.Tests/Persistence/` for `sll` persistence.

5. **Session view model + UI (App)**
   - Add `src/FusionCanvas.App/SllGeneration/SllGenerationSessionViewModel.cs` mirroring `ConceptRefinementSessionViewModel`: Generate + Regenerate commands, `IsBusy`, `ErrorMessage`, availability + completeness gating, one-op concurrency + cancellation on item switch, persists via inspector. Implement D8 stale-marker (`IsStale` + "stale — complete the triangle to regenerate" UI) when an SLL exists and score < 100.
   - Location for rendering: add `src/FusionCanvas.App/SllGeneration/SllSectionView.axaml(+.cs)` OR inline section in `MainWindow.axaml` below the refinement section (~line 902), gated on `ShowsConceptStageTool`, with `AutomationProperties.Name` per action and a monospace read-only rendering of the ASCII sketch.
   - Wire in `src/FusionCanvas.App/Views/MainWindowViewModel.cs` (construct `SllGenerationSessionViewModel` beside `ConceptRefinement`; provide `DisabledSllGenerationService`/`DisabledSllAccessStatus` fallbacks mirroring the Concept `Disabled*` fallbacks; subscribe `Settings.Ai.SettingsChanged`/`AvailabilityChanged → SllGeneration.RefreshAvailabilityAsync`) and `src/FusionCanvas.App/Workspace/AppWorkspaceFactory.cs` + `AppWorkspaceRuntime` (add `ISllGenerationService`, `ISllAccessStatus`); compose like the Concept siblings.
   - Tests: `tests/FusionCanvas.App.Tests/SllGeneration/SllGenerationSessionViewModelTests.cs` (gate on score, regenerate replaces, failure preserves existing SLL, busy/cancel, stale-marker when score drops) and a focused Avalonia headless view test (`SllSectionHeadlessTests.cs`) for construction/binding/state of the section.

6. **AI settings editing surface (App)**
   - `src/FusionCanvas.App/Settings/AiSettingsViewModel.cs` and settings AXAML: add the SLL purpose mirroring the `Concept` block — `SllUseGeneral` bool, `Sll` (`AiProfileEditorViewModel`), `SllReadiness`, the `ApplyProfiles` Sll arm, `ApplyModelFilter` `Sll.Models` assignment, and `NotifyReadiness` `SllReadiness` notification; add the matching editor + readiness block to the settings AXAML.
   - Tests: extend `tests/FusionCanvas.App.Tests/AiSettingsViewModelTests.cs` for the Sll profile defaults/round-trip.

7. **Verification**
   - Baseline: `dotnet test .\FusionCanvas.sln`.
   - `openspec validate` strict; tasks/verification mapped criterion-by-criterion.
   - Optional live desktop check (disposable DB/workspace) for visual judgment of the rendered ASCII sketch — supplemental only.
