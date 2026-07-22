## MODIFIED Requirements

### Requirement: Listing inspector presents the listing as a product concept
FusionCanvas SHALL display a Listing Inspector in the document window detail area for the active listing document context, showing the working title, lifecycle status, current workflow stage, topic path, description, idea, phrase, graphic direction, notes, linked tags, and related assets so the user can understand what the listing is and what needs to happen next.

#### Scenario: User opens a listing
- **WHEN** a listing becomes the active document context
- **THEN** the detail area displays the inspector with the listing's working title, status, workflow stage, topic path, description, idea, phrase, graphic direction, notes, tags, and related assets
- **AND** the content reflects the persisted listing record

#### Scenario: Listing has no creative details yet
- **WHEN** the active listing has no description, idea, phrase, graphic direction, notes, tags, or assets
- **THEN** the inspector shows quiet empty placeholders for those sections
- **AND** does not fabricate content or block viewing

#### Scenario: User browses between listings
- **WHEN** the user selects a different listing and the current inspector has no unsaved changes
- **THEN** the inspector updates to the newly active listing without opening additional surfaces
- **AND** the user keeps orientation through the unchanged tab and navigator context

### Requirement: Listing inspector manages listing tags
FusionCanvas SHALL allow the user to view the listing's linked tags, link an existing reusable store tag, create a new reusable store tag when the entered name has no case-insensitive match in the store, and remove tag links, SHALL normalize tag names to nonblank single-line values, SHALL persist each tag addition or removal immediately and atomically when the user applies it, and SHALL NOT delete reusable tag records when a link is removed.

#### Scenario: User links an existing tag
- **WHEN** the user adds a tag whose name matches an existing store tag
- **THEN** FusionCanvas links that reusable tag to the listing when the addition is committed
- **AND** does not create a duplicate tag record

#### Scenario: User creates a new tag inline
- **WHEN** the user adds a tag name with no case-insensitive match in the listing's store
- **THEN** FusionCanvas creates one normalized reusable store tag and links it to the listing when the addition is committed

#### Scenario: User removes a tag link
- **WHEN** the user removes a tag from the listing
- **THEN** FusionCanvas removes only the listing-tag link when the removal is committed
- **AND** preserves the reusable tag record and its other links

#### Scenario: User submits an invalid tag name
- **WHEN** the user attempts to add an empty or whitespace-only tag name
- **THEN** FusionCanvas rejects the addition
- **AND** leaves the listing's current tag links unchanged

### Requirement: Listing inspector keeps inactive listings read-only
FusionCanvas SHALL present an archived or otherwise effectively inactive listing in a read-only inspector with a clear inactive notice and a restore action, and SHALL disable editing controls rather than hiding the listing's context.

#### Scenario: User opens an archived listing
- **WHEN** an archived listing becomes the active document context
- **THEN** the inspector displays the listing's details read-only
- **AND** shows an inactive notice with a restore action
- **AND** disables editing controls

#### Scenario: Listing is inactive through archived ancestry
- **WHEN** the active listing is effectively inactive because its store, niche, group, or a group ancestor is archived
- **THEN** the inspector applies the same read-only presentation and restore guidance

## REMOVED Requirements

### Requirement: Listing inspector edits core creative fields with explicit save
**Reason:** Creative-field edits now persist automatically on field exit; see "Listing inspector edits core creative fields with automatic save".

FusionCanvas SHALL allow the user to edit the listing's working title, idea, phrase, graphic direction, and notes in the inspector and SHALL persist those edits only through an explicit save that validates the title, normalizes values, preserves listing identity and placement, and preserves unknown metadata keys and inherited provenance.

#### Scenario: User saves valid creative edits
- **WHEN** the user changes the title, idea, phrase, graphic direction, or notes and invokes Save with a valid nonblank single-line title
- **THEN** FusionCanvas persists the normalized values and updated timestamp
- **AND** keeps the listing's identity, topic placement, archive state, status, stage, assets, prompts, and unknown metadata unchanged
- **AND** refreshes the inspector draft baseline to the saved state

#### Scenario: User submits an invalid title
- **WHEN** the user attempts to save with an empty, whitespace-only, or multi-line title
- **THEN** FusionCanvas rejects the save
- **AND** reports an inline validation error
- **AND** preserves both the persisted listing and the user's editable draft for correction

#### Scenario: Phrase is normalized to one line
- **WHEN** the user saves a phrase containing line breaks
- **THEN** FusionCanvas normalizes the phrase to a single line before persisting

#### Scenario: Save preserves unrelated metadata and provenance
- **WHEN** the listing's metadata contains keys or inherited-provenance entries the inspector does not edit
- **THEN** a successful inspector save preserves those entries unchanged

### Requirement: Listing inspector guards unsaved changes
**Reason:** With automatic save the steady state is "nothing unsaved"; leaving the inspector now commits pending edits instead of prompting. See "Listing inspector commits pending edits when the context changes".

FusionCanvas SHALL track unsaved inspector edits as a draft distinct from persisted state and SHALL ask whether to save, discard, or cancel when the user starts another listing selection, switches document tabs, or closes a tab with meaningful unsaved changes.

#### Scenario: User switches listing with unsaved changes
- **WHEN** the inspector has meaningful unsaved changes and the user selects another listing or topic
- **THEN** FusionCanvas asks whether to save, discard, or cancel
- **AND** keeps the current listing active with its draft intact when the user cancels

#### Scenario: User closes a tab with unsaved changes
- **WHEN** the user closes a document tab whose inspector has meaningful unsaved changes
- **THEN** FusionCanvas asks whether to save, discard, or cancel before closing
- **AND** keeps the tab open with the draft intact when the user cancels

#### Scenario: User discards changes
- **WHEN** the user chooses to discard unsaved changes
- **THEN** the inspector reverts to the last persisted listing state before the transition continues

## ADDED Requirements

### Requirement: Listing inspector edits core creative fields with automatic save
FusionCanvas SHALL allow the user to edit the listing's working title, description, idea, phrase, graphic direction, and notes inline in the inspector and SHALL persist edits automatically when the edited field loses focus, validating the title, normalizing values, preserving listing identity and placement, and preserving unknown metadata keys and inherited provenance, without an explicit save action.

#### Scenario: User leaves a field with valid edits
- **WHEN** the user changes the title, description, idea, phrase, graphic direction, or notes and the edited field loses focus
- **THEN** FusionCanvas persists the normalized values and updated timestamp atomically
- **AND** keeps the listing's identity, topic placement, archive state, status, stage, assets, prompts, and unknown metadata unchanged
- **AND** refreshes the inspector draft baseline to the saved state

#### Scenario: User leaves a field with an invalid title
- **WHEN** the user empties the working title or enters a multi-line title and the field loses focus
- **THEN** FusionCanvas reverts the title to its last persisted value
- **AND** reports an inline validation error explaining the revert
- **AND** still persists any other valid edited fields

#### Scenario: Phrase is normalized to one line
- **WHEN** the user commits a phrase containing line breaks
- **THEN** FusionCanvas normalizes the phrase to a single line before persisting

#### Scenario: Commit preserves unrelated metadata and provenance
- **WHEN** the listing's metadata contains keys or inherited-provenance entries the inspector does not edit
- **THEN** a successful inspector commit preserves those entries unchanged

#### Scenario: Commit fails to persist
- **WHEN** the repository cannot complete an inspector commit
- **THEN** FusionCanvas reports a recoverable inline error
- **AND** keeps the user's editable draft for a later retry
- **AND** leaves no partially applied listing, tag, or tag-link changes

### Requirement: Listing inspector commits pending edits when the context changes
FusionCanvas SHALL commit any pending inspector edits when the user starts another listing or topic selection, switches document tabs, or closes a tab, and SHALL NOT prompt to save, discard, or cancel.

#### Scenario: User switches listing with pending edits
- **WHEN** the inspector has pending edits and the user selects another listing or topic
- **THEN** FusionCanvas commits the edits following the automatic-save rules before the transition
- **AND** completes the transition without prompting

#### Scenario: Persistence fails during a leave commit
- **WHEN** the repository cannot persist pending edits while the context is changing
- **THEN** FusionCanvas reports a recoverable inline error
- **AND** leaves the persisted listing unchanged
- **AND** allows the transition to complete so the user is never trapped

### Requirement: Listing inspector hosts listing lifecycle actions
FusionCanvas SHALL provide archive, restore, and confirmed permanent-delete actions for the active listing in the inspector, SHALL keep archived listings reachable and restorable through the tree's archived view and the read-only inspector, and SHALL keep inline creation, rename, move, and duplication in the tree.

#### Scenario: User archives the active listing
- **WHEN** the user confirms archive for the active listing
- **THEN** FusionCanvas marks the listing archived through the existing listing-management behavior
- **AND** the inspector presents the read-only inactive state with a restore action
- **AND** the tree and document context refresh to the persisted state

#### Scenario: User restores an archived listing
- **WHEN** the user restores an archived listing from the inspector
- **THEN** FusionCanvas restores the listing to its preserved topic through the existing listing-management behavior
- **AND** reports an actionable error when the preserved topic path is not active

#### Scenario: User permanently deletes the active listing
- **WHEN** the user confirms permanent deletion for the active listing
- **THEN** FusionCanvas deletes the listing through the existing listing-management deletion guard
- **AND** surfaces the connected-records reason when deletion is blocked
- **AND** selects a sensible nearby active listing when one exists, otherwise the active parent topic

#### Scenario: Lifecycle operation is in progress or fails
- **WHEN** archive, restore, or deletion is running or fails validation or persistence
- **THEN** FusionCanvas prevents duplicate submission
- **AND** keeps the inspector available to report success or an actionable error
