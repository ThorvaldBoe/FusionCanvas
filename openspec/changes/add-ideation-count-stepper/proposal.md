## Why

The Ideation dialog's "Number of ideas" field is a plain text box. Typing a
number is fine, but the count is a small, frequently adjusted value with a
narrow range (1–20) and a default of 5, so a quick click-stepper is the
natural fit. Today the user must type and may trip the "Enter a whole number
from 1 to 20" error for trivial off-by-one adjustments that arrow buttons
would avoid. This module adds count-up and count-down arrow buttons next to
the existing text field so the count can be adjusted with a single click
while preserving free-text entry and the existing range validation.

## What Changes

- Add two arrow buttons (up and down) immediately beside the existing
  "Number of ideas" `TextBox` in the Ideation dialog.
- The up arrow increments the desired count by one within the accepted
  1–20 range; the down arrow decrements by one within the same range.
- The text field, its `[1, 20]` range, the default of 5, the invalid-count
  error message, and Generate gating behavior are unchanged. Users can still
  type any value; invalid text continues to produce the existing range
  guidance and keeps Generate unavailable.
- When the current text is a parseable integer inside the range, the arrows
  step from that value. When it is parseable but out of range, the arrows
  clamp first, then step. When the text is empty or not a complete valid
  number, the up arrow sets the count to the default (5) and the down arrow
  sets it to the minimum (1), after which further clicks step normally.
- The up arrow is disabled (and a no-op when invoked by keyboard) when the
  current count is already at the maximum (20); the down arrow is disabled
  when the current count is already at the minimum (1). When the current
  text is invalid, both arrows are enabled so the user can recover by
  clicking.
- The arrows follow the dialog's existing busy/disabled rules: they are
  disabled while a generation batch is running, alongside the existing
  count `TextBox`, mode selector, guidance field, and Generate button.
- The arrows are keyboard reachable in the dialog's logical control order,
  placed between the count `TextBox` and the Generate button, and carry
  accessible names ("Increment idea count" / "Decrement idea count").

## Capabilities

### New Capabilities
<!-- None. The ideation capability is introduced by the in-progress `add-ideation-tool` change. -->

### Modified Capabilities
- `ideation`: The count-input requirement gains observable behavior for
  arrow-stepper buttons that adjust the desired candidate count within the
  existing 1–20 range, including clamping, invalid-text recovery, disabled
  limit states, and busy-state disabling. The keyboard-reachability
  requirement is extended to place the new arrows in the tab order with
  meaningful accessible names.

## Impact

- **App layer** — `src/FusionCanvas.App/Ideation/IdeationViewModel.cs`:
  new `IncrementCountCommand` and `DecrementCountCommand` (or equivalent)
  with `CanExecute` tied to the current count, busy state, and limit;
  helpers to compute the next count text from the current text using the
  existing `MinimumCount`/`MaximumCount`/`DefaultCount` constants and
  `TryGetCount`.
- **App layer** — `src/FusionCanvas.App/Ideation/IdeationWindow.axaml`:
  two `Button` controls with arrow glyphs beside the existing count
  `TextBox`, bound to the new commands, sharing the `IsNotBusy` disable
  state and carrying `AutomationProperties.Name`.
- **Tests** — `tests/FusionCanvas.App.Tests/Ideation/`: framework-free
  view-model tests for step direction, clamping, invalid-text recovery,
  limit-disabled `CanExecute`, and busy-disabled `CanExecute`; plus
  Avalonia headless view coverage for button presence, bindings, and
  disabled state at the limits in
  `tests/FusionCanvas.App.Tests/Settings`/existing ideation view tests
  location.
- **Dependencies** — The `ideation` capability is not yet in
  `openspec/specs/`; it is introduced by the in-progress
  `add-ideation-tool` change. This change therefore declares an
  archive-ordering dependency on `add-ideation-tool` and must be archived
  after it. No other capabilities, persistence, or external services are
  affected.

## Verification Approach

- Framework-free view-model tests cover every observable stepper behavior:
  increment, decrement, clamping at both limits, recovery from invalid and
  empty text, and `CanExecute` states across busy/not-busy and at-limit
  conditions.
- Avalonia headless view tests confirm both arrow buttons render beside the
  count field, bind to the right commands, report the agreed accessible
  names, are keyboard reachable in order, and reflect disabled state at the
  limits and during generation.
- The existing ideation view and view-model regression suites remain green
  to prove free-text entry, range validation, the invalid-count error, and
  Generate gating are unchanged.
- `openspec validate add-ideation-count-stepper --strict` and
  `openspec validate --all --strict` pass.

## Scope and Reviewability

This is one cohesive, narrowly scoped UI module: two buttons, their
commands, and their tests, all inside the existing Ideation dialog. It
introduces no new capability, no persistence, no external integration, and
no workflow change. It is independently verifiable through focused
view-model and headless view tests. The only cross-change concern is the
archive-ordering dependency on `add-ideation-tool`, which is recorded
explicitly.
