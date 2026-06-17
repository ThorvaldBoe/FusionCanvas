# FC-0009 - Tabbed Document Window

## Summary

The Tabbed Document Window lets users work with multiple items at the same time, inspired by the document pane model in Obsidian.

The left side of FusionCanvas behaves like the navigation pane. The right side behaves like a document window with tabs. Each tab represents an open topic, item, workflow stage, or related working view.

## User Need

As a creator, I need to keep several ideas, concepts, designs, or listings open at once so I can move between related work without losing my place.

## Goals

- Support multiple open working contexts.
- Keep navigation and document editing coordinated.
- Make the active tab drive the visible workflow stage and detail view.
- Preserve user context while moving through many items.
- Support a creative workflow where ideas branch and cross-reference.

## Requirements

- The main workspace includes a document window.
- The document window supports multiple tabs.
- Each tab has a title that identifies the open item, topic, or view.
- Selecting a tab makes it the active working context.
- The navigation pane updates to show where the active tab's item or topic lives in the hierarchy.
- The workflow stage navigator updates to match the active tab.
- The document window is split into a top workflow area and a larger lower detail area.
- The lower detail area displays the relevant stage tool or view for the current workflow stage.
- The lower detail area can host built-in tools and plugin-provided tools through the Stage Tool Host.
- Users can open multiple items from navigation without replacing all prior context.
- Users can close tabs when they are no longer needed.

## Layout Model

```text
Main Window
├── Left Navigation Pane
└── Document Window
    ├── Tabs
    ├── Workflow Stage Navigator
    └── Stage Detail View
```

## User Workflows

### Work Across Multiple Items

The user opens several related ideas or designs in tabs and switches between them while comparing or refining work.

### Restore Navigation Context

The user clicks a tab, and the navigation pane highlights or reveals the item's location in the store, niche, and topic tree.

### Keep Workflow Visible

The user switches from an idea tab to a design tab, and the workflow area updates to show the correct current stage for that item.

## Acceptance Criteria

- A user can open more than one item in the document window.
- A user can switch between tabs.
- The active tab controls the active document context.
- The navigation pane reflects the active tab's location.
- The workflow stage navigator reflects the active tab's current item and stage.
- The detail area changes based on the active tab and workflow stage.
- The detail area can show the default tool for the current stage.

## Out of Scope

- Advanced split panes beyond the initial document window model
- Real-time collaboration
- Tab session restore across app restarts unless separately specified
- Custom tab groups
- Full plugin marketplace behavior

## Open Questions

- Should topics open in tabs, or only concrete items?
- Should a different workflow stage for the same item open as a separate tab or update the same tab?
- Should closed tabs be restorable during the same session?

## Related Notes

- [[Phase 0 - Foundation]]
- [[FC-0001 - Application Shell]]
- [[FC-0008 - Workflow Stage Navigator]]
- [[FC-0005 - Navigation Tree]]
- [[FC-0011 - Stage Tool Host]]
