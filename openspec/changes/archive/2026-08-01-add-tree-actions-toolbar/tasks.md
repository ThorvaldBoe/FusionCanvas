# Tasks: add-tree-actions-toolbar

## 1. View-model toggle behavior

- [x] 1.1 In `src/FusionCanvas.App/Navigation/WorkspaceTreeViewModel.cs` add `_nextToggleExpands` (default true), `NextToggleExpands`, `CanToggleExpandCollapseAll` (`!IsFiltering` and at least one visible node with children), and `ExpandCollapseAllTooltip` with the exact strings and precedence from design Decision 5 ("Filtering already expands all groups" > "No groups to expand or collapse" > "Expand all groups"/"Collapse all groups").
- [x] 1.2 Add `ToggleExpandCollapseAllCommand` (constructed in the constructor with the other commands) and the private `ToggleExpandCollapseAll` method: guard on `CanToggleExpandCollapseAll`, run the expand or collapse path, flip `_nextToggleExpands`, raise `NextToggleExpands` and `ExpandCollapseAllTooltip`.
- [x] 1.3 Implement the expand path: set `IsExpanded = true` on every live node in `Flatten(Roots)` with `HasChildren` and add each non-draft node's `EntityId` to `_expandedIds` (in-place mutation only; never call `RefreshProjection` from the toggle).
- [x] 1.4 Implement the collapse path with draft protection: recursively collect `_editingNode`'s ancestor nodes from the visual tree, then collapse every `HasChildren` node outside that ancestor set and remove its `EntityId` from `_expandedIds` (leave other stores' remembered IDs untouched).
- [x] 1.5 Raise `CanToggleExpandCollapseAll` and `ExpandCollapseAllTooltip` from `RefreshProjection` (normal and early-return exits) so filter transitions, reloads, structural edits, and cancel-edit refresh enablement.

## 2. View-model tests

- [x] 2.1 `tests/FusionCanvas.App.Tests/WorkspaceTreeViewModelTests.cs`: default state — pending action is expand, tooltip is "Expand all groups", toggle enabled on a tree whose topics have children.
- [x] 2.2 First toggle expands every niche and group node including nested levels (all `HasChildren` nodes report `IsExpanded`).
- [x] 2.3 Second toggle collapses every topic node.
- [x] 2.4 Remembered-state semantics: after expand-all, manually collapse one node, then toggle → every topic node collapses and the pre-click tooltip was "Collapse all groups".
- [x] 2.5 Retention: expand-all, then rebuild the projection (reload through the fake repository) → topic nodes remain expanded.
- [x] 2.6 Filter interaction: setting `QueryText` (or any filter) disables the toggle and shows the filtering explanation tooltip; clearing filters re-enables it.
- [x] 2.7 Empty case: a store whose topics have no children (or no active store) disables the toggle with the "No groups to expand or collapse" tooltip.
- [x] 2.8 Draft protection: begin create on a nested group, then collapse-all → the draft's ancestor chain stays expanded and the draft remains in editing state while unrelated branches collapse.

## 3. Toolbar view

- [x] 3.1 In `src/FusionCanvas.App/Views/MainWindow.axaml` insert `TreeActionsToolbar` (`DockPanel.Dock="Top"`, small bottom margin matching the pane rhythm) between the filter `StackPanel` and the tree `Grid`, containing a horizontal `StackPanel`.
- [x] 3.2 Add `ExpandCollapseAllButton` with `Classes="iconButton"`, command/enablement/tooltip/automation-name bindings per design §2, and a `Grid` content with two 14px `PathIcon`s (expand-all and collapse-all path data from design Decision 6) whose `IsVisible` binds `WorkspaceTree.NextToggleExpands` and its negation, using the existing icon foreground brush. No code-behind or new styles.

## 4. Headless view tests

- [x] 4.1 `tests/FusionCanvas.App.Tests/MainWindowLayoutTests.cs`: `TreeActionsToolbar` is constructed in the left pane between the filter controls and `WorkspaceTreeControl` (assert order within the `DockPanel`).
- [x] 4.2 `ExpandCollapseAllButton` tooltip and `AutomationProperties.Name` equal `WorkspaceTree.ExpandCollapseAllTooltip` and track it after a toggle; the two `PathIcon`s swap `IsVisible`; `IsEnabled` tracks `CanToggleExpandCollapseAll` (enabled by default in the fixture workspace, disabled once filter text is set).

## 5. Verification and baseline

- [x] 5.1 Criterion-level pass: confirm every acceptance scenario in `specs/navigation-tree/spec.md` is covered by a named test from tasks 2.1–2.8 and 4.1–4.2 per the design mapping table; run each new test individually and record results for `verification.md`.
- [x] 5.2 `openspec validate add-tree-actions-toolbar --strict` and `openspec validate --all --strict` pass.
- [x] 5.3 `dotnet build .\FusionCanvas.sln` succeeds warning-clean.
- [x] 5.4 `dotnet test .\FusionCanvas.sln` passes (full baseline, including the new tests).