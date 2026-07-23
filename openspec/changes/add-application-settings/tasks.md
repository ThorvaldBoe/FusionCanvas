## 1. Prerequisites and Baseline

- [ ] 1.1 Confirm `basic-product-creation-workflow` is completed and verified, inspect its final MainWindow changes, and stop for artifact reconciliation if they invalidate this package's shell assumptions.
- [ ] 1.2 Confirm the separately approved Avalonia 12 headless package and shared fixture are present in `FusionCanvas.App.Tests`; stop and deliver that prerequisite change if the harness is absent.
- [ ] 1.3 Run `dotnet test .\FusionCanvas.sln` before feature edits and record the clean baseline or resolve any pre-existing failure with the user.

## 2. Application Settings Persistence Boundary

- [ ] 2.1 Add the focused `ApplicationSettings`, `IApplicationSettingsStore`, and recoverable load/save result types under `FusionCanvas.Application.Settings` without adding Domain behavior.
- [ ] 2.2 Implement `JsonApplicationSettingsStore` under `FusionCanvas.Integration.Settings` with version-1 JSON validation, missing/invalid/unreadable fallback, cancellation, same-directory temporary writes, and atomic replacement.
- [ ] 2.3 Add App composition for `%LOCALAPPDATA%\FusionCanvas\settings.json` plus the `FUSIONCANVAS_SETTINGS_PATH` isolation override, keeping workspace database and metadata untouched.
- [ ] 2.4 Add isolated Integration tests for missing, valid Light/Dark, unknown-property, invalid JSON, wrong-shape, unsupported-version, replacement, cancellation, and deterministic read/write failure behavior.

## 3. Settings and Theme Presentation State

- [ ] 3.1 Add the App-layer `IApplicationThemeController` and Avalonia implementation that selects explicit Light or Dark through `Application.RequestedThemeVariant`.
- [ ] 3.2 Add `SettingsSection` and a focused `SettingsViewModel` with General default selection, open/close state, active/no-workspace projection, existing workspace-management delegation, and inline load/save feedback.
- [ ] 3.3 Implement immediate UI-thread theme application and serialized generation-based latest-wins persistence, including shutdown flush and protection from stale completion errors.
- [ ] 3.4 Refactor the App composition root to load settings and apply the selected theme before constructing the main window, while injecting one long-lived Settings view model without moving workspace business behavior.
- [ ] 3.5 Add framework-free App tests for section state, reopen reset, theme controller calls, load warnings, save failures, rapid-toggle latest-wins behavior, shutdown flush, workspace projection, and management-command delegation.

## 4. Shared Light and Dark Theme Resources

- [ ] 4.1 Add complete Light and Dark semantic theme dictionaries to `App.axaml`, make Light the explicit no-preference default, and cover surfaces, text, borders, commands, selection, hover, disabled, informational, success, warning, and destructive states.
- [ ] 4.2 Convert `MainWindow.axaml` appearance-bearing colors, template fallback values, tree/tab/stage states, and styles to dynamic semantic resources while preserving intentional data-derived colors only with documented rationale.
- [ ] 4.3 Convert `WorkspaceManagementWindow.axaml` and `StoreEditorWindow.axaml` to the same semantic resources without changing their existing editing, validation, archive, restore, or deletion behavior.
- [ ] 4.4 Convert `AssetsWindow.axaml` and `GroupDeleteConfirmationWindow.axaml` to the same semantic resources without changing their existing commands or confirmation behavior.
- [ ] 4.5 Inspect theme-sensitive converters and content-color fallbacks, correct unreadable Light/Dark foreground pairs, and run a direct-color inventory that identifies and justifies every remaining application XAML color literal.

## 5. Settings Window and Main Shell

- [ ] 5.1 Add the compiled-binding `SettingsWindow` with an accessible vertical General/Workspace selector, one visible pane, Dark mode ToggleSwitch, inline persistence feedback, current/no-workspace label, and `Manage workspaces` button.
- [ ] 5.2 Implement the Settings window's predictable keyboard order, initial focus, normal dismissal, and focus-return hooks without Save, Apply, Cancel, or unsaved-changes behavior.
- [ ] 5.3 Replace the old Workspaces block in `MainWindow.axaml` with the approved four-column header containing FusionCanvas identity, constrained workspace PathIcon/name indicator, exact tooltip, and upper-right Settings button.
- [ ] 5.4 Extend the existing MainWindow presentation coordinator to enforce one owned modeless Settings window, activate an existing instance, reset General on a new open, and return focus to the Settings button on close.
- [ ] 5.5 Route `Manage workspaces` through the existing `WorkspaceManagementViewModel`, enforce one management window owned/focused relative to Settings, retain the Workspace section, and restore focus to the invoking button on close.
- [ ] 5.6 Verify active-workspace notifications update Settings and the shell indicator from the existing authoritative state, including `No workspace`, without adding any workspace mutation command to Settings.

## 6. Deterministic UI and Regression Tests

- [ ] 6.1 Add focused Avalonia headless tests for Settings construction and compiled bindings, General default/reopen, section selection and pane visibility, keyboard navigation, ToggleSwitch propagation, and Settings close focus return.
- [ ] 6.2 Add headless tests that switch Light/Dark with MainWindow, Settings, and a management window open and assert representative semantic resources for normal, selection, disabled, warning, danger, and accent states update coherently.
- [ ] 6.3 Add headless shell tests at the navigation pane's minimum width for workspace icon/name ellipsis, exact tooltip, Settings availability, `No workspace`, and absence of the former Workspaces label/cog row.
- [ ] 6.4 Add coordinator tests for unique Settings and workspace-management instances, existing-instance activation where deterministic, ownership/return flow, and retained Workspace section.
- [ ] 6.5 Extend MainWindow and workspace-management regression tests for active-workspace switching, store/tree refresh, synchronized settings/header names, preserved lifecycle commands, and theme independence from workspace persistence.

## 7. Documentation and Criterion-Level Verification

- [ ] 7.1 Update `docs/ui-guidelines.md` so FusionCanvas supports coherent user-selected Light and Dark appearances with Light as the initial default, without changing unrelated UI direction.
- [ ] 7.2 Create `verification.md` and copy every acceptance scenario from the three delta specs into a criterion-level evidence table before running final gates.
- [ ] 7.3 Execute each mapped focused test or inspection, record its result and material evidence, and correct implementation or artifacts when a criterion fails instead of relying on an aggregate pass.
- [ ] 7.4 Review proposal, specs, design, tasks, and implementation for drift; correct the authoritative change artifacts when approved behavior or implementation detail has changed.

## 8. Validation and Completion QA

- [ ] 8.1 Run focused Application, Integration, App view-model, and App headless tests for the changed settings, persistence, theme, shell, and workspace-management surfaces.
- [ ] 8.2 Run `dotnet test .\FusionCanvas.sln` and record the deterministic solution-level result in `verification.md`.
- [ ] 8.3 Run strict validation for `add-application-settings` and all active OpenSpec changes, correct every validation error, and rerun until clean.
- [ ] 8.4 Run `git diff --check` and the final direct-color inventory; record intentional remaining literals or remove accidental ones.
- [ ] 8.5 Perform scoped completion QA for Clean Architecture boundaries, application-data isolation, atomic preference writes, latest-wins behavior, theme consistency, compact layout, keyboard/focus behavior, existing workspace regressions, and changed-scope spec drift.
- [ ] 8.6 Finalize `verification.md` with every criterion's result, commands, evidence, limitations, and any optional supplemental visual observation; do not mark the module complete while a mandatory criterion is missing or failed.
