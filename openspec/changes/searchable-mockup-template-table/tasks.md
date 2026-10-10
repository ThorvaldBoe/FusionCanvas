## 1. Search and archive presentation state

- [x] 1.1 Add transient Mockup Template search to `CatalogSetupViewModel`, filtering all displayed fields case-insensitively in source order and clearing on management exit or Offering change.
- [x] 1.2 Replace immediate template archive with request/confirm/cancel state tied to an active template identity; ensure cancel and dismissal cannot call the archive service.

## 2. Virtual table and guarded row actions

- [x] 2.1 Replace the Mockup Template card list with a bounded aligned, single-select VirtualDataGrid showing all required summary columns; add selected-row full-detail and contextual actions, and preserve true-empty, no-results, provider, and no-Design-Area states.
- [x] 2.2 Bind the grid to a view-owned provider snapshot and refresh/rebind/detach subscriptions across row updates, search changes, DataContext replacement, and window close.
- [x] 2.3 Preserve selected-template Edit and Duplicate routes, add accessible contextual action names, add the Cancel-first soft-archive confirmation window, and return focus correctly after cancellation or archive.

## 3. Acceptance coverage and evidence

- [x] 3.1 Add framework-free App tests for filtering semantics and archive request/cancel/confirm behavior; retain existing service coverage for soft-archive revision retention.
- [x] 3.2 Add headless Avalonia coverage for grid columns, provider refresh and scoping, no-results and no-Design-Area states, full-text accessibility, pointer/keyboard action routing, and confirmation focus/dismissal.
- [x] 3.3 Create `verification.md` mapping each delta scenario to focused tests and final results; correct any acceptance mismatch and rerun affected checks.
- [x] 3.4 Run focused App tests, `dotnet test .\FusionCanvas.sln -m:1`, `openspec validate --strict --type change searchable-mockup-template-table`, `openspec validate --strict --all`, and `git diff --check`; review changed scope and record results.
