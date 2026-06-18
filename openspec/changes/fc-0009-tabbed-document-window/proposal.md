## Why

FusionCanvas needs a working document-window model so creators can keep several related ideas, designs, listings, or workflow views open without losing navigation context. This is the next Phase 0 foundation step after the workflow stage navigator because the active tab must become the workspace context that coordinates navigation, workflow stage state, and the detail view.

## What Changes

- Add a tabbed document window to the main workspace shell.
- Allow users to open multiple document contexts from navigation without replacing previously opened work.
- Make each tab identify its open item, topic, workflow stage, or working view with a clear title.
- Make tab selection update the active document context for the workspace.
- Coordinate the active tab with the navigation pane, workflow stage navigator, and stage detail view.
- Split the document window into a tab strip, a workflow area, and a larger lower detail area.
- Allow users to close tabs when the context is no longer needed.
- Keep advanced split panes, tab persistence across restarts, tab groups, collaboration, and full plugin marketplace behavior out of scope.

## Capabilities

### New Capabilities

- `tabbed-document-window`: Defines how the workspace opens, selects, closes, and coordinates multiple document contexts through tabs.

### Modified Capabilities

- None.

## Impact

- Affected code will primarily live in `FusionCanvas.App` presentation state, Avalonia views, and view models for the main workspace shell.
- The active document context may require small application/domain-facing contracts or simple models if existing UI state cannot represent item identity, location, and workflow stage cleanly.
- The implementation should integrate with the existing navigation tree and workflow stage navigator foundations without adding persistence, session restore, plugin marketplace behavior, or advanced split-pane infrastructure.
- Tests should cover UI-owned document tab state transitions and context coordination logic where that behavior is not purely static Avalonia markup.
