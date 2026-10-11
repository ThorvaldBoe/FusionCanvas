## 1. Virtualize Printify product selection

- [x] 1.1 Add a complete, stable accessible selection name to the catalog-import item view model.
- [x] 1.2 Replace the Printify product ItemsControl with the fixed-row VirtualDataGrid and aligned, non-sortable columns while preserving existing panel states/actions.

## 2. Verify selector behavior

- [x] 2.1 Add a large-catalog headless view test for row virtualization, full text access, checkbox keyboard/accessibility, selection through row recycling, and confirmation of exact selected product IDs.
- [x] 2.2 Run focused Store Editor and catalog-import view-model tests; verify existing Store scope, draft, cancellation, and failure coverage.
- [x] 2.3 Run the solution test baseline and strict OpenSpec validation; record every acceptance scenario in `verification.md` and review the final diff.
