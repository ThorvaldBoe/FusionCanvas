# FC-0302 - Bulk Status Changes

## Summary

Bulk Status Changes let users update workflow stage or lifecycle status for many listings at once.

## User Need

Creators need to move batches of listings through workflow stages or action states efficiently.

## Requirements

- Users can select multiple listings.
- Users can apply a workflow stage or lifecycle status to the selection.
- The action should show enough context to avoid accidental changes.
- Stage and status changes should preserve listing history where available.
- Archived and rejected changes should be deliberate.

## Acceptance Criteria

- A user can move multiple listings to the same workflow stage or status.
- A user can confirm broad or potentially destructive status changes.
- A user can filter afterward and see updated status results.
- Non-listing selections are handled clearly.

## Out of Scope

- Custom workflows
- Automated status transitions
- Marketplace status sync
- Undo/redo

## Related Notes

- [[Roadmap]]
- [[FC-0105 - Listing Lifecycle Status]]
- [[FC-0301 - Multi-Select Operations]]
