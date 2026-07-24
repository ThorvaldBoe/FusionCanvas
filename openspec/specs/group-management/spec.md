# Group Management

## Purpose

Defines user-facing group management behavior for the niche-rooted hierarchical tree that organizes active groups and future items within a store, including inline creation, renaming, structural move/copy, archive/restore, filtering, keyboard access, and deletion.

## Requirements

### Requirement: Group management presents a niche-rooted editable hierarchy
FusionCanvas SHALL present active niches as the roots of a compact hierarchical tree containing their active groups and future items, with visible ancestry, expansion state, selection state, and extensible visual indicators.

#### Scenario: User views groups in a selected store
- **WHEN** the selected store contains active niches and nested groups
- **THEN** the navigation pane shows each niche as a top-level root and each group beneath its direct parent
- **AND** expanders and hierarchy guides make parent-child relationships visually unambiguous
- **AND** the presentation does not flatten the hierarchy into unrelated navigation buttons

#### Scenario: Tree row exposes rich presentation slots
- **WHEN** a niche or group row is rendered
- **THEN** FusionCanvas can display an entity icon, optional color token, name, counts, warnings, status, tags, or other supplied indicators
- **AND** absence of an optional property does not leave misleading content

### Requirement: Group management creates and renames groups inline
FusionCanvas SHALL make inline tree editing the primary workflow for creating and renaming groups.

#### Scenario: User creates a child group by keyboard
- **WHEN** the user invokes New Group or presses `Ctrl+Shift+N` with a valid effective destination
- **THEN** FusionCanvas inserts a non-persisted inline group draft beneath that destination
- **AND** supplies a unique default name with the complete name selected for immediate replacement
- **AND** does not open the detailed group editor

#### Scenario: User commits or cancels an inline draft
- **WHEN** the inline name editor has focus
- **THEN** Enter validates and saves the group and Escape cancels the draft without persistence
- **AND** a validation or persistence failure retains the editable name and provides corrective feedback

#### Scenario: User renames the selected group
- **WHEN** the user presses F2 on an active group
- **THEN** FusionCanvas edits the name in place while preserving the group's identity, parent, descendants, listings, and metadata

#### Scenario: User creates many sibling groups
- **WHEN** the user presses Shift+Enter while committing a valid new group
- **THEN** FusionCanvas saves that group and immediately begins another inline group beneath the same creation anchor
- **AND** repeated creation does not accidentally nest each new group beneath the previous one

### Requirement: Group management resolves creation destination deterministically
FusionCanvas SHALL resolve the destination for a new group from canonical tree selection and the selected store's default niche.

#### Scenario: Group is selected
- **WHEN** an active group is selected and the user creates a group
- **THEN** the selected group is the new group's direct parent

#### Scenario: Item is selected
- **WHEN** a future item is selected and the user creates a group
- **THEN** FusionCanvas uses the item's containing group, or its niche when the item is ungrouped, as the destination

#### Scenario: Niche is selected
- **WHEN** an active niche is selected and the user creates a group
- **THEN** the new group is created at the root of that niche

#### Scenario: No applicable topic is selected
- **WHEN** the selected store has a valid active default niche and no group, item, or niche supplies a destination
- **THEN** FusionCanvas creates the group at that default niche

#### Scenario: Multiple niches have no valid default
- **WHEN** a store has multiple active niches, no applicable topic is selected, and no valid default niche exists
- **THEN** FusionCanvas blocks creation with guidance to select or configure a default niche
- **AND** does not guess from alphabetical order

### Requirement: Stores persist a valid default niche
FusionCanvas SHALL support one store-scoped default niche for fallback group and future item creation.

#### Scenario: Store has one active niche
- **WHEN** a store first has exactly one active niche
- **THEN** FusionCanvas may establish that niche as the default automatically

#### Scenario: Default niche becomes inactive
- **WHEN** the default niche is archived or becomes unavailable
- **THEN** FusionCanvas requires or guides selection of a valid replacement before fallback creation is available
- **AND** never creates content beneath the inactive niche

### Requirement: Group selection is independent of document tabs
FusionCanvas SHALL maintain canonical tree selection separately from persistent document tabs.

#### Scenario: User normally selects a node
- **WHEN** the user clicks a niche, group, or future item without a tab-opening modifier, or reaches it by keyboard
- **THEN** FusionCanvas selects and reveals that node and updates the reusable right-side inspector
- **AND** does not create a new tab

#### Scenario: User explicitly opens a node in a tab
- **WHEN** the user Ctrl-clicks a group or future item
- **THEN** FusionCanvas opens or activates a persistent tab for that entity
- **AND** avoids opening duplicate tabs for the same entity

### Requirement: Group management validates names within the direct parent
FusionCanvas SHALL require a non-empty trimmed group name and SHALL keep normalized active group names unique among siblings with the same direct parent.

#### Scenario: Duplicate active sibling name is submitted
- **WHEN** an active sibling under the same niche or group has the same case-insensitive normalized name
- **THEN** FusionCanvas rejects create, rename, restore, or copy that would introduce the duplicate
- **AND** retains recoverable input without partially changing the hierarchy

#### Scenario: Separate branches reuse a name
- **WHEN** the same normalized name exists beneath a different direct parent
- **THEN** FusionCanvas permits the name

### Requirement: Group management edits detailed properties safely
FusionCanvas SHALL present the active group's detailed properties inline in the document window details pane: name, description, and notes edit inline and persist automatically when the edited field loses focus, while destination move, archive, and restore remain explicit confirmed actions, and ordinary inline create or rename remains in the tree.

#### Scenario: User reviews group details
- **WHEN** a group becomes the active document context
- **THEN** the details pane displays the group's name, description, notes, topic path, and archive state
- **AND** the main tree selection and document context remain preserved
- **AND** ordinary inline create or rename remains in the tree

#### Scenario: User leaves a field with valid edits
- **WHEN** the user changes the group's name, description, or notes and the edited field loses focus
- **THEN** FusionCanvas validates and persists the normalized values and updated timestamp atomically
- **AND** preserves descendant groups, contained listings, placement, archive state, and unknown metadata

#### Scenario: User leaves a field with an invalid name
- **WHEN** the user empties the group name or enters a duplicate active sibling name and the field loses focus
- **THEN** FusionCanvas reverts the name to its last persisted value
- **AND** reports an inline validation error explaining the revert
- **AND** still persists any other valid edited fields

#### Scenario: User moves, archives, or restores the group
- **WHEN** the user chooses a destination and invokes Move, or confirms archive, or invokes restore
- **THEN** FusionCanvas performs the existing validated operation with confirmation for destructive actions
- **AND** reports an actionable error when the operation is unavailable or fails
- **AND** the details pane and tree refresh to the persisted state

#### Scenario: Commit fails to persist
- **WHEN** the repository cannot complete a details commit
- **THEN** FusionCanvas reports a recoverable inline error
- **AND** keeps the user's editable draft for a later retry
- **AND** leaves the persisted group unchanged

### Requirement: Group management moves complete subtrees within one store
FusionCanvas SHALL move a group beneath an active niche or group in the same store while preserving the moved group, descendant groups, contained listings, and connected context.

#### Scenario: User drops a group onto a valid container
- **WHEN** the user drops an active group onto an active same-store niche or non-descendant group
- **THEN** FusionCanvas changes the moved root's direct parent and appends it to the destination's children
- **AND** preserves descendant parent relationships and contained record identities

#### Scenario: User cuts and pastes a group
- **WHEN** the user presses Ctrl+X on a group and Ctrl+V on a valid destination
- **THEN** FusionCanvas performs the same validated subtree move as drag-and-drop
- **AND** clears cut state only after a successful save or clipboard replacement

### Requirement: Group movement rejects invalid destinations atomically
FusionCanvas SHALL reject movement to self, a descendant, a missing or archived topic, or a topic in another store before saving any part of the operation.

#### Scenario: User attempts an invalid drop or paste
- **WHEN** the requested destination would create a cycle or violates store or active-path rules
- **THEN** FusionCanvas shows blocked-target feedback and leaves the canonical hierarchy unchanged

#### Scenario: Persistence fails during a valid move
- **WHEN** a validated move cannot be saved
- **THEN** FusionCanvas restores the last confirmed parent, position, selection, and expansion projection
- **AND** reports a recoverable error without a partial subtree move

### Requirement: Group management copies group subtrees with new identities
FusionCanvas SHALL support copying and pasting group structure and group metadata beneath a valid same-store destination.

#### Scenario: User copies and pastes a group
- **WHEN** the user presses Ctrl+C on a group and Ctrl+V on a valid destination
- **THEN** FusionCanvas creates new identities for the copied group and every descendant group
- **AND** preserves their relative hierarchy and eligible group metadata
- **AND** appends the copied root beneath the destination

#### Scenario: Groups contain future items
- **WHEN** item management has not defined item-inclusive copy semantics
- **THEN** FC-0103 copies group structure and group metadata only
- **AND** does not silently duplicate or relink item records

### Requirement: Group management persists sibling order
FusionCanvas SHALL persist deterministic parent-scoped sibling order for groups and update affected siblings atomically.

#### Scenario: User drops a group between siblings
- **WHEN** the drop occurs before or after a visible sibling and placement is unambiguous
- **THEN** FusionCanvas persists the new parent and sibling position
- **AND** the group remains at that position after reload

#### Scenario: Existing groups are migrated
- **WHEN** an older workspace without explicit group order is opened after upgrade
- **THEN** FusionCanvas assigns deterministic order using the previously visible case-insensitive name order
- **AND** preserves all hierarchy, identities, archive state, listings, and metadata

### Requirement: Structural changes save automatically and recover safely
FusionCanvas SHALL persist confirmed inline edits and structural operations immediately without requiring a separate Save Structure action.

#### Scenario: Structural operation succeeds
- **WHEN** create, rename, move, reorder, paste, archive, or restore succeeds
- **THEN** FusionCanvas updates the confirmed tree projection and retains meaningful selection and expansion state

#### Scenario: Structural operation fails
- **WHEN** validation or persistence rejects an operation
- **THEN** FusionCanvas restores or retains the last confirmed canonical structure
- **AND** preserves recoverable draft input where applicable
- **AND** prevents conflicting structural commands while a save is in progress

### Requirement: Group management filters a canonical hierarchy
FusionCanvas SHALL support free-text filtering through an extensible query projection that can later include status, tags, archive state, entity type, warnings, and other properties.

#### Scenario: Free-text query matches a nested group
- **WHEN** a nested group's searchable text matches the query
- **THEN** FusionCanvas shows the matching group and every ancestor required to understand its path
- **AND** supplies match metadata for highlighting

#### Scenario: User clears filtering
- **WHEN** the active query is cleared
- **THEN** FusionCanvas restores the pre-filter expansion state
- **AND** identifies selection by stable entity ID rather than filtered row position

#### Scenario: Filter hides siblings during reorder
- **WHEN** hidden siblings make a before/after insertion position ambiguous
- **THEN** FusionCanvas disables positional reorder with corrective feedback
- **AND** may still allow an unambiguous drop onto a visible container

### Requirement: Group management archives groups without deleting context
FusionCanvas SHALL archive an active group while preserving its descendant subtree, listings, sibling-order intent, and connected context.

#### Scenario: User archives a group
- **WHEN** the user confirms archive for an active group
- **THEN** FusionCanvas marks that group archived and hides its complete subtree from active navigation
- **AND** archived review retains the preserved structure
- **AND** selection falls back to the nearest active ancestor niche

### Requirement: Group management restores archived groups to valid preserved locations
FusionCanvas SHALL restore an archived group to its preserved active parent when the parent path and sibling name are valid.

#### Scenario: User restores an archived group
- **WHEN** the preserved parent path is active and no active sibling name conflicts
- **THEN** FusionCanvas restores the group and preserved descendants at the preserved or deterministically normalized sibling position
- **AND** reveals and selects the restored group

#### Scenario: Restore destination is invalid
- **WHEN** the parent path is inactive, missing, or has a conflicting sibling name
- **THEN** FusionCanvas blocks restoration with corrective guidance
- **AND** leaves the group archived

### Requirement: Active and archived projections remain coherent
FusionCanvas SHALL exclude archived groups and descendants hidden by archived ancestors from normal navigation and SHALL expose archived groups through a separate intentional review projection.

#### Scenario: Tree contains mixed lifecycle states
- **WHEN** active groups, archived roots, and descendants of archived roots exist
- **THEN** the active tree shows only nodes whose complete ancestor path is active
- **AND** archived review shows preserved archived roots and descendants without making them active workspace context

### Requirement: Essential group management is keyboard accessible
FusionCanvas SHALL provide predictable keyboard navigation and shortcuts without requiring pointer interaction.

#### Scenario: Tree has keyboard focus
- **WHEN** the user operates the tree by keyboard
- **THEN** arrow keys navigate and expand or collapse nodes, `Ctrl+Shift+N` creates a group, F2 renames, Ctrl+C/X/V copy/cut/paste, Enter commits inline editing, and Escape cancels it
- **AND** text-edit focus prevents unrelated global structural shortcuts from firing

#### Scenario: Operation is unavailable
- **WHEN** a shortcut has no valid source or destination
- **THEN** FusionCanvas leaves the workspace unchanged and communicates actionable guidance

### Requirement: Group rows expose contextual management actions
FusionCanvas SHALL provide a context menu when the user right-clicks an active group row.

#### Scenario: User opens a group context menu
- **WHEN** the user right-clicks an active group
- **THEN** FusionCanvas selects that group and offers New group, Rename, Copy, Cut, Paste, and Delete actions
- **AND** New group creates a direct child of the clicked group
- **AND** Paste reflects whether the application clipboard currently contains a group operation

### Requirement: Group deletion is confirmed, permanent, and atomic
FusionCanvas SHALL require explicit confirmation before permanently deleting a group subtree and its contained items.

#### Scenario: User requests permanent group deletion
- **WHEN** the user invokes Delete from a group context menu
- **THEN** FusionCanvas shows the group name and warns that the group, every descendant group, and every contained item will be permanently lost
- **AND** canceling leaves all data and selection unchanged

#### Scenario: User confirms permanent group deletion
- **WHEN** the user confirms the destructive warning
- **THEN** FusionCanvas atomically removes the selected group, all descendant groups, all listings/items contained anywhere in that subtree, and dependent listing relationships
- **AND** removes links to deleted entities without deleting otherwise reusable asset records
- **AND** closes document tabs whose entity was deleted
- **AND** selects the nearest surviving parent group or niche

#### Scenario: Permanent deletion cannot be saved
- **WHEN** persistence fails while deleting a confirmed subtree
- **THEN** FusionCanvas leaves the complete confirmed hierarchy and related records unchanged
- **AND** reports a recoverable error
