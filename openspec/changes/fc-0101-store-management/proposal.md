## Why

FusionCanvas has the foundation for store-shaped workspace data, but creators still need a usable way to create, maintain, select, archive, and restore stores as the top-level boundary for Print on Demand work. This is the first Phase 1 MVP workspace feature because niches, groups, listings, tags, assets, search, and context-aware tools all depend on a clear active store.

## What Changes

- Add store management workflows for creating a first store without advanced configuration.
- Allow users to rename stores and edit basic store-level context such as description, notes, target market, brand direction, or planning guidance.
- Allow users to archive inactive stores so they do not clutter the active workspace, while preserving them for review or restoration.
- Allow users to restore archived stores to the active workspace.
- Expose active and archived store lists distinctly in the UI.
- Allow users to open/select a store as the primary workspace scope for browsing niches, groups, listings, tags, assets, and future context-aware tools.
- Keep marketplace account connection, publishing destination setup, store analytics, billing, multi-user permissions, store templates, and automated store setup out of scope.

## Capabilities

### New Capabilities

- `store-management`: Defines user-facing creation, editing, archiving, restoration, active-store selection, and store-context behavior for top-level business or brand contexts.

### Modified Capabilities

- None.

## Impact

- Affected application areas: store application services, workspace repository usage, Avalonia shell/navigation UI, active workspace context state, and tests around store workflows.
- Existing domain and SQLite models already include stores with names, descriptions, archive state, timestamps, and metadata; implementation may extend service and UI behavior without replacing the persistence foundation.
- Existing navigation-tree and context-aware-tools specs remain accepted dependencies: this change uses them for opening a store and exposing active store scope rather than changing their requirements.
