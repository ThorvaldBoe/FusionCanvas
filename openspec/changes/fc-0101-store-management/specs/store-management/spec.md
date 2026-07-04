## ADDED Requirements

### Requirement: Store management creates stores
FusionCanvas SHALL allow users to create stores as top-level business, brand, client, or publishing contexts without requiring advanced setup.

#### Scenario: User creates first store
- **WHEN** a workspace has no stores and the user creates a store with a valid name
- **THEN** FusionCanvas persists a new active store with stable identity, name, timestamps, and optional context
- **AND** the store is available as a top-level workspace context

#### Scenario: User creates store with optional context
- **WHEN** the user creates a store with a name and optional description, notes, target market, brand direction, or planning context
- **THEN** FusionCanvas persists the provided store context with the store
- **AND** the user is not required to configure marketplace accounts, publishing destinations, analytics, permissions, or templates

### Requirement: Store management edits store context
FusionCanvas SHALL allow users to rename stores and edit basic store-level context while preserving the store identity and contained work.

#### Scenario: User renames a store
- **WHEN** the user renames an existing store to a valid new name
- **THEN** FusionCanvas persists the new store name
- **AND** niches, groups, listings, tags, assets, prompts, and other store-scoped context remain associated with the same store identity

#### Scenario: User edits store notes
- **WHEN** the user updates store-level notes, description, target market, brand direction, or planning context
- **THEN** FusionCanvas persists the updated context for use while working inside that store
- **AND** the update does not require changes to child niches, groups, or listings

### Requirement: Store management distinguishes active and archived stores
FusionCanvas SHALL distinguish active stores from archived stores and SHALL keep archived stores out of the normal active workspace by default.

#### Scenario: User archives a store
- **WHEN** the user archives an active store
- **THEN** FusionCanvas marks the store as archived
- **AND** the store no longer appears in the normal active store selector or active workspace list
- **AND** the store's existing context and contained work remain preserved

#### Scenario: User reviews archived stores
- **WHEN** the user opens the archived store view or intentionally includes archived stores
- **THEN** FusionCanvas shows archived stores separately from active stores
- **AND** each archived store remains available for review

### Requirement: Store management restores archived stores
FusionCanvas SHALL allow users to restore archived stores so inactive business context can return to the active workspace.

#### Scenario: User restores an archived store
- **WHEN** the user restores an archived store
- **THEN** FusionCanvas marks the store as active
- **AND** the store appears in the normal active store selector or active workspace list
- **AND** the restored store retains its existing context and contained work

### Requirement: Store management selects the active store scope
FusionCanvas SHALL allow users to open or select an active store as the primary workspace scope for browsing and context-aware work.

#### Scenario: User opens an active store
- **WHEN** the user opens an active store
- **THEN** FusionCanvas sets that store as the active workspace scope
- **AND** the workspace can show that store's niches, groups, and listings through the navigation experience

#### Scenario: Store context is available inside selected store
- **WHEN** the user works inside a selected store
- **THEN** FusionCanvas makes the store identity and store-level context available to creation, editing, navigation, and context-aware tool flows that require the active store

#### Scenario: User cannot select archived store as active workspace
- **WHEN** the user attempts to open an archived store from the normal active workspace flow
- **THEN** FusionCanvas does not set that archived store as the active workspace scope
- **AND** FusionCanvas requires the store to be restored before it can be used as active workspace context
