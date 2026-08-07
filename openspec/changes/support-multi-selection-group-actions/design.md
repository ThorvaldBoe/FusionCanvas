## Context

The current Avalonia navigation tree has one `SelectedNode`, uses Ctrl-click to open a persistent tab, and already supports single-source group/item drag-and-drop. The requested behavior needs desktop-style multi-selection without losing the distinction between the active inspector context and the set affected by an operation.

The primary workflow is frequent navigation and organization of Items and groups in the main workspace. Selection is a high-frequency interaction and belongs directly in the tree; destructive and multi-step actions should remain progressively disclosed through the existing row context menu and focused confirmation surfaces.

## Goals / Non-Goals

**Goals:**

- Add stable-ID multi-selection with plain, Ctrl, Shift, Ctrl+Shift, and Ctrl+A semantics.
- Keep active context, selection anchor, selected IDs, and persistent tabs as separate state.
- Use dimmed selection styling for non-active selected rows and brighter styling for the active row.
- Provide middle-click and context-menu tab opening without duplicate tabs.
- Execute the initial group actions on normalized Item/group selections.
- Make drag/drop safe for mixed selections and hierarchy cycles.
- Preserve filtering, keyboard accessibility, current tab synchronization, and existing single-source operations.
- Verify pointer/keyboard routing and visual state with deterministic headless tests where framework behavior is material.

**Non-Goals:**

- Shopify publishing, marketplace rate limiting, background jobs, or an always-visible job center.
- AI auto-grouping.
- Bulk edits such as status, stage, tags, price, or mockup template; those remain a later module.
- Changing the persistence schema unless an implementation spike proves a stored selection or batch record is required; selection itself is session state.
- Adding niche rows to the multi-selection set; niches remain navigation/drop destinations.

## Decisions

### 1. Keep active context separate from multi-selection

The navigation view model will retain one canonical active `WorkspaceTreeSelection` for inspector/document synchronization and add a session-only selection model containing selected entity IDs, an anchor, and the active entity ID. This avoids making the inspector ambiguous while allowing operations to target several entities.

Alternative considered: use the existing `SelectedNode` as a collection. Rejected because tab synchronization, inspector loading, and current group-management commands all assume one canonical selection.

### 2. Use desktop modifier semantics in the tree

Plain click replaces, Ctrl-click toggles, Shift-click selects a visible display-order range, Ctrl+Shift-click unions a range, and Ctrl+A selects all visible selectable active Items/groups. Right-click preserves an existing selection only when the clicked row is already selected; otherwise it first makes that row the sole selection.

Alternative considered: an explicit selection mode with checkboxes. Rejected because it consumes persistent workspace space and is less familiar for the requested Photoshop/Affinity workflow.

### 3. Use middle-click for tabs

Ctrl-click becomes selection toggle. Middle-click and `Open in new tab` open a persistent tab. Opening multiple tabs preserves the selection and does not create duplicate tabs. This keeps navigation selection and tab creation discoverable without overloading one gesture.

### 4. Normalize hierarchy sources before operations

The application layer will normalize selected IDs into effective roots. If a selected group contains another selected group or Item, the descendant is excluded from hierarchy operations. Validation will then reject selected targets, descendants of selected groups, cross-store destinations, inactive destinations, and cycles before any save.

This normalization is required for mixed selections: selecting a group and one of its Items must not move or delete the Item twice.

### 5. Reuse application services behind a selection-aware orchestration boundary

The App layer will translate UI gestures into stable selection snapshots. A selection-aware application service or coordinator will validate and orchestrate duplicate, archive, delete, export, group, and multi-source move operations using existing Item, Group, CSV export, and repository contracts. Domain rules remain in existing hierarchy/item policies; Avalonia event handlers do not implement business validation.

### 6. Keep group actions in the context menu

The tree will not gain a permanent group-action toolbar. The context menu will show the current selection count and only the actions applicable to that selection. Grouping uses a focused name/destination dialog, while Delete and Archive use confirmation surfaces. Export uses the existing file-picker boundary and exports exactly the selected effective entities.

### 7. Preserve selection through projection changes by ID

Filtering, reload, and action completion will reconcile selected IDs against the authoritative projection. Visible surviving IDs remain selected; hidden IDs are not included by Ctrl+A; removed IDs are cleared. The active context falls back to a surviving selected entity or nearest valid context when the previous active entity is removed.

## Risks / Trade-offs

- [Risk] Avalonia TreeView pointer routing and modifier handling may vary between row, expander, and context-menu visuals. → Keep gesture interpretation in the existing MainWindow code-behind boundary, add headless routed-input tests, and preserve expander hit-target behavior.
- [Risk] Range selection over a hierarchy can feel ambiguous. → Define range order as visible flattened tree display order and exclude non-selectable niches; test collapsed, filtered, and mixed Item/group ranges.
- [Risk] Mixed group/Item moves can partially mutate the hierarchy. → Validate the complete normalized source set before persistence and require an atomic application boundary for multi-source moves.
- [Risk] Existing single-item services may not provide atomic multi-entity operations. → Introduce a narrow selection-aware orchestration contract and add repository snapshot rollback/transaction support where required; do not fake atomicity in the UI.
- [Risk] Delete/archive can invalidate open tabs and the active inspector. → Reconcile authoritative state after completion, close removed tabs, and retain surviving selection by stable ID.
- [Risk] Middle-click behavior may not be obvious on trackpads. → Keep `Open in new tab` in the context menu and update row tooltip/accessibility text.

## Migration Plan

No persisted migration is expected because selection is session state. Update the existing Ctrl-click tab behavior and tooltip/context-menu wording in one UI change. Existing saved workspaces, clipboard payloads, item/group identities, and tab persistence remain compatible.

If multi-source move persistence requires a repository transaction, implement it behind the existing repository abstraction and verify save-failure rollback with temporary test resources. Rollback is code-level only; no database schema migration is planned.

## Open Questions

No product-level questions remain for this module. Implementation must not reopen the following decisions: niche rows are not multi-selected; visible flattened tree order defines ranges; mixed hierarchy moves normalize nested sources; invalid drops are blocked before persistence; and bulk field edits, Shopify, AI grouping, and background jobs are out of scope.

## Implementation Plan

1. Add a focused navigation selection model and records for selected IDs, anchor ID, active ID, visible-range resolution, and effective hierarchy-source normalization. Keep it framework-free and test it in `tests/FusionCanvas.App.Tests` or a small Application-facing unit boundary as ownership dictates.
2. Extend `WorkspaceTreeViewModel` to reconcile selection with projection/reload, expose selected-row state and action eligibility, preserve canonical active selection, and keep existing single-selection commands compatible.
3. Update `MainWindow.axaml.cs` pointer and keyboard routing: plain/Ctrl/Shift clicks, Ctrl+A, right-click selection preservation, middle-click tab opening, and drag initiation from the effective selection. Do not trigger global shortcuts from text editors or inline editors.
4. Update tree row presentation and context-menu bindings in `MainWindow.axaml` for bright active versus dim selected styles, accessible gesture guidance, single versus multi-selection menus, and selected counts.
5. Add selection-aware application orchestration for duplicate, archive, delete, export, group creation/move, and multi-source move validation. Reuse existing `IItemManagementService`, `IGroupManagementService`, CSV export, and repository boundaries where they can preserve atomicity; add only narrow contracts needed for complete validation and result reporting.
6. Extend drag/drop payloads from one `kind:id` source to a stable selection snapshot, while accepting the existing single-source payload for compatibility. Validate selected targets, descendants, cycles, store boundaries, active destinations, and filtered positional ambiguity before showing a move effect.
7. Add focused confirmation/result surfaces for multi-delete, multi-archive, and grouping. Preserve invoking context, support keyboard confirmation/cancellation, and report skipped/failed entities without hiding partial outcomes.
8. Add deterministic tests for selection semantics, visual state, context-menu routing, middle-click, Ctrl+A focus protection, effective-source normalization, invalid mixed drops, successful multi-source moves, persistence failures, tab deduplication, and post-action selection reconciliation.
9. Run strict OpenSpec validation and the full `dotnet test .\\FusionCanvas.sln` baseline. Use an optional disposable live desktop check only if headless tests cannot establish middle-click or native drag behavior.

## Acceptance-to-Verification Plan

| Acceptance area | Planned evidence |
| --- | --- |
| Plain/Ctrl/Shift/Ctrl+Shift/Ctrl+A selection | Framework-free selection tests plus Avalonia headless routed-input tests |
| Active versus dim selected visual states | Avalonia headless view test inspecting row classes/resources and active context |
| Middle-click and new-tab context action | Headless pointer/context-menu tests plus tab-deduplication unit tests |
| Right-click selection preservation | Headless context-menu routing test |
| Group-action availability and confirmations | View-model/application tests and headless menu-state tests |
| Duplicate/archive/delete/export/group outcomes | Application orchestration tests with deterministic repository/file-picker doubles |
| Mixed selection normalization | Framework-free hierarchy normalization tests |
| Safe multi-source drag/drop | Application validation tests plus headless drag/drop feedback tests |
| Save failure rollback and selection reconciliation | Repository failure tests and view-model projection tests |
| Keyboard/focus safety | Headless focus/routed-key tests and existing inline-editor regression tests |
