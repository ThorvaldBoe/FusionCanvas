# Listing Management

## Purpose

Defines how FusionCanvas creates, organizes, edits, duplicates, archives, restores, and permanently deletes listings within active topic context while preserving local data integrity and coordinated desktop navigation.

## Requirements

### Requirement: Listing management creates product concepts in active topics
FusionCanvas SHALL allow a user to create a listing beneath an active niche or group using one nonblank single-line working title or idea, and SHALL NOT require assets, mockups, marketplace metadata, or other later-stage records.

#### Scenario: User captures a listing from a selected topic
- **WHEN** the user commits one valid line of idea text while an active niche or group is canonically selected
- **THEN** FusionCanvas creates a new listing with stable identity beneath that topic
- **AND** the listing belongs to the topic's store
- **AND** the listing starts as a draft
- **AND** the listing can have no attached assets

#### Scenario: Selected listing supplies its containing topic
- **WHEN** an active listing is selected and the user starts another listing
- **THEN** FusionCanvas uses the selected listing's containing group, or its niche when ungrouped, as the new listing destination

#### Scenario: Store context uses the default niche
- **WHEN** an active store has no applicable topic or listing selection and has a valid active default niche
- **THEN** FusionCanvas creates the listing beneath that default niche
- **AND** does not create a store-level listing

#### Scenario: Store has one active niche
- **WHEN** the active store has exactly one active niche and no valid default has yet been persisted
- **THEN** FusionCanvas may establish that niche as the store default through the existing default-niche workflow
- **AND** uses it for listing creation

#### Scenario: Store has no resolvable topic
- **WHEN** the active store has no active niche, or has multiple active niches with no applicable selection and no valid default niche
- **THEN** FusionCanvas blocks listing creation
- **AND** guides the user to create or select a niche or configure the store default
- **AND** does not guess from alphabetical order

#### Scenario: User supplies optional details during creation
- **WHEN** the user creates a listing with a valid idea line and optional description or notes
- **THEN** FusionCanvas persists the supplied optional details
- **AND** omitted optional details do not block creation

#### Scenario: User submits invalid idea text
- **WHEN** the user submits an empty, whitespace-only, or multi-line required idea value
- **THEN** FusionCanvas rejects the creation
- **AND** preserves the draft for correction
- **AND** leaves the persisted workspace unchanged

### Requirement: Listing destinations honor active group ancestry
FusionCanvas SHALL validate listing niche and group destinations against the active workspace and store, and SHALL treat a group as available only when its store, effective niche, and complete group ancestry are active.

#### Scenario: Nested active group is selected
- **WHEN** the user creates, moves, duplicates, or restores a listing into an active nested group
- **THEN** FusionCanvas resolves the group's effective niche and store through the shared group hierarchy
- **AND** accepts the destination without requiring a redundant direct niche parent on the group

#### Scenario: Destination has an archived ancestor
- **WHEN** a requested group is active by its own flag but has an archived group, niche, or store ancestor
- **THEN** FusionCanvas rejects the destination with actionable guidance
- **AND** leaves persisted listing state unchanged

#### Scenario: Destination belongs to another workspace or store
- **WHEN** the requested topic is outside the active workspace or outside the listing's store for a move or duplicate
- **THEN** FusionCanvas rejects the operation
- **AND** creates no partial listing or relationship changes

### Requirement: Listing creation applies applicable inherited context
FusionCanvas SHALL resolve the destination topic's applicable inherited tags and metadata and apply them to a newly created listing while preserving whether values originated from parent context or explicit user input.

#### Scenario: Topic context provides applicable defaults
- **WHEN** the user creates a listing from a niche or group whose resolved context contains tags or metadata applicable to new work
- **THEN** FusionCanvas applies those values to the listing
- **AND** records their inherited provenance

#### Scenario: User overrides a creation default
- **WHEN** the user changes an inherited creation value before saving the listing
- **THEN** FusionCanvas persists the user's explicit value
- **AND** does not overwrite it with the inherited default

#### Scenario: Parent context has no applicable values
- **WHEN** the user creates a listing in a context with no applicable inherited tags or metadata
- **THEN** FusionCanvas creates the listing without fabricated tags or metadata

### Requirement: Listing management edits core listing details safely
FusionCanvas SHALL allow users to rename a listing inline and edit its FC-0104 description or notes while preserving identity, placement, archive state, status, tags, assets, prompts, and unknown metadata.

#### Scenario: User renames a listing inline
- **WHEN** the user invokes inline rename for an active listing, enters a valid single-line title, and commits
- **THEN** FusionCanvas persists the normalized title and updated timestamp
- **AND** keeps the listing selected at the same topic location

#### Scenario: User saves optional listing properties
- **WHEN** the user changes description or notes in the secondary properties surface and saves
- **THEN** FusionCanvas persists the normalized values and updated timestamp
- **AND** preserves the listing's stable identity and related context

#### Scenario: User submits an invalid title
- **WHEN** the user attempts to commit an empty, whitespace-only, or multi-line required title
- **THEN** FusionCanvas rejects the edit
- **AND** preserves both the persisted listing and the recoverable inline input

### Requirement: Listing management moves listings through the editable tree
FusionCanvas SHALL allow a listing to move to an active niche or group within its existing store through drag/drop, cut/paste, or an accessible move command while preserving listing identity and all related context.

#### Scenario: User drops a listing onto an active topic
- **WHEN** the user drags a listing onto an active niche or group in the same store
- **THEN** FusionCanvas changes only the listing's topic placement and modification timestamp
- **AND** preserves its title, details, status, archive state, notes, tags, prompts, assets, and metadata
- **AND** selects and reveals the listing at the destination

#### Scenario: User cuts and pastes a listing
- **WHEN** the user cuts a listing and pastes it into an active niche or group in the same store
- **THEN** FusionCanvas performs the same validated move as drag/drop
- **AND** clears or updates clipboard state consistently after success

#### Scenario: User attempts a cross-store move
- **WHEN** the user selects a destination that belongs to another store
- **THEN** FusionCanvas rejects the move
- **AND** preserves the listing and all relationships at the original location

#### Scenario: User attempts an unavailable destination
- **WHEN** the requested destination is missing, archived, hidden by an archived ancestor, or inconsistent with its store ancestry
- **THEN** FusionCanvas rejects the move with an actionable error
- **AND** restores any optimistic tree projection to its last confirmed state

#### Scenario: User attempts before or after listing placement
- **WHEN** the user attempts to position a listing before or after a sibling listing
- **THEN** FusionCanvas does not promise manual sibling placement
- **AND** listings remain alphabetically ordered within the destination topic

### Requirement: Listing management duplicates listings as independent variations
FusionCanvas SHALL duplicate a listing into an active niche or group in the same store with a new identity, new timestamps, draft status, the idea workflow stage, copied core details, copied metadata and tags, and no copied asset, prompt, or future dependent-record relationships.

#### Scenario: User duplicates a listing in place
- **WHEN** the user invokes Duplicate without choosing another destination
- **THEN** FusionCanvas creates a new listing in the source topic
- **AND** gives it a distinct identity and collision-safe copy title
- **AND** resets it to draft status and the idea workflow stage regardless of the source stage or status
- **AND** copies the source description, notes, metadata, and tag links
- **AND** leaves all asset and prompt relationships attached only to the source

#### Scenario: User reviews a duplicated listing through the workflow
- **WHEN** the user opens a duplicate whose source had advanced beyond the idea stage
- **THEN** the duplicate presents the idea stage as current
- **AND** its copied creative details remain available for review as the user advances it stage by stage

#### Scenario: User copies and pastes into another topic
- **WHEN** the user copies a listing and pastes it into another active niche or group in the same store
- **THEN** FusionCanvas creates the independent duplicate at that destination with draft status and the idea workflow stage
- **AND** selects and reveals the duplicate without changing the source

#### Scenario: User attempts to duplicate across stores
- **WHEN** the user chooses a destination in another store
- **THEN** FusionCanvas rejects duplication
- **AND** creates no partial listing or relationships

### Requirement: Listing management archives and restores listings reversibly
FusionCanvas SHALL preserve a listing's topic and all related context when archiving or restoring it, SHALL exclude effectively archived listings from normal active navigation, and SHALL expose archived listings through an intentional lifecycle surface.

#### Scenario: User archives an active listing
- **WHEN** the user confirms archive for an active listing
- **THEN** FusionCanvas marks the listing archived
- **AND** removes it from normal active navigation
- **AND** preserves its topic, status, notes, tags, prompts, attached assets, and metadata

#### Scenario: Parent group is archived
- **WHEN** an otherwise active listing is beneath an archived group ancestor
- **THEN** normal navigation hides the listing through the shared archive-aware projection
- **AND** listing management does not treat it as an active selectable or destination context

#### Scenario: User restores a listing to an active topic
- **WHEN** the user restores an archived listing whose preserved topic path is active and valid
- **THEN** FusionCanvas marks the listing active
- **AND** returns it to normal navigation at its preserved topic with all context intact
- **AND** selects and reveals it in the current reusable working tab without creating an additional tab

#### Scenario: User restores into an unavailable parent context
- **WHEN** the listing's preserved niche, group, group ancestor, or store is archived or unavailable
- **THEN** FusionCanvas blocks restoration
- **AND** explains that the parent path must be restored or another active same-store topic selected
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
- **THEN** FusionCanvas leaves the listing, canonical selection, and all relationships unchanged

### Requirement: Listing management persists operations atomically
FusionCanvas SHALL persist listing creation, editing, movement, duplication, archive, restore, deletion, metadata, and tag-link changes in the active workspace database as atomic snapshot operations.

#### Scenario: Workspace reloads after listing operations
- **WHEN** a successful listing operation is followed by an application or workspace database reload
- **THEN** the resulting listings, topics, details, archive states, metadata, and tag links match the last successful persisted state

#### Scenario: Persistence fails after an optimistic tree change
- **WHEN** the repository cannot save a listing operation after the tree has projected a draft or structural change
- **THEN** FusionCanvas reports a recoverable error
- **AND** restores the last confirmed hierarchy, filter, expansion, selection, and clipboard state
- **AND** retains recoverable user input needed to retry when applicable

### Requirement: Listing capture uses the niche-rooted editable tree
FusionCanvas SHALL provide non-persisted inline listing capture in the existing editable workspace tree using canonical selection and default-niche resolution.

#### Scenario: User starts inline capture
- **WHEN** the user invokes New Listing with a resolvable active topic
- **THEN** FusionCanvas inserts an inline draft beneath that topic
- **AND** moves keyboard focus to the selected required idea line
- **AND** exposes commit and cancel behavior without opening a modal surface

#### Scenario: Inline capture succeeds
- **WHEN** the user commits a valid inline listing draft
- **THEN** FusionCanvas persists, selects, and reveals the new listing
- **AND** preserves the current filter and expansion context
- **AND** opens or replaces the current reusable working tab without creating additional tabs

#### Scenario: User cancels inline capture
- **WHEN** the user presses Escape or otherwise cancels an uncommitted listing draft
- **THEN** FusionCanvas removes the draft without persisting a listing
- **AND** returns focus to the creation anchor

#### Scenario: Capture is unavailable
- **WHEN** there is no active store or no topic/default niche can be resolved
- **THEN** FusionCanvas blocks capture with a clear store, niche, selection, or default-niche setup path
- **AND** does not show a misleading enabled commit action

### Requirement: Canonical listing selection coordinates a reusable working tab
FusionCanvas SHALL make normal listing selection update canonical workspace context and open or replace one current working tab, SHALL retain at least one tab after a context has been opened, and SHALL provide an explicit additive action for opening or activating another listing tab.

#### Scenario: User selects a listing normally
- **WHEN** the user selects a listing row in the tree
- **THEN** that listing becomes canonical workspace selection
- **AND** the current working tab is created or reused for that listing
- **AND** repeated normal selections do not accumulate tabs

#### Scenario: User explicitly opens a listing tab
- **WHEN** the user Ctrl-clicks a listing or invokes Open in Tab
- **THEN** FusionCanvas preserves the existing working tab and opens or activates another persistent tab for that listing
- **AND** avoids creating duplicate tabs for the same listing identity

#### Scenario: Listing mutation succeeds
- **WHEN** create, rename, move, duplicate, or restore succeeds
- **THEN** FusionCanvas selects and reveals the resulting listing in the tree
- **AND** updates the reusable working tab without accumulating additional tabs

#### Scenario: User closes the final working tab
- **WHEN** only one ordinary working tab remains and the user invokes Close
- **THEN** FusionCanvas keeps that tab and its active context visible

### Requirement: Secondary listing management protects properties and lifecycle actions
FusionCanvas SHALL provide a secondary focused surface for optional listing properties, listing tag editing, archived review, restore, and permanent deletion while keeping inline creation, rename, move, and duplication in the tree.

#### Scenario: User opens listing properties
- **WHEN** the user invokes Edit Properties for a selected listing
- **THEN** FusionCanvas opens the focused surface with that listing preselected
- **AND** keeps canonical tree and document context intact
- **AND** progressively discloses the listing tag editor, archived review, restore, and permanent deletion

#### Scenario: User edits listing tags on the properties surface
- **WHEN** the user applies or removes a tag through the listing tag editor on the focused surface
- **THEN** FusionCanvas persists the tag change immediately and atomically through the tag management service
- **AND** the tag change does not require or await the description/notes Save action
- **AND** the description/notes draft and its explicit Save remain independent

#### Scenario: User leaves meaningful unsaved properties
- **WHEN** the focused surface contains meaningful unsaved description or notes changes and the user switches listing or closes
- **THEN** FusionCanvas asks whether to save, discard, or cancel
- **AND** retains the current draft and focus when cancellation is chosen

#### Scenario: Selected listing leaves active navigation
- **WHEN** the selected listing is archived or permanently deleted
- **THEN** FusionCanvas selects a sensible nearby active listing when one exists
- **AND** otherwise selects the active parent topic
- **AND** returns keyboard focus to the replacement tree row

#### Scenario: Lifecycle operation is in progress or fails
- **WHEN** archive, restore, or deletion is running or fails validation or persistence
- **THEN** FusionCanvas prevents duplicate submission
- **AND** keeps the surface available to report success or an actionable error
- **AND** preserves selection and recoverable input after failure

### Requirement: Listing management follows shared desktop control guidance
FusionCanvas SHALL use compact action sizing, clear tooltips for icon-only commands, predictable keyboard flow, and accessible confirmation and cancellation behavior in tree and focused listing surfaces.

#### Scenario: Listing actions are presented
- **WHEN** create, rename, move, duplicate, save, archive, restore, discard-confirmation, or delete-confirmation actions are shown
- **THEN** their buttons are sized to their command groups rather than evenly stretched across the surface
- **AND** essential actions are reachable without pointer-only interaction

#### Scenario: Icon-only listing command is shown
- **WHEN** an icon-only listing command appears in the workspace tree
- **THEN** the command exposes a tooltip that describes its action
