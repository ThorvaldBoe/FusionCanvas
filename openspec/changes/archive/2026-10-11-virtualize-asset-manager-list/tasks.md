## 1. Virtualized asset presentation

- [x] 1.1 Configure the Assets window collection with a vertically virtualizing panel while preserving the existing row template and bounded scrolling viewport.
- [x] 1.2 Add a headless view test with a large asset collection that verifies bounded row realization, scrolling to later rows, full accessible text, and preserved row controls.

## 2. Verification and delivery artifacts

- [x] 2.1 Run focused asset view and view-model tests; fix any regressions in import, preview, purpose editing, removal confirmation, and empty-state behavior.
- [x] 2.2 Map every acceptance scenario to concrete results in `verification.md`, then run strict OpenSpec validation.
- [x] 2.3 Run `dotnet test .\FusionCanvas.sln -m:1` and `git diff --check`; record results in `verification.md`.
