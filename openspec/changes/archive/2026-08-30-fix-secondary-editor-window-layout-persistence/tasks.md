## 1. Persisted-window boundary and wiring

- [x] 1.1 Add stable `WindowLayoutKeys` entries for Option Value Management, Add Variant, Bulk Add Variants, Design Area Editor, and Mockup Template Editor.
- [x] 1.2 Propagate the existing `IWindowGeometryStore` from `MainWindow` to `StoreEditorWindow` through an internal UI-layer property before the Store Editor is shown.
- [x] 1.3 Attach `WindowGeometryPersistence` once, before `ShowDialog`, to each named reusable Store Management editor using its own key and minimum dimensions.
- [x] 1.4 Keep Design Area Archive and all other transient confirmation dialogs outside the geometry-persistence boundary; preserve their existing ownership, draft, cancellation, and focus behavior.

## 2. Deterministic coverage

- [x] 2.1 Extend `WindowGeometryPersistenceTests` to verify normal-state geometry captures and restores both position and size, and remains isolated by window key.
- [x] 2.2 Add focused Avalonia headless Store Editor coverage through the shared persistence helper and existing editor/confirmation interaction tests; the code inspection confirms only the five reusable editors are attached and confirmations are excluded.
- [x] 2.3 Run the focused App test project while iterating and correct any implementation or artifact drift revealed by the scenarios.

## 3. Completion verification

- [x] 3.1 Run `openspec validate fix-secondary-editor-window-layout-persistence --strict` and correct any artifact validation failures.
- [x] 3.2 Run `dotnet test .\FusionCanvas.sln` and record criterion-level results in `verification.md` before archiving.
- [x] 3.3 Optional live Windows multi-monitor check not run; deterministic headless and normalizer coverage are the completion evidence.
