## ADDED Requirements

### Requirement: Main Workspace Includes A Document Window
FusionCanvas SHALL ensure that the main workspace includes a document window.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The main workspace includes a document window

### Requirement: Document Window Supports Multiple Tabs
FusionCanvas SHALL ensure that the document window supports multiple tabs.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The document window supports multiple tabs

### Requirement: Each Tab Has A Title That Identifies The Open Item, Topic, Or View
FusionCanvas SHALL ensure that each tab has a title that identifies the open item, topic, or view.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Each tab has a title that identifies the open item, topic, or view

### Requirement: Selecting A Tab Makes It The Active Working Context
FusionCanvas SHALL ensure that selecting a tab makes it the active working context.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Selecting a tab makes it the active working context

### Requirement: Navigation Pane Updates To Show Where The Active Tab's Item Or Topic Lives In The Hierarchy
FusionCanvas SHALL ensure that the navigation pane updates to show where the active tab's item or topic lives in the hierarchy.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The navigation pane updates to show where the active tab's item or topic lives in the hierarchy

### Requirement: Workflow Stage Navigator Updates To Match The Active Tab
FusionCanvas SHALL ensure that the workflow stage navigator updates to match the active tab.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The workflow stage navigator updates to match the active tab

### Requirement: Document Window Is Split Into A Top Workflow Area And A Larger Lower Detail Area
FusionCanvas SHALL ensure that the document window is split into a top workflow area and a larger lower detail area.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The document window is split into a top workflow area and a larger lower detail area

### Requirement: Lower Detail Area Displays The Relevant Stage Tool Or View For The Current Workflow Stage
FusionCanvas SHALL ensure that the lower detail area displays the relevant stage tool or view for the current workflow stage.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The lower detail area displays the relevant stage tool or view for the current workflow stage

### Requirement: Lower Detail Area Can Host Built-in Tools And Plugin-provided Tools Through The Stage Tool Host
FusionCanvas SHALL ensure that the lower detail area can host built-in tools and plugin-provided tools through the Stage Tool Host.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The lower detail area can host built-in tools and plugin-provided tools through the Stage Tool Host

### Requirement: Users Can Open Multiple Items From Navigation Without Replacing All Prior Context
FusionCanvas SHALL allow users to open multiple items from navigation without replacing all prior context.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** the user can open multiple items from navigation without replacing all prior context

### Requirement: Users Can Close Tabs When They Are No Longer Needed
FusionCanvas SHALL allow users to close tabs when they are no longer needed.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** the user can close tabs when they are no longer needed

### Requirement: Acceptance Criteria Remain Satisfied
FusionCanvas SHALL satisfy the acceptance criteria captured in the source PRD.

#### Scenario: User Can Open More Than One Item In The Document Window
- **WHEN** the corresponding capability is delivered
- **THEN** A user can open more than one item in the document window.

#### Scenario: User Can Switch Between Tabs
- **WHEN** the corresponding capability is delivered
- **THEN** A user can switch between tabs.

#### Scenario: Active Tab Controls The Active Document Context
- **WHEN** the corresponding capability is delivered
- **THEN** The active tab controls the active document context.

#### Scenario: Navigation Pane Reflects The Active Tab's Location
- **WHEN** the corresponding capability is delivered
- **THEN** The navigation pane reflects the active tab's location.

#### Scenario: Workflow Stage Navigator Reflects The Active Tab's Current Item And Stage
- **WHEN** the corresponding capability is delivered
- **THEN** The workflow stage navigator reflects the active tab's current item and stage.

#### Scenario: Detail Area Changes Based On The Active Tab And Workflow Stage
- **WHEN** the corresponding capability is delivered
- **THEN** The detail area changes based on the active tab and workflow stage.

#### Scenario: Detail Area Can Show The Default Tool For The Current Stage
- **WHEN** the corresponding capability is delivered
- **THEN** The detail area can show the default tool for the current stage.

## Source PRD

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
Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬ Left Navigation Pane
Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬ Document Window
    Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬ Tabs
    Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬ Workflow Stage Navigator
    Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬ Stage Detail View
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
