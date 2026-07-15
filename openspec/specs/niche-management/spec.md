# Niche Management

## Purpose

Niche management defines how FusionCanvas creates, edits, archives, restores, deletes, selects, and presents store-scoped niches as top-level creative context areas.

## Requirements

### Requirement: Niche management creates store-scoped niches
FusionCanvas SHALL allow users to create niches inside an active or explicitly selected store as top-level topic areas for audiences, themes, markets, or creative directions.

#### Scenario: User creates a niche in the active store
- **WHEN** an active store exists and the user creates a niche with a valid name
- **THEN** FusionCanvas persists a new active niche with stable identity, store identity, name, timestamps, and optional context
- **AND** the niche is available as a top-level topic inside that store

#### Scenario: User creates a niche with optional context
- **WHEN** the user creates a niche with a name and optional audience, humor style, visual style guidance, constraints, risks, research notes, or general notes
- **THEN** FusionCanvas persists the provided niche context with the niche
- **AND** the user is not required to create groups, listings, assets, marketplace data, AI research, trend scores, or analytics data

#### Scenario: User attempts to create a niche without store context
- **WHEN** the user attempts to create a niche without an active or explicitly selected active store
- **THEN** FusionCanvas rejects the create operation
- **AND** FusionCanvas leaves the workspace unchanged

#### Scenario: User attempts to create a duplicate active niche name in one store
- **WHEN** a store already contains an active niche with a name and the user creates another active niche in the same store with the same normalized name
- **THEN** FusionCanvas rejects the create operation
- **AND** FusionCanvas explains that active niche names must be unique within the store

#### Scenario: User reuses a niche name in another store
- **WHEN** another store contains a niche with the same normalized name
- **AND** the user creates a niche in the selected store
- **THEN** FusionCanvas allows the niche when the selected store does not already contain an active niche with that normalized name

### Requirement: Niche management edits niche context
FusionCanvas SHALL allow users to rename niches and edit niche-level creative context while preserving niche identity and contained work.

#### Scenario: User renames a niche
- **WHEN** the user renames an existing niche to a valid new name
- **THEN** FusionCanvas persists the new niche name
- **AND** the renamed niche remains renamed after the application reloads the workspace database
- **AND** child groups, listings, prompts, assets, tag links, asset links, metadata, and other niche-scoped context remain associated with the same niche identity

#### Scenario: User edits niche creative context
- **WHEN** the user updates audience, humor style, visual style guidance, constraints, risks, research notes, or general notes for a niche
- **THEN** FusionCanvas persists the updated context for use while working inside that niche
- **AND** the updated context remains available after the application reloads the workspace database
- **AND** the update does not require changes to child groups or listings

#### Scenario: User attempts to rename a niche to a duplicate active name
- **WHEN** the user renames an active niche to the same normalized name as another active niche in the same store
- **THEN** FusionCanvas rejects the rename operation
- **AND** the original niche name and context remain unchanged

### Requirement: Niche management distinguishes active and archived niches
FusionCanvas SHALL distinguish active niches from archived niches and SHALL keep archived niches out of normal active navigation by default.

#### Scenario: User archives a niche
- **WHEN** the user archives an active niche
- **THEN** FusionCanvas marks the niche as archived
- **AND** the niche no longer appears in the normal active niche list or active store navigation
- **AND** the niche's existing context and contained work remain preserved

#### Scenario: User reviews archived niches
- **WHEN** the user opens the archived niche area for a store
- **THEN** FusionCanvas shows archived niches for that store separately from active niches
- **AND** each archived niche remains available for review

#### Scenario: User cannot archive a niche from another store through current store actions
- **WHEN** the active store is selected
- **AND** the user attempts to archive a niche that belongs to a different store through current store niche actions
- **THEN** FusionCanvas rejects the archive operation
- **AND** the other store's niche remains unchanged

### Requirement: Niche management restores archived niches
FusionCanvas SHALL allow users to restore archived niches so inactive creative context can return to the active store workspace.

#### Scenario: User restores an archived niche
- **WHEN** the user restores an archived niche
- **THEN** FusionCanvas marks the niche as active
- **AND** the niche appears in the normal active niche list or active store navigation
- **AND** the restored niche retains its existing context and contained work

#### Scenario: User restores a niche whose name now conflicts
- **WHEN** the user attempts to restore an archived niche and an active niche in the same store already has the same normalized name
- **THEN** FusionCanvas blocks restoration until the conflict is resolved
- **AND** the archived niche remains archived

### Requirement: Niche management deletes only empty niches
FusionCanvas SHALL allow permanent niche deletion only after explicit warning or confirmation and only when no niche-scoped data is connected to the niche.

#### Scenario: User deletes an empty niche
- **WHEN** the user requests permanent deletion for a niche with no connected groups, listings, prompts, assets, tag links, asset links, metadata links, or other niche-scoped data
- **AND** the user confirms the deletion warning
- **THEN** FusionCanvas permanently removes the niche
- **AND** the niche no longer appears in active or archived niche lists

#### Scenario: User attempts to delete a niche with connected data
- **WHEN** the user requests permanent deletion for a niche that has connected niche-scoped data
- **THEN** FusionCanvas blocks the deletion
- **AND** FusionCanvas explains that the niche must be emptied or archived instead

#### Scenario: User cancels deletion warning
- **WHEN** the user requests permanent deletion for an empty niche and cancels the warning
- **THEN** FusionCanvas keeps the niche unchanged

### Requirement: Niche management exposes active niche context
FusionCanvas SHALL allow users to open or select an active niche as the current workspace context for browsing and context-aware work.

#### Scenario: User opens an active niche
- **WHEN** the user opens an active niche in the active store
- **THEN** FusionCanvas sets that niche as the active navigation context
- **AND** the workspace can show that niche's groups and listings through the navigation experience

#### Scenario: Niche context is available inside selected niche
- **WHEN** the user works inside a selected niche
- **THEN** FusionCanvas makes the store identity, niche identity, and niche-level context available to creation, editing, navigation, and context-aware tool flows that require the active niche

#### Scenario: User cannot select archived niche as active workspace context
- **WHEN** the user attempts to open an archived niche from the normal active workspace flow
- **THEN** FusionCanvas does not set that archived niche as the active navigation context
- **AND** FusionCanvas requires the niche to be restored before it can be used as active workspace context

### Requirement: Niche management presents niches as store top-level sidebar folders
FusionCanvas SHALL present active niches as the default top-level folder/topic nodes under the selected store in the main window sidebar.

#### Scenario: Store contains active niches
- **WHEN** the user opens an active store that contains active niches
- **THEN** FusionCanvas shows those niches as top-level folder/topic entries directly under the selected store in the main window sidebar
- **AND** the user can select a niche to browse its child groups and listings when they exist

#### Scenario: Store contains no active niches
- **WHEN** the user opens an active store that has no active niches
- **THEN** FusionCanvas shows an empty active-niche state with a clear way to create a niche
- **AND** FusionCanvas does not require the user to create a group or listing first

#### Scenario: Store contains archived and active niches
- **WHEN** the user opens an active store that contains both active and archived niches
- **THEN** FusionCanvas shows active niches in normal store navigation
- **AND** FusionCanvas keeps archived niches out of normal store navigation unless the user intentionally opens archived niche review

### Requirement: Niche management keeps regular workspace controls lightweight
FusionCanvas SHALL keep regular workspace niche controls focused on selection, creation entry points, and editor access rather than heavy inline context editing.

#### Scenario: Regular workspace shows niche browsing controls
- **WHEN** the regular workspace UI is shown for an active store
- **THEN** FusionCanvas provides controls for selecting active niches and starting niche creation or management
- **AND** the regular workspace UI does not require inline editing of every niche context field inside the navigation tree

#### Scenario: User opens store management niches tab
- **WHEN** the user chooses to manage or edit niches for the active store
- **THEN** FusionCanvas opens the existing store management window on its Niches tab for the selected store
- **AND** the selected active niche is pre-selected when one exists
- **AND** create, save, archive, restore, delete, and niche-context editing actions are available from that tab

#### Scenario: Store management separates store info from niches
- **WHEN** the store management window is open
- **THEN** FusionCanvas provides a Basic info tab for store-level fields and store lifecycle actions
- **AND** FusionCanvas provides a Niches tab for active and archived niches that belong to the selected store
- **AND** switching between these tabs does not change the selected store

#### Scenario: Niche tab follows compact action sizing
- **WHEN** the Niches tab shows save, archive, restore, delete, discard-confirmation, or warning-confirmation actions
- **THEN** those action buttons use compact fixed or content-based widths
- **AND** the buttons are not evenly stretched across the full store management window width
