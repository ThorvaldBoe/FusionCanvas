# Design: add-tree-actions-toolbar

## Context

The left navigation pane in `MainWindow.axaml` is a `DockPanel`: header, store selector, creation buttons, search/filter controls (all `DockPanel.Dock="Top"`), then the workspace `TreeView` as the fill child. Topic nodes (niches at the root, nested groups beneath) start collapsed. Expansion state lives in two places that stay in sync: `WorkspaceTreeNodeViewModel.IsExpanded` (two-way bound to `TreeViewItem.IsExpanded` via a window style) and `WorkspaceTreeViewModel._expandedIds` (a `HashSet<Guid>` that survives projection rebuilds; `ToNode` restores `IsExpanded = _expandedIds.Contains(id) || IsFiltering`). While any filter is active, the projection force-expands every node, per-node expansion capture is suspended, and the pre-filter expansion set is restored when filters clear.

Creators currently expand topic nodes one at a time. The UI guidelines already list "compact tree actions" as a primary navigation-pane element and require icon-only buttons to be small, square, close to what they control, and tooltip-bearing; the store selector toggle (`ToggleStoreSelectorCommand` + `SelectorToggleGlyph` + `SelectorToggleTooltip`, "Expand stores"/"Collapse stores") is the in-repo template for this kind of button.

Discovery (with the product owner) resolved: the toggle acts on all topic nodes (niches + groups); it is disabled while filters are active; the icon is a swapping vector `PathIcon`; collapse-all keeps an in-progress inline editor's ancestors expanded.

## Goals / Non-Goals

**Goals:**

- A tree-actions toolbar strip docked between the filter controls and the workspace tree, designed to host tree-wide actions now and later.
- One tool on it: an expand/collapse-all toggle (icon-only vector button, hover tooltip + accessibility name naming the pending action).
- Toggle semantics: first activation expands all topics; subsequent activations alternate with the remembered previous toolbar action; manual per-node changes do not redirect it.
- Enablement: disabled while filters are active (tooltip explains why) and when the visible tree has no expandable topic nodes.
- Bulk expansion flows through the existing session expansion retention so it survives projection refreshes.
- Collapse-all never hides an in-progress inline create/rename editor.

**Non-Goals:**

- No other toolbar tools, no keyboard shortcut, no menu entry.
- No expansion persistence across application restarts.
- No changes to filter semantics, per-node expansion, reveal-on-tab-activation, or drag-and-drop auto-expansion.
- No Domain/Application/Integration changes; no persistence or schema work.

## Decisions

1. **Mutate the live node tree in place; never rebuild the projection on toggle.** Expand/collapse-all sets `IsExpanded` on the existing `WorkspaceTreeNodeViewModel` instances (containers follow through the two-way style binding) and mirrors the change into `_expandedIds` so future refreshes keep the state. Alternative considered: update `_expandedIds` and call `RefreshProjection()` — rejected because a rebuild recreates `Roots` from the projector and silently discards an in-progress draft node (drafts are inserted into `Children` outside the projection; `CancelEdit` already demonstrates that a refresh drops them).

2. **Toggle state is a remembered last action on the view model, default expand.** A private `_nextToggleExpands = true` field flips after every successful toggle; `NextToggleExpands`, `ExpandCollapseAllTooltip`, and the icon derive from it. Manual per-node expand/collapse, reveal-on-tab, and drag auto-expansion do not touch it (spec scenario "Manual node changes do not redirect the toggle"). Consequence accepted with the owner: after switching stores, the remembered action survives, so a single no-op click is possible (e.g. remembered "collapse" while the newly shown store is fully collapsed); the click still flips the pending action. Alternative considered: derive the next action from actual tree state (any collapsed → expand) — rejected; the owner specified last-action semantics and it stays predictable under manual edits.

3. **Scope is every expandable topic node in the current visual tree.** `Flatten(Roots)` filtered to `HasChildren` covers niches and groups (items are leaves; draft nodes report no children). Niches are included because expanding only groups beneath collapsed niches would render nothing. `_expandedIds` is updated per visible node — never cleared wholesale — because the set also carries remembered expansion for previously visited stores on the same view-model instance; clearing it would make switching back to an expanded store forget its state.

4. **Enablement is a bound property, not command `CanExecute`.** The shared `RelayCommand` accepts an optional `canExecute` but never raises `CanExecuteChanged` (add/remove are no-ops), so command-driven enablement would go stale; the house pattern for tree commands omits `canExecute` and binds `IsEnabled` to a view-model property (e.g. `CanCreateGroup`). New property: `CanToggleExpandCollapseAll => !IsFiltering && Flatten(Roots).Any(node => node.HasChildren)`. The command body still guards with the same predicate so logic stays correct if invoked programmatically.

5. **Disabled-state tooltip explains the reason.** `ExpandCollapseAllTooltip` returns, in precedence order: "Filtering already expands all groups" while filtering; "No groups to expand or collapse" when nothing is expandable; otherwise the pending-action text "Expand all groups" / "Collapse all groups". `AutomationProperties.Name` binds the same property so the accessibility name always matches the tooltip (spec requirement). Wording decision: the owner framed the feature as "expand/collapse all groups"; "groups" is kept as product vocabulary even though niches also toggle. Alternative considered: "Expand all topics" — recorded, not adopted; revisiting is a one-string change.

6. **Icon swaps between two vector `PathIcon`s.** The button content is a `Grid` containing two 14px `PathIcon`s whose `IsVisible` binds `NextToggleExpands` and its negation. Suggested artwork (Apache-2.0 Material Symbols, 24px viewport, visually equivalent substitutions are fine):
   - Expand all (`unfold_more`): `M12 5.83 15.17 9l1.41-1.41L12 3 7.41 7.59 8.83 9 12 5.83zm0 12.34L8.83 15l-1.41 1.41L12 21l4.59-4.59L15.17 15 12 18.17z`
   - Collapse all (`unfold_less`): `M7.41 18.59 8.83 20 12 16.83 15.17 20l1.41-1.41L12 14l-4.59 4.59zm9.18-13.18L15.17 4 12 7.17 8.83 4 7.41 5.41 12 10l4.59-4.59z`
   Alternative considered: text chevron glyphs matching the store toggle's ▲/▼ — the vector pair carries the expand/collapse-all metaphor more precisely and the header already uses a `PathIcon`.

7. **Collapse-all preserves the editing draft's ancestor chain.** When `_editingNode` is set, a recursive walk of the visual tree collects the draft's ancestor nodes; collapse skips those (both the `IsExpanded` set and the `_expandedIds` removal). The draft node itself has no children, so it is never an expansion target. Expand-all needs no special case (expanding hides nothing).

8. **Property-change plumbing rides the existing choke points.** `RefreshProjection` (every rebuild path: filter transitions, reloads, structural edits, cancel-edit) raises `CanToggleExpandCollapseAll` and `ExpandCollapseAllTooltip`; the toggle method raises `NextToggleExpands` and `ExpandCollapseAllTooltip`. No new events or services.

## Risks / Trade-offs

- [In-place mutation could drift from `_expandedIds` on a code path that rebuilds without capture] → `RefreshProjection(captureExpanded: true)` already re-captures from live nodes, and the toggle writes both sides; the "survives refresh" scenario has a dedicated test.
- [Store switch plus remembered toggle state can produce one no-op click] → accepted with the owner (Decision 2); the click still flips the pending action and harms nothing.
- [Icon path data is hand-copied vector artwork] → artwork is decorative and swappable; the spec asserts icon *swap*, not specific geometry, and the headless test checks `IsVisible` state, not path data.
- [Tooltip says "groups" while niches also toggle] → owner-approved vocabulary (Decision 5); single-string change if revisited.

## Migration Plan

Not applicable — presentation-only change; no data, schema, settings, or API changes. Rollback is a code rollback.

## Open Questions

None. All high-impact decisions were resolved during discovery (scope, filter interaction, icon approach, draft-edit behavior).

## Implementation Plan

Single coherent slice, two files of production code plus tests.

### 1. View model — `src/FusionCanvas.App/Navigation/WorkspaceTreeViewModel.cs`

Add to `WorkspaceTreeViewModel`:

- Field `private bool _nextToggleExpands = true;`
- `public bool NextToggleExpands => _nextToggleExpands;`
- `public bool CanToggleExpandCollapseAll => !IsFiltering && Flatten(Roots).Any(node => node.HasChildren);`
- `public string ExpandCollapseAllTooltip` with the precedence in Decision 5 (exact strings: "Filtering already expands all groups", "No groups to expand or collapse", "Expand all groups", "Collapse all groups").
- `public ICommand ToggleExpandCollapseAllCommand { get; }` — constructed in the constructor alongside the other commands: `new RelayCommand(_ => ToggleExpandCollapseAll())`.
- `private void ToggleExpandCollapseAll()`: guard on `CanToggleExpandCollapseAll`; if `_nextToggleExpands` call the expand path else the collapse path; flip `_nextToggleExpands`; raise `NextToggleExpands` and `ExpandCollapseAllTooltip`.
- Expand path: for every `node` in `Flatten(Roots)` with `node.HasChildren` and not on the draft path — set `IsExpanded = true`; if `!node.IsDraft` add `node.EntityId` to `_expandedIds`.
- Collapse path: compute the ancestor set of `_editingNode` (empty when null) via a new private recursive helper that walks `Roots` collecting ancestors of a target node; for every `node` in `Flatten(Roots)` with `node.HasChildren` whose `EntityId` is not in the ancestor set — set `IsExpanded = false` and remove `node.EntityId` from `_expandedIds`.
- In `RefreshProjection`, add `OnPropertyChanged(nameof(CanToggleExpandCollapseAll));` and `OnPropertyChanged(nameof(ExpandCollapseAllTooltip));` (both early-return and normal exits).

### 2. View — `src/FusionCanvas.App/Views/MainWindow.axaml`

Between the filter `StackPanel` (`DockPanel.Dock="Top"`, the block ending with the tree error `TextBlock`) and the tree `Grid`, insert:

- A `Border` (or plain `StackPanel`) with `DockPanel.Dock="Top"`, `x:Name="TreeActionsToolbar"`, small bottom margin (~8, matching pane rhythm), containing a horizontal `StackPanel`.
- One `Button x:Name="ExpandCollapseAllButton"`, `Classes="iconButton"`, `Command="{Binding WorkspaceTree.ToggleExpandCollapseAllCommand}"`, `IsEnabled="{Binding WorkspaceTree.CanToggleExpandCollapseAll}"`, `ToolTip.Tip="{Binding WorkspaceTree.ExpandCollapseAllTooltip}"`, `AutomationProperties.Name="{Binding WorkspaceTree.ExpandCollapseAllTooltip}"`; content is a `Grid` with the two 14px `PathIcon`s (Decision 6) bound to `NextToggleExpands` / `!NextToggleExpands`, themed with the existing icon foreground brush.

No code-behind changes; no new styles (the existing `iconButton` class covers chrome).

### 3. Tests

- `tests/FusionCanvas.App.Tests/WorkspaceTreeViewModelTests.cs` (framework-free, existing fake repository/snapshot helpers):
  1. Default state: pending expand, tooltip "Expand all groups", enabled on a tree with children.
  2. First toggle expands every niche/group node including nested levels; items become reachable in the flattened tree. (Requires a nested fixture — niche -> group -> subgroup with an item under the subgroup; the flat `Sample.Create(withGroup: true)` helper gives the group no children, so extend it or build a custom snapshot. Test 8 reuses the same nested fixture.)
  3. Second toggle collapses every topic node; only roots remain expanded=false.
  4. After expand-all, manually collapse one node, then toggle → everything collapses (remembered state; pre-click tooltip was "Collapse all groups").
  5. Expand-all then a projection refresh (reload via the fake repository) → topic nodes still expanded.
  6. Setting `QueryText` (or any filter) disables the toggle and switches the tooltip to the filtering explanation; clearing re-enables.
  7. A store whose topics have no children (or no store) → disabled, "No groups to expand or collapse".
  8. Collapse-all with an in-progress draft (begin create on a nested group): the draft's ancestor chain stays expanded, the draft remains visible/editing; unrelated branches collapse.
- `tests/FusionCanvas.App.Tests/MainWindowLayoutTests.cs` (headless, `MainWindowFixture`):
  9. `TreeActionsToolbar` exists in the left pane between the filter controls and `WorkspaceTreeControl` (assert visual-tree order within the `DockPanel`).
  10. `ExpandCollapseAllButton` tooltip and `AutomationProperties.Name` equal the view model's `ExpandCollapseAllTooltip`, track state after a toggle, and the two `PathIcon`s swap `IsVisible`; `IsEnabled` tracks `CanToggleExpandCollapseAll` (default enabled in the fixture workspace; disabled once a filter text is set).

### Sequencing

1. View-model members + unit tests (red→green).
2. AXAML toolbar + headless tests.
3. Full baseline: `dotnet build .\FusionCanvas.sln`, `dotnet test .\FusionCanvas.sln`, strict OpenSpec validation.

### Decisions not to reopen during implementation

Last-action toggle semantics (not state-derived); niche inclusion; disabled-while-filtering; in-place mutation instead of projection rebuild; tooltip wording "groups"; per-store preservation of `_expandedIds`.

## Acceptance scenario → verification mapping

| Spec scenario | Verification |
|---|---|
| Toolbar sits between filters and tree | Headless layout test 9 |
| Toggle presents the pending action (icon + tooltip + accessibility name) | Headless test 10; VM test 1 (tooltip string) |
| First activation expands every topic | VM test 2 |
| Second activation collapses every topic | VM test 3 |
| Manual node changes do not redirect the toggle | VM test 4 |
| Toggle expansion survives a tree refresh | VM test 5 |
| Toggle is disabled while filters are active | VM test 6; headless test 10 (`IsEnabled` tracking) |
| Toggle is disabled when nothing can expand | VM test 7 |
| Collapse-all protects an in-progress edit | VM test 8 |

All verification is deterministic (`dotnet test`); no live desktop scenario is required — the feature carries no native-window, OS-input, or visual-judgment risk beyond what headless tests observe.