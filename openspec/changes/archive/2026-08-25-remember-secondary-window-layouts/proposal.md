## Origin

- GitHub issue: #214 — [Feature]: Remember the last position and size of every window, not just the main window
- https://github.com/ThorvaldBoe/FusionCanvas/issues/214

## Why

FusionCanvas already remembers the main window's normal position, size, and navigation-pane width, but every secondary window (Settings, Workspace Management, Store Editor, Assets, Ideation and its sub-windows) reopens at its XAML default placement. Creators who arrange these windows across their screens must reposition them on every reopen, which is repetitive friction during normal creative work.

## What Changes

- Extend the versioned application-settings document with per-window normal-state geometry keyed by a stable window identity, alongside the existing main-window layout section.
- Persist on close and restore on open the normal-state position and size of each non-transient secondary window: Settings, Workspace Management, Store Editor, Assets, Ideation, Reject Idea, Snowclone Library, Rejected Phrases, Design Preview, and Item Import.
- Define the persistence boundary explicitly: non-transient windows persist placement; transient confirmation dialogs (Group action confirmations, Ideation discard confirmation, Design Area archive confirmation) keep default placement.
- Reuse the existing main-window normalization behavior (finite/positive, within min/max constraints, visible on a current screen, clamp to usable position; maximized/fullscreen are not normal geometry) for every persisted window.
- Preserve backward compatibility: a settings document written by the current version (with only the single main-window `windowLayout` section) loads cleanly, and the main window keeps using that section.
- Keep persistence local to the user's machine and free of personal or workspace content; failures to save or load secondary geometry do not break the session.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `window-layout-persistence`: Extend layout persistence from the main window only to every non-transient window, define which windows persist and which transient dialogs do not, and require the same screen-safe restoration for each persisted window.
- `application-settings`: Define compatibility and fallback behavior for the new optional per-window geometry section in the versioned settings document.

## Impact

- Affects `ApplicationSettings` and the JSON application-settings store (new optional per-window geometry dictionary and document version), `SettingsViewModel` (settings update path), `MainWindowLayoutNormalizer` (generalized for windows without a navigation width), the secondary window code-behind and `MainWindow` window-synchronization helpers (capture on close, restore on open), and related Application/Integration/App tests.
- Adds no external dependencies and changes no workspace database or package schema.
- No new user-facing controls, prompts, or destructive actions; the surface is the windows themselves as they are moved and resized today.
- The module is cohesive because settings serialization, per-window lifecycle capture/restore, shared screen-safe normalization, and their tests form one independently verifiable "resume my window arrangement" outcome.

## Scope

Included:

- Per-window geometry records keyed by stable window identity in the versioned settings document.
- Restore-on-open and capture-on-close for each non-transient secondary window.
- Shared normalization reused from the main-window behavior.
- Backward compatibility for existing single-layout settings documents.
- Deterministic Integration tests for the new document shape and App tests for normalization and capture/restore.

Non-goals:

- No per-workspace window layouts; placement stays application-wide and local.
- No layout export/import or reset UI.
- No persistence of maximized, minimized, fullscreen, or platform-specific window-state flags.
- No persistence for transient confirmation dialogs.
- No changes to the main window's existing `windowLayout` section semantics or to workspace, theme, or AI settings behavior.

## Risks

- A monitor can disappear between save and restore, leaving a secondary window off-screen. Reuse the existing screen selection/clamping so every persisted window returns to a usable position.
- A settings file may contain partially corrupt per-window geometry. Validate each window entry independently and discard only that entry while preserving readable appearance, AI, and main-window layout settings.
- Adding window capture/restore to many windows risks duplicated Avalonia-specific logic. Centralize capture/restore/normalize in shared UI-layer helpers and keep window code-behind thin.
- Headless Avalonia tests do not model real OS monitor topology. Cover pure validation/clamping with injected screen data and restore-on-open through deterministic fixtures; native multi-monitor behavior remains optional supplemental evidence.

## Verification Approach

- Integration tests for the new settings-document version: round-trip per-window geometry, backward compatibility with existing single-layout documents, and independent invalid-entry discard.
- App tests for shared normalization of windows without a navigation width and for capture-on-close/restore-on-open behavior using deterministic screen fixtures and isolated recording settings stores.
- Existing main-window layout and settings tests remain green.
- `dotnet test .\FusionCanvas.sln` and strict `openspec validate` pass.
