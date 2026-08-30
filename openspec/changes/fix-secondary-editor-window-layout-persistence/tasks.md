## 1. Persisted-window boundary and wiring

- [ ] 1.1 Add stable `WindowLayoutKeys` entries for Option Value Management, Add Variant, Bulk Add Variants, Design Area Editor, and Mockup Template Editor.
- [ ] 1.2 Propagate the existing `IWindowGeometryStore` from `MainWindow` to `StoreEditorWindow` through an internal UI-layer property before the Store Editor is shown.
- [ ] 1.3 Attach `WindowGeometryPersistence` once, before `ShowDialog`, to each named reusable Store Management editor using its own key and minimum dimensions.
- [ ] 1.4 Keep Design Area Archive and all other transient confirmation dialogs outside the geometry-persistence boundary; preserve their existing ownership, draft, cancellation, and focus behavior.

## 2. Deterministic coverage

- [ ] 2.1 Extend `WindowGeometryPersistenceTests` to verify normal-state geometry captures and restores both position and size, and remains isolated by window key.
- [ ] 2.2 Add focused Avalonia headless Store Editor tests covering the five editor-window persistence attachments and absence of a transient-confirmation geometry key.
- [ ] 2.3 Run the focused App test project while iterating and correct any implementation or artifact drift revealed by the scenarios.

## 3. Completion verification

- [ ] 3.1 Run `openspec validate fix-secondary-editor-window-layout-persistence --strict` and correct any artifact validation failures.
- [ ] 3.2 Run `dotnet test .\FusionCanvas.sln` and record criterion-level results in `verification.md` before archiving.
- [ ] 3.3 Optionally perform and record a supplemental disposable-settings Windows check for reopening Manage Stores and one nested editor, including a safe return from an unavailable screen.
