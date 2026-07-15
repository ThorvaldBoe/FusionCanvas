## ADDED Requirements

### Requirement: Store management creates stores
FusionCanvas SHALL allow users to create stores as top-level business, brand, client, or publishing contexts without requiring advanced setup.

#### Scenario: User creates first store
- **WHEN** a workspace has no stores and the user accepts the first-store prompt and creates a store with a valid name
- **THEN** FusionCanvas persists a new active store with stable identity, name, timestamps, and optional context
- **AND** the store is available as a top-level workspace context

#### Scenario: User declines first-store creation
- **WHEN** a workspace has no stores and the user declines the first-store prompt
- **THEN** FusionCanvas leaves the workspace in an empty state
- **AND** the user can open the store editor later to create a store

#### Scenario: User creates store with optional context
- **WHEN** the user starts a new store draft from the store editor, enters a name and optional description, notes, target market, brand direction, or planning context, and saves
- **THEN** FusionCanvas persists the provided store context with the store
- **AND** the user is not required to configure marketplace accounts, publishing destinations, analytics, permissions, or templates

#### Scenario: New store remains draft until saved
- **WHEN** the user clicks New store in the store editor
- **THEN** FusionCanvas shows a local "New store" draft in the editor side panel
- **AND** FusionCanvas places keyboard focus in the Store name field
- **AND** FusionCanvas does not persist the store until the user clicks Save
- **AND** the Save action creates the store when the selected editor item is a new-store draft

### Requirement: Store management edits store context
FusionCanvas SHALL allow users to rename stores and edit basic store-level context while preserving the store identity and contained work.

#### Scenario: User renames a store in the editor
- **WHEN** the user renames an existing store to a valid new name in the store editor
- **THEN** FusionCanvas persists the new store name
- **AND** the renamed store remains renamed after the application reloads the workspace database
- **AND** niches, groups, listings, tags, assets, prompts, and other store-scoped context remain associated with the same store identity

#### Scenario: User edits store notes in the editor
- **WHEN** the user updates store-level notes, description, target market, brand direction, or planning context in the store editor
- **THEN** FusionCanvas persists the updated context for use while working inside that store
- **AND** the updated context remains available after the application reloads the workspace database
- **AND** the update does not require changes to child niches, groups, or listings

#### Scenario: Save updates the selected existing store
- **WHEN** the selected editor item is an existing store and the user clicks Save after changing the store name or context
- **THEN** FusionCanvas updates that store in place
- **AND** FusionCanvas preserves the store identity and contained work

#### Scenario: User switches stores with unsaved editor changes
- **WHEN** the user has unsaved changes in the store editor and clicks another store
- **THEN** FusionCanvas asks whether to discard changes
- **AND** choosing Yes discards the unsaved changes and selects the clicked store
- **AND** choosing No keeps the user editing the current store or draft

#### Scenario: User closes editor with unsaved changes
- **WHEN** the user has unsaved changes in the store editor and clicks the window close control
- **THEN** FusionCanvas asks whether to discard changes
- **AND** choosing Yes discards the unsaved changes and closes the editor
- **AND** choosing No leaves the editor open on the current store or draft

### Requirement: Store management distinguishes active and archived stores
FusionCanvas SHALL distinguish active stores from archived stores and SHALL keep archived stores out of the normal active workspace by default.

#### Scenario: User archives a store in the editor
- **WHEN** the user archives an active store from the store editor
- **THEN** FusionCanvas marks the store as archived
- **AND** the store no longer appears in the normal active store selector or active workspace list
- **AND** the store's existing context and contained work remain preserved

#### Scenario: User reviews archived stores
- **WHEN** the user opens the archived store area in the store editor
- **THEN** FusionCanvas shows archived stores separately from active stores
- **AND** each archived store remains available for review

### Requirement: Store management restores archived stores
FusionCanvas SHALL allow users to restore archived stores so inactive business context can return to the active workspace.

#### Scenario: User restores an archived store in the editor
- **WHEN** the user restores an archived store from the store editor
- **THEN** FusionCanvas marks the store as active
- **AND** the store appears in the normal active store selector or active workspace list
- **AND** the restored store retains its existing context and contained work

### Requirement: Store management deletes only empty stores
FusionCanvas SHALL allow permanent store deletion only from the store editor, only after explicit warning or confirmation, and only when no store-scoped data is connected to the store.

#### Scenario: User deletes an empty store
- **WHEN** the user requests permanent deletion for a store with no connected niches, groups, listings, tags, assets, prompts, listing tags, asset links, or other store-scoped data
- **AND** the user confirms the deletion warning
- **THEN** FusionCanvas permanently removes the store
- **AND** the store no longer appears in active or archived store lists
- **AND** FusionCanvas selects another remaining store by default when one exists

#### Scenario: User attempts to delete a store with connected data
- **WHEN** the user requests permanent deletion for a store that has connected store-scoped data
- **THEN** FusionCanvas blocks the deletion
- **AND** FusionCanvas explains that the store must be emptied or archived instead

#### Scenario: User cancels deletion warning
- **WHEN** the user requests permanent deletion for an empty store and cancels the warning
- **THEN** FusionCanvas keeps the store unchanged

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

### Requirement: Store selector stays lightweight
FusionCanvas SHALL keep regular workspace store UI focused on store selection and editor access rather than inline store editing.

#### Scenario: Regular workspace shows store selector only
- **WHEN** the regular workspace UI is shown
- **THEN** FusionCanvas provides controls for selecting the active store and opening store actions from a modern icon menu
- **AND** the regular workspace UI does not show inline controls for renaming, saving, archiving, restoring, deleting, or editing store context

#### Scenario: User opens store editor
- **WHEN** the user chooses manage stores from the regular workspace icon menu
- **THEN** FusionCanvas opens a dedicated store editor window
- **AND** the active store is pre-selected in the editor
- **AND** new-store draft, save, archive, restore, delete, and store-context editing actions are available from that editor
- **AND** the Save action handles both creating a selected draft and updating a selected existing store
- **AND** editor action buttons use compact fixed or content-based widths rather than stretching evenly across the window

#### Scenario: Store editor actions are enabled only when relevant
- **WHEN** the selected editor item is a new-store draft
- **THEN** FusionCanvas enables Save
- **AND** FusionCanvas disables Archive and Delete
- **WHEN** the selected editor item is an unchanged existing store
- **THEN** FusionCanvas disables Save
- **AND** FusionCanvas enables Delete
- **AND** FusionCanvas enables Archive only when the existing store is active
- **WHEN** the selected editor item is an existing store with unsaved changes
- **THEN** FusionCanvas enables Save

### Requirement: Store selector supports compact and expanded modes
FusionCanvas SHALL allow the active store selector to collapse to a compact selected-store view or expand to show active stores.

#### Scenario: Compact selector shows selected store
- **WHEN** the store selector is collapsed and an active store is selected
- **THEN** FusionCanvas shows the selected store in a highlighted compact control
- **AND** the user can expand the selector to see other active stores
- **AND** clicking the highlighted selected store expands the selector

#### Scenario: Selector toggle sits beside label
- **WHEN** the regular workspace UI shows the Stores label
- **THEN** FusionCanvas shows a small arrow toggle beside the label
- **AND** the arrow indicates expanded or collapsed state
- **AND** the toggle exposes a tooltip that describes the action as expand or collapse
- **AND** FusionCanvas does not show a separate visible collapse button elsewhere in the store selector

#### Scenario: Expanded selector shows all active stores
- **WHEN** the store selector is expanded
- **THEN** FusionCanvas shows active stores available for switching
- **AND** the selected store is highlighted
- **AND** the user can collapse the selector back to the compact view

#### Scenario: Many stores do not consume persistent rail space
- **WHEN** the user has many active stores
- **THEN** FusionCanvas allows the store list to remain collapsed during normal work
- **AND** store switching remains available without permanently taking space from navigation content

### Requirement: Store management follows shared UI element guidelines
FusionCanvas SHALL follow the shared button and icon-button guidance in `docs/ui-guidelines.md` for store prompts, store selectors, and store editor actions.

#### Scenario: First-store prompt button sizing
- **WHEN** the first-store prompt is shown
- **THEN** its action buttons use compact fixed or content-based widths
- **AND** the buttons are not evenly stretched across the full prompt width

#### Scenario: Store editor action button sizing
- **WHEN** the store editor shows save, archive, restore, delete, discard-confirmation, or warning-confirmation actions
- **THEN** those action buttons use compact fixed or content-based widths
- **AND** the buttons are not evenly stretched across the full editor width
