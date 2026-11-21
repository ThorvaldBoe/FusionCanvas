## 1. Align the change package

- [x] 1.1 Reconcile the active `add-in-app-updates` proposal/design placement wording with the global utility rail, preserving its existing Stable/Preview policy and update-flow requirements.
- [x] 1.2 Review this proposal, delta specs, and design together; correct any scope or acceptance mismatch before implementation.

## 2. Separate workspace context from application Settings

- [x] 2.1 Replace the passive active-workspace header label with a keyboard-accessible selector for active workspaces and a separate `Manage workspaces` action.
- [x] 2.2 Route workspace selection through the existing workspace-management transition and verify store/context reset, selected-workspace persistence, and archived-workspace exclusion.
- [x] 2.3 Route `Manage workspaces` to the existing single workspace-management window; preserve its create, edit, import/export, archive, restore, delete confirmation, and focus-return behavior.
- [x] 2.4 Remove workspace administration from the Settings section list and present the telemetry controls as `Workspace Diagnostics` with active-workspace identity and a no-workspace state.
- [x] 2.5 Preserve app-wide AI configuration, appearance, terms, update-track selection, and About/update controls in the existing single Settings window.
- [x] 2.6 Add focused view-model tests for selector choices, switch behavior, manager routing, archived/no-workspace cases, and workspace-scoped diagnostic refresh/isolation.

## 3. Add the global utility rail

- [x] 3.1 Add the fixed-width global rail beside the resizable workspace pane and document surface, keeping workspace actions out of the rail and deferring Home.
- [x] 3.2 Move the Settings shortcut to the bottom of the rail and bind it to the existing single-window Settings command and focus-return flow.
- [x] 3.3 Move the existing actionable update control above Settings; verify it is hidden without a compatible update, invokes the current download command, and follows the selected Stable/Preview track.
- [x] 3.4 Add Avalonia headless view coverage for rail construction, visible controls, command bindings, accessible names/tooltips, keyboard focus, and minimum supported window layout.

## 4. Verify acceptance and regressions

- [x] 4.1 Map every scenario in the three delta specs to focused tests or explicit source/UI inspection evidence; fix any failed criterion and rerun its relevant regression checks.
- [x] 4.2 Run focused `FusionCanvas.App.Tests` coverage for MainWindow, workspace management, Settings, and workspace telemetry.
- [x] 4.3 Run `dotnet test .\\FusionCanvas.sln`.
- [x] 4.4 Run strict OpenSpec validation for `add-global-utility-sidebar` and the reconciled `add-in-app-updates` change; review changed-scope drift, persistence boundaries, accessibility, and focus behavior.
- [x] 4.5 Record criterion-level results, test commands, limitations, and rationale for not adding a new Appium journey in the change verification evidence.
