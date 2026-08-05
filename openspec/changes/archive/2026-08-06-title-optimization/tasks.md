## 1. Domain policy

- [x] 1.1 Add `src/FusionCanvas.Domain/Items/TitleUniquenessPolicy.cs` with `MaximumAttempts = 4`, `HasCreativeContent(metadata)` (non-whitespace Idea, or at least one of Concept idea/Phrase/Graphic direction), `IsUnique`, `DistinctTitles` (case-insensitive, excluding the active item, archived items, and `Rejected` items, scoped by store), and `WithNumericSuffix`.
- [x] 1.2 Add focused domain tests at `tests/FusionCanvas.Domain.Tests/Items/TitleUniquenessPolicyTests.cs` covering whole-store scope, active-item/archived/Rejected exclusion, case-insensitive comparison, word preference, numeric-suffix fallback ordering (`2`, `3`, …), and `HasCreativeContent` cases.

## 2. Application orchestration

- [x] 2.1 Add `Title` to `AiRequestPurpose` and extend `AiConfigurationResolver.ProfileFor` to map `Title` to `settings.General` (no new AI settings UI or persistence shape).
- [x] 2.2 Add `ITitleOptimizationService`, `TitleOptimizationRequest`, and `TitleOptimizationResult` in `src/FusionCanvas.Application/TitleOptimization/`.
- [x] 2.3 Implement `TitleOptimizationService` that resolves the item from `IWorkspaceRepository`, guards with `TitleUniquenessPolicy.HasCreativeContent`, assembles creative content (idea, concept.idea, phrase, graphicDirection) plus creative context, excludes operational/secret fields, and runs the bounded loop via `IAiTextGenerationService` with `Title` purpose and `TitleUniquenessPolicy` (numeric suffix fired unconditionally at the bound).
- [x] 2.4 Add deterministic tests at `tests/FusionCanvas.Application.Tests/TitleOptimization/TitleOptimizationServiceTests.cs` (fake `IAiTextGenerationService`) covering first-candidate-unique, collision→distinguishing-word, bounded loop→numeric fallback (identical and non-identical data), no-content guard, operational/secret exclusion, and failure/cancellation leaving no accepted title.

## 3. View-model wiring

- [x] 3.1 Inject `ITitleOptimizationService` into `ItemInspectorViewModel` and add an `Optimize` command using the existing `RelayCommand`/`Run(...)` pattern.
- [x] 3.2 Wire availability gating + `CanOptimize` (Title purpose ready AND Domain `HasCreativeContent` AND item editable / not read-only), tooltip guidance, and re-evaluation after AI settings change, mirroring Concept refinement.
- [x] 3.3 Enforce single in-flight operation with a per-document `CancellationTokenSource` cancelled on active-item change/close; disable the command and make the Working title field non-editable while running (no autosave fires in that window).
- [x] 3.4 Commit the accepted, one-line-normalized title through the existing automatic-save / expected-state path; surface recoverable inline errors without overwriting.
- [x] 3.5 Add/extend tests at `tests/FusionCanvas.App.Tests/ItemInspectorViewModelTests.cs` for command availability (incl. read-only/archived disabled), guidance, busy, field-lock, cancellation, and persistence through the repo.

## 4. View

- [x] 4.1 Add an `Optimize` button beside the Working title field in `src/FusionCanvas.App/Views/MainWindow.axaml` with `AutomationProperties.Name="Optimize title"`, bound tooltip, and theme-resolved disabled/busy states.
- [x] 4.2 Add a focused headless view test (e.g. `MainWindowLayoutTests.cs`) for button presence, disabled-with-tooltip when unavailable, keyboard/document order (title field then Optimize), disabled-while-running, and non-editable Working title field during the operation.

## 5. Verification gate

- [x] 5.1 Run `dotnet test .\FusionCanvas.sln` and confirm the full deterministic baseline passes.
- [x] 5.2 Run `openspec validate` and confirm strict validation succeeds for the `title-optimization` change.
- [x] 5.3 Record criterion-level evidence mapping every acceptance scenario in the delta spec to its verification result.
