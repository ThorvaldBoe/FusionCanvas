# FC-0008 - Workflow Stage Navigator

## Summary

The Workflow Stage Navigator makes the core FusionCanvas workflow visible for the current item.

FusionCanvas is built around a creative progression:

```text
Idea -> Concept -> Design -> Listing
```

Archive may appear as a fifth related state for retired, rejected, or retracted work that should remain available for learning.

## User Need

As a creator, I need to see where the current item is in the creative workflow and navigate back to completed stages without losing context.

## Goals

- Make the four-step workflow a visible top-level concept.
- Tie workflow navigation to the currently open item.
- Let users move between available stages for an item.
- Clearly show stages that have not been reached yet.
- Preserve archived or retracted work for learning.

## Requirements

- The document window shows a workflow area for the current item.
- The workflow area displays Idea, Concept, Design, and Listing as distinct stages.
- The current stage is visually emphasized.
- Completed or available previous stages are enabled.
- Future unavailable stages are disabled.
- Clicking an enabled stage opens the relevant stage view for the current item.
- Clicking a disabled stage does not navigate.
- The workflow display updates when the selected item or active tab changes.
- Archive can be represented as a related destination or state without replacing the core four-step workflow.
- The workflow navigator should help users understand the item's maturity at a glance.

## Stage Behavior

### Idea

The earliest captured product seed, phrase fragment, visual thought, or market/audience observation.

### Concept

The clarified creative direction, including the intended audience reaction, phrase, graphic direction, and risks.

### Design

The concrete visual execution or design work derived from the concept, including imported or generated variants, related assets, and final artwork selection.

### Listing

The marketplace-ready product representation, including title, description, keywords, mockups, and publishing preparation.

### Archive

A retained inactive state for work that is rejected, retracted, paused indefinitely, superseded, or useful only as learning material.

## User Workflows

### Review Current Stage

The user opens an item and immediately sees whether they are working on the idea, concept, design, or listing stage.

### Navigate to Completed Stage

The user is working on a concept and clicks the Idea stage to review the original idea that led to it.

### Prevent Premature Navigation

The user is working on a concept and sees Design and Listing as disabled because the item has not reached those stages yet.

### Fast-Forward Work

The user starts with a phrase and graphic direction and can create or advance an item directly into Concept or Design with minimal required information.

## Acceptance Criteria

- A user can see the current workflow stage for an open item.
- Available stages are clickable.
- Unavailable future stages are visibly disabled.
- The current stage is visually distinct from other stages.
- Switching tabs updates the workflow navigator to match the active item.
- Archived work remains available for review without cluttering active workflow stages.

## Out of Scope

- Detailed visual design
- Strict mandatory stage transitions
- Automated stage advancement
- Marketplace publishing sync
- Full undo/redo

## Open Questions

- Should Archive appear as a permanent fifth box, a separate command, or a status outside the main stage row?
- Should Mockup remain part of Listing preparation or become a visible stage later?
- Should users be able to skip stages freely, or should some minimum stage data be required?

## Related Notes

- [[Phase 0 - Foundation]]
- [[FC-0009 - Tabbed Document Window]]
- [[FC-0105 - Listing Lifecycle Status]]
- [[FC-0201 - Idea Inbox]]
- [[FC-0202 - Concept Versions]]
- [[FC-0203 - Design Records]]
- [[FC-0211 - Basic Design Tool]]
- [[FC-0207 - Listing Metadata Editor]]
