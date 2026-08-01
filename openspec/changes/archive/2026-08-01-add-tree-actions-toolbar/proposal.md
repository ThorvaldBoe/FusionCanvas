# Proposal: add-tree-actions-toolbar

## Why

The workspace tree starts fully collapsed, and creators with many niches, groups, and items must expand topic nodes one at a time to survey or reorganize their work. The left navigation pane also has no home for actions that apply to the tree as a whole — each action today is per-node (context menu) or per-workflow. A compact tree-actions toolbar gives the pane a durable place for bulk actions and ships its first, highest-value tool: expand/collapse all topics in one click.

## What Changes

- **Add a tree-actions toolbar** in the left navigation pane: a compact horizontal strip docked between the filter area and the workspace `TreeView`. It is the designated home for present and future actions that apply to the visible tree as a whole ("do something to everything below it"). This change ships exactly one tool on it.
- **Add an expand/collapse-all toggle button**: icon-only (vector icon that swaps with state), with a hover tooltip that names the pending action ("Expand all groups" / "Collapse all groups") and a matching accessibility name.
- **Toggle semantics**: the first click expands all topic nodes (niches and groups), because the tree starts collapsed by default; each subsequent click performs the opposite of the previous toolbar action. Expanding or collapsing individual nodes by hand does not change the toggle's remembered state.
- **Enablement states**: the toggle is disabled while any tree filter is active (filtering already force-expands the tree and restores pre-filter expansion on clear) with the tooltip explaining why, and disabled when the visible tree contains no expandable topic nodes.
- **Draft-edit protection**: collapse-all keeps the ancestor chain of an in-progress inline create/rename draft expanded so the active editor stays visible.
- **State retention**: bulk expansion changes flow through the existing session expansion-state retention, so the resulting expansion survives tree projection refreshes (filter transitions, reloads, structural edits) exactly like manual expansion does. No persistence across application restarts.

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `navigation-tree`: adds requirements for the tree-actions toolbar surface (placement, icon-only toggle with tooltip and accessibility name, enablement) and for bulk expand/collapse behavior (toggle semantics, default action, filter interaction, draft-edit protection, expansion-state retention alongside the existing per-node expansion requirement).

## Impact

- **Code**: `src/FusionCanvas.App/Views/MainWindow.axaml` (toolbar strip between the filter panel and the tree grid; one `iconButton`-style toggle with swapping `PathIcon` content, tooltip, and automation name), `src/FusionCanvas.App/Navigation/WorkspaceTreeViewModel.cs` (toggle command, icon/tooltip/enablement properties, bulk expand/collapse over the live node tree synchronized with the retained expansion set). No Domain, Application, or Integration changes; no persistence or schema changes; no public API changes.
- **Tests**: `tests/FusionCanvas.App.Tests/WorkspaceTreeViewModelTests.cs` (framework-free toggle behavior tests) and `tests/FusionCanvas.App.Tests/MainWindowLayoutTests.cs` (headless view coverage for toolbar construction, binding, and control state).
- **UX preflight**: the primary workflow is a creator browsing or reshaping a store's tree; expand/collapse-all is an occasional but repeated convenience action, so per the UX guidelines it belongs in the primary workspace as a compact, progressive-disclosure-friendly icon button rather than a dialog or menu. Persistent footprint is one slim strip (~one icon row) directly above the tree it controls, matching the "compact tree actions" element the UI guidelines list for the navigation pane. States resolved during discovery: initial (expand-all offered), filtered (disabled with explanation), empty tree (disabled), draft-edit (ancestors stay expanded). No destructive, unsaved-changes, or cancellation states apply; selection and focus are unaffected by the toggle.
- **Risks**: (1) bulk state changes must not rebuild the tree projection, because a rebuild silently discards an in-progress create/rename draft — the design mutates node expansion in place; (2) the tooltip wording says "groups" while the action also covers niches (top-level topics) — resolved during discovery as acceptable product vocabulary, alternatives recorded in design; (3) no coordination needed with other active changes: none of the in-progress changes touch the navigation pane surface or expansion state.

## Verification approach

- Framework-free view-model tests: default action is expand-all; toggle alternates and ignores manual per-node changes; expand-all expands every topic node (roots and nested) and the state survives a projection refresh; collapse-all collapses every topic node; toggle disabled while filters are active and when no expandable nodes exist; collapse-all preserves the in-progress draft's ancestor chain.
- Headless Avalonia view tests: toolbar is constructed between the filter area and the tree; button tooltip/automation name track the pending action; disabled state reflects the view model.
- Baseline: `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln` green; strict `openspec validate` clean. Optional live desktop check is ad hoc only, on a disposable workspace, and not a completion gate.

## Non-goals

- No other toolbar tools (bulk delete, bulk tag, sort, expand-to-depth) — the toolbar is built so future modules can add them, but this change specifies and ships only the expand/collapse-all toggle.
- No persistence of expansion state across application restarts (current session-only behavior is unchanged).
- No keyboard shortcut or menu command for expand/collapse-all.
- No changes to filter semantics, per-node expansion behavior, reveal-on-tab-activation, or drag-and-drop auto-expansion.