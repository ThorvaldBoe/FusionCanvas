# FC-0806 - Scheduled Tasks

## Summary

Scheduled Tasks run selected sync, import, validation, or reporting actions on a schedule.

## Requirements

- Users can schedule supported tasks.
- Tasks can be enabled, disabled, edited, or deleted.
- Users can see last run, next run, and outcome.
- Failed tasks should be visible.
- Scheduled actions should respect approval and privacy boundaries.

## Acceptance Criteria

- A user can schedule a supported task.
- A user can see whether the task ran successfully.
- A user can disable a task.

## Out of Scope

- Cloud-hosted scheduling
- Team task assignment
- Autonomous external publishing

## Related Notes

- [[Roadmap]]
- [[FC-0607 - Publishing Status Sync]]
- [[FC-0804 - Automation Recipes]]
