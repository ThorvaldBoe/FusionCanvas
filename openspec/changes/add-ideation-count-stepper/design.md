## Context

The Ideation dialog (introduced by the in-progress `add-ideation-tool`
change) captures a desired candidate count in a free-text `TextBox`
(`CountText`) constrained to whole numbers from 1 to 20 with a default of
5. Parsing and validation live in `IdeationViewModel.TryGetCount`, which
both gates the `GenerateCommand` and supplies the `CountError` message.
The dialog is presented by `IdeationWindow.axaml` / `.axaml.cs` and is
already covered by framework-free view-model tests and Avalonia headless
view tests.

This module adds count-up and count-down arrow buttons beside the existing
`TextBox` so the user can adjust the count with a single click. It is a
small, self-contained UI change that does not alter workflow, persistence,
or external integration.

## Goals / Non-Goals

**Goals:**
- Provide single-click increment and decrement of the desired idea count
  within the existing 1–20 range.
- Preserve free-text entry, the existing range validation, the invalid-count
  error message, and Generate gating exactly as they are today.
- Provide a predictable, accessible, keyboard-reachable placement for the
  new arrows inside the existing dialog control order.
- Keep the implementation in the App layer only; no Domain, Application, or
  Integration changes.

**Non-Goals:**
- Replacing the `TextBox` with a `NumericUpDown` or any control that
  prevents the user from typing invalid text. The invalid-count error
  scenario is preserved.
- Changing the count range, default, or any other Ideation behavior.
- Touching the candidate list, generation pipeline, rejection flow, or
  discard confirmation.
- Adding repeat-on-hold, keyboard arrow-key acceleration, or custom theming
  for the buttons. They use standard `Button` styling.

## Decisions

### Decision 1: Two standard `Button` controls, not `NumericUpDown`
Use two plain Avalonia `Button` controls with up/down arrow glyphs beside
the existing `TextBox`, bound to new `IncrementCountCommand` and
`DecrementCountCommand` on `IdeationViewModel`.

**Rationale:** The accepted `ideation` spec already defines an invalid-count
scenario in which the user can type a value outside 1–20 or a non-number and
see the range error while keeping their guidance. `NumericUpDown` would
make invalid text hard or impossible to enter, removing that scenario and
weakening the contract. Plain buttons preserve every existing behavior and
add only the click-stepper affordance.

**Alternatives considered:**
- `NumericUpDown` with `Minimum=1`, `Maximum=20`: rejected as above.
- A single reusable numeric stepper user control: premature for one field;
  revisit if more numeric inputs appear.

### Decision 2: Stepping algorithm
A single private helper computes the next count text from the current text
and a direction:

```
if int.TryParse(currentText, out int n):
    n = clamp(n, MinimumCount, MaximumCount)
    n = clamp(n + direction, MinimumCount, MaximumCount)  // direction is +1 or -1
    result = n.ToString()
else:
    result = (direction > 0 ? DefaultCount : MinimumCount).ToString()
```

`CountText`'s existing setter already raises `CountError` and
`RaiseCommandState`, so the buttons reuse that path by setting `CountText`.

**Edge cases resolved by this algorithm:**
- Valid in-range: steps by one.
- At-limit: clamp produces the same value; setting `CountText` to the same
  value is a no-op via `SetField`, which is fine. `CanExecute` disables the
  button at the limit so the click never lands.
- Out-of-range parseable: clamps first, then steps. A single click on `25`
  with the down arrow yields `19` (25→20→19). A single click on `-3` with the
  up arrow yields `2` (-3→1→2).
- Invalid/empty: up yields `5`, down yields `1`.

### Decision 3: `CanExecute` for the new commands
`IncrementCountCommand.CanExecute` returns true when the dialog is not busy
AND the current parsed count is not at `MaximumCount` (and the current text
is invalid, in which case clicking recovers). Concretely:

- `IncrementCountCommand.CanExecute` = `!IsBusy && !(TryGetCount(out int n) && n == MaximumCount)`
- `DecrementCountCommand.CanExecute` = `!IsBusy && !(TryGetCount(out int n) && n == MinimumCount)`

When the text is invalid, `TryGetCount` is false, so both commands stay
enabled — matching the spec scenario that lets the user recover by
clicking.

The existing `CountText` setter must call `RaiseCommandState` (it already
does) so the buttons re-evaluate `CanExecute` when the count changes. The
existing `IsBusy` setter must also invalidate these commands; it already
calls `RaiseCommandState` via the Generate command path — verify during
implementation and add explicit invalidation if needed.

### Decision 4: Placement and accessibility
In `IdeationWindow.axaml`, insert the two buttons inside the existing
horizontal `StackPanel` that contains the "Number of ideas" label, count
`TextBox`, and Generate button. Order: label, `TextBox`, up arrow, down
arrow, Generate, spinner, progress. This places the arrows immediately
after the field and before Generate, matching the spec's keyboard order.

Both buttons share the existing `IsEnabled="{Binding IsNotBusy}"` baseline
through their command's `CanExecute` (do not also bind `IsEnabled` to
`IsNotBusy`, to avoid double-gating conflicts — let the command own it).

Accessible names: `Increment idea count` and `Decrement idea count`, set
via `AutomationProperties.Name`.

Glyphs: use simple text content `▲` and `▼` (or `^\`/`v` equivalents if
font rendering is an issue in headless tests). Final glyph choice is an
implementation detail; the spec only requires two distinct buttons with
the agreed accessible names.

### Decision 5: Test placement
- Framework-free view-model tests in
  `tests/FusionCanvas.App.Tests/Ideation/` (mirror the existing
  `IdeationViewModelTests` location) for the stepping algorithm,
  `CanExecute` at limits, invalid-text recovery, and busy-disabled state.
- Avalonia headless view tests in the existing ideation headless test file
  for button presence, accessible names, keyboard order, and disabled state
  at the limits.

## Risks / Trade-offs

- **Spec drift on the upstream `ideation` capability:** The `ideation`
  capability is not yet in `openspec/specs/`; it lives in the in-progress
  `add-ideation-tool` change. This delta MODIFIES requirements introduced
  there. → Mitigation: declare the archive-ordering dependency explicitly;
  archive `add-ideation-tool` first, then this change. If `add-ideation-tool`
  changes its requirement names before archive, this delta must be re-aligned.
- **`CanExecute` staleness:** If `IsBusy` changes do not invalidate the new
  commands, the buttons could remain clickable during a batch. → Mitigation:
  the implementation plan verifies `IsBusy`'s setter raises command state,
  and a headless view test asserts the disabled-during-batch scenario.
- **Double-binding `IsEnabled`:** Binding both `IsEnabled` and a
  `CanExecute` can conflict in Avalonia. → Mitigation: rely on
  `CanExecute` only, as decided above.
- **Glyph rendering in headless tests:** Unusual Unicode glyphs could render
  differently in headless mode. → Mitigation: headless tests assert on
  button presence, accessible name, and disabled state, not on glyph
  rendering.

## Implementation Plan

### Affected layers and files
- **App layer (production):**
  - `src/FusionCanvas.App/Ideation/IdeationViewModel.cs` — add
    `IncrementCountCommand` and `DecrementCountCommand` (`RelayCommand`),
    their `CanExecute` helpers, and a private `GetNextCountText(int
    direction)` helper. Wire `CountText`'s setter and `IsBusy`'s setter to
    raise state for the new commands (verify; the existing `RaiseCommandState`
    call site should already cover both if it raises for all commands — the
    `RelayCommand` implementation must support per-command `CanExecute`
    invalidation).
  - `src/FusionCanvas.App/Ideation/IdeationWindow.axaml` — insert two
    `Button` elements with arrow content, bound to the new commands, with
    `AutomationProperties.Name` set.
- **App layer (tests):**
  - `tests/FusionCanvas.App.Tests/Ideation/IdeationViewModelTests.cs` (or a
    new `IdeationCountStepperTests.cs` file in the same folder) —
    framework-free tests.
  - The existing ideation headless view test file — add assertions for the
    stepper buttons.

### Responsibility placement
- Domain/Application/Integration: no changes. Count range constants
  (`DefaultCount`, `MinimumCount`, `MaximumCount`) already live on
  `IdeationViewModel` and are reused.
- All new behavior is view-model presentation state in the App layer, which
  is the correct home per the architecture rules.

### Algorithms and edge cases
- The stepping algorithm and `CanExecute` rules are specified in the
  Decisions section. Edge cases (valid in-range, at-limit, out-of-range
  parseable, invalid/empty, busy) each map to one or more scenarios in the
  delta spec and one or more tests.

### Sequencing
1. Add the view-model commands and helper; wire state invalidation.
2. Add framework-free tests; confirm green.
3. Add the two buttons to the AXAML with accessible names and bindings.
4. Add headless view tests for presence, names, order, and disabled states.
5. Run the full baseline: `dotnet test .\FusionCanvas.sln`.
6. Run `openspec validate add-ideation-count-stepper --strict` and
   `openspec validate --all --strict`.
7. Write `verification.md` mapping every delta scenario to its test
   evidence.

### Test locations
- Framework-free view-model tests: `tests/FusionCanvas.App.Tests/Ideation/`.
- Headless view tests: existing ideation headless test file under
  `tests/FusionCanvas.App.Tests/` (locate via the existing ideation view
  tests during implementation).

### Migration / compatibility
- No persistence, schema, or settings migration. The count is transient
  dialog state.
- No breaking API changes. The two new commands are additive.

### Decisions not to reopen
- Do not replace the `TextBox` with `NumericUpDown`.
- Do not change the count range, default, or error message.
- Do not add repeat-on-hold or keyboard-acceleration behavior.

## Acceptance-to-Verification Mapping

| Delta scenario | Verification method |
|---|---|
| Dialog opens for a group (arrows visible beside count field) | Headless view test: open dialog, assert two buttons exist next to the count `TextBox`. |
| Dialog opens at niche root | Existing regression coverage; no new assertion needed. |
| User enters an invalid count (arrows stay enabled) | Framework-free view-model test: set `CountText` to invalid, assert both commands' `CanExecute` is true. |
| Click up from valid in-range | Framework-free test: `CountText = "5"`, invoke `IncrementCountCommand`, assert `CountText == "6"`. |
| Click down from valid in-range | Framework-free test: `CountText = "5"`, invoke `DecrementCountCommand`, assert `CountText == "4"`. |
| Up disabled at max | Framework-free test: `CountText = "20"`, assert `IncrementCountCommand.CanExecute` is false. Headless: assert up button is disabled. |
| Down disabled at min | Framework-free test: `CountText = "1"`, assert `DecrementCountCommand.CanExecute` is false. Headless: assert down button is disabled. |
| Up recovers invalid text | Framework-free test: `CountText = "abc"`, invoke up, assert `CountText == "5"`. |
| Down recovers invalid text | Framework-free test: `CountText = ""`, invoke down, assert `CountText == "1"`. |
| Arrow clamps out-of-range parseable then steps | Framework-free tests: `"25"` + down → `"19"`; `"-3"` + up → `"2"`; `"25"` + up → `"20"` (clamped, no step beyond); `"-3"` + down → `"1"` (clamped). |
| Arrows disabled while batch running | Framework-free test: start a generation (or simulate `IsBusy`), assert both commands' `CanExecute` is false. Headless: assert both buttons disabled during busy. |
| Keyboard order includes arrows between count and Generate | Headless view test: traverse focus order, assert arrows appear between the count `TextBox` and the Generate button. |
| Accessible names | Headless view test: assert `AutomationProperties.Name` on both buttons. |
| Candidate action completes (focus) | Existing regression; no new assertion. |
| Theme changes | Existing regression; no new assertion. |
