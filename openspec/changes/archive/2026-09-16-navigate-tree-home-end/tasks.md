## 1. Navigation behavior

- [x] 1.1 Add visible-tree depth-first boundary selection to `WorkspaceTreeViewModel`, preserving normal single-selection coordination and no-op behavior for empty projections.
- [x] 1.2 Handle Home and End in `MainWindow` keyboard routing, focus the selected tree container, and preserve text-box caret behavior.

## 2. Verification

- [x] 2.1 Add focused ViewModel and Avalonia headless tests covering Home/End, nesting, collapsed/filtered/empty projections, selection aftermath, and text-box exclusion.
- [x] 2.2 Run criterion-level focused tests and update `verification.md` with evidence for every acceptance scenario.
- [x] 2.3 Run `openspec validate` and `dotnet test .\FusionCanvas.sln`; resolve any failures within scope.
