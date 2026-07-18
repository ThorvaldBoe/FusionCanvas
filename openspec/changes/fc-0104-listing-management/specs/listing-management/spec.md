## ADDED Requirements

### Requirement: Listing management creates product concepts in valid active locations
FusionCanvas SHALL allow a user to create a listing in an active store, niche, or group using one nonblank single-line working title or idea, and SHALL NOT require assets, mockups, marketplace metadata, or other later-stage records.

#### Scenario: User captures a listing from a selected topic
- **WHEN** the user enters one valid line of idea text while an active niche or group is selected
- **THEN** FusionCanvas creates a new listing with stable identity in that selected topic
- **AND** the listing belongs to the topic's store
- **AND** the listing starts as a draft
- **AND** the listing can have no attached assets

#### Scenario: User captures a listing from a selected store
- **WHEN** the user enters one valid line of idea text while an active store is selected and no topic is selected
- **THEN** FusionCanvas creates the listing directly under that store
- **AND** the listing is not assigned an implicit niche or group

#### Scenario: User supplies optional details during creation
- **WHEN** the user creates a listing with a valid idea line and optional description or notes
- **THEN** FusionCanvas persists the supplied optional details
- **AND** omitted optional details do not block creation

#### Scenario: User submits invalid idea text
- **WHEN** the user submits an empty, whitespace-only, or multi-line required idea value
- **THEN** FusionCanvas rejects the creation
- **AND** preserves the draft for correction
- **AND** leaves the workspace unchanged

#### Scenario: User targets an unavailable location
- **WHEN** the selected store, niche, or group is missing, archived, outside the active workspace, or inconsistent with its store ancestry
- **THEN** FusionCanvas blocks listing creation
- **AND** explains that an active valid location is required
- **AND** leaves the workspace unchanged

### Requirement: Listing creation applies applicable inherited context
FusionCanvas SHALL resolve the selected location's applicable inherited tags and metadata and apply them to a newly created listing while preserving whether values originated from parent context or explicit user input.

#### Scenario: Topic context provides applicable defaults
- **WHEN** the user creates a listing from a selected niche or group whose resolved context contains tags or metadata applicable to new work
- **THEN** FusionCanvas applies those values to the listing
- **AND** records their inherited provenance

#### Scenario: User overrides a creation default
- **WHEN** the user changes an inherited creation value before saving the listing
- **THEN** FusionCanvas persists the user's explicit value
- **AND** does not overwrite it with the inherited default

#### Scenario: Parent context has no applicable values
- **WHEN** the user creates a listing in a context with no applicable inherited tags or metadata
- **THEN** FusionCanvas creates the listing without fabricated tags or metadata

### Requirement: Listing management edits core listing details
FusionCanvas SHALL allow users to rename a listing and edit its FC-0104 core description or notes while preserving its identity, placement, archive state, status, tags, assets, prompts, and unknown metadata.

#### Scenario: User saves valid listing edits
- **WHEN** the user changes the working title, description, or notes of an existing listing and saves
- **THEN** FusionCanvas persists the normalized values and updated timestamp
- **AND** preserves the listing's stable identity and related context

#### Scenario: User saves an invalid title
- **WHEN** the user attempts to save an empty, whitespace-only, or multi-line required title
- **THEN** FusionCanvas rejects the edit
- **AND** preserves both the persisted listing and the user's unsaved input

### Requirement: Listing management moves listings without rewriting context
FusionCanvas SHALL allow a listing to move among valid active store, niche, and group locations within its existing store while preserving listing identity, core details, status, archive state, notes, tags, prompts, attached assets, metadata, and timestamps other than the modification timestamp.

#### Scenario: User moves a listing to a niche or group
- **WHEN** the user moves a listing to an active niche or group in the listing's store
- **THEN** FusionCanvas changes only the listing's placement and modification timestamp
- **AND** the listing appears at the destination with all related context preserved

#### Scenario: User moves a listing directly under its store
- **WHEN** the user moves a listing from a niche or group to its active store
- **THEN** FusionCanvas removes its niche and group placement
- **AND** shows the same listing directly under the store

#### Scenario: User attempts a cross-store move
- **WHEN** the user selects a destination that belongs to another store
- **THEN** FusionCanvas rejects the move
- **AND** preserves the listing and all relationships at the original location

#### Scenario: User attempts to move to an unavailable location
- **WHEN** the requested destination is missing, archived, or has inconsistent ancestry
- **THEN** FusionCanvas rejects the move with an actionable error
- **AND** leaves the listing unchanged

### Requirement: Listing management duplicates listings as independent variations
FusionCanvas SHALL duplicate a listing into a valid location in the same store with a new identity, new timestamps, draft status, copied core details, copied listing metadata and tags, and no copied asset, prompt, or future dependent-record relationships.

#### Scenario: User duplicates a listing in place
- **WHEN** the user duplicates an existing listing without choosing another destination
- **THEN** FusionCanvas creates a new draft listing beside the source
- **AND** gives it a distinct identity and a non-destructive copy title
- **AND** copies the source description, notes, metadata, and tag links
- **AND** leaves all asset and prompt relationships attached only to the source

#### Scenario: User duplicates to another location in the same store
- **WHEN** the user chooses another valid active store, niche, or group location in the source store before confirming duplication
- **THEN** FusionCanvas creates the independent duplicate at that destination

#### Scenario: User attempts to duplicate across stores
- **WHEN** the user chooses a destination in another store
- **THEN** FusionCanvas rejects duplication
- **AND** creates no partial listing or relationships

### Requirement: Listing management archives and restores listings reversibly
FusionCanvas SHALL preserve a listing's location and all related context when archiving or restoring it, SHALL exclude archived listings from normal active navigation, and SHALL expose archived listings through an intentional management view.

#### Scenario: User archives an active listing
- **WHEN** the user archives an active listing
- **THEN** FusionCanvas marks the listing archived
- **AND** removes it from normal active navigation
- **AND** preserves its location, status, notes, tags, prompts, attached assets, and metadata

#### Scenario: User restores a listing to an active location
- **WHEN** the user restores an archived listing whose store and preserved location are active and valid
- **THEN** FusionCanvas marks the listing active
- **AND** returns it to normal navigation at its preserved location with all context intact

#### Scenario: User restores into an unavailable parent context
- **WHEN** the listing's preserved store, niche, or group is archived or otherwise unavailable
- **THEN** FusionCanvas blocks restoration
- **AND** explains that the parent must be restored or an active destination selected
- **AND** keeps the listing archived

### Requirement: Listing management guards permanent deletion
FusionCanvas SHALL permanently delete a listing only after explicit confirmation and only when no prompt, asset link, or other dependent creative record references it; successful deletion SHALL remove listing-specific tag links without deleting reusable tags.

#### Scenario: User deletes a listing without dependent creative records
- **WHEN** the user requests permanent deletion for a listing with no prompt, asset-link, or other dependent creative record
- **AND** confirms the warning
- **THEN** FusionCanvas permanently removes the listing and its listing-tag links atomically
- **AND** preserves reusable tag records

#### Scenario: User attempts to delete a connected listing
- **WHEN** a prompt, asset link, or other dependent creative record references the listing
- **THEN** FusionCanvas blocks permanent deletion
- **AND** explains that the related work must be detached or the listing archived instead

#### Scenario: User cancels deletion
- **WHEN** the user cancels the permanent-deletion warning
- **THEN** FusionCanvas leaves the listing, selection, and all relationships unchanged

### Requirement: Listing management persists complete operations
FusionCanvas SHALL persist listing creation, editing, movement, duplication, archive, restore, deletion, metadata, and tag-link changes in the active workspace database.

#### Scenario: Workspace reloads after listing operations
- **WHEN** a successful listing operation is followed by an application or workspace database reload
- **THEN** the resulting listings, locations, details, archive states, metadata, and tag links match the last successful persisted state

#### Scenario: Persistence fails
- **WHEN** the workspace repository cannot save a listing operation
- **THEN** FusionCanvas reports a recoverable error
- **AND** does not present the operation as successful
- **AND** retains user input needed to retry when applicable

### Requirement: Listing capture stays in the primary workspace
FusionCanvas SHALL provide compact one-line listing capture near the active navigation context without permanently consuming the document window or requiring the focused listing-management surface.

#### Scenario: User starts listing capture
- **WHEN** the user invokes New Listing for an active store, niche, or group
- **THEN** FusionCanvas shows a compact capture draft scoped to that location
- **AND** moves keyboard focus to the required idea line
- **AND** exposes a clear create and cancel path

#### Scenario: Listing capture succeeds
- **WHEN** the user successfully creates a listing from the compact capture draft
- **THEN** FusionCanvas refreshes and expands the required navigation path
- **AND** selects and reveals the new listing
- **AND** makes it available to the existing document workflow

#### Scenario: No active store is available
- **WHEN** the user has no active store context
- **THEN** FusionCanvas blocks listing capture with a clear store selection or setup path
- **AND** does not show a misleading enabled create action

#### Scenario: User cancels compact capture
- **WHEN** the user cancels an unchanged capture draft or confirms discarding meaningful draft input
- **THEN** FusionCanvas closes the draft without creating a listing
- **AND** returns focus to the invoking navigation control

### Requirement: Occasional listing management uses a focused surface
FusionCanvas SHALL place rename/edit, move, duplicate, archived review, restore, and permanent delete actions in a focused listing-management surface that preserves main-window context and preselects the invoking listing.

#### Scenario: User opens focused management
- **WHEN** the user invokes listing management from a selected listing or compact listing action menu
- **THEN** FusionCanvas opens the focused surface with that listing selected
- **AND** keeps the main workspace context intact
- **AND** progressively discloses occasional and destructive actions

#### Scenario: User has unsaved edits and changes selection or closes
- **WHEN** the focused surface contains meaningful unsaved changes and the user changes listing selection, changes destination context, or closes the surface
- **THEN** FusionCanvas asks whether to discard the changes
- **AND** retains the current draft and focus when discard is declined

#### Scenario: Action availability follows selection state
- **WHEN** an active listing, archived listing, unchanged record, changed record, or new duplicate draft is selected
- **THEN** FusionCanvas enables only actions valid for that state
- **AND** distinguishes archive from restore and permanent deletion

#### Scenario: Management operation is in progress
- **WHEN** a save, move, duplicate, archive, restore, or delete operation is running
- **THEN** FusionCanvas prevents duplicate submission
- **AND** preserves visible context until success or recoverable failure is known

#### Scenario: Selected listing disappears from active navigation
- **WHEN** the selected listing is archived or permanently deleted
- **THEN** FusionCanvas selects a sensible nearby active listing when one exists
- **AND** otherwise selects the parent location and shows an actionable empty state
- **AND** returns keyboard focus to the replacement selection

#### Scenario: Focused operation fails validation or persistence
- **WHEN** a management operation fails with a validation or recoverable persistence error
- **THEN** FusionCanvas shows the error near the affected control
- **AND** preserves the listing draft, destination choice, and selection for correction or retry

### Requirement: Listing management follows shared desktop control guidance
FusionCanvas SHALL use compact fixed or content-based action sizing, clear tooltips for icon-only commands, and keyboard-accessible confirmation and cancellation behavior in listing capture and focused management surfaces.

#### Scenario: Listing actions are presented
- **WHEN** capture, save, move, duplicate, archive, restore, discard-confirmation, or delete-confirmation actions are shown
- **THEN** their buttons are sized to their command groups rather than evenly stretched across the surface
- **AND** essential actions are reachable without pointer-only interaction

#### Scenario: Icon-only listing command is shown
- **WHEN** an icon-only listing command appears in the navigation workspace
- **THEN** the command exposes a tooltip that describes its action
