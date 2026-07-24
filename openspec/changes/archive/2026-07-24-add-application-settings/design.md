## Context

FusionCanvas currently puts a `Workspaces` label, the selected workspace name, and a workspace-management cog in a dedicated block near the top of the left navigation pane. `App.axaml` requests Avalonia's default theme variant, but the application is visually Light because the main window and four supporting windows contain more than 300 direct color literals. The application has no settings model, settings persistence boundary, or Settings window.

The existing `MainWindow` code-behind coordinates one owned workspace-management window, store editor, assets window, and other presentation-only windows from view-model state. `WorkspaceManagementViewModel` already owns active-workspace selection and lifecycle commands and emits active-workspace changes; this module must reuse it rather than introduce a second management path.

Settings is an occasional global workflow. It belongs in a focused owned window, while the active workspace remains compact persistent context in the shell. Appearance is application-wide rather than domain or workspace behavior. Its local persistence belongs in Integration behind an Application contract; Avalonia theme selection and settings presentation belong in App. No domain or SQLite workspace-schema change is justified.

Two prerequisites constrain sequencing:

1. Finish and stabilize `basic-product-creation-workflow`, which currently changes `MainWindow` and has incomplete verification tasks.
2. Deliver the repository's general Avalonia headless test harness in its own change. `FusionCanvas.App.Tests` currently has no Avalonia headless package or fixture, and this module must not invent general test-infrastructure policy while implementing Settings.

## Goals / Non-Goals

**Goals:**

- Provide one discoverable Settings entry at the upper-right of the navigation-pane header.
- Keep the active workspace visible in the same header without reserving a separate workspace block.
- Provide a focused, keyboard-usable General/Workspace settings surface with General selected on every open.
- Apply Light/Dark appearance changes immediately and consistently to every current and subsequently opened window.
- Persist the most recent appearance locally, default safely to Light, and recover from invalid or unavailable settings storage.
- Delegate workspace administration to the existing workspace-management view model and window.
- Keep settings responsibilities focused and verifiable at Application, Integration, view-model, and Avalonia headless layers.

**Non-Goals:**

- Per-workspace themes, system-theme following, custom themes, custom colors, scheduling, or theme import/export.
- Additional settings sections, search, deep links, plugin-contributed sections, or a general settings-extension framework.
- Moving workspace creation, editing, archive, restore, deletion, or selection controls into Settings.
- Redesigning workspace management, store management, assets, stage tools, or other application workflows.
- Changing the workspace SQLite schema or storing preferences in workspace metadata.
- Pixel-perfect visual regression or a mandatory live-desktop pass.

## Decisions

### Place global settings and workspace context in one shell header

Use a single header grid with four logical columns:

```text
| FusionCanvas | flexible spacer | workspace indicator | Settings |
```

The Settings button is a compact square icon button at the upper-right. The workspace indicator immediately precedes it and contains a small reusable workspace `PathIcon` plus a single-line name. Its name has an explicit maximum width and character ellipsis so `FusionCanvas` and Settings remain visible at the navigation pane's accepted 240-pixel minimum. The indicator is informational, not a second command; its tooltip is `Manage workspaces in settings`. When no workspace is active it displays `No workspace`.

Remove the dedicated `Workspaces` heading, its cog, and its selected-name row. Store controls remain the first full navigation group below the header.

Alternative considered: place Settings at the bottom-left. Rejected because it separates a global command from application identity, consumes persistent vertical navigation space, and makes the workspace indicator harder to associate with its management route.

Alternative considered: make the workspace indicator clickable. Rejected because Settings is the single clear owner of the management route and a second visible command would duplicate ownership.

### Use one owned, modeless Settings window

Add a roughly 720×480 `SettingsWindow`, with a useful minimum around 640×420. It is owned by the main window, modeless, and unique per main window. This preserves the user's working tabs and navigation context and allows the existing workspace-management window to open in front of Settings. Repeated Settings activation brings the existing window forward rather than creating a duplicate.

The window uses a fixed-width left rail (approximately 170–190 pixels) and a flexible right content pane. A small enum-backed `SettingsSection` contains only `General` and `Workspace`; no extension registry is introduced. General becomes selected each time a new Settings window opens. Section controls expose selection state to Avalonia accessibility and support arrow/tab navigation through a `ListBox` or equivalent selector. Normal title-bar dismissal applies immediately and needs no Save, Apply, Cancel, or unsaved-change flow.

When Settings closes, focus returns to the shell Settings button if the main window is still available. Validation or persistence feedback appears inline in the active pane and does not open another dialog.

Alternative considered: a modal dialog. Rejected because the user must be able to open and complete the existing owned workspace-management flow without disabling or replacing the main-window coordinator.

Alternative considered: render Settings as a document tab. Rejected because occasional application administration should not compete with working documents or tab state.

### Keep settings presentation separate from the main workspace coordinator

Add a focused `SettingsViewModel` in `FusionCanvas.App.Settings` rather than expanding `MainWindowViewModel` with section, persistence, and error state. `MainWindowViewModel` exposes the long-lived Settings view model or a narrow settings coordinator for binding and window-state observation. The Settings view model receives:

- the application settings store contract;
- an App-layer `IApplicationThemeController`;
- the existing `WorkspaceManagementViewModel`;
- a dispatcher-safe error reporting path already embodied by its own properties.

The view model owns `SelectedSection`, `IsDarkMode`, `ErrorMessage`, `IsOpen`, open/close commands, and the command that delegates to `WorkspaceManagement.OpenWorkspaceManagementCommand`. It projects `SelectedWorkspace.Name` or `No workspace` and subscribes to the existing workspace change/property notifications. It does not create, edit, archive, restore, delete, or select workspaces itself.

`IApplicationThemeController` is an App-layer interface because it protects a real framework/test boundary. Its Avalonia implementation sets `Application.RequestedThemeVariant` to `ThemeVariant.Light` or `ThemeVariant.Dark`. It is not placed in Application because theme variants are presentation concerns.

Alternative considered: let the Settings window code-behind load files and change the theme. Rejected because persistence, latest-write behavior, workspace projection, and errors would become hard to test and would mix external I/O into the view.

### Persist one versioned application settings document outside workspaces

Add Application-layer types under `FusionCanvas.Application.Settings`:

- `ApplicationSettings`, initially containing only `bool DarkMode`;
- `IApplicationSettingsStore` with asynchronous load and save operations;
- narrow load/save result types that distinguish a valid value, default fallback, and recoverable failure without leaking JSON or file-system exceptions into App.

Add `JsonApplicationSettingsStore` under `FusionCanvas.Integration.Settings`. Its default path is:

```text
%LOCALAPPDATA%\FusionCanvas\settings.json
```

Support `FUSIONCANVAS_SETTINGS_PATH` as a composition-root override for isolated tests and optional live checks, matching the project's existing workspace-path override pattern. The JSON document is deliberately small and versioned:

```json
{
  "version": 1,
  "darkMode": false
}
```

The adapter creates the parent directory when saving, writes UTF-8 JSON to a same-directory temporary sibling, and atomically replaces or moves it to the final path. A missing file is a normal first-run result with Light/default and no error. Invalid JSON, unsupported version, wrong value shape, access failure, or read failure returns Light/default plus a recoverable warning; it never changes a workspace database. Unknown properties are ignored for forward-compatible additive fields.

A save failure returns a recoverable failure. The selected theme remains active for the current process and the General pane explains that it may not survive restart. A later toggle retries persistence.

Alternative considered: store Dark mode in workspace metadata or SQLite. Rejected because the preference is application-wide, must exist before a workspace opens, and must survive workspace switching without mutating user content.

Alternative considered: App-layer direct file access. Rejected because file persistence is an Integration responsibility and would be difficult to isolate.

### Load before creating the main window and serialize latest-wins writes

`App.OnFrameworkInitializationCompleted` is the composition root:

1. Resolve and create the settings store.
2. Load the persisted settings before constructing or showing `MainWindow`.
3. Select explicit Light when no valid preference exists; do not leave the first-run result as system `Default`.
4. Create the long-lived Settings view model/coordinator with the loaded value and any recoverable load warning.
5. Construct the main window with the existing workspace runtime and settings state.

Changing `IsDarkMode` applies the theme synchronously on the UI thread, then queues persistence asynchronously. Preference writes use a single serialized latest-wins pipeline:

- increment a generation whenever the toggle changes;
- after acquiring the save gate, skip superseded generations;
- save the newest current value;
- only the newest generation may set or clear the visible save error;
- retain a final flush task so application shutdown can complete the latest queued save without allowing an older write to overwrite it.

This prevents rapid on/off changes from persisting in completion order rather than user order. Closing Settings does not dispose the long-lived coordinator or cancel its current save.

Alternative considered: synchronous file writes in the toggle setter. Rejected because filesystem latency and errors must not block the UI thread.

### Use application theme dictionaries and semantic resources

Set the application-level initial `RequestedThemeVariant` to `Light`. Define Light and Dark theme dictionaries in `App.axaml` with semantic brush keys rather than control- or page-specific color names. At minimum cover:

- application background and elevated/panel surfaces;
- primary, secondary, muted, and inverse text;
- standard and strong borders;
- primary/accent commands and their foreground;
- selection, hover, available/current, and disabled states;
- informational, success, warning, and destructive backgrounds, borders, and text;
- input, tree, tab, dialog, and overlay surfaces that currently use direct literals.

Update these current appearance-bearing views to use `{DynamicResource ...}`:

- `Views/MainWindow.axaml`;
- `Settings/SettingsWindow.axaml`;
- `Workspace/WorkspaceManagementWindow.axaml`;
- `Stores/StoreEditorWindow.axaml`;
- `Assets/AssetsWindow.axaml`;
- `Groups/GroupDeleteConfirmationWindow.axaml`.

Framework controls continue to use `FluentTheme`; changing `RequestedThemeVariant` updates both Fluent control resources and application semantic resources. Appearance-dependent fallback values in templates and converters must also become theme-aware. User/data-derived Tag colors may remain content colors, but their surrounding surface, fallback colors, borders, and readable foreground treatment must work in both themes.

Do not mechanically convert colors whose meaning is data-driven without checking their contrast role. After conversion, an inventory search for direct color literals in application XAML must leave only documented data/asset constants or none.

Alternative considered: mutate hundreds of brush instances in code when toggled. Rejected because Avalonia theme dictionaries and dynamic resources provide automatic propagation to existing and future windows with less state synchronization.

### Preserve the existing workspace-management owner and return path

`Manage workspaces` delegates to the existing `WorkspaceManagementViewModel`. The main-window presentation coordinator continues to enforce one `WorkspaceManagementWindow`, but when Settings is open it uses Settings as the immediate owner/focus return target. Settings remains open behind the management window.

If workspace management is already open, the command activates that instance. When it closes, the Settings Workspace section remains selected and focus returns to `Manage workspaces`. Active-workspace changes continue through the existing service/view-model event; the main shell and Settings projection both update from that authoritative state. No new workspace commands or persistence routes are introduced.

### Verification uses the lowest reliable deterministic layer

- Application contract/result behavior receives framework-free unit tests where it contains normalization or fallback decisions.
- `JsonApplicationSettingsStore` receives isolated temporary-directory tests for missing, valid, invalid, unsupported-version, atomic replacement, and deterministic save-failure behavior.
- `SettingsViewModel` receives framework-free tests for initial section, section changes, workspace projection, delegation, immediate theme calls, latest-wins persistence, load/save messages, and reopen state.
- Avalonia headless tests cover meaningful framework risks: Settings construction with compiled bindings, selected pane visibility, keyboard/selection state, ToggleSwitch-to-theme propagation, dynamic-resource changes across multiple windows, header truncation at minimum width, duplicate-window prevention where practical, and focus return.
- Existing `MainWindowViewModelTests` and `WorkspaceManagementViewModelTests` remain regression coverage for active-scope refresh and lifecycle behavior.
- No mandatory live desktop test is planned. A brief isolated visual check may be recorded as supplemental evidence if a human wants to judge palette balance, but it cannot replace deterministic gates.

## Implementation Plan

### 0. Enforce prerequisites and establish a clean baseline

1. Confirm `basic-product-creation-workflow` is completed, verified, and no longer changing the same main-window regions. If it is still active, stop rather than merging overlapping shell assumptions.
2. Confirm `FusionCanvas.App.Tests` already has the approved Avalonia 12 headless package and shared fixture from its prerequisite change. If absent, stop and deliver that independent change first.
3. Run `dotnet test .\FusionCanvas.sln` before feature edits and record the baseline in the eventual `verification.md`.

### 1. Add the application settings boundary and JSON adapter

1. Create `src/FusionCanvas.Application/Settings/ApplicationSettings.cs`, `IApplicationSettingsStore.cs`, and focused result types. Keep the contract limited to application-wide preferences and recoverable load/save outcomes.
2. Create `src/FusionCanvas.Integration/Settings/JsonApplicationSettingsStore.cs` with injected path, version validation, safe defaulting, directory creation, same-directory temporary write, atomic replacement, and cancellation.
3. Add a composition helper in App (for example `Settings/AppSettingsFactory.cs`) that resolves `FUSIONCANVAS_SETTINGS_PATH` or `%LOCALAPPDATA%\FusionCanvas\settings.json`; keep environment/path lookup out of the Application contract.
4. Add Integration tests under `tests/FusionCanvas.Integration.Tests/Settings` using isolated temporary directories. Do not use or alter the contributor's normal application-data folder.

### 2. Add App-layer theme and settings presentation state

1. Create `src/FusionCanvas.App/Settings/IApplicationThemeController.cs` and `AvaloniaApplicationThemeController.cs`.
2. Create `SettingsSection.cs` and `SettingsViewModel.cs` with General default selection, open/close state, workspace-name projection, existing management-command delegation, immediate theme application, serialized latest-wins saves, and inline recoverable errors.
3. Wire `App.axaml.cs` to load settings and apply Light/Dark before creating the main window. Refactor constructors/composition only as far as needed to inject one long-lived Settings view model; do not move existing workspace business behavior.
4. Add framework-free App tests under `tests/FusionCanvas.App.Tests/Settings` using fake store/theme collaborators and the existing in-memory workspace services.

### 3. Establish shared Light and Dark resources

1. Add application theme dictionaries and semantic brushes to `App.axaml`; explicitly make Light the initial variant.
2. Replace appearance-bearing direct colors in MainWindow, WorkspaceManagementWindow, StoreEditorWindow, AssetsWindow, and GroupDeleteConfirmationWindow with dynamic semantic resources.
3. Check converters, template fallback values, hover/selected/disabled styles, warning and destructive panels, tabs, tree rows, stage controls, and window backgrounds. Preserve data-derived colors only with documented rationale and readable foregrounds.
4. Update `docs/ui-guidelines.md` from “visually dark by default” to a user-selectable Light/Dark direction with Light as the initial default.

### 4. Build the Settings window and shell entry

1. Add `SettingsWindow.axaml` and its minimal code-behind with compiled bindings, left section selector, General ToggleSwitch and inline persistence message, Workspace name/empty state, and `Manage workspaces`.
2. Add styles through semantic resources; keep button sizing content-based and the section rail keyboard accessible.
3. Refactor the top of `MainWindow.axaml` into the approved four-column header. Add a vector workspace icon, truncated workspace projection, exact tooltip, and Settings icon button; remove the old workspace block.
4. Extend the existing MainWindow window coordinator to enforce one Settings window and one workspace-management window, select the correct owner, activate existing instances, and restore focus on close.
5. Ensure workspace changes notify both the header projection and open Workspace settings pane without duplicating mutation logic.

### 5. Add framework-level and regression verification

1. Add focused headless tests in the established App headless fixture for Settings construction/bindings, General default, section switching, keyboard selection/focus, ToggleSwitch behavior, dynamic resources on multiple windows, no-workspace display, minimum-width header truncation, and owned-window uniqueness/return focus where the harness can represent it reliably.
2. Extend existing App regression tests for active-workspace changes, existing workspace-management commands, and main-shell projection.
3. Create `verification.md` in this change and record criterion-level results for every scenario in the mapping below.
4. Run focused test projects, `dotnet test .\FusionCanvas.sln`, strict `openspec validate --change add-application-settings --strict` (or the installed CLI's equivalent), validation of all active changes, `git diff --check`, and a direct-color inventory.
5. Perform changed-scope completion QA for architecture, persistence isolation, theme consistency, keyboard/focus behavior, and spec drift. Correct failures and rerun affected criteria plus regressions.

### Decisions implementation must not reopen

- Settings lives in the upper-right navigation header, not bottom-left or a document tab.
- The workspace indicator is informational and Settings is the single management entry.
- General is selected whenever a new Settings window opens.
- Appearance changes apply immediately, have no Save/Cancel flow, and Light is the no-preference default.
- The preference is global JSON in local application data, never workspace metadata or SQLite.
- Workspace administration remains in the existing management view model/window.
- General-purpose headless infrastructure is a prerequisite change, not hidden scope in this module.

## Acceptance-to-Verification Mapping

| Capability | Acceptance scenario | Planned verification |
| --- | --- | --- |
| `application-settings` | User opens Settings | Headless window/command test: one owned Settings instance, main context unchanged, General pane visible |
| `application-settings` | User navigates settings sections | View-model test plus headless selector/content visibility and selected-state test |
| `application-settings` | User reopens Settings | View-model/window lifecycle test closes on Workspace and reopens on General |
| `application-settings` | User operates Settings with a keyboard | Headless tab/selection/focus-return test |
| `application-settings` | No appearance preference has been saved | Isolated missing-file Integration test plus startup/controller test for explicit Light and toggle off |
| `application-settings` | User enables Dark mode | View-model theme-controller test plus headless dynamic-resource assertions across open windows |
| `application-settings` | User disables Dark mode | View-model theme-controller test plus headless Light resource assertions |
| `application-settings` | User closes Settings after changing appearance | Window lifecycle test: theme retained, no save/apply/unsaved prompt surface |
| `application-settings` | User changes appearance repeatedly | Deterministic gated fake-store test proving latest generation persists and stale completion cannot overwrite it |
| `application-settings` | Saved Dark preference survives restart | Integration save/reload test plus composition/startup test |
| `application-settings` | User switches active workspace | App regression test proving theme unchanged and workspace snapshot untouched |
| `application-settings` | Saved preference cannot be read | Integration missing/invalid/unsupported/unreadable cases; Light fallback and subsequent successful save |
| `application-settings` | Preference cannot be saved | Integration adapter failure test plus SettingsViewModel inline-error/current-theme test |
| `application-settings` | User changes appearance with multiple windows open | Headless resource-resolution test on Main, Settings, and one management window before/after toggle |
| `application-settings` | User reviews functional color states | Semantic-resource inventory plus headless readable/changed resource assertions for warning, danger, selection, disabled, and accent states |
| `application-settings` | Workspace section has an active workspace | View-model projection test plus headless label/button binding test |
| `application-settings` | Workspace section has no active workspace | View-model/headless test for `No workspace` with enabled management button |
| `application-settings` | Active workspace changes while Settings is open | App coordination test for synchronized Settings and shell projections |
| `desktop-application-foundation` | User views workspace shell with active workspace | Headless minimum-width visual-tree/layout test, exact tooltip/icon/name bindings, and absence of old workspace row |
| `desktop-application-foundation` | User views workspace shell without an active workspace | Headless shell test for icon, `No workspace`, and enabled Settings button |
| `desktop-application-foundation` | User switches workspace from workspace management | Existing and extended MainWindowViewModel regression tests for header, store, and tree refresh |
| `desktop-application-foundation` | User opens workspace management | Headless/coordination test proving Settings → Workspace → existing management surface |
| `workspace-management` | User activates Manage workspaces | View-model delegation and owned-window coordination test |
| `workspace-management` | User attempts to open workspace management again | Headless/window coordinator uniqueness test or focused coordinator test if native activation is not represented headlessly |
| `workspace-management` | User closes workspace management | Headless lifecycle test for Settings foreground, retained Workspace section, and returned focus |
| `workspace-management` | Workspace management changes active workspace | Existing workspace-management tests plus App coordination regression; inspect that no new mutation command exists in Settings |

## Risks / Trade-offs

- **[Hard-coded colors survive the conversion]** → Inventory every current XAML file, replace semantic literals with dynamic resources, document any deliberate data color, and test representative resources in both variants.
- **[Dark palette is technically switched but unreadable]** → Define semantic foreground/background pairs for functional states and verify representative pairs headlessly; allow an optional supplemental visual review.
- **[Rapid toggles persist out of order]** → Serialize writes with generation-based latest-wins coalescing and a deterministic delayed-store test.
- **[Settings file is corrupted or unavailable]** → Default to explicit Light, surface recoverable warnings, use atomic writes, and never touch workspace persistence.
- **[MainWindow gains more coordination responsibilities]** → Keep behavior in `SettingsViewModel` and a narrow theme controller; code-behind only owns native window lifetime/focus, matching existing practice.
- **[Settings and workspace management create confusing stacked windows]** → Enforce unique owned instances, keep Settings behind the focused manager, and return focus to the invoking button.
- **[Header crowds at minimum navigation width]** → Fix identity and Settings columns, constrain/ellipsis the workspace name, and add a headless measure/layout assertion.
- **[Prerequisite changes are incomplete]** → Stop at Implementation Plan step 0; do not guess against an evolving main-window surface or omit mandatory headless coverage.

## Migration Plan

1. Complete the two prerequisite changes.
2. Add the optional application settings file and default existing users to Light when it does not exist.
3. Replace visual resources without changing workspace data or SQLite schema.
4. On first theme change, create `%LOCALAPPDATA%\FusionCanvas\settings.json` atomically.
5. If rollback is required, remove the Settings entry/window and restore the previous XAML resources. The additive settings file may remain harmlessly on disk; no workspace rollback or data migration is needed.

## Open Questions

None. Product, UX, persistence, ownership, failure, sequencing, and verification decisions required for implementation are resolved above.
