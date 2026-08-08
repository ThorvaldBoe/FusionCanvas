## 1. Selection model and projection state

- [x] 1.1 Add framework-free selection state with selected stable IDs, active ID, anchor ID, visible-range resolution, toggle/range/select-all operations, and reconciliation after filtering or reload.
- [x] 1.2 Add effective hierarchy-source normalization that removes selected descendants beneath selected groups and exposes deterministic validation inputs for mixed Item/group operations.
- [x] 1.3 Add unit tests for plain selection, Ctrl toggling, Shift and Ctrl+Shift ranges, Ctrl+A visible-only behavior, anchor updates, nested-source normalization, and removed/hidden ID reconciliation.

## 2. Navigation view model and tree presentation

- [x] 2.1 Extend `WorkspaceTreeViewModel` to own or coordinate the selection model while preserving canonical active selection, inspector synchronization, existing clipboard behavior, and tab synchronization.
- [x] 2.2 Update tree row state and theme classes/resources so the active row uses a brighter highlight and other selected rows use a dimmer highlight while inactive/archive and focus states remain readable.
- [x] 2.3 Update `MainWindow.axaml` context-menu bindings and accessibility text for single-selection versus multi-selection actions, selected counts, and the new middle-click/Ctrl/Shift gestures.
- [x] 2.4 Add Avalonia headless tests for row construction, active-versus-selected visual state, selection-aware menu visibility, and preservation of selection when filtering or refreshing.

## 3. Pointer and keyboard interaction

- [x] 3.1 Replace Ctrl-click tab opening in `MainWindow.axaml.cs` with plain/Ctrl/Shift/Ctrl+Shift selection routing and preserve expander and inline-edit hit behavior.
- [x] 3.2 Add Ctrl+A handling that selects visible selectable entities only and does not intercept text-editor selection or inline editing shortcuts.
- [x] 3.3 Add middle-click tab opening and context-menu `Open in new tab`/`Open in new tabs` commands with duplicate-tab prevention and multi-selection preservation.
- [x] 3.4 Implement standard right-click behavior: preserve a selection when the clicked row is selected and replace it with a sole selection otherwise.
- [ ] 3.5 Add deterministic headless routed-input tests for pointer modifiers, keyboard focus protection, middle-click, right-click selection behavior, and tab deduplication.

## 4. Selection-aware application operations

- [x] 4.1 Define narrow application-facing request/result contracts for captured selections, normalized sources, skipped entities, failures, and post-operation selection reconciliation.
- [x] 4.2 Implement selection-aware Duplicate orchestration using existing Item/group services and effective-source semantics.
- [x] 4.3 Implement confirmed multi-entity Archive and Delete orchestration, including protected/ineligible entities, atomicity where supported, tab closure, and surviving-context selection.
- [x] 4.4 Implement selected-entity Export using the existing CSV/file-picker boundary and exact effective selection scope.
- [x] 4.5 Implement Group creation flow with name validation, common-parent defaulting, explicit destination selection for mixed parents, and atomic move under the new group.
- [ ] 4.6 Add application tests for successful actions, cancellation, skipped entities, duplicate prevention, name/destination validation, partial-result reporting, and persistence failure recovery.

## 5. Multi-source drag and drop

- [x] 5.1 Extend drag payload creation and parsing to carry a stable selection snapshot while retaining compatibility with the existing single `kind:id` payload.
- [x] 5.2 Add pre-drop validation for selected sources, selected descendants, cycles, cross-store destinations, archived/missing destinations, mixed-source legality, and filtered positional ambiguity.
- [x] 5.3 Implement one validated multi-source move path for Items and groups, normalizing nested sources and restoring the confirmed projection on save failure.
- [x] 5.4 Update drag-over/drop feedback and effects so invalid mixed destinations are visibly blocked and valid drops operate on the complete effective selection.
- [ ] 5.5 Add application and headless tests for Item-only, group-only, mixed selection, self/descendant rejection, selected-hierarchy rejection, valid ungrouping, and failed-save rollback.

## 6. Acceptance verification and quality gates

- [ ] 6.1 Run criterion-level tests for every scenario in `multi-selection`, `group-actions`, and modified `group-management` delta specs; add or correct tests where any scenario lacks direct evidence.
- [x] 6.2 Review the changed scope for UI guideline alignment, keyboard/focus behavior, destructive confirmation, selection preservation, architecture boundaries, and no accidental bulk-edit/Shopify/AI scope.
- [x] 6.3 Run `openspec validate` and resolve all validation errors in the change artifacts.
- [x] 6.4 Run the required solution baseline `dotnet test .\\FusionCanvas.sln` and record the result for the completed change.
