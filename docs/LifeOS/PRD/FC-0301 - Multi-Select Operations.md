# FC-0301 - Multi-Select Operations

## Summary

Multi-Select Operations let users select multiple listings, assets, or groups and act on them together.

## User Need

Creators working in batches need to perform common actions on many items without repeating the same steps one by one.

## Requirements

- Users can select multiple compatible objects.
- The product shows which objects are selected.
- Available actions reflect the selected object types.
- Users can clear or change the selection.
- Multi-select should support later bulk workflows.

## Acceptance Criteria

- A user can select more than one listing.
- A user can see the current selection count.
- A user can access only actions that make sense for the selection.
- Selection behavior is predictable across navigation contexts.

## Out of Scope

- Every bulk command
- Cross-workspace selection
- Complex selection rules
- Automation recipes

## Related Notes

- [[Roadmap]]
- [[Product]]
- [[Principles]]
