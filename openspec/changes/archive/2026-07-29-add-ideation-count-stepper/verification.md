# Verification: add-ideation-count-stepper

## Build and baseline

| Gate | Command | Result |
|---|---|---|
| Build | `dotnet build .\FusionCanvas.sln` (c Debug) | Build succeeded, 0 errors. New/changed files produce no warnings; 59 pre-existing warnings in unrelated files remain. |
| Tests | `dotnet test .\FusionCanvas.sln` (c Debug, --no-build) | 709 passed, 0 failed, 0 skipped across all four test projects (was 697 before this change; +12 new tests). |
| OpenSpec (change) | `openspec validate add-ideation-count-stepper --strict` | Valid. |
| OpenSpec (all) | `openspec validate --all --strict` | 32 passed, 0 failed. |

## Acceptance-scenario mapping

### Requirement: The dialog captures mode, guidance, count, and visible scope

| Scenario | Method | Evidence |
|---|---|---|
| Dialog opens for a group (arrows visible beside count field) | Avalonia headless view test | `IdeationWindowTests.CountStepperButtonsRenderBesideCountFieldWithLimitAndBusyDisabledStates` asserts an `Increment idea count` button and a `Decrement idea count` button exist in the visual tree after the `Number of ideas` `TextBox`. |
| Dialog opens at niche root | Existing regression | Covered by the pre-existing `IdeationWindowTests.WindowConstructsWithScopeInputModeCountAndAccessibleCandidateList` (unchanged, still green). |
| User enters an invalid count (arrows stay enabled) | Framework-free view-model test | `IdeationCountStepperTests.IncrementRecoversInvalidTextToDefault` and `.DecrementRecoversInvalidTextToMinimum` assert `CanIncrementCount` / `CanDecrementCount` are true when `CountText` is invalid; the `[InlineData]` theory covers `""`, `"   "`, and `"garbage"`. |
| Click up from valid in-range | Framework-free test | `IncrementFromInRangeStepsUpByOne`: `CountText = "5"` → invoke up → `"6"`, no error. |
| Click down from valid in-range | Framework-free test | `DecrementFromInRangeStepsDownByOne`: `CountText = "5"` → invoke down → `"4"`, no error. |
| Up disabled at max | Framework-free + headless | `IncrementIsDisabledAtMaximum` (vm: `CountText = "20"` → `CanIncrementCount` false; invoking is a no-op). Headless test asserts the increment button `IsEnabled == false` at `CountText = "20"`. |
| Down disabled at min | Framework-free + headless | `DecrementIsDisabledAtMinimum` (vm). Headless test asserts the decrement button `IsEnabled == false` at `CountText = "1"`. |
| Up recovers invalid text | Framework-free test | `IncrementRecoversInvalidTextToDefault`: `"abc"` → `"5"`, no error. |
| Down recovers invalid text | Framework-free test | `DecrementRecoversInvalidTextToMinimum`: `""` / `"   "` / `"garbage"` → `"1"`, no error. |
| Arrow clamps out-of-range parseable then steps | Framework-free test | `IncrementClampsOutOfRangeParseableBeforeStepping`: `"25"` + up → `"20"`, `"-3"` + up → `"2"`. `DecrementClampsOutOfRangeParseableBeforeStepping`: `"25"` + down → `"19"`, `"-3"` + down → `"1"`. |
| Arrows disabled while batch running | Framework-free + headless | `BothCommandsAreDisabledWhileBusy` (vm: starts a pending generation, asserts both `CanExecute` are false). Headless test asserts both buttons `IsEnabled == false` while `IsBusy` is true and re-enabled after the batch completes. |

### Requirement: Ideation remains accessible and theme coherent

| Scenario | Method | Evidence |
|---|---|---|
| Keyboard order includes arrows between count and Generate | Headless view test | `CountStepperButtonsRenderBesideCountFieldWithLimitAndBusyDisabledStates` asserts visual-tree index order: `Number of ideas` `TextBox` < `Increment idea count` < `Decrement idea count` < `Generate ideas`. The arrows are declared in the `StackPanel` immediately after the count field and before the Generate button, so keyboard tab order follows visual order. |
| Count arrows expose accessible names | Headless view test | Same headless test locates the buttons by `AutomationProperties.Name` values `Increment idea count` and `Decrement idea count`. |
| Candidate action completes (focus) | Existing regression | Unchanged; pre-existing ideation tests still green. |
| Theme changes | Existing regression | Unchanged; no new theming surface introduced. The new buttons inherit the standard `Button` style and shared theme brushes. |

## Scope and drift review

- **Layers touched:** App layer only. `src/FusionCanvas.App/Ideation/IdeationViewModel.cs` (commands, properties, helper, state invalidation) and `src/FusionCanvas.App/Ideation/IdeationWindow.axaml` (two buttons). No Domain, Application, or Integration changes. Verified by diff inspection.
- **No new capability:** The `ideation` capability is introduced by the active base change `add-ideation-tool`; this change only MODIFIES two of its requirements. Archive-ordering dependency recorded in the proposal and design.
- **No persistence, schema, or external-service change:** Count is transient dialog state; no repository, settings, or AI-request changes.
- **No security surface:** No new external input, no credential handling. The count is a local integer constrained to 1–20.

## Deviation from design (noted, not a drift)

Design Decision 3/4 said the new buttons should rely on the commands' `CanExecute` for `IsEnabled` and should not bind `IsEnabled`. The codebase's established convention (used by `GenerateCommand`/`CanGenerate` and `ManageSnowclonesCommand`/`CanManageSnowclones`) is to bind `Button.IsEnabled` to a computed `bool` property, because `RelayCommand.CanExecuteChanged` is a no-op and does not notify the binding system. To stay consistent with the surrounding code and to ensure `IsEnabled` actually updates on `CountText`/`IsBusy` changes, the implementation binds `IsEnabled` to the new `CanIncrementCount` / `CanDecrementCount` properties. The `RelayCommand.CanExecute` delegates to the same bools so `command.CanExecute(null)` works in framework-free tests. Observable behavior (disabled at limits, disabled while busy, enabled for invalid text) is identical to the spec; this is a mechanical implementation choice, not a behavior change.
