## Context

The Store Editor's Mockup Template management region currently renders one three-action card per active template. Each card repeats the template name, target Design Area, joined Color labels, compatible Variant count, and revision/readiness status. The list is already scoped to the selected Offering and the no-Design-Area warning already blocks the user's next step with a route to Design Area management. The existing `MockupTemplateCardViewModel` is the presentation projection for these rows.

The repository's grid experiment shows that `VirtualDataGrid` needs an `IDataProvider`; an `ObservableCollection` alone will not measure/render rows. Its in-memory provider also has sorting snapshot caveats, so this change will disable sorting and replace the provider from the authoritative card snapshot whenever catalog rows or search changes. The existing archive service performs a reversible soft archive, retaining revisions, but the current row button invokes it immediately.

## Goals / Non-Goals

**Goals:**
- Present template summaries as aligned columns in a compact, bounded virtual table.
- Filter active templates by visible summary text with transient case-insensitive search while preserving order.
- Preserve full summary information, all row actions, Offering scoping, and the no-Design-Area warning.
- Require a deliberate confirmation before the existing soft-archive operation and return focus predictably.

**Non-Goals:**
- No sorting, selection model, paging, server/provider changes, database/API changes, or edits to template readiness and revision rules.
- No restore UI for archived templates; restore availability is outside the requested active-list workflow.
- No Appium journey for this focused catalog management change.

## Conceptual and Functional Design

The frequent workflow is scanning and locating a template in Store → Products → Offering → Manage Mockup Templates; Edit and Duplicate continue into the existing focused editor. Archive is occasional and destructive, so it remains on the affected row but requires a focused confirmation dialog. The list stays in the Store Editor's existing workspace region, with a finite viewport so it cannot displace the surrounding Offering setup indefinitely.

The table has fixed, non-sortable columns for Template name, Design Area, Colors, compatible Variants, and Revision/readiness. Summary values may be ellipsized at compact widths; each value's tooltip and AutomationProperties name retain its full text. Selecting a row exposes its complete summary and separately focusable Edit, Duplicate, and Archive buttons with names that include the template below the grid. Search matches case-insensitive substrings in the fields shown by the table, preserves catalog order, and has separate empty-catalog and no-match guidance. If a query hides the selected row, selection and contextual actions clear. Clearing the query restores every active template. Search text is session-only and clears when leaving this management view or changing Offering.

The no-Design-Area warning remains visible independently of the table/search state. Search does not enable template creation or suggest a missing Design Area can be skipped. The archive confirmation defaults keyboard focus to Cancel, closes without mutation on Cancel/Escape/window dismissal, and invokes the existing `ArchiveTemplateAsync` path only on explicit confirmation. After cancel, focus returns to the same row's Archive button. After successful archive, focus moves to the search field or next available row action as appropriate.

## Decisions

1. **Use the existing Mockup Template card projection and a view-owned provider snapshot.** Keep row construction/readiness in `CatalogSetupViewModel.RefreshCatalogViews` and keep `InMemoryDataProvider<MockupTemplateCardViewModel>` in `StoreEditorWindow`, matching the repo's virtual-grid experiment and other Store Editor grids. Reset the provider from filtered card rows on card collection updates and search changes. This avoids a new application/domain abstraction and preserves existing Offering scoping.
2. **Search displayed fields and do not sort.** Search includes name, target Design Area, joined Colors, compatible Variant summary, and revision/readiness; uses `StringComparison.OrdinalIgnoreCase`; preserves the current active-template order; and is not persisted. Sorting is excluded because it is not part of the title's explicit requirement and the current provider's sort snapshot behavior is documented as unsafe for incremental updates.
3. **Retain all summaries as text, tooltip, and accessible name.** Columns use fixed row heights and single-line ellipsis for bounded scanning. Full text remains available with a tooltip and the accessible name, so dynamic multi-line row sizing is unnecessary.
4. **Use selected-row contextual actions outside the grid.** An initial headless trial showed that the grid's tunneling pointer handler suppresses a button embedded in a cell even with explicit command binding. Place the buttons in a compact action strip below the grid and bind them to the selected template. Route pointer row selection into the presentation state and handle Up/Down while the grid is focused because the component does not select rows with the keyboard in this configuration. This also provides a stable detail area for full summaries. Headless tests verify row selection, identity-specific actions, and keyboard order.
5. **Confirm the soft archive in a focused dialog.** The modal names the selected template, explains that active-list/new-generation availability ends while saved revisions remain retained, defaults focus to Cancel, and closes false on Escape or dismissal. Confirmation delegates to the existing soft-archive service. This adds an explicit safeguard without changing persistence lifecycle.
6. **No real-desktop Appium journey.** Headless Avalonia tests can deterministically exercise template realization, alignment, row selection, contextual button routing, search binding, tooltip/accessibility values, and modal focus/dismissal. Existing application/integration tests cover soft-archive persistence and revision retention; no native OS interaction or visual-only judgment is unique to this small surface.

## Risks / Trade-offs

- [Risk] Row buttons may intercept or lose pointer/keyboard input inside the virtual grid → Exercise real realized cell templates with headless pointer clicks and Tab traversal; correct focus or event routing before delivery.
- [Risk] Filtering or Offering changes leave stale provider rows visible → Reset the provider from current active card snapshots on collection changes, query changes, and DataContext replacement; detach subscriptions on window close.
- [Risk] Compact columns obscure useful summary detail → Provide full-text tooltips and accessible names and verify the full values in headless tests.
- [Risk] A dismissed archive dialog could archive or leave focus stranded → Only a true dialog result invokes the command; Cancel is initially focused and cancel/confirm outcomes have explicit focus handling.
- [Trade-off] Fixed row height prevents wrapping → It keeps the viewport dense and virtualized while preserving full values through accessible text and tooltips.

## Migration Plan

No data migration is needed. The table uses the existing template card projections and soft-archive service. Rollback consists of restoring the existing ItemsControl card list and removing the Store Editor provider and archive confirmation dialog/state.

## Open Questions

None. Search semantics, sort exclusion, full-text treatment, action placement, and archive confirmation behavior are resolved above.

## Implementation Plan

1. **Presentation filtering and selection:** In `src/FusionCanvas.App/Stores/CatalogSetupViewModel.cs`, add a transient `MockupTemplateSearchText`, filtered-card projection, and a selected-card property. Search all displayed labels case-insensitively without reordering; clear selection when filtering hides it; reset query on leaving template management or switching Offering. Expose no-results and selected-detail state without changing catalog data.
2. **Table and provider lifecycle:** In `src/FusionCanvas.App/Stores/StoreEditorWindow.axaml`, replace the Mockup Template `ItemsControl` with a named `VirtualDataGrid`, header/search, bounded height, fixed-height aligned columns, full-text tooltip/automation names, single-row selection, and a selected-template detail/action strip below the grid. Keep the existing provider message, true-empty message, Design Area blocked warning/route, and Add control. In `StoreEditorWindow.axaml.cs`, own `InMemoryDataProvider<MockupTemplateCardViewModel>`, route pointer selection and focused-grid Up/Down navigation to the selected card, subscribe to the active `CatalogSetupViewModel` card collection and search property, refresh whole snapshots, rebind on DataContext replacement, and detach on close.
3. **Archive safety and focus:** In `CatalogSetupViewModel`, replace immediate Archive execution with request/confirm/cancel state carrying the active selected template ID/name and a confirmation-request event. Confirmation delegates to the existing archive service; cancel and dismissal clear pending state without mutation. Add `MockupTemplateArchiveConfirmationWindow` and its code-behind, default focus to Cancel, handle Escape/window close as cancel, and route confirmation results from `StoreEditorWindow`. Return focus to the selected-template Archive action on cancel and search on success.
4. **Tests:** Extend `CatalogSetupViewModelTests` for case-insensitive filtering across every displayed field, stable order, whitespace clearing, query reset on scope/navigation changes, and archive request/cancel/confirm service behavior. Extend `StoreEditorHeadlessTests` or focused grid integration coverage to verify real table construction, aligned columns, provider refresh/rebind, empty/no-results and no-Design-Area guidance, full-text accessibility/tooltips, keyboard and pointer actions routing to the intended row, and confirmation focus/dismissal.
5. **Verification:** Add `verification.md` mapping every delta scenario to focused evidence. Run focused App tests, `dotnet test .\FusionCanvas.sln -m:1`, strict OpenSpec change validation and full strict validation, plus `git diff --check` and changed-scope review. No persistence changes or migration are expected.

## Acceptance Scenario Verification Plan

| Scenario | Planned verification |
| --- | --- |
| User scans Mockup Template rows | Headless Store Editor test asserts row identities, visible summary columns, equal cell bounds for rows, finite virtual viewport, and complete selected-template detail/action state. |
| User searches displayed template details | Catalog Setup test checks case-insensitive matches against each displayed value, stable source order, and no catalog mutation; headless binding test checks provider refresh. |
| User clears search or receives no matches | Catalog Setup test checks whitespace restores all active rows; headless view test checks no-results guidance and editable query. |
| User reads a long summary | Headless test checks tooltip and automation names retain untruncated Design Area, Color, Variant, and revision/readiness strings. |
| Offering has no Design Areas | Existing or extended Store Editor headless test asserts warning and Manage Design Areas route remain visible with an empty table. |
| Search text is not persisted | Catalog Setup test checks reset on leaving management and Offering switch; no persistence layer or DTO is touched. |
| User edits or duplicates the selected template | Headless tests select a table row, invoke its contextual command, and assert editor or duplicate identity. |
| User selects a template with a keyboard or assistive technology | Headless tests verify row selection and announcement, contextual action names, and predictable Tab order. |
| User cancels archive confirmation | View-model test verifies no service call; headless dialog test checks Cancel focus, Escape/dismissal, and focus restoration to the contextual Archive action. |
| User confirms archive | View-model test verifies only the requested ID is sent to the existing service; existing application tests verify soft archive and retained revision data; headless test verifies row removal. |
