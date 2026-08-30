## Context

The existing `window-layout-persistence` capability stores each named non-transient window's normal geometry in the optional application-wide `windowGeometry` dictionary. `WindowGeometryPersistence` restores and captures geometry through `IWindowGeometryStore`, while `WindowLayoutKeys` prevents key drift. The Store Management window is attached by `MainWindow`, but editor windows opened by `StoreEditorWindow` were introduced after the accepted capability and currently do not participate.

This module completes that defined boundary without changing the settings document or any editor workflow. Creators open these focused editors during catalog maintenance; remembering placement lets them continue that work without displacing the primary workspace or requiring a new command.

UX preflight: management remains in focused surfaces, not the main workspace. The behavior has no new interactive states or controls. Existing draft-save/discard, cancellation, confirmation, validation, focus return, loading, empty, blocked, and error behavior remains the owner of each editor; layout restoration must not alter it. Headless coverage is meaningful because window opening, ownership, positioning, and closing are Avalonia framework behavior.

## Goals / Non-Goals

**Goals:**

- Persist and restore valid normal geometry for each reusable Store Management editor window.
- Keep each editor's geometry independent through stable keys.
- Reuse the existing local settings and screen-safe normalization path.
- Close the coverage gap by verifying both position and size capture/restore and the Store Management wiring.

**Non-Goals:**

- No per-workspace layout, settings UI, reset action, settings document version change, or new persistence abstraction.
- No persistence of maximized, minimized, fullscreen, or other platform-managed states.
- No persistence for confirmation dialogs or other short-lived prompts.
- No changes to catalog records, draft handling, editor commands, editor visuals, or focus behavior.

## Decisions

### Treat reusable editor windows as non-transient

Option Value Management, Add Variant, Bulk Add Variants, Design Area Editor, and Mockup Template Editor are focused, independently sized editors that may be reopened repeatedly. They receive their own persisted geometry. Confirmation windows remain transient because they are short decision prompts rather than user-arranged work surfaces.

Alternative: persist every `Window` created beneath Store Management. Rejected because it would include confirmations and makes the product boundary depend on implementation rather than user intent.

### Reuse the shared geometry helper and local settings dictionary

Add lowercase camel-case constants for the five editors to `WindowLayoutKeys`. Pass the existing `IWindowGeometryStore` from `MainWindow` into `StoreEditorWindow` before it is shown, then call `WindowGeometryPersistence.Attach` exactly once for each editor immediately after construction and before `ShowDialog`.

Alternative: let `StoreEditorWindow` depend directly on `SettingsViewModel`. Rejected because the existing narrow Application-facing contract is sufficient and preserves the UI boundary and test seam.

### Preserve keys by editor type, not by catalog record

Each editor type has one key shared across its invocations, regardless of selected Store, Product, Offering, Variant, Design Area, or Mockup Template. Geometry is an application-wide display preference and must not include workspace content.

Alternative: key geometry by edited record. Rejected because it creates unbounded local settings and does not match the accepted application-wide layout model.

### Test framework behavior at the shared helper and integration point

Extend `WindowGeometryPersistenceTests` to assert a moved normal window restores both its position and size independently of another key. Add focused Store Editor headless tests that trigger each editor and verify the shared helper is attached through a recording geometry store (including close/capture where deterministic) without attaching the Design Area archive confirmation.

Alternative: test only `WindowLayoutKeys` or inspect source. Rejected because those tests would not verify Avalonia `Opened`, `PositionChanged`, and `Closing` behavior.

## Risks / Trade-offs

- [Risk] Nested dialogs need the settings store passed through a UI boundary. → Use the existing `IWindowGeometryStore` contract and an internal `StoreEditorWindow` property, assigned by `MainWindow` before the editor can open.
- [Risk] A captured position is unsafe after monitor changes. → Continue using the existing normalizer and screen list; do not duplicate or weaken its validation.
- [Risk] Repeated event subscriptions could save a window more than once. → Construct each editor once per invocation and attach once before showing it; retain existing ownership guards.
- [Risk] Headless tests cannot faithfully represent native multi-monitor behavior. → Exercise capture/restore and key isolation deterministically; retain the normalizer's existing screen-safety tests. A real desktop check is optional supplemental evidence, not a completion gate.

## Migration Plan

1. Existing settings remain valid because `windowGeometry` already supports independent optional entries.
2. The new keys are absent on upgrade, so each editor initially uses its XAML default placement.
3. After its first valid close, an editor writes only its own geometry entry and restores it on later opens.
4. Rollback is safe: older builds ignore unknown dictionary keys and continue to use supported entries.

## Open Questions

None. The user accepted the scope of reusable editor windows while excluding transient confirmations.

## Implementation Plan

1. **Define the persisted-window boundary (App + OpenSpec)**
   - Update `WindowLayoutKeys` with stable keys for Option Value Management, Add Variant, Bulk Add Variants, Design Area Editor, and Mockup Template Editor.
   - Update the `window-layout-persistence` delta to name these reusable editors as non-transient and retain the explicit confirmation exclusion.

2. **Pass the existing geometry contract into Store Management (App)**
   - Add an internal `IWindowGeometryStore?` property to `StoreEditorWindow`.
   - In `MainWindow.SyncStoreEditorWindow`, assign `_settings` to that property before the Store Editor is shown.
   - Do not expose `SettingsViewModel`, change composition-root ownership, or alter application-settings serialization.

3. **Attach persistence to nested editor windows (App)**
   - In each relevant `StoreEditorWindow` event handler, after constructing an editor and before `ShowDialog`, conditionally call `WindowGeometryPersistence.Attach` with the propagated store, matching stable key, and the editor's `MinWidth` and `MinHeight`.
   - Cover `OptionValueManagementWindow`, `AddVariantWindow`, `BulkAddVariantsWindow`, `DesignAreaEditorWindow`, and `MockupTemplateEditorWindow`.
   - Do not attach `DesignAreaArchiveConfirmationWindow` or any existing confirmation dialog.

4. **Verify persistence and regression behavior (App tests)**
   - Extend `WindowGeometryPersistenceTests` to assert both restored and captured `PixelPoint` position and dimensions, including isolation between two editor keys and normal-state-only capture.
   - Add/extend `StoreEditorHeadlessTests` with a deterministic `IWindowGeometryStore` fixture to assert that each named editor is constructed with persisted geometry support and that a transient confirmation does not write a key.
   - Keep existing normalizer and settings-document compatibility tests green; no new Application or Integration tests are required because the document shape and contract do not change.

5. **Completion verification**
   - Run `openspec validate fix-secondary-editor-window-layout-persistence --strict`.
   - Run `dotnet test .\FusionCanvas.sln`.
   - Optionally perform a manual Windows check with disposable settings: move and resize Manage Stores plus one nested editor, close and reopen each, then verify a disconnected-screen placement returns visibly.

## Acceptance-to-Verification Plan

| Acceptance scenario | Planned verification |
| --- | --- |
| Any named non-transient secondary window reopens with its last normal geometry | Extend `WindowGeometryPersistenceTests` to exercise restore and close capture for position and size. |
| Each reusable Store Management editor keeps separate geometry | Store Editor Avalonia headless tests open each editor with a recording store and verify the expected independent key/geometry path. |
| A transient confirmation uses default placement and has no key | Store Editor headless test opens the Design Area archive confirmation and verifies no geometry-store update. |
| Unsafe saved geometry remains safe | Existing `MainWindowLayoutNormalizerTests` remain in the deterministic baseline; the shared helper continues to use that normalizer. |
| Existing settings and main-window behavior remain unchanged | Existing Integration settings tests and App main-window layout tests run in `dotnet test .\FusionCanvas.sln`. |
