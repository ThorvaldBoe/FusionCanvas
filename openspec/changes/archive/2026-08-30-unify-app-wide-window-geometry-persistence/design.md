## Context

The accepted window-layout behavior already covers the main window and a defined set of non-transient secondary windows. The implementation currently attaches persistence in several owners and uses separate property-change/close flows. Recent regressions showed that native Windows movement can leave Avalonia's managed `Position` unchanged and that synchronous view-model close synchronization can re-enter `Window.Close` before capture completes.

## Goals / Non-Goals

**Goals:**

- Provide one application-owned registration API for all non-transient windows.
- Centralize stable keys, normal-state capture, native coordinate capture, screen-safe restore, and save failure handling.
- Make close ordering explicit: capture before native teardown and synchronize view-model state after close processing.
- Preserve the existing JSON shape and stable keys for compatibility.
- Cover the registration matrix and representative native/headless lifecycle paths.

**Non-Goals:**

- No new user-facing layout controls or settings page.
- No persistence for transient confirmations, selection dialogs, or other intentionally short-lived surfaces.
- No cross-device synchronization, workspace-specific geometry, or changes to main-window layout semantics.

## Decisions

1. **Application-wide registrar over per-window helper calls.** A registrar owned by the App/MainWindow shell will accept a window, stable key, and minimum dimensions, then attach the shared lifecycle. This keeps window owners focused on creation and domain behavior. A shared registrar is preferred over conventions or reflection because registration is explicit, auditable, and testable.

2. **Native coordinate capture with managed fallback.** On Windows, close-time capture will query the native window rectangle while the handle is valid; on other platforms or unavailable handles, Avalonia's position is used. This addresses the observed native move discrepancy without introducing a platform abstraction for every window.

3. **Capture before close and persist after confirmed close when possible.** The lifecycle captures on size/position changes, on a short active-window sampling interval, and in `Closing` before native teardown. `Closed` remains a final fallback. Cancelled closes do not overwrite saved geometry. View-model synchronization triggered by close is dispatched after the close event returns.

4. **Stable identity remains data, not type name.** Existing `WindowLayoutKeys` values remain the persistence contract. The registrar will reject empty or duplicate registrations within one shell session rather than derive keys from class names.

5. **Deterministic tests plus one native smoke path.** Normalization, key isolation, cancellation, and registration completeness use deterministic tests. Avalonia headless tests cover construction and event ordering. A live Windows smoke check is supplemental evidence for native title-bar movement because headless tests cannot produce that OS event.

## Risks / Trade-offs

- **[Sampling overhead]** A timer adds small UI-thread work while secondary windows are open → use one low-frequency timer per registered window, stop it immediately on close, and retain event-driven capture.
- **[Platform API coupling]** `user32.dll` is Windows-specific → guard native calls by operating system and keep the managed fallback.
- **[Registration omissions]** A new window may be created without registration → maintain a single registration list/test matrix and require explicit transient exclusion.
- **[Close cancellation]** Unsaved-change prompts may cancel close → check cancellation before persisting and retain the last valid geometry in memory.
- **[Multiple owners]** Nested editors have independent owners and lifetimes → route all registration through the same registrar instance and preserve stable per-editor keys.

## Migration Plan

1. Introduce the registrar behind the existing geometry store and move current attachment sites to it without changing keys or JSON.
2. Add registration-matrix and lifecycle tests, then run the full solution baseline.
3. Ship as a backward-compatible settings change; old files continue to load with defaults for missing keys.
4. Rollback is code-only: revert the registrar wiring while retaining the existing settings fields and keys.

## Implementation Plan

1. Define the registrar/registration contract in `FusionCanvas.App.Views` and consolidate `WindowGeometryPersistence` behind it.
2. Move stable window-key ownership and registration calls into the main shell and nested window owners; explicitly enumerate transient exclusions.
3. Centralize deferred synchronization for open-state property changes across Settings, Workspace Management, Store Editor, Assets, Ideation, and related owners.
4. Keep native Windows capture and managed fallback in the shared lifecycle; ensure timers are disposed on `Closed`.
5. Add focused unit/headless tests for registration completeness, duplicate keys, cancellation, native fallback, per-key isolation, and restoration.
6. Verify each accepted scenario, run `openspec validate`, and run `dotnet test .\\FusionCanvas.sln`.

## Verification Mapping

- Registration and stable identity scenarios → registrar unit tests and a complete registered-window matrix.
- Native move/resize capture and close ordering → Windows smoke check plus headless event-order tests.
- Restore, normalization, invalid values, and missing settings → existing normalizer and persistence tests.
- Transient exclusions → registration matrix assertion that excluded dialog types have no geometry entry.
- Save failure tolerance → SettingsViewModel and integration persistence tests.
