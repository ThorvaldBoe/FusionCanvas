# Verification: add-tree-actions-toolbar

## Acceptance scenario → test mapping

| Spec scenario | Test | Result |
|---|---|---|
| Toolbar sits between filters and tree | `MainWindowLayoutTests.TreeActionsToolbar_IsBetweenFilterAreaAndWorkspaceTree` | PASS |
| Toggle presents the pending action (icon + tooltip + accessibility name) | `MainWindowLayoutTests.ExpandCollapseAllButton_TracksViewModelState` + `WorkspaceTreeViewModelTests.DefaultState_ToggleIsExpandAllAndEnabled` | PASS |
| First activation expands every topic | `WorkspaceTreeViewModelTests.FirstToggle_ExpandsEveryTopicNodeIncludingNestedLevels` | PASS |
| Second activation collapses every topic | `WorkspaceTreeViewModelTests.SecondToggle_CollapsesEveryTopicNode` | PASS |
| Manual node changes do not redirect the toggle | `WorkspaceTreeViewModelTests.RememberedState_ManualCollapseDoesNotRedirectToggle` | PASS |
| Toggle expansion survives a tree refresh | `WorkspaceTreeViewModelTests.ToggleExpansion_SurvivesTreeRefresh` | PASS |
| Toggle is disabled while filters are active | `WorkspaceTreeViewModelTests.FilterActive_DisablesToggleAndShowsFilterTooltip` + `MainWindowLayoutTests.ExpandCollapseAllButton_TracksViewModelState` (IsEnabled tracking) | PASS |
| Toggle is disabled when nothing can expand | `WorkspaceTreeViewModelTests.NoExpandableNodes_DisablesToggleWithNoGroupsTooltip` | PASS |
| Collapse-all protects an in-progress edit | `WorkspaceTreeViewModelTests.CollapseAll_ProtectsDraftAncestorChain` (verifies ancestor chain stays expanded while unrelated branch collapses) | PASS |

## Baseline commands

| Command | Result |
|---|---|
| `dotnet build .\FusionCanvas.sln` | warning-clean (0 errors) |
| `dotnet test .\FusionCanvas.sln` | 780/780 passed (Domain: 143, Application: 225, Integration: 121, App: 291) |
| `openspec validate add-tree-actions-toolbar --strict` | valid |
| `openspec validate --all --strict` | 35/35 passed |

## Evidence

- All 8 view-model tests pass (WorkspaceTreeViewModelTests) — including strengthened draft-protection test with unrelated-branch assertion
- Both new headless view tests pass (MainWindowLayoutTests)
- All 291 App tests pass (including the 10 new ones)
- Full solution 780 tests, 0 failures
- Tooltip strings verified: "Expand all groups", "Collapse all groups", "Filtering already expands all groups", "No groups to expand or collapse"
- Toggle icon swap verified: PathIcon IsVisible tracks NextToggleExpands
- Draft protection verified: ancestor chain stays expanded, draft remains in editing state; **unrelated branch collapses** (new assertion addressing VR-001)
- No Domain, Application, or Integration files touched — only App-layer production and tests

## Limitations

- Icon path data is visually verified only through IsVisible bindings (not pixel comparison); artwork is decorative and swappable per design Decision 6
- No live desktop check performed — the feature has no native-window, OS-input, or visual-judgment risk beyond what headless tests observe