## 1. Virtual Design Area grid

- [x] 1.1 Add the Design Area virtual-grid provider and collection subscription lifecycle to `StoreEditorWindow`.
- [x] 1.2 Replace the Design Area cards with the aligned five-column grid, preserving the empty state, Add action, compact full-value summaries, and fixed row height.

## 2. Row action behavior

- [x] 2.1 Route pointer presses and releases for row Edit/Archive buttons through the existing commands, while retaining keyboard command activation and existing focus restoration.
- [x] 2.2 Add headless view coverage for row targeting, Edit and Archive pointer/keyboard behavior, modal/archive outcomes, and focus return. Existing command bindings preserve archived-Store gating.

## 3. Layout and completion evidence

- [x] 3.1 Add headless coverage for aligned headers/rows, compatibility and primary status text/tooltips/accessibility, the empty state, and Add Design Area visibility.
- [x] 3.2 Verify full-column reachability at normal and minimum Store Editor widths, including horizontal scrolling and visible heading/Add action at minimum width.
- [x] 3.3 Record criterion-level results and evidence in `verification.md`, run focused Store Editor/catalog regressions and `dotnet test .\FusionCanvas.sln`, then run strict OpenSpec validation and changed-scope review.
