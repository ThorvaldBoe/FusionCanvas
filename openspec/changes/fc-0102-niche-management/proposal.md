## Why

FusionCanvas can now model and manage stores as the top-level workspace boundary, but creators still need a practical way to organize work inside a store by audience, theme, market, or creative direction. Niche management is the next Phase 1 workspace capability because groups, listings, search, tags, assets, and later AI context all need a stable niche layer that carries creative guidance instead of acting as a plain folder.

## What Changes

- Add niche management workflows for creating niches inside an active store.
- Allow users to rename niches and edit niche context such as audience notes, style guidance, humor, constraints, legal risks, recurring patterns, and research notes.
- Allow users to archive inactive niches so they do not clutter normal active work, while preserving them for review or restoration.
- Allow users to restore archived niches to the active store workspace.
- Keep niches useful before any groups or listings exist inside them.
- Present active niches as the default top-level folder/topic nodes under the selected store in the main window sidebar.
- Extend the existing store management window with Basic info and Niches tabs so store-scoped setup stays in one place.
- Support opening/selecting a niche as the current workspace context for browsing future groups and listings and for context-aware tools.
- Preserve niche identity and store association when renaming, editing context, archiving, or restoring.
- Persist niche management edits through the desktop app's SQLite workspace database.
- Keep AI niche research, trend tracking, niche scoring, marketplace demand analysis, cross-store niche libraries, and automated niche recommendations out of scope.

## Capabilities

### New Capabilities

- `niche-management`: Defines user-facing creation, editing, archiving, restoration, active niche browsing, and niche-context behavior for store-scoped audience, theme, market, and creative-direction containers.

### Modified Capabilities

- None.

## Impact

- Affected application areas: niche application services, workspace repository usage, Avalonia shell/navigation UI, active workspace context state, and tests around niche workflows.
- Existing domain and SQLite models already include niches associated with stores; implementation should build on those foundations before introducing schema changes.
- Existing store-management, navigation-tree, and context-aware-tools specs remain accepted dependencies: this change uses them for active store scope, top-level topic browsing, and niche-aware context rather than changing their requirements.
