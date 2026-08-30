## 1. Enlarged editor surface

- [x] 1.1 Add a focused responsive `MockupPlacementEditorWindow` that hosts the existing placement control with shared view-model bindings, accessible Close action, Escape dismissal, and predictable focus.
- [x] 1.2 Wire the Mockup Template editor to open/close the transient enlarged window only for an editable selected image, without adding a second save or discard path.

## 2. Compact preview launch and interaction

- [x] 2.1 Add the lower-right magnifying-glass-plus launch control with tooltip, automation name, keyboard activation, and correct enabled/visible state.
- [x] 2.2 Preserve compact preview drag/resize behavior and shared mapping synchronization, including selected image/path/dimensions and archived/read-only gating.

## 3. Automated verification

- [x] 3.1 Add or update focused placement-control tests for enlarged-size drag, independent resize, clamping, and keyboard behavior.
- [x] 3.2 Add Avalonia headless editor tests for launch, accessible names/focus, state preservation, two-way synchronization, Escape/Close behavior, and narrow responsive layout.
- [x] 3.3 Run criterion-level verification for every scenario in both delta specs and record method, result, evidence, and limitations in `verification.md`.

## 4. Completion gates

- [x] 4.1 Run strict OpenSpec validation and correct any artifact/spec issues found.
- [x] 4.2 Run the deterministic solution baseline `dotnet test .\\FusionCanvas.sln`; record unrelated existing failures without changing scope.
- [x] 4.3 Complete the change retrospective/learning review with reusable lessons promoted or explicitly deferred.
