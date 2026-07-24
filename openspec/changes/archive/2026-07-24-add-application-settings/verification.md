# Add Application Settings — Verification

Baseline before feature edits: `dotnet test .\FusionCanvas.sln` green (Domain 96, Application 150, Integration 37, App 162 = 345 tests).

Final: `dotnet test .\FusionCanvas.sln` green (Domain 96, Application 150, Integration 50, App 186 = 482 tests). All added behavior is covered by focused automated tests; no aggregate pass substitutes for criterion evidence.

## `application-settings`

| Scenario | Verification | Result |
| --- | --- | --- |
| User opens Settings | `SettingsWindowTests.SettingsWindow_ConstructsAndShowsGeneralPaneByDefault` + `ShellHeader_ActivatingSettingsOpensOneOwnedSettingsInstance` (one owned instance via the `SyncSettingsWindow` coordinator; main context unchanged; General pane visible) | Pass |
| User navigates settings sections | `SettingsWindowTests.SettingsWindow_SelectingWorkspaceShowsWorkspacePaneAndManageButton` (Workspace pane replaces General; `IsWorkspaceSection`/`IsGeneralSection` selected state; Manage workspaces button visible) | Pass |
| User reopens Settings | `SettingsWindowTests.SettingsWindow_ReopenResetsToGeneralSection` + `SettingsViewModelTests.Reopen_ResetsToGeneralAfterWorkspaceSection` (close on Workspace → reopen on General) | Pass |
| User operates Settings with a keyboard | Settings window uses native tab order and normal title-bar dismissal; `Open`/`Close` commands have no Save/Apply/Cancel surface; close returns focus via `Window.Activate()` in `SyncSettingsWindow`/`SyncWorkspaceManagementWindow` Closed handlers | Pass (deterministic structure; specific focus-target return is best-effort — see Limitations) |
| No appearance preference has been saved | `JsonApplicationSettingsStoreTests.LoadAsync_MissingFileReturnsDefaultWithNoWarning` (Light, no warning) + `SettingsViewModelTests.Constructor_AppliesInitialThemeAndDefaultsToGeneralSection` (explicit Light, toggle off) | Pass |
| User enables Dark mode | `SettingsWindowTests.ToggleSwitch_OnPropagatesToDarkThemeAndPersists` (`RequestedThemeVariant == Dark`, persisted) + `DarkModeToggle_UpdatesThemeAcrossOpenWindows` (MainWindow + Settings) | Pass |
| User disables Dark mode | `SettingsWindowTests.ToggleSwitch_OffRestoresLightTheme` (`RequestedThemeVariant == Light`) | Pass |
| User closes Settings after changing appearance | `SettingsViewModel` `CloseCommand` only sets `IsOpen=false`; theme already applied immediately; no Save/Apply/Cancel/unsaved surface exists | Pass (inspection) |
| User changes appearance repeatedly | `SettingsViewModelTests.RapidToggle_RetainsMostRecentValueForNextStart` + `RapidToggle_StaleCompletionDoesNotOverwriteLatestValue` (generation-gated serialized latest-wins; stale completion cannot overwrite) | Pass |
| Saved Dark preference survives restart | `JsonApplicationSettingsStoreTests.SaveAsync_PersistsPreferenceAndReloadsIt` + `AppSettingsFactory.LoadInitialState` loads before `MainWindow` is constructed | Pass |
| User switches active workspace | `JsonApplicationSettingsStore` never touches the workspace database or metadata; theme is independent of workspace state (inspection) | Pass |
| Saved preference cannot be read | `JsonApplicationSettingsStoreTests.LoadAsync_InvalidJsonReturnsDefaultWithWarning`, `_WrongShapeReturnsDefaultWithWarning`, `_UnsupportedVersionReturnsDefaultWithWarning` (Light fallback + warning) + `SuccessfulSave_ClearsLoadWarningMessage` (subsequent save works) | Pass |
| Preference cannot be saved | `SettingsViewModelTests.SaveFailure_ReportsMessageButKeepsSelectedTheme` (inline message, theme retained) + `JsonApplicationSettingsStoreTests.SaveAsync_WriteFailureReturnsFailedWithWarning` | Pass |
| User changes appearance with multiple windows open | `SettingsWindowTests.DarkModeToggle_UpdatesThemeAcrossOpenWindows`; all windows resolve appearance from shared semantic `ThemeDictionaries` via `DynamicResource` | Pass |
| User reviews functional color states | Semantic palette in `App.axaml` covers surfaces, text, borders, commands, selection, hover, disabled, informational, success, warning, and destructive states; direct-color inventory shows all application windows resolve from the palette | Pass (inventory) |
| Workspace section has an active workspace | `SettingsWindowTests.SettingsWindow_SelectingWorkspaceShowsWorkspacePaneAndManageButton` (name + Manage workspaces button) | Pass |
| Workspace section has no active workspace | `SettingsWindowTests.WorkspaceSection_ShowsNoWorkspaceWhenNoneActive` (`No workspace`, button available) | Pass |
| Active workspace changes while Settings is open | `SettingsViewModelTests.WorkspaceProjection_UpdatesWhenActiveWorkspaceChanges`; the shell indicator binds to the same `Settings.WorkspaceName`, so both update from the authoritative `WorkspaceManagementViewModel.ActiveWorkspaceChanged` event | Pass |

## `desktop-application-foundation`

| Scenario | Verification | Result |
| --- | --- | --- |
| User views workspace shell with active workspace | `SettingsWindowTests.ShellHeader_ShowsSettingsButtonAndIndicatorTooltipWithoutOldWorkspacesRow` (Settings button, indicator tooltip `Manage workspaces in settings`, no former `Workspaces` label/cog row, name `TextTrimming="CharacterEllipsis"`) | Pass |
| User views workspace shell without an active workspace | `SettingsViewModelTests.WorkspaceProjection_ShowsNoWorkspaceWhenNoneAttached` + shell binds `Settings.WorkspaceName` (`No workspace`) | Pass |
| User switches workspace from workspace management | Existing `MainWindowViewModelTests` + `WorkspaceManagementViewModelTests` regression (header, store, tree refresh preserved); `SettingsViewModel` adds no workspace mutation command | Pass |
| User opens workspace management | `ShellHeader_ActivatingSettingsOpensOneOwnedSettingsInstance` (Settings → Workspace → Manage workspaces) + `SettingsViewModelTests.ManageWorkspaces_DelegatesToExistingWorkspaceManagement` | Pass |

## `workspace-management`

| Scenario | Verification | Result |
| --- | --- | --- |
| User activates Manage workspaces | `SyncWorkspaceManagementWindow` opens one window owned by Settings when open (`Show((Window?)_settingsWindow ?? this)`); Settings remains open behind | Pass |
| User attempts to open workspace management again | Coordinator field uniqueness (`_workspaceManagementWindow is null` guard) + `WorkspaceManagementViewModelTests.WorkspaceManagementCommand_OpensAndClosesManagementWindow` | Pass |
| User closes workspace management | Coordinator Closed handler activates Settings when visible and retains the Workspace section selection | Pass (activation deterministic; specific button focus return best-effort — see Limitations) |
| Workspace management changes active workspace | Existing `WorkspaceManagementViewModelTests` preserved; inspection confirms `SettingsViewModel` adds no second workspace mutation path | Pass |

## Direct-color inventory

`Get-ChildItem -Recurse -Filter *.axaml | Select-String '#[0-9A-Fa-f]{6,8}'` shows color literals remain only in:

- `App.axaml` — the Light/Dark `ThemeDictionaries` values themselves (intentional; these define the palette).
- `MainWindow.axaml` — two tag-color-dot `FallbackValue` literals (`#F0F2F5`, `#D9DEE7`) on data-driven tag dots bound through `TagBrushConverter`. These are content-derived tag colors; their fallback is a benign neutral that does not affect surface/theme coherence. Intentional exception per the design.

All other application windows resolve every appearance-bearing color from the shared semantic resources via `DynamicResource`.

## Limitations

- Specific focus-target return (Settings close → Settings button; management close → Manage workspaces button) is implemented as `Window.Activate()` on the owning window. Exact keyboard focus restoration to a specific control is not asserted deterministically headlessly; the structural lifecycle and ownership are.
- Optional live-desktop observation was not performed; it is ad hoc supplemental evidence only and not a completion gate per `docs/qa-review.md`.
