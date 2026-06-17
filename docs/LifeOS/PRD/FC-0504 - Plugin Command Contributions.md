# FC-0504 - Plugin Command Contributions

## Summary

Plugin Command Contributions let plugins add commands to menus, context actions, queues, or entity views.

## Requirements

- Plugins can declare commands they contribute.
- Commands can describe where they should appear.
- Commands should indicate which entity types or contexts they support.
- Users should see contributed commands in relevant places.
- Unavailable commands should fail clearly.

## Acceptance Criteria

- A plugin can add a command to a relevant context.
- Users can identify plugin-provided commands.
- Commands do not appear where they cannot act.

## Out of Scope

- Full scripting engine
- Command marketplace
- Undo/redo for all plugin commands

## Related Notes

- [[Roadmap]]
- [[Architecture]]
- [[FC-0503 - Plugin Dependency Registration]]
