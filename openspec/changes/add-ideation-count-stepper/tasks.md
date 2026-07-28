# Tasks: add-ideation-count-stepper

## 1. View-model stepper commands

- [x] 1.1 In `src/FusionCanvas.App/Ideation/IdeationViewModel.cs`, add a private `GetNextCountText(int direction)` helper implementing the stepping algorithm in `design.md` Decision 2: parse `CountText`, clamp then step when parseable, return `DefaultCount` for invalid + up, return `MinimumCount` for invalid + down.
- [x] 1.2 Add `IncrementCountCommand` and `DecrementCountCommand` (`RelayCommand`) that set `CountText` via `GetNextCountText(+1)` / `GetNextCountText(-1)`.
- [x] 1.3 Implement `CanExecute` for both commands per `design.md` Decision 3: `IncrementCountCommand.CanExecute` is `!IsBusy && !(TryGetCount(out int n) && n == MaximumCount)`; the decrement variant checks `n == MinimumCount`. Invalid text leaves both enabled.
- [x] 1.4 Confirm `CountText`'s setter and `IsBusy`'s setter both raise command state for the new commands (via the existing `RaiseCommandState` path). If `IsBusy` does not invalidate the new commands, add an explicit call when transitioning.

## 2. Framework-free view-model tests

- [x] 2.1 Add `tests/FusionCanvas.App.Tests/Ideation/IdeationCountStepperTests.cs` (or extend the existing `IdeationViewModelTests`) covering: increment and decrement from a valid in-range count; up disabled at `MaximumCount`; down disabled at `MinimumCount`; up from invalid text yields `DefaultCount`; down from invalid/empty text yields `MinimumCount`; out-of-range parseable clamps then steps (`"25"` + down → `"19"`, `"-3"` + up → `"2"`, `"25"` + up → `"20"`, `"-3"` + down → `"1"`); both commands disabled while `IsBusy`.

## 3. Dialog view buttons

- [x] 3.1 In `src/FusionCanvas.App/Ideation/IdeationWindow.axaml`, insert two `Button` elements inside the existing horizontal `StackPanel` immediately after the count `TextBox` and before the `Generate ideas` button. Bind `Command` to `IncrementCountCommand` and `DecrementCountCommand`, set `AutomationProperties.Name` to `Increment idea count` and `Decrement idea count`, and give them distinct up/down arrow content.
- [x] 3.2 Do not bind `IsEnabled` on the new buttons to `IsNotBusy`; rely on the commands' `CanExecute` for both busy and limit disabling.

## 4. Headless view tests

- [x] 4.1 Extend the existing ideation headless view test file: assert both arrow buttons render beside the count `TextBox`; assert their `AutomationProperties.Name` values; assert keyboard focus order reaches the up arrow after the count `TextBox` and before the down arrow and the Generate button; assert the up button is disabled when `CountText` is `20`; assert the down button is disabled when `CountText` is `1`; assert both buttons are disabled while a generation batch is running.

## 5. Verification and completion gates

- [x] 5.1 Run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln` with zero warnings and zero failures.
- [x] 5.2 Run `openspec validate add-ideation-count-stepper --strict` and `openspec validate --all --strict`.
- [x] 5.3 Write `verification.md` mapping every delta-spec scenario to its test evidence; confirm the archive-ordering dependency on `add-ideation-tool` and that no Domain, Application, or Integration changes were made.
