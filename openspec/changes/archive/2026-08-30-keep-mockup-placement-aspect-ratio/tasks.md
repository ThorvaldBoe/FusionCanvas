## 1. Ratio model and interaction logic

- [x] 1.1 Extend `MockupPlacementEditor` with safe `AspectRatio` and `KeepAspectRatio` properties, proportional pointer/keyboard resize, and in-bounds rounding/clamping.
- [x] 1.2 Extend `CatalogSetupViewModel` to derive the ratio from the selected Design Area, default/reset the option safely on selection changes, and synchronize numeric width/height edits while enabled.

## 2. Accessible editor integration

- [x] 2.1 Bind ratio and opt-out state into the compact and enlarged placement editors with an accessible responsive **Keep aspect ratio** checkbox.
- [x] 2.2 Preserve existing shared mapping, save/reopen, archived/read-only, and invalid-context behavior without introducing a second persistence path.

## 3. Automated verification

- [x] 3.1 Add framework-free placement-control tests for valid/invalid ratios, drag/resize, keyboard resize, clamping, and opt-out behavior.
- [x] 3.2 Add view-model tests for ratio derivation, numeric width/height synchronization, Design Area changes, and saved/reopened effective state.
- [x] 3.3 Add Avalonia headless coverage for checkbox accessibility, enabled/default state, responsive layout, and compact/enlarged binding synchronization.
- [x] 3.4 Run criterion-level verification for every scenario in both delta specs and record method, result, evidence, and limitations in `verification.md`.

## 4. Completion gates

- [x] 4.1 Run strict OpenSpec validation and correct any artifact/spec issues found.
- [x] 4.2 Run the deterministic solution baseline `dotnet test .\\FusionCanvas.sln` and resolve scoped failures.
- [x] 4.3 Complete the change retrospective/learning review with reusable lessons promoted or explicitly deferred.
