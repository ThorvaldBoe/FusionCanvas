## Why

When a user switches stores in the main store selector, the selection currently exists only for the running session. After quitting and restarting FusionCanvas, the application falls back to the first store instead of reopening the store the user was working in. Persisting this lightweight application preference removes repeated navigation friction while preserving workspace-scoped store ownership.

## What Changes

- Persist the currently selected active store ID in the local application-settings document.
- Restore that store as the initial selection on startup when it still belongs to the active workspace and is active.
- Save the preference immediately after a successful store-selector change, using the existing asynchronous settings persistence path.
- Fall back to the existing first-store behavior when the saved store is missing, archived, belongs to another workspace, or cannot be read.
- Preserve existing settings-file backward compatibility and existing workspace/store data.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `application-settings`: application settings include the selected store preference and restore it safely across restart.
- `store-management`: the selected active store remains the selected store after application restart when it is still valid.

## Impact

- `ApplicationSettings` and JSON settings serialization gain one optional store identity field.
- Store-management composition and startup initialization pass the persisted identity into the application service.
- The main store selector updates settings after successful selection.
- Existing settings files remain readable because the new field is optional; invalid or stale identities are ignored.
- Focused application/UI and integration settings tests will cover selection persistence, startup restoration, fallback, and serialization compatibility.
