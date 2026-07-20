## ADDED Requirements

### Requirement: Tag management creates store-scoped tags
FusionCanvas SHALL allow users to create tags inside an active or explicitly selected store as reusable, store-scoped classification labels with a name, optional description, and optional color.

#### Scenario: User creates a tag in the active store
- **WHEN** an active store exists and the user creates a tag with a valid nonblank single-line name
- **THEN** FusionCanvas persists a new active tag with stable identity, store identity, name, empty optional description when omitted, default color when none is chosen, timestamps, and empty metadata
- **AND** the tag is available to apply to listings in that store

#### Scenario: User creates a tag with a color
- **WHEN** the user creates a tag with a valid name and chooses a color
- **THEN** FusionCanvas persists the color in normalized `#RRGGBB` form on the tag
- **AND** the tag's chip renders in that color across tag surfaces

#### Scenario: User attempts to create a tag without store context
- **WHEN** the user attempts to create a tag without an active or explicitly selected active store
- **THEN** FusionCanvas rejects the create operation
- **AND** the workspace remains unchanged

#### Scenario: User attempts to create a duplicate active tag name in one store
- **WHEN** a store already contains an active tag with a name and the user creates another active tag in the same store with the same normalized name
- **THEN** FusionCanvas rejects the create operation
- **AND** explains that active tag names must be unique within the store

#### Scenario: User reuses a tag name in another store
- **WHEN** another store contains an active tag with the same normalized name
- **AND** the user creates a tag in the selected store
- **THEN** FusionCanvas allows the tag when the selected store does not already contain an active tag with that normalized name

#### Scenario: User submits an invalid tag name
- **WHEN** the user submits an empty, whitespace-only, or multi-line tag name
- **THEN** FusionCanvas rejects the create operation
- **AND** preserves the draft for correction
- **AND** leaves the workspace unchanged

#### Scenario: User submits an invalid color
- **WHEN** the user submits a color that is not a valid hex color form
- **THEN** FusionCanvas rejects the create or update operation
- **AND** preserves the draft for correction

### Requirement: Tag management edits tag identity and appearance
FusionCanvas SHALL allow users to rename a tag and edit its optional description and color while preserving the tag's stable identity, store ownership, archive state, and all `ListingTag` links.

#### Scenario: User renames a tag
- **WHEN** the user renames an active tag to a valid new name
- **THEN** FusionCanvas persists the normalized name and updated timestamp
- **AND** the renamed tag remains renamed after the application reloads the workspace database
- **AND** every `ListingTag` link and every listing chip continues to reference the same tag identity

#### Scenario: User changes a tag color
- **WHEN** the user changes an active tag's color to a valid hex color
- **THEN** FusionCanvas persists the normalized color and updated timestamp
- **AND** every chip for that tag across the listing editor, listing rows, tree filter, and Tags tab updates to the new color

#### Scenario: User edits a tag description
- **WHEN** the user updates the optional description of a tag
- **THEN** FusionCanvas persists the updated description and updated timestamp
- **AND** the update does not require changes to any listing or `ListingTag` link

#### Scenario: User attempts to rename a tag to a duplicate active name
- **WHEN** the user renames an active tag to the same normalized name as another active tag in the same store
- **THEN** FusionCanvas rejects the rename operation
- **AND** the original tag name, color, description, and links remain unchanged

### Requirement: Tag management distinguishes active and archived tags
FusionCanvas SHALL distinguish active tags from archived tags, SHALL keep archived tags out of active tag surfaces by default, and SHALL preserve `ListingTag` links for archived tags.

#### Scenario: User archives a tag
- **WHEN** the user archives an active tag
- **THEN** FusionCanvas marks the tag as archived
- **AND** the tag no longer appears in the listing tag editor autocomplete, the tree tag filter control, or the active tag list
- **AND** existing `ListingTag` links remain preserved
- **AND** listings still applied to the archived tag continue to display it in a muted archived chip style

#### Scenario: User reviews archived tags
- **WHEN** the user opens the archived tags area in the Tags tab for the selected store
- **THEN** FusionCanvas shows archived tags for that store separately from active tags
- **AND** each archived tag remains available for restore

#### Scenario: User cannot archive a tag from another store through current store actions
- **WHEN** the active store is selected
- **AND** the user attempts to archive a tag that belongs to a different store through current store tag actions
- **THEN** FusionCanvas rejects the archive operation
- **AND** the other store's tag remains unchanged

### Requirement: Tag management restores archived tags
FusionCanvas SHALL allow users to restore archived tags so inactive vocabulary can return to active tag surfaces.

#### Scenario: User restores an archived tag
- **WHEN** the user restores an archived tag
- **THEN** FusionCanvas marks the tag as active
- **AND** the tag reappears in the listing tag editor autocomplete and the tree tag filter control
- **AND** preserved `ListingTag` links resume normal active chip rendering

#### Scenario: User restores a tag whose name now conflicts
- **WHEN** the user attempts to restore an archived tag and an active tag in the same store already has the same normalized name
- **THEN** FusionCanvas blocks restoration until the conflict is resolved
- **AND** the archived tag remains archived

### Requirement: Tag management guards permanent deletion with link cleanup
FusionCanvas SHALL permanently delete a tag only from the Tags tab, only after explicit confirmation that names the tag and the count of listings it is applied to, and SHALL remove the tag and every `ListingTag` link referencing it in one atomic operation.

#### Scenario: User deletes a tag applied to listings
- **WHEN** the user requests permanent deletion for a tag that is applied to one or more listings
- **AND** confirms the warning that states the tag will be removed from all listings
- **THEN** FusionCanvas permanently removes the tag and every `ListingTag` link referencing it atomically
- **AND** preserves every listing record and all other relationships
- **AND** the tag no longer appears in active or archived tag lists or on any listing

#### Scenario: User deletes an unused tag
- **WHEN** the user requests permanent deletion for a tag with no `ListingTag` links
- **AND** confirms the warning
- **THEN** FusionCanvas permanently removes the tag
- **AND** the tag no longer appears in active or archived tag lists

#### Scenario: User cancels deletion
- **WHEN** the user cancels the permanent-deletion warning
- **THEN** FusionCanvas leaves the tag, its links, and canonical selection unchanged

#### Scenario: Deletion is in progress or fails
- **WHEN** deletion is running or persistence fails
- **THEN** FusionCanvas prevents duplicate submission
- **AND** keeps the Tags tab available to report success or an actionable error
- **AND** preserves the confirmed snapshot on failure

### Requirement: Tag management applies tags to listings
FusionCanvas SHALL allow users to apply active tags in the selected store to an active listing through the listing secondary properties surface, and SHALL persist each apply operation immediately and atomically.

#### Scenario: User applies an existing active tag to a listing
- **WHEN** the user selects an active tag from the listing tag editor autocomplete and confirms it
- **THEN** FusionCanvas persists a `ListingTag` link between the listing and the tag in one atomic save
- **AND** the tag's chip appears on the listing immediately
- **AND** the listing properties description/notes draft remains untouched and unsaved

#### Scenario: User applies a tag from another store
- **WHEN** the user attempts to apply a tag that belongs to a store other than the listing's store
- **THEN** FusionCanvas rejects the apply operation
- **AND** leaves the listing and all `ListingTag` links unchanged

#### Scenario: User applies a duplicate tag to a listing
- **WHEN** the user applies a tag that is already applied to the listing
- **THEN** FusionCanvas does not create a duplicate `ListingTag` link
- **AND** the chip remains present once

#### Scenario: Apply is in progress or fails
- **WHEN** an apply operation is running or persistence fails
- **THEN** FusionCanvas prevents duplicate submission
- **AND** restores the confirmed chip state on failure
- **AND** preserves recoverable input

### Requirement: Tag management removes tags from listings
FusionCanvas SHALL allow users to remove a tag from a listing through the listing tag editor and SHALL persist each removal immediately and atomically without affecting the reusable tag record or other listings.

#### Scenario: User removes a tag from a listing
- **WHEN** the user removes a tag chip from a listing through the listing tag editor
- **THEN** FusionCanvas removes only the `ListingTag` link between that listing and that tag in one atomic save
- **AND** the reusable tag record remains available for other listings and the Tags tab
- **AND** other listings and their tag links remain unchanged

#### Scenario: User removes a tag from a listing whose parent is archived
- **WHEN** the listing is effectively hidden because an ancestor topic is archived
- **THEN** FusionCanvas blocks removal through the active listing properties surface
- **AND** explains that the listing must be restored before tag edits are available

#### Scenario: Removal is in progress or fails
- **WHEN** a removal operation is running or persistence fails
- **THEN** FusionCanvas prevents duplicate submission
- **AND** restores the confirmed chip state on failure

### Requirement: Tag management supports create-on-the-fly from the listing editor
FusionCanvas SHALL let users create a new active tag and apply it to the active listing in one operation when the typed name does not match an existing active tag in the store.

#### Scenario: User types a new tag name and commits
- **WHEN** the user types a name that does not match any active tag in the listing's store and commits
- **THEN** FusionCanvas creates a new active tag with default color and empty description in that store
- **AND** applies it to the listing in one atomic save
- **AND** the new tag's chip appears on the listing and becomes available for autocomplete and filtering

#### Scenario: Typed name matches an existing active tag
- **WHEN** the user types a name whose normalized form matches an existing active tag in the store and commits
- **THEN** FusionCanvas applies the existing tag instead of creating a duplicate
- **AND** no new tag record is created

#### Scenario: Typed name matches an archived tag
- **WHEN** the user types a name whose normalized form matches an archived tag in the store
- **THEN** FusionCanvas offers to restore the archived tag and apply it
- **AND** if the user declines, no tag is created or applied

#### Scenario: User cancels create-on-the-fly
- **WHEN** the user presses Escape or otherwise cancels an uncommitted tag input
- **THEN** FusionCanvas clears the input without creating or applying a tag
- **AND** keeps keyboard focus in the tag editor for continued entry

### Requirement: Tag management exposes the active tag vocabulary for autocomplete and filtering
FusionCanvas SHALL expose the active tags of the active store as the source of autocomplete in the listing tag editor and as the selectable entries in the tree tag filter control, and SHALL exclude archived tags from both surfaces.

#### Scenario: Listing tag editor lists active tags
- **WHEN** the user focuses the tag input on the listing properties surface
- **THEN** FusionCanvas offers the active tags of the listing's store as autocomplete entries
- **AND** excludes archived tags
- **AND** excludes tags already applied to the listing from the top of the list or marks them as applied

#### Scenario: Tree tag filter lists active tags
- **WHEN** the user opens the tree tag filter control
- **THEN** FusionCanvas offers the active tags of the active store as toggleable chips
- **AND** excludes archived tags

### Requirement: Tag management persists operations atomically
FusionCanvas SHALL persist tag create, rename, recolor, description edit, archive, restore, delete, apply-to-listing, and remove-from-listing in the active workspace database as atomic snapshot operations.

#### Scenario: Workspace reloads after tag operations
- **WHEN** a successful tag operation is followed by an application or workspace database reload
- **THEN** the resulting tags, colors, archive states, and `ListingTag` links match the last successful persisted state

#### Scenario: Persistence fails after an optimistic tag change
- **WHEN** the repository cannot save a tag operation after the UI has projected a chip or list change
- **THEN** FusionCanvas reports a recoverable error
- **AND** restores the last confirmed tags, chips, filter, expansion, selection, and clipboard state
- **AND** retains recoverable user input needed to retry when applicable

### Requirement: Tag management presents tags in a focused Tags tab
FusionCanvas SHALL present tag management in a dedicated Tags tab inside the existing store management window, with active and archived tag lists, a side editor, and lifecycle actions, while the regular workspace exposes only the per-listing tag editor and the tree tag filter.

#### Scenario: User opens the Tags tab
- **WHEN** the user chooses to manage tags for the active store
- **THEN** FusionCanvas opens the existing store management window on its Tags tab for the selected store
- **AND** active and archived tags for that store are shown separately
- **AND** the side editor exposes name, color, optional description, save, archive, restore, and delete actions
- **AND** switching between Basic info, Niches, and Tags tabs does not change the selected store

#### Scenario: Tags tab separates active and archived tags
- **WHEN** the Tags tab is open for a store with both active and archived tags
- **THEN** FusionCanvas shows active tags in the normal active list
- **AND** keeps archived tags out of the active list unless the user intentionally opens archived tag review

#### Scenario: Store has no tags
- **WHEN** the active store has no active or archived tags
- **THEN** FusionCanvas shows an empty active-tag state with a clear way to create a tag
- **AND** does not require the user to create a listing first

#### Scenario: User switches tags with unsaved editor changes
- **WHEN** the Tags tab side editor contains meaningful unsaved name, color, or description changes and the user switches tag or closes the window
- **THEN** FusionCanvas asks whether to save, discard, or cancel
- **AND** retains the current draft and focus when cancellation is chosen

#### Scenario: Tags tab follows compact action sizing
- **WHEN** the Tags tab shows save, archive, restore, delete, discard-confirmation, or warning-confirmation actions
- **THEN** those action buttons use compact fixed or content-based widths
- **AND** the buttons are not evenly stretched across the full store management window width

### Requirement: Tag management follows shared desktop control guidance
FusionCanvas SHALL use compact action sizing, clear tooltips for icon-only commands, predictable keyboard flow, and accessible confirmation and cancellation behavior across the Tags tab, listing tag editor, and tree tag filter.

#### Scenario: Tag actions are presented
- **WHEN** create, rename, recolor, save, archive, restore, delete-confirmation, apply, or remove actions are shown
- **THEN** their buttons are sized to their command groups rather than evenly stretched across the surface
- **AND** essential actions are reachable without pointer-only interaction

#### Scenario: Icon-only tag command is shown
- **WHEN** an icon-only tag command appears in the Tags tab, listing tag editor, or tree tag filter
- **THEN** the command exposes a tooltip that describes its action

#### Scenario: Tag editor supports keyboard flow
- **WHEN** the listing tag editor is focused
- **THEN** arrow keys navigate autocomplete, Enter applies or creates the selected entry, Backspace removes the last applied chip, and Escape clears the input without losing existing chips
- **AND** focus remains in the editor after an apply or remove so rapid multi-tag entry is possible
