# FC-0505 - Plugin Data Storage

## Summary

Plugin Data Storage lets plugins attach structured data to core entities without changing the core schema.

## Requirements

- Plugins can store data associated with supported entity types.
- Plugin data remains scoped to the plugin that owns it.
- Core entities can retain plugin data through normal movement or organization changes.
- Users should not lose core data if plugin data is unavailable.
- Plugin data should not require core schema changes for each plugin.

## Acceptance Criteria

- A plugin can attach data to a listing or other supported entity.
- Plugin data can be retrieved by that plugin later.
- Removing or disabling a plugin does not corrupt core entities.

## Out of Scope

- Arbitrary database access
- Cross-plugin data contracts
- Plugin data analytics

## Related Notes

- [[Roadmap]]
- [[Data Model]]
- [[Architecture]]
