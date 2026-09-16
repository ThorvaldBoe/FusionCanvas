## ADDED Requirements

### Requirement: Store selector persists the selected store
FusionCanvas SHALL save the selected active store preference after a successful store-selector change and SHALL use it as the startup selection when it is valid for the active workspace.

#### Scenario: User changes stores and restarts
- **WHEN** the user changes the selected store using the regular store selector and quits the application
- **THEN** the next application start selects the store chosen most recently
- **AND** the store's workspace-scoped navigation opens for that store

#### Scenario: Saved store is no longer selectable
- **WHEN** the store saved as the startup selection is archived, deleted, or no longer belongs to the active workspace
- **THEN** FusionCanvas does not select it
- **AND** it selects the first available active store using the existing fallback behavior

#### Scenario: Failed selection is not persisted
- **WHEN** a store-selector request is rejected because the store is missing, archived, or outside the active workspace
- **THEN** FusionCanvas does not replace the previously persisted selected-store preference

