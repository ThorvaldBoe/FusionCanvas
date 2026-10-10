## 1. Implement virtual Variant pricing grid

- [x] 1.1 Replace the Variant pricing ItemsControl with an aligned, bounded VirtualDataGrid and direct editable cell templates.
- [x] 1.2 Bind the grid to a refreshed in-memory snapshot of the active Manual Listing Details Variant rows; clean up collection subscriptions with view lifetime.
- [x] 1.3 Add headless coverage for aligned columns, identity-preserving TwoWay edits, keyboard focus traversal among realized rows, and editability gating.

## 2. Verify and record

- [x] 2.1 Run focused tests and `dotnet test .\FusionCanvas.sln`; record limitations and results in verification.md.
- [x] 2.2 Run strict OpenSpec validation and changed-scope review; update acceptance evidence and prepare the retrospective.
