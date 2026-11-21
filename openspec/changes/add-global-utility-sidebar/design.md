## Context

The main shell currently has a resizable workspace navigation pane beside the document surface. Its header combines the app name, a passive active-workspace label, an update action, and Settings. The Settings window has one section rail; its Workspace section currently mixes active workspace administration with genuinely workspace-scoped telemetry controls. AI configuration and update-track selection are stored in the app-wide settings document. The existing workspace manager already owns switching, creation, editing, transfer, archive, restore, and delete behavior.

An active OpenSpec change, `add-in-app-updates`, defines the update flow and currently describes its action as beside existing shell controls. This change establishes the final shell location as the utility rail. Before either overlapping shell implementation is considered complete, reconcile the active updater design/spec wording with this location. The update service's track, discovery, download, verification, and installer policies remain owned by the updater capability.

## Goals / Non-Goals

**Goals:**

- Establish a compact global utility rail at the leading edge of the existing main window.
- Put Settings at the bottom of the rail and show the existing update action directly above it only when actionable for the selected Stable or Preview track.
- Keep the workspace name, workspace switching, and workspace administration in the workspace navigation pane, outside the rail.
- Provide quick selection of active workspaces while keeping the existing workspace-management window as the sole owner of workspace administration and destructive actions.
- Keep one Settings window for application-wide preferences, including AI configuration and update-track selection; identify workspace diagnostics as scoped to the active workspace.
- Defer Home until a separate home destination is defined.

**Non-Goals:**

- Adding a dashboard, Home destination, plugin/tool destinations, or workspace actions to the utility rail.
- Changing AI preference ownership, adding per-workspace AI profiles, or moving AI configuration out of the app-wide settings document.
- Changing update release channels, version selection, background checks, downloads, checksums, or installer handoff.
- Duplicating workspace create/edit/import/export/archive/restore/delete workflows in the shell or selector.
- Changing workspace/database schemas, migration behavior, workspace selection persistence, or document-tab behavior.
- Adding a new settings window or a second Settings button.

## Decisions

### The leading rail is global utility, not workspace navigation

Add a compact rail as a sibling of the current resizable navigation pane. It owns app-wide controls only. Settings is anchored at the bottom. The actionable update control sits immediately above Settings and is hidden when the selected update track has no compatible update. Other rail positions remain empty until FusionCanvas has defined global destinations for them; no placeholder Home button is added.

Alternatives considered:

- Put the update action in the navigation header beside the workspace label — rejected because it keeps unrelated app-wide and workspace-scoped controls mixed and does not establish a clear global rail.
- Add a Home icon that returns to the current document view — rejected because there is no distinct Home state or agreed reset behavior; a named control without a unique destination would be misleading.
- Add workspace management to the rail — rejected because users encounter and act on workspace scope in the workspace pane, and the rail is deliberately global.

The rail uses a fixed compact width (target approximately 48–56 px) and does not replace the workspace pane splitter. Update visibility changes do not resize or reorder the Settings target. Layout constraints must preserve the existing document and navigation minimum widths at the current minimum main-window dimensions.

### The workspace control combines selection discovery with a management entry point

Turn the passive workspace name/folder indicator at the top of the navigation pane into a keyboard-accessible control. It presents active workspace choices and a distinct `Manage workspaces` action. Selecting an active workspace uses the existing `WorkspaceManagementViewModel` selection path so store reset, navigation refresh, tab behavior, and persisted active workspace identity remain consistent. `Manage workspaces` opens/focuses the existing dedicated window. The selector does not edit workspace fields or expose archived workspaces as active choices.

Alternatives considered:

- Keep the current label passive and require Settings to open management — rejected because it hides workspace actions in a global preference surface.
- Inline workspace editing and destructive actions in the selector — rejected because these are occasional administration tasks and already have a focused management window with confirmation behavior.
- Create a separate workspace Settings window — rejected because it adds another settings surface and duplicates concepts already owned by the workspace manager.

On workspace switch, close the selector and return focus to the workspace control after the existing transition completes. If no active workspace exists, display `No workspace` and preserve the manage action. Loading/error and first-workspace states continue to use the current workspace-management behavior.

### The Settings window remains application-wide, with scoped diagnostics explicitly identified

Keep one Settings window and its internal section navigation. Keep General, AI, Terms, and About as application-wide sections (including update-track selection and manual update controls where they currently reside). Remove the workspace administration section. Present the existing telemetry controls under a clearly named `Workspace Diagnostics` section that shows the active workspace name and becomes unavailable when no workspace is selected. When the active workspace changes while Settings is open, reload the telemetry settings for that workspace before presenting its controls or records.

AI settings remain global because `ApplicationSettings.Ai` is persisted in the local app settings document and its view model does not partition profiles by workspace. Per-workspace AI profiles are deferred and require a separate product decision if needed.

Alternatives considered:

- Move workspace administration into global Settings as another section — rejected because it perpetuates the scope ambiguity and forces users to enter Settings for a workspace action.
- Move workspace diagnostics into the manager window — rejected for this module because it is a diagnostic/settings task, not an administrative workspace lifecycle action; a clearly labeled scoped section retains the existing discovery path without conflating it with workspace administration.
- Move AI profiles into each workspace — rejected because no accepted behavior or persistence model currently supports per-workspace AI configuration.

### Preserve update capability ownership and coordinate the active change

The rail binds to the existing update presentation state and command. `Update available` begins the current explicit download flow; in-progress presentation follows the existing updater behavior. Track selection remains in the About/updates area and continues to drive checks for Stable or Preview. This change does not change whether discovery succeeds or whether an update may install.

Before implementation, update the active `add-in-app-updates` change's placement description to point to the utility rail, so the two OpenSpec changes do not prescribe conflicting UI locations. Do not copy or redefine update verification/install requirements in this change beyond the rail's visibility and command-binding contract.

### User-facing verification is deterministic

Use framework-free view-model tests for workspace choice and command-routing state. Use Avalonia headless view tests where binding, menu/flyout state, accessible naming, focus return, Settings section visibility, or layout constraints are material. Static icon markup alone does not need a separate test. No new Appium journey is warranted: there is no new native integration, and the affected selection and management paths can be verified in headless views and focused view-model tests. Optional manual visual review may confirm the compact rail proportions but is not a completion gate.

## Risks / Trade-offs

- **[The new rail reduces horizontal workspace space]** → Keep it compact, preserve the existing navigation splitter, and verify the minimum supported window dimensions in headless layout coverage.
- **[Users could confuse the workspace picker with workspace editing]** → Show active choices separately from a clearly labeled `Manage workspaces` command and keep all editing/destructive actions in the existing manager.
- **[Settings scope remains mixed because diagnostics are workspace-specific]** → Label that section `Workspace Diagnostics`, identify the active workspace in the section, disable controls with no active workspace, and refresh scope on workspace changes.
- **[The active updater change describes a different button location]** → Reconcile its design/spec text before implementation and use the same update view model/command.
- **[Moving Settings control can disrupt focus return or single-window behavior]** → Preserve the existing Settings window coordinator and test open/focus/close behavior from the rail.
- **[Workspaces change while Settings diagnostics are open]** → Reuse workspace change notification to update the visible identity and reload scoped telemetry; guard against late results from the previous workspace.

## Migration Plan

No user data migration is expected. The active workspace, selected store, AI profiles, appearance, and update track retain their current storage and semantics. The Settings `Workspace` section is replaced in presentation with `Workspace Diagnostics`; old settings files remain readable because section selection is transient rather than persisted. Rollback is a code rollback; no database or settings migration is needed.

Implementation sequencing:

1. Reconcile the shell placement in the active `add-in-app-updates` planning artifact.
2. Add the workspace pane selector and `Manage workspaces` entry using existing workspace management state and verify scope/focus behavior.
3. Add the utility rail and move Settings/update controls into their global positions.
4. Split workspace administration from the Settings section list and label/reload scoped diagnostics.
5. Add focused VM and Avalonia headless coverage, then validate all scenarios and the repository baseline.

## Open Questions

None. The rail is global-only; no Home action is included; workspace selection and management stay in the navigation context; AI remains app-wide; and workspace diagnostics remain explicitly scoped within the single Settings window.

## Implementation Plan

### Affected layers and responsibilities

- **App only:** This is a shell and presentation-boundary change. No Domain, Application, or Integration changes are expected.
- `src/FusionCanvas.App/Views/MainWindow.axaml`: Add the utility rail column and host it as a sibling of the existing workspace pane and document content. Move the existing Settings and update controls into the rail, preserve control commands, place Settings at the bottom, and retain the workspace pane's resizable column.
- `src/FusionCanvas.App/Views/MainWindow.axaml.cs` and `MainWindowWindowCoordinator.cs`: Preserve one Settings window, invoking-control focus return, and workspace-management window activation/focus behavior. Reuse the existing coordinator rather than creating new window ownership.
- `src/FusionCanvas.App/Workspace/WorkspaceManagementViewModel.cs`: Expose selector data/commands suitable for the navigation-pane control only if the current surface API is insufficient. Reuse active-workspace selection and `OpenWorkspaceManagementCommand`; do not duplicate management operations.
- `src/FusionCanvas.App/Views/MainWindow.axaml` and `MainWindowViewModel.cs`: Bind the workspace control to the selected active workspace and active choice list. Switching follows existing `SelectWorkspaceCommand`/`SelectWorkspaceAsync`, lets the current workspace-switch coordinator reset child context, and closes the selector. Keep `Manage workspaces` separate from choosing a workspace.
- `src/FusionCanvas.App/Settings/SettingsSection.cs`, `SettingsViewModel.cs`, and `SettingsWindow.axaml`: Replace the Workspace administration section with a clearly scoped Workspace Diagnostics section. Retain AI/global settings and update controls in existing settings areas. Remove `ManageWorkspacesCommand` routing from Settings. Keep the diagnostics view model connected to current active-workspace notifications and show the name in its section.
- `src/FusionCanvas.App/Settings/WorkspaceTelemetrySettingsView.axaml` and `WorkspaceTelemetrySettingsViewModel.cs`: Add or bind the visible active workspace identity and ensure workspace changes cancel/ignore stale search/export/delete results where existing async behavior requires it; do not change telemetry retention semantics.
- Existing tests: `tests/FusionCanvas.App.Tests/MainWindowViewModelTests.cs`, `WorkspaceManagementViewModelTests.cs`, `Settings/SettingsWindowTests.cs`, `Settings/WorkspaceTelemetrySettingsTests.cs`, and the main-window headless fixture/view tests. Add a focused headless MainWindow rail/selector test in the existing UI test conventions rather than testing static XAML in isolation.

### UI behavior and edge cases

- Use a fixed-width leading rail, visually distinct from the resizable workspace pane. Preserve the existing minimum document width and navigation-pane min/max behavior at the 900×600 minimum main window.
- Keep Settings anchored at the rail bottom. Keep the update action above Settings, hidden until `IsUpdateActionVisible`; use the existing label, command, tooltip, and accessible automation identity. When no update exists, leave the rail's bottom Settings position stable.
- Use descriptive tooltips and `AutomationProperties.Name` for icon-only rail controls. Tab order follows visual order, and Settings closure returns focus to the rail Settings button.
- Workspace control shows the selected name (ellipsis where necessary) and opens a selector with active workspaces and `Manage workspaces`. Empty state reads `No workspace`; if no active choices exist, Manage remains available. Selection does not expose archived items; those remain managed in the dedicated window.
- After selecting a workspace, use existing transition behavior to clear/reselect store and navigation context. Close the selector and return focus to its invoking control. No second workspace-switch implementation is introduced.
- Workspace Diagnostics identifies its current workspace. On switch while open, update identity immediately and load only the new workspace's telemetry settings. A late async response for an old workspace must not overwrite the current view. No-workspace controls remain unavailable and explanatory.
- Removing the Workspace settings section must not change the General, AI, Terms, About, dark-mode, update-track, or updater status behavior.

### Data, persistence, compatibility, and migration

- No schema changes or settings document migration.
- Continue persisting active workspace ID, active store ID, AI settings, appearance, and update track through existing application settings.
- `SettingsSection` selection is not persisted; new sessions continue selecting General.
- Any telemetry response handling remains guarded by workspace identity so data is not presented under the wrong workspace.

### Sequence and verification

1. Update the active updater change's placement wording; confirm its Stable/Preview policy remains intact.
2. Implement workspace selector shell state/entry point and framework-free selector/view-model tests.
3. Introduce rail layout and move existing Settings/update bindings; add headless assertions for rail controls, visibility, accessible names, minimum layout, and focus return.
4. Split Settings section composition; add focused Settings/diagnostics tests for section navigation, active workspace identity, workspace change, no-workspace state, and stale results.
5. Run focused App tests, full `dotnet test .\\FusionCanvas.sln`, strict validation for this change and `add-in-app-updates`, then complete scoped changed-area review.

### Acceptance-to-verification mapping

| Acceptance scenario | Planned evidence |
| --- | --- |
| Rail Settings opens/focuses the single Settings surface and restores focus | Avalonia headless MainWindow/window-coordinator test using the rail button |
| Rail excludes workspace actions | Headless visual-tree/binding test and source review of rail command ownership |
| Update visible for selected track, hidden otherwise, and starts existing command | UpdateViewModel tests plus Avalonia headless rail binding test with fake update states for Stable and Preview |
| Rail keyboard/accessibility and minimum layout | Headless focus/accessibility assertions and layout construction at 900×600 |
| Workspace selector shows active choices and management | WorkspaceManagementViewModel tests plus headless selector test |
| Switching workspace updates scope and returns focus | Existing workspace-switch tests extended for selector invocation; headless selection/focus test |
| No-workspace state retains management route | View-model and headless empty-state test |
| Manager opens once, retains workflows, and closes with focus return | Existing coordinator/window tests extended from the pane's Manage command |
| Active choices exclude archived workspaces | WorkspaceManagementViewModel test and selector view assertion |
| Workspace Diagnostics identifies, refreshes, and isolates telemetry scope | `WorkspaceTelemetrySettingsTests` plus SettingsWindow headless test for active, changed, and absent workspace |
| Settings sections distinguish app preferences and diagnostics | SettingsWindow headless section navigation test and SettingsViewModel tests confirming AI/update track remain global |
