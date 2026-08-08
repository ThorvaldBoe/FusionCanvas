## Why

FusionCanvas currently treats the navigation tree as a single-selection surface, which makes repeated operations on related Items and groups unnecessarily slow. Issue #138 calls for group actions, and the agreed interaction model uses familiar desktop selection semantics so creators can select, organize, and act on a set without introducing a persistent selection-mode toolbar.

This module establishes multi-selection as a first-class navigation interaction and provides safe, selection-aware actions and drag moves for the most common organizational operations.

## What Changes

- Add Photoshop/Affinity-style navigation selection using plain click, Ctrl-click, Shift-click, Ctrl+Shift-click, and Ctrl+A.
- Keep canonical active context separate from the multi-selection: the active row uses the brighter highlight, while other selected rows use a dimmer selection treatment.
- Replace Ctrl-click tab opening with middle-click and an `Open in new tab` context-menu action; support opening all selected entities in new tabs.
- Preserve a multi-selection when opening a context menu on a selected row, while right-clicking an unselected row makes it the sole selection first.
- Add selection-aware group actions for opening tabs, duplicating, deleting, archiving, exporting, and grouping selected Items/groups.
- Make drag-and-drop operate on the effective multi-selection when the drag begins on a selected row.
- Validate mixed Item/group moves before drop, reject cycles and drops into the selected hierarchy, normalize nested selected sources, and provide blocked-target feedback.
- Preserve existing single-entity navigation, editing, tab, clipboard, filtering, archive, and group-management behavior where it does not conflict with multi-selection.
- Use confirmation and per-entity outcome reporting for destructive or partially applicable actions.

## Capabilities

### New Capabilities

- `multi-selection`: Desktop-style selection state, range/toggle keyboard and pointer interaction, active-versus-selected visual treatment, and selection-aware navigation input.
- `group-actions`: Context-menu actions, selection-aware duplication/archive/delete/export/group operations, new-tab opening, and safe multi-entity drag moves.

### Modified Capabilities

- `group-management`: Group and Item tree interaction, tab-opening input, context menus, drag/drop validation, and selection behavior change to support multi-selection while preserving hierarchy safety.

## Impact

- `FusionCanvas.App` navigation tree input, selection presentation, context menus, keyboard handling, middle-click handling, tab-opening commands, and drag/drop orchestration.
- `FusionCanvas.Application` selection-oriented orchestration and batch validation/use-case contracts for Item/group actions and mixed hierarchy moves.
- `FusionCanvas.Domain` only if shared selection normalization or hierarchy-move invariants require a domain-level rule; no UI concepts should enter the domain.
- Focused Avalonia headless tests for routed pointer/keyboard input, context-menu state, visual selection states, and drag/drop feedback, plus framework-free application tests for validation and action orchestration.
- Existing item, group, CSV export, persistence, and document-tab boundaries will be reused where possible. Shopify, AI auto-grouping, background jobs, and marketplace rate-limiting are explicitly outside this first module.
