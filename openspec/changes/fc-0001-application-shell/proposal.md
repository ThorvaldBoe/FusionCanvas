## Why

FusionCanvas needs a real application shell so contributors can build Phase 0 and Phase 1 workspace features into stable, recognizable regions instead of repeatedly reshaping the startup UI. FC-0001 defines the first usable desktop container: launchable, clear when empty, and structured for navigation, tabs, workflow stage context, and detail content.

## What Changes

- Replace the current mostly empty startup page with a minimal workspace shell.
- Add a persistent left navigation region that communicates where workspace navigation will live without implementing full navigation tree behavior.
- Add a right document window region that contains a tab strip area, workflow stage navigator area, and detail view host area.
- Add empty, loading, and error presentation states where useful for the shell and document region.
- Expose core application command locations at shell level without implementing full product commands.
- Preserve the Phase 0 boundary by avoiding store, niche, group, listing, marketplace, plugin, AI, persistence, and workflow automation behavior.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `desktop-application-foundation`: Refine the initial desktop shell requirements from a mostly empty startup page into the FC-0001 application shell with navigation, document window, tab, workflow stage, detail, command, and shell state regions.

## Impact

- Affects `FusionCanvas.App` startup UI, main window layout, shell view/view-model structure, and UI-owned state presentation.
- May add lightweight UI tests or view-model tests for shell state and layout decisions where practical.
- Does not require domain, persistence, marketplace, AI, plugin, or external service dependencies.
