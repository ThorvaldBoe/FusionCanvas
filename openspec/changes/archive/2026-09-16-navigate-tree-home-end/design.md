## Context

The workspace navigation surface is an Avalonia `TreeView` whose built-in keyboard handling supports hierarchical arrow navigation but does not select the first or last visible tree item for Home/End. FusionCanvas already owns canonical selection in `WorkspaceTreeViewModel` and renders a filtered, expanded projection in the main window.

## Goals / Non-Goals

**Goals:**

- Make Home and End select the first and last currently visible navigation nodes.
- Respect expansion and active filtering.
- Keep canonical selection, inspector context, multi-selection state, focus, and scrolling coherent.
- Preserve text-box editing behavior.

**Non-Goals:**

- No changes to domain or persistence behavior.
- No new toolbar command, configurable shortcut, or alternate navigation surface.
- No change to the meaning of arrow keys, Ctrl/Shift selection gestures, or draft editing.

## Decisions

- Handle Home/End at the main window/tree input boundary because the visible order is a presentation concern and the existing tree projection already knows which nodes are visible.
- Define visible order as the depth-first order of `WorkspaceTreeViewModel.Roots` and expanded `Children`, after all active filters have been applied. This matches the order users see and the existing Up/Down TreeView behavior.
- Add a small ViewModel method to select a boundary node through the existing selection path, rather than mutating selection state from code-behind. The code-behind only translates key input and focuses/brings the selected container into view.
- Ignore Home/End when a `TextBox` has focus, preserving caret navigation and inline editor semantics. Other existing global shortcuts remain unchanged.

Alternatives considered:

- Rely on Avalonia `TreeView` defaults: rejected because Avalonia maps Home/End to First/Last but does not resolve those directions to a tree container.
- Flatten and cache a second navigation list: rejected because it duplicates the already-rendered projection and risks stale state after filtering or expansion changes.

## Risks / Trade-offs

- [Filtered or empty projections] → Resolve the first/last node from the current `Roots` projection and no-op when none exists.
- [Virtualized/unrealized target] → Use the existing visible node selection path and `AutoScrollToSelectedItem`/container focus behavior; headless tests verify selection, while layout behavior remains owned by Avalonia.
- [Multi-selection] → Boundary navigation is a normal single-node navigation action and replaces the current multi-selection, matching ordinary keyboard focus movement.

## Migration Plan

No migration is required. The change is local to App input and presentation behavior and is backward compatible with existing workspaces.

## Open Questions

None.

## Implementation Plan

1. Extend `WorkspaceTreeViewModel` with a boundary-selection operation that traverses the current `Roots`/`Children` projection in visible depth-first order and routes through existing `Select` behavior.
2. Add Home/End handling to `MainWindow.OnWindowKeyDown`, excluding focused text boxes, and focus the selected visible tree container after selection.
3. Add framework-free ViewModel tests for first/last selection, nesting, collapsed branches, and empty projections.
4. Add Avalonia headless tests for routed Home/End input through the main window and text-box exclusion.
5. Run focused tests, the full solution baseline, and strict OpenSpec validation; record criterion-level evidence in `verification.md`.

## Verification Mapping

| Acceptance scenario | Verification |
| --- | --- |
| Tree has keyboard focus | ViewModel traversal tests plus Avalonia headless routed-key tests for Home and End in expanded and filtered trees |
| Home or End is pressed with no visible nodes | ViewModel no-op test and headless empty-filter test |
| Operation is unavailable | Existing shortcut/unavailable-state regression tests plus solution baseline |

