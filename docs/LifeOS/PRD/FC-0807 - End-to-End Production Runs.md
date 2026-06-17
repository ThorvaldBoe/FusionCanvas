# FC-0807 - End-to-End Production Runs

## Summary

End-to-End Production Runs coordinate larger batch workflows while requiring human approval at important creative and publishing points.

## Requirements

- Users can define a production run from a set of listings or a queue.
- The run can include steps such as validation, asset checks, metadata preparation, export, mockup review, and publishing preparation.
- Progress and blockers should be visible.
- Users approve important creative and external publishing steps.
- Run history should remain available for review.

## Acceptance Criteria

- A user can start a production run for selected work.
- The product shows progress and unresolved blockers.
- Human approval is required before high-impact actions.
- Run results remain connected to the involved listings.

## Out of Scope

- Fully autonomous product business operation
- Guaranteed marketplace success
- External fulfillment management

## Related Notes

- [[Roadmap]]
- [[FC-0306 - Work Queues]]
- [[FC-0804 - Automation Recipes]]
