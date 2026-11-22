## Why

FusionCanvas currently places application settings, the active workspace indicator, and update actions together in the workspace-navigation header, while workspace administration is nested inside global Settings. This blurs application-wide controls and workspace actions; a narrow global utility rail plus a clear workspace-context control will make both easier to find without adding another competing settings surface.

## What Changes

- Add a narrow, persistent global utility rail alongside the resizable workspace navigation pane, with Settings at the bottom and an update action above it only when an update is available for the selected Stable or Preview track.
- Move workspace switching and workspace administration to an actionable control in the workspace pane; keep workspace-specific actions off the global rail.
- Keep one application-wide Settings window. Keep AI configuration there as an application-wide preference, remove workspace administration from its sections, and expose workspace diagnostics with the active workspace clearly identified.
- Keep the existing workspace-management window as the owner of workspace creation, editing, archival, restoration, deletion, and selection behavior.
- Defer a Home button until FusionCanvas has a distinct home destination.

## Capabilities

### New Capabilities

- `global-utility-rail`: Provides global shell shortcuts for Settings and an actionable in-app update, separate from workspace-scoped navigation and actions.

### Modified Capabilities

- `application-settings`: Clarifies that Settings owns application-wide preferences and update-track choice, while any workspace-scoped diagnostics identify their active workspace; removes workspace administration from Settings.
- `workspace-management`: Makes workspace switching and management discoverable from the workspace context in the main navigation pane while preserving the dedicated management workflow.

## Impact

- Affects `FusionCanvas.App` shell layout, main-window presentation state, Settings section navigation, workspace selection entry point, and workspace-management window ownership/focus behavior.
- Reuses the existing update-track preference and update presentation command; this proposal does not change release discovery, Stable/Preview policies, download, verification, or installer behavior.
- Requires coordination with the active `add-in-app-updates` change, whose current shell placement description predates this proposal. Reconcile that description before implementing either overlapping shell surface.
- No domain or persistence schema change is expected. Workspace selection remains persisted through existing application settings, and AI configuration remains stored in the current app-wide settings document.
- Primary workflow is frequent use of the global update shortcut when it appears and occasional access to Settings; workspace switching and management remain in the workspace context. The rail is limited to a compact strip and does not displace the resizable tree or document surface beyond its width.
- UX states include hidden update action when no update is actionable, visible update action for the selected track, workspace selector empty/one/multiple choices, and the existing manager's loading/error/confirmation states. Keyboard access, focus return, and existing unsaved/dangerous management flows remain explicit verification concerns.
- Avalonia headless view coverage is warranted for rail visibility and placement, update visibility/command binding, workspace control interaction, Settings section changes, and focus/accessibility names. No new Appium journey is planned: the change reuses existing navigation and management workflows and has no OS-level interaction risk beyond optional visual review.
