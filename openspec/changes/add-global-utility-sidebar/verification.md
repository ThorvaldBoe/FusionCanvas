# Verification

## Acceptance scenario evidence

| Delta requirement / scenario | Verification evidence | Result |
|---|---|---|
| Global utility rail / User opens global Settings | `SettingsWindowTests.ShellHeader_ActivatingSettingsOpensOneOwnedSettingsInstance`; `MainWindowWindowCoordinator` source review for focus restoration after close | Pass |
| Global utility rail / Rail contains no workspace-scoped actions | `SettingsWindowTests.MainShell_ShowsGlobalSettingsRailAndWorkspaceContextControl`; `MainWindow.axaml` source review confirms selector and manager are in the workspace pane | Pass |
| Actionable updates / Update is available on selected track | `SettingsWindowTests.UtilityRail_UpdateActionIsHiddenUntilAnUpdateIsAvailable` verifies visibility and existing download command binding | Pass |
| Actionable updates / No actionable update | `SettingsWindowTests.UtilityRail_UpdateActionIsHiddenUntilAnUpdateIsAvailable` verifies hidden state before an update is available | Pass |
| Actionable updates / Track changes | Existing `UpdateViewModelTests` and `SettingsWindowTests.AboutSection_ShowsUpdateCheckAndInstallControls`; reconciled update capability spec retains track policy | Pass |
| Keyboard and accessibility / User navigates rail with keyboard | `SettingsWindowTests.MainShell_ShowsGlobalSettingsRailAndWorkspaceContextControl` checks rail control construction, names, and tooltips; `MainWindow.axaml` review confirms declaration/tab order and focusable buttons | Pass; keyboard traversal reviewed in source, not exercised with native input |
| Keyboard and accessibility / Main window at minimum size | `SettingsWindowTests.MainShell_RailAndWorkspaceControlFitAtMinimumWidth` and `MainWindowLayoutTests` | Pass |
| Workspace diagnostics / Active workspace | `SettingsWindowTests.SettingsWindow_SelectingWorkspaceDiagnosticsShowsItsActiveScope`; `SettingsViewModelTests.WorkspaceProjection_ShowsActiveWorkspaceNameWhenAttached` | Pass |
| Workspace diagnostics / Active workspace changes while open | `WorkspaceTelemetrySettingsTests.WorkspaceChange_IgnoresSettingsLoadThatCompletesForPreviousWorkspace`; `SettingsViewModelTests.WorkspaceProjection_UpdatesWhenActiveWorkspaceChanges` | Pass |
| Workspace diagnostics / No active workspace | `SettingsWindowTests.WorkspaceDiagnostics_ShowsNoWorkspaceWhenNoneActive`; `SettingsViewModelTests.WorkspaceProjection_ShowsNoWorkspaceWhenNoneAttached` | Pass |
| Settings sections / Navigate sections | `SettingsWindowTests.SettingsWindow_ConstructsAndShowsGeneralPaneByDefault`; `SettingsViewModelTests.Sections_ExposeWorkspaceDiagnosticsAndKeepAppPreferencesTogether` | Pass |
| Workspace selector / Open selector | `SettingsWindowTests.WorkspaceSelector_OffersWorkspaceChoicesAndSeparateManagementAction` | Pass |
| Workspace selector / Switch active workspace | `WorkspaceManagementViewModelTests.CreateAndSelectWorkspace_UpdatesActiveWorkspace`; `MainWindowViewModelTests` workspace-selection coverage | Pass |
| Workspace selector / No workspace selected | `SettingsWindowTests.WorkspaceSelector_NoWorkspaceStillOffersManagement` | Pass |
| Workspace selector / Open workspace management | `WorkspaceManagementViewModelTests.WorkspaceManagementCommand_OpensAndClosesManagementWindow`; `SettingsWindowTests.WorkspaceSelector_OffersWorkspaceChoicesAndSeparateManagementAction` | Pass |
| Workspace selector / Close manager and return focus | `MainWindowWindowCoordinator` source review confirms main-window activation and selector focus on close | Pass; focus behavior is source-reviewed, not exercised by a native window test |
| Workspace selector / Active choices only | `WorkspaceManagementViewModelTests.SelectorWorkspaces_ContainsOnlyActiveWorkspacesAndMarksCurrentChoice` | Pass |

## Commands and results

- Focused app coverage: `dotnet .\tests\FusionCanvas.App.Tests\bin\Debug\net10.0\FusionCanvas.App.Tests.dll -class "FusionCanvas.App.Tests.Settings.SettingsWindowTests" -class "FusionCanvas.App.Tests.Settings.SettingsViewModelTests" -class "FusionCanvas.App.Tests.Settings.WorkspaceTelemetrySettingsTests" -class "FusionCanvas.App.Tests.WorkspaceManagementViewModelTests" -class "FusionCanvas.App.Tests.MainWindowLayoutTests" -class "FusionCanvas.App.Tests.MainWindowViewModelTests" -class "FusionCanvas.App.Tests.Settings.UpdateViewModelTests" -reporter quiet` — passed.
- Full baseline: `dotnet test .\FusionCanvas.sln` — passed; Domain 294, UI description 29, Application 724, Integration 465, and App 1,047 (2,559 passed; 0 failed; 0 skipped).
- Strict change validation: `openspec validate add-global-utility-sidebar --strict` and `openspec validate add-in-app-updates --strict` — passed.
- `git diff --check` — passed.

## Coverage decision and limitations

No new Appium journey was added. The change reuses existing Settings, updater, and workspace-management windows and commands; the changed shell layout and view bindings are covered by headless Avalonia tests. A separate desktop journey would add native-window lifecycle coverage but is not necessary to establish the current acceptance criteria. Focus restoration and tab traversal were inspected in the coordinator and view markup; the headless tests do not simulate operating-system focus traversal or native window activation.
