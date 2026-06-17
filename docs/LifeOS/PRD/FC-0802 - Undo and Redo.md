# FC-0802 - Undo and Redo

## Summary

Undo and Redo allow users to reverse and reapply supported commands where practical.

## Requirements

- Supported commands can be undone.
- Undone commands can be redone where valid.
- Users can understand what action will be undone or redone.
- Commands that cannot be undone should be clear.
- Undo should protect user data and avoid inconsistent state.

## Acceptance Criteria

- A user can undo supported workspace changes.
- A user can redo a previously undone supported change.
- Unsupported operations communicate their limits.

## Out of Scope

- Undo for every external integration
- Infinite history
- Cross-session command replay unless explicitly supported

## Related Notes

- [[Roadmap]]
- [[FC-0801 - Command History]]
- [[Architecture]]
