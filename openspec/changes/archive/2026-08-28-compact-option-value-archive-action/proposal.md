## Why

The focused Option-values dialog repeats a large, strongly styled destructive button on every value row, so routine value management is visually dominated by archive actions. The dialog now exists through issue #194, making this a bounded follow-up that can improve hierarchy without changing catalog lifecycle behavior.

## Origin

- Primary issue: [#195](https://github.com/ThorvaldBoe/FusionCanvas/issues/195)

## What Changes

- Replace each row's **Archive Option Value** presentation with a compact **Archive** action aligned consistently at the row edge.
- Preserve the existing archive command, eligibility, dependency safeguards, persistence, and recoverable errors.
- Give each action a target-specific accessible name such as **Archive Black** while keeping the concise visible label.
- Keep long value names readable without colliding with the action at supported dialog widths.
- Add focused Avalonia headless coverage for Color, Size, and custom Option values, layout hierarchy, command binding, accessible naming, and keyboard order.

This is one cohesive, independently verifiable presentation module. It depends on the completed focused Option-values dialog (#194). It does not redesign Option-level archiving, introduce cascade behavior, or change confirmation and persistence rules.

The workflow is an occasional destructive action inside an already focused management dialog. It remains directly discoverable per row but visually secondary to dialog completion and value creation. Loading and persistence states are unchanged; blocked/error behavior continues through the existing command path.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `variant-management`: Define the compact, secondary, target-specific archive action used by Option Value rows in the focused dialog.

## Impact

- `src/FusionCanvas.App/Stores/OptionValueManagementWindow.axaml`
- A small presentation projection or converter only if required for accessible naming; no Domain, Application, Integration, or persistence changes are planned.
- `tests/FusionCanvas.App.Tests/StoreEditorHeadlessTests.cs`
- Accepted `variant-management` behavior and the dialog's UI description if its row action label is represented there.
- No new package or external-service dependency.
