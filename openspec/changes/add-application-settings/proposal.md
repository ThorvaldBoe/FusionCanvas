## Why

FusionCanvas has no focused place for application-wide preferences, while workspace administration currently occupies permanent navigation-pane space despite being an occasional task. This module introduces a compact Settings entry point so users can control appearance and reach workspace management without displacing their daily creative workflow.

**Module outcome:** A user can open Settings from the main shell, switch between General and Workspace sections, apply a persistent application-wide Light or Dark appearance, and launch the existing workspace-management workflow while the active workspace remains visible in the shell.

The settings window, theme preference, shell indicator, and workspace-management route form one coherent module because they share one global entry point, one focused interaction flow, and one acceptance pass. AI, plugin, marketplace, store, and workflow-specific settings remain separate future outcomes.

## What Changes

- Add a compact Settings button at the upper-right of the left navigation-pane header, beside FusionCanvas identity and the active workspace indicator.
- Replace the current `Workspaces` label and sidebar cog row with a compact, non-editing workspace indicator on the header row. The indicator uses a workspace icon, truncates long names, shows `No workspace` when none is active, and exposes the tooltip `Manage workspaces in settings`.
- Add a focused Settings window with a vertically stacked section rail on the left and the selected section pane on the right. `General` is selected on each open; `Workspace` is the second initial section.
- Add a General setting named `Dark mode`. It is off when no preference has been saved, applies the selected Light or Dark appearance immediately across all open FusionCanvas windows, and persists locally across restarts.
- Add a Workspace section that displays the current workspace name and provides a `Manage workspaces` button. The button opens the existing workspace-management window; lifecycle, selection, validation, and destructive actions remain owned by that existing surface.
- Keep settings changes immediate: the Settings window has no Save, Apply, or unsaved-changes prompt, and normal window dismissal preserves already-applied preferences.
- Introduce shared semantic Light and Dark theme resources for all current application windows so the appearance setting changes the complete application rather than only framework-default controls.
- Reconcile the UI guidance that currently describes Dark as the default with the accepted Light-default setting.
- Add deterministic tests for preference persistence and presentation logic plus focused Avalonia headless tests for material settings-window bindings, section selection, commands, and application-wide theme switching.
- Treat completion of `basic-product-creation-workflow` as a sequencing dependency because it currently changes the main-window surface. The repository's Avalonia headless test harness must also be delivered by its own prerequisite change before this user-facing module can satisfy the testing baseline; do not absorb general-purpose test-infrastructure design into this settings module.

## Capabilities

### New Capabilities

- `application-settings`: Defines the focused Settings surface, initial sections, application-wide appearance preference, immediate application, local persistence, and settings interaction behavior.

### Modified Capabilities

- `desktop-application-foundation`: Moves workspace context into the main shell header and makes Settings the regular global entry point for occasional configuration.
- `workspace-management`: Routes workspace administration from the Workspace settings section while preserving the existing management behavior.

## Impact

- **Application:** Add a narrow application-settings contract for loading and saving the global appearance preference; no domain behavior changes.
- **Integration:** Add a local settings adapter using an isolated application-data file, with missing, invalid, and write-failure handling that does not alter workspace data.
- **App:** Add settings presentation state and window coordination; update `App`, `MainWindow`, and all current windows to consume semantic theme resources and react to the selected theme.
- **Persistence:** Store only application-wide preferences outside the workspace database. Existing workspaces and SQLite schema remain unchanged, and the first run remains Light.
- **Documentation:** Update current UI guidance to allow user-selected Light and Dark appearances with Light as the initial default.
- **Dependencies:** Finish and stabilize the in-progress `basic-product-creation-workflow` main-window changes before applying overlapping shell edits. Ensure the deterministic Avalonia headless harness exists before verification.
- **Non-goals:** Per-workspace themes, system-theme following, custom palettes, additional settings sections, inline workspace editing, redesigning the workspace-management window, plugin-contributed settings, cloud sync, and importing or migrating preferences from another application.
- **Primary risks:** Incomplete replacement of hard-coded colors could produce mixed themes; settings-file corruption or write failure could make preference state surprising; multiple owned windows could diverge in theme or lifecycle state; and header content could crowd the resizable sidebar. Verification will cover semantic-resource usage, restart persistence and recovery, synchronized open windows, compact-width truncation, section navigation, and existing workspace-management regressions.
