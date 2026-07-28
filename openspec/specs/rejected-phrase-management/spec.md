# Rejected Phrase Management

## Purpose

Defines how FusionCanvas lets creators view, filter, edit, create, and delete durable ideation rejections (the `IdeationRejection` records captured during Ideation or curated manually) so the negative-guidance library used by Ideation stays accurate and useful.

## Requirements

### Requirement: Rejected phrases are managed in a focused dialog launched from Ideation
FusionCanvas SHALL expose rejected-phrase management as one focused Rejected Phrases dialog opened from a single `Manage rejected phrases…` action in the Ideation dialog, SHALL own that dialog as a single-instance modal nested over the Ideation dialog, and SHALL NOT add a launcher to the main workspace window or application settings.

#### Scenario: Creator opens the manager from Ideation
- **WHEN** the Ideation dialog is open and the creator chooses `Manage rejected phrases…`
- **THEN** one Rejected Phrases dialog opens owned by the Ideation dialog
- **AND** opening a second time while the dialog is already open focuses the existing dialog instead of creating another

#### Scenario: Manager is absent from the main workspace
- **WHEN** a contributor reviews the completed module's entry points
- **THEN** the main workspace window and application settings contain no Rejected Phrases launcher
- **AND** the dialog remains constructible and testable for its Ideation-owned opener

#### Scenario: Manager does not disturb Ideation state
- **WHEN** the Rejected Phrases dialog opens, runs, or closes
- **THEN** the Ideation dialog's candidates, progress, selection, mode, guidance, count, and rejection draft remain unchanged

### Requirement: The manager lists workspace rejections with live search
FusionCanvas SHALL load every durable ideation rejection in the active workspace into the Rejected Phrases dialog, SHALL display each as a list row showing the rejected phrase text and optional reason, SHALL preselect a sensible first row when one exists, and SHALL filter the visible list as the creator types using case-insensitive substring matching across both phrase and reason.

#### Scenario: Dialog opens with existing rejections
- **WHEN** the active workspace contains durable ideation rejections and the creator opens the manager
- **THEN** every workspace rejection appears as a row with its phrase and reason
- **AND** the first row is selected by default
- **AND** the editor shows that rejection's phrase and reason

#### Scenario: Dialog opens with no rejections
- **WHEN** the active workspace contains no durable ideation rejections
- **THEN** the dialog presents an empty state with a clear New action
- **AND** no row is selected

#### Scenario: Search matches phrase or reason
- **WHEN** the creator enters search text contained in a rejection's phrase or reason with different or matching casing
- **THEN** the visible list includes that rejection

#### Scenario: Search has no matches
- **WHEN** no phrase or reason contains the current search text
- **THEN** the dialog shows a no-results state
- **AND** provides a clear way to change or clear the search

### Requirement: The manager filters by store, niche, and optional group scope
FusionCanvas SHALL expose a scope filter in the Rejected Phrases dialog that defaults to the active Ideation scope (store, niche, and optional group) when the dialog opens, SHALL allow the creator to narrow the list to any one store, niche, or store-plus-niche-plus-optional-group scope present in the active workspace, and SHALL allow returning to the whole-workspace view.

#### Scenario: Default scope matches active Ideation scope
- **WHEN** the creator opens the manager from an Ideation dialog whose resolved scope is group `Pugs` in niche `Dogs`
- **THEN** the scope filter is set to that group scope
- **AND** only rejections whose store, niche, and optional group match that scope are visible

#### Scenario: Creator narrows to niche scope
- **WHEN** the creator changes the scope filter from a group scope to its parent niche scope
- **THEN** the visible list includes every rejection in that niche regardless of group association
- **AND** rejections in other niches or stores are absent

#### Scenario: Creator returns to whole-workspace view
- **WHEN** the creator clears or resets the scope filter
- **THEN** every workspace rejection is visible regardless of scope

#### Scenario: Active scope filter does not silently discard input
- **WHEN** the active scope filter excludes the currently selected rejection
- **THEN** the editor does not silently change or discard meaningful unsaved input
- **AND** the visible selection and editor state remain coherent

### Requirement: Selecting a rejection loads its phrase and reason into the editor
FusionCanvas SHALL load the selected rejection's phrase and reason into the editor, SHALL keep that editor input as an in-memory draft until explicitly saved, and SHALL keep the selected rejection's persisted values authoritative until a save succeeds.

#### Scenario: Creator selects a rejection
- **WHEN** the creator selects a visible rejection row
- **THEN** the editor loads that rejection's current phrase and reason
- **AND** the editor is not marked as dirty

#### Scenario: Creator edits the editor
- **WHEN** the creator changes the phrase or reason in the editor
- **THEN** the editor is marked as dirty
- **AND** Save becomes available once the phrase is non-empty

### Requirement: Editing preserves identity, scope, mode, and creation time
FusionCanvas SHALL allow the creator to edit only the phrase and reason of a selected rejection and SHALL preserve its stable identity, store, niche, optional group, generation mode, and `CreatedAt` value, advancing a new optional `UpdatedAt` timestamp on each successful save.

#### Scenario: Creator saves an edit
- **WHEN** the creator changes a selected rejection's phrase or reason to a valid, unique value and saves
- **THEN** FusionCanvas updates that rejection in place
- **AND** preserves its identity, store, niche, optional group, mode, and `CreatedAt`
- **AND** sets or advances its `UpdatedAt` to the save time

#### Scenario: Creator edits only the reason
- **WHEN** the creator changes only the reason and saves
- **THEN** the phrase, identity, scope, mode, and `CreatedAt` remain unchanged
- **AND** `UpdatedAt` advances

#### Scenario: Creator cancels an edit
- **WHEN** the creator discards unsaved edits for a selected rejection
- **THEN** the editor restores that rejection's current persisted phrase and reason
- **AND** the editor is no longer marked as dirty

### Requirement: Rejected phrases are unique within their scope after normalization
FusionCanvas SHALL compare rejection phrase text within the same store, niche, and optional group scope using a canonical duplicate key that trims outer whitespace, collapses whitespace runs, and compares text without case sensitivity, and SHALL refuse a create or edit that would collide with another rejection in the same scope.

#### Scenario: Create duplicates an existing phrase in the same scope
- **WHEN** a new rejection's phrase differs from an existing rejection's phrase in the same store, niche, and optional group scope only by casing or insignificant whitespace
- **THEN** FusionCanvas refuses to create the duplicate
- **AND** leaves the existing record unchanged
- **AND** keeps the creator's recoverable draft

#### Scenario: Edit collides with another phrase in the same scope
- **WHEN** an edited phrase normalizes to the phrase of a different rejection in the same scope
- **THEN** FusionCanvas refuses the edit
- **AND** preserves the selected record and its recoverable draft

#### Scenario: Same phrase is allowed in a different scope
- **WHEN** a new or edited phrase normalizes to the phrase of a rejection in a different store, niche, or group scope
- **THEN** FusionCanvas accepts the phrase
- **AND** persists the record

### Requirement: Creators can create rejected phrases manually
FusionCanvas SHALL allow the creator to create a new rejected phrase from an in-memory draft, SHALL default the new record's store, niche, and optional group to the active scope filter's values when they identify a single store, niche, and optional group, SHALL set its generation mode to the existing `Basic` ideation mode, SHALL set `CreatedAt` to the save time and leave `UpdatedAt` null, and SHALL select the created record after a successful save.

#### Scenario: Creator saves a new rejected phrase at the active scope
- **WHEN** the scope filter is set to group `Pugs` in niche `Dogs` and the creator saves a valid, unique new phrase and optional reason
- **THEN** FusionCanvas creates one persisted rejection with that store, niche, and group
- **AND** sets its mode to `Basic`
- **AND** selects the created rejection

#### Scenario: Creator saves a new rejected phrase at whole-workspace view
- **WHEN** the scope filter is cleared and the creator saves a valid, unique new phrase and optional reason
- **THEN** FusionCanvas refuses the creation
- **AND** communicates that a single store and niche scope is required to create a manual rejection
- **AND** keeps the creator's recoverable draft

#### Scenario: Creator cancels or abandons a blank draft
- **WHEN** the creator starts a new rejection but provides no meaningful input and cancels or selects another record
- **THEN** FusionCanvas discards the blank draft without prompting
- **AND** does not persist a rejection

### Requirement: Permanent deletion is explicit and confirmed
FusionCanvas SHALL permanently delete a rejection only after the creator requests deletion for an existing record and confirms a warning, SHALL select a sensible remaining visible rejection when one exists after deletion, and SHALL otherwise show the empty or no-results state appropriate to the active search and scope.

#### Scenario: Creator confirms deletion
- **WHEN** the creator requests deletion of a selected rejection and confirms the warning
- **THEN** FusionCanvas permanently removes that rejection
- **AND** selects a sensible remaining visible rejection when one exists
- **AND** otherwise shows the empty or no-results state appropriate to the active search and scope

#### Scenario: Creator cancels deletion
- **WHEN** the creator cancels the deletion warning
- **THEN** FusionCanvas keeps the selected rejection unchanged
- **AND** returns focus to a meaningful control for that record

#### Scenario: New draft cannot be deleted
- **WHEN** an unsaved new draft is active
- **THEN** deletion is unavailable until the draft is saved or discarded

### Requirement: The dialog protects drafts and supports keyboard use
FusionCanvas SHALL protect meaningful unsaved phrase or reason edits during selection changes, scope changes, search changes, and dialog close, and SHALL make search, scope selection, list selection, editing, saving, confirmation, cancellation, and closing keyboard reachable.

#### Scenario: Creator leaves meaningful unsaved edits
- **WHEN** the creator changes selection, changes scope, changes search, or closes the dialog with meaningful unsaved input
- **THEN** FusionCanvas offers Save, Discard, and Cancel
- **AND** Cancel keeps the current draft, selection, and focus context

#### Scenario: Creator starts a new draft
- **WHEN** the creator chooses New
- **THEN** FusionCanvas creates only an in-memory draft
- **AND** places keyboard focus in the phrase field
- **AND** disables deletion until the draft is saved

#### Scenario: Creator completes or cancels a confirmation
- **WHEN** a save/discard decision or deletion confirmation completes or is cancelled
- **THEN** keyboard focus returns to the next meaningful editor, list, or invoking control
- **AND** no essential action requires pointer-only interaction

#### Scenario: Creator operates the manager with a keyboard
- **WHEN** keyboard focus enters the dialog
- **THEN** search, scope filter, list selection, phrase, reason, New, Save, Delete, and Close are reachable in a predictable order

### Requirement: Manager operations are durable, atomic, and recoverable
FusionCanvas SHALL load the active workspace snapshot before each management operation, SHALL persist create, edit, and delete operations atomically through the existing workspace save path so that no partial rejection change is ever committed, SHALL refresh open representations of the workspace after a successful save, and SHALL report a recoverable error and preserve the last confirmed state plus the creator's recoverable draft when an operation fails.

#### Scenario: Save succeeds
- **WHEN** a create, edit, or delete operation persists successfully
- **THEN** the workspace snapshot reflects the change
- **AND** the navigation tree and other open representations refresh from authoritative workspace state
- **AND** the manager reflects the confirmed state

#### Scenario: Save fails
- **WHEN** persistence fails before a create, edit, or delete completes
- **THEN** no partial rejection row or partial workspace snapshot is committed
- **AND** the manager reports a recoverable error
- **AND** preserves the last confirmed state plus the input needed to retry when applicable

#### Scenario: Concurrent operations are serialized
- **WHEN** a management operation is in progress
- **THEN** the manager prevents duplicate submission and disables conflicting mutation actions
- **AND** shows an appropriate busy state

### Requirement: Manual curation remains within the rejected-phrase surface
The rejected-phrase-management module SHALL NOT change the Ideation candidate generation flow, SHALL NOT change Ideation context assembly, SHALL NOT add CSV import or export, archive or restore rejections, synchronize rejections through cloud services or workspace transfer, attach rejections to creative records, categorize or tag rejections, or provide whole-application backup behavior.

#### Scenario: Contributor reviews module scope
- **WHEN** a contributor reviews the completed implementation
- **THEN** the implementation supplies view, filter, edit, create, and delete behavior for the existing durable ideation rejections only
- **AND** future ideation and transfer behavior continues to consume those records unchanged
