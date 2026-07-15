## MODIFIED Requirements

### Requirement: Store management creates stores
FusionCanvas SHALL allow users to create stores as workspace-scoped business, brand, client, or publishing contexts without requiring advanced setup.

#### Scenario: User creates first store in active workspace
- **WHEN** an active workspace has no stores and the user accepts the first-store prompt and creates a store with a valid name
- **THEN** FusionCanvas persists a new active store with stable identity, active workspace identity, name, timestamps, and optional context
- **AND** the store is available as a business context inside the active workspace

#### Scenario: User declines first-store creation
- **WHEN** an active workspace has no stores and the user declines the first-store prompt
- **THEN** FusionCanvas leaves that workspace in an empty store state
- **AND** the user can open the store editor later to create a store in the active workspace

#### Scenario: User creates store with optional context
- **WHEN** the user starts a new store draft from the store editor, enters a name and optional description, notes, target market, brand direction, or planning context, and saves
- **THEN** FusionCanvas persists the provided store context with the store
- **AND** the store belongs to the active workspace
- **AND** the user is not required to configure marketplace accounts, publishing destinations, analytics, permissions, or templates

#### Scenario: New store remains draft until saved
- **WHEN** the user clicks New store in the store editor
- **THEN** FusionCanvas shows a local "New store" draft in the editor side panel
- **AND** FusionCanvas places keyboard focus in the Store name field
- **AND** FusionCanvas does not persist the store until the user clicks Save
- **AND** the Save action creates the store in the active workspace when the selected editor item is a new-store draft

### Requirement: Store management selects the active store scope
FusionCanvas SHALL allow users to open or select an active store in the active workspace as the primary store scope for browsing and context-aware work.

#### Scenario: User opens an active store in active workspace
- **WHEN** the user opens an active store that belongs to the active workspace
- **THEN** FusionCanvas sets that store as the active store scope
- **AND** the workspace can show that store's niches, groups, and listings through the navigation experience

#### Scenario: Store context is available inside selected store
- **WHEN** the user works inside a selected store
- **THEN** FusionCanvas makes the workspace identity, store identity, and store-level context available to creation, editing, navigation, and context-aware tool flows that require the active store

#### Scenario: User cannot select archived store as active workspace
- **WHEN** the user attempts to open an archived store from the normal active workspace flow
- **THEN** FusionCanvas does not set that archived store as the active store scope
- **AND** FusionCanvas requires the store to be restored before it can be used as active store context

#### Scenario: User cannot select store from another workspace
- **WHEN** the active workspace is selected
- **AND** the user attempts to select a store that belongs to another workspace through normal store actions
- **THEN** FusionCanvas rejects the selection
- **AND** the active workspace and active store remain consistent

### Requirement: Store selector supports compact and expanded modes
FusionCanvas SHALL allow the active store selector to collapse to a compact selected-store view or expand to show active stores in the active workspace.

#### Scenario: Compact selector shows selected store
- **WHEN** the store selector is collapsed and an active store is selected
- **THEN** FusionCanvas shows the selected store in a highlighted compact control
- **AND** the user can expand the selector to see other active stores in the active workspace
- **AND** clicking the highlighted selected store expands the selector

#### Scenario: Selector toggle sits beside label
- **WHEN** the regular workspace UI shows the Stores label
- **THEN** FusionCanvas shows a small arrow toggle beside the label
- **AND** the arrow indicates expanded or collapsed state
- **AND** the toggle exposes a tooltip that describes the action as expand or collapse
- **AND** FusionCanvas does not show a separate visible collapse button elsewhere in the store selector

#### Scenario: Expanded selector shows active workspace stores
- **WHEN** the store selector is expanded
- **THEN** FusionCanvas shows active stores available in the active workspace
- **AND** the selected store is highlighted
- **AND** the user can collapse the selector back to the compact view
- **AND** stores from other workspaces are not shown

#### Scenario: Many stores do not consume persistent rail space
- **WHEN** the active workspace has many active stores
- **THEN** FusionCanvas allows the store list to remain collapsed during normal work
- **AND** store switching remains available without permanently taking space from navigation content

## ADDED Requirements

### Requirement: Store names are unique within a workspace
FusionCanvas SHALL enforce active store name uniqueness inside one workspace while allowing the same store name in different workspaces.

#### Scenario: Duplicate active store name in active workspace
- **WHEN** an active store in the active workspace already uses a normalized name
- **AND** the user creates or renames another active store in that workspace to the same normalized name
- **THEN** FusionCanvas rejects the operation
- **AND** the existing stores remain unchanged

#### Scenario: Same store name in another workspace
- **WHEN** another workspace contains an active store with a normalized name
- **AND** the user creates a store with that name in the active workspace
- **THEN** FusionCanvas allows the store when the active workspace does not already contain an active store with that normalized name

### Requirement: Store management lists are workspace-scoped
FusionCanvas SHALL show only stores from the active workspace in normal store management and selection surfaces.

#### Scenario: Store editor opens inside active workspace
- **WHEN** the user opens the store editor from the regular workspace UI
- **THEN** FusionCanvas shows active and archived stores that belong to the active workspace
- **AND** stores from other workspaces are hidden from that editor context

#### Scenario: Active workspace changes
- **WHEN** the user changes the active workspace
- **THEN** FusionCanvas reloads store selector and editor state from stores in the newly active workspace
- **AND** first-store prompting is based on whether the newly active workspace has stores
