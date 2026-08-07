# Verification

## Current Status

Implementation is in progress. Selection state, selection-aware tab opening, drag payloads/validation, multi-source move orchestration, and multi-selection duplicate are implemented. Destructive group actions, selected export/group dialogs, and headless routed-input coverage remain.

## Criterion Evidence

| Acceptance area | Result | Evidence / limitation |
| --- | --- | --- |
| Plain/Ctrl/Shift/Ctrl+Shift/Ctrl+A selection | Partial pass | `WorkspaceTreeMultiSelectionTests` covers the framework-free model; routed Avalonia input remains to be tested. |
| Active versus dim selected visual states | Implemented, unverified | `MainWindow.axaml` and `App.axaml` add `multiSelected` styling; headless visual test remains. |
| Middle-click and new-tab context action | Implemented, unverified | Pointer routing and tab events are wired; headless pointer/context-menu test remains. |
| Right-click selection preservation | Implemented, unverified | `PrepareContextSelection` is wired from tree pointer handling; headless routed-input test remains. |
| Group-action availability and confirmations | Partial | Multi-selection Duplicate and tab actions are wired; Archive/Delete/Export/Group flows remain. |
| Duplicate outcome | Implemented, unverified | Selection-aware Item/group duplication uses effective-source normalization and restoration on save failure; focused orchestration tests remain. |
| Mixed selection normalization | Pass at unit level | `WorkspaceTreeMultiSelectionTests.NormalizeSelectionRemovesItemsInsideSelectedGroup`. |
| Safe multi-source drag/drop | Partial pass | Stable selection payloads, pre-drop validation, and restoration path are implemented; focused move/rollback tests remain. |
| Save failure rollback and selection reconciliation | Implemented, unverified | Multi-source move and duplicate use repository restoration and selection restoration; failure-path tests remain. |
| Keyboard/focus safety | Partial | Ctrl+A is guarded by existing TextBox focus check; headless focus regression test remains. |

## Commands Run

- `openspec validate support-multi-selection-group-actions` — passed before implementation.
- `dotnet build .\\src\\FusionCanvas.App\\FusionCanvas.App.csproj --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet test .\\tests\\FusionCanvas.App.Tests\\FusionCanvas.App.Tests.csproj --no-restore --filter FullyQualifiedName~WorkspaceTreeMultiSelectionTests -v normal` — build passed; this checkout did not emit a test execution summary.
- `openspec validate support-multi-selection-group-actions` — passed after implementation changes.
- `dotnet test .\\FusionCanvas.sln --no-restore -v normal` — solution VSTest target/build passed with 0 warnings and 0 errors; this checkout did not emit individual test execution summaries.

## Remaining Verification

- Add and run Avalonia headless tests for routed pointer/keyboard/context-menu behavior and visual state.
- Add application tests for multi-source move, duplicate restoration, and remaining group actions.
- Implement and verify Archive, Delete, Export, and Group flows.
- Run `openspec validate` and the full `dotnet test .\\FusionCanvas.sln` baseline before completion.
