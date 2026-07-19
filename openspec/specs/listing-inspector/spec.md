# Listing Inspector

## Purpose

Defines how FusionCanvas presents a focused, durable Listing Inspector in the document window detail area for the active listing, so a creator can understand a listing as a product concept and refine its core creative context (title, idea, phrase, graphic direction, notes, tags, and related assets) without leaving the workspace or turning Phase 1 into a marketplace listing editor.

## Requirements

### Requirement: Listing inspector presents the listing as a product concept
FusionCanvas SHALL display a Listing Inspector in the document window detail area for the active listing document context, showing the working title, lifecycle status, current workflow stage, topic path, idea, phrase, graphic direction, notes, linked tags, and related assets so the user can understand what the listing is and what needs to happen next.

#### Scenario: User opens a listing
- **WHEN** a listing becomes the active document context
- **THEN** the detail area displays the inspector with the listing's working title, status, workflow stage, topic path, idea, phrase, graphic direction, notes, tags, and related assets
- **AND** the content reflects the persisted listing record

#### Scenario: Listing has no creative details yet
- **WHEN** the active listing has no idea, phrase, graphic direction, notes, tags, or assets
- **THEN** the inspector shows quiet empty placeholders for those sections
- **AND** does not fabricate content or block viewing

#### Scenario: User browses between listings
- **WHEN** the user selects a different listing and the current inspector has no unsaved changes
- **THEN** the inspector updates to the newly active listing without opening additional surfaces
- **AND** the user keeps orientation through the unchanged tab and navigator context

### Requirement: Listing inspector edits core creative fields with explicit save
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

### Requirement: Listing inspector aligns status and workflow stage with document context
FusionCanvas SHALL display the listing's current lifecycle status and current workflow stage in the inspector, SHALL keep them synchronized with the workflow stage navigator and the document-context status control, and SHALL NOT introduce a second lifecycle-status selector in the same document context.

#### Scenario: Status changes elsewhere in the document context
- **WHEN** the listing's lifecycle status changes through the document-context status control while the inspector is visible
- **THEN** the inspector reflects the new status without requiring a reload
- **AND** no competing status selector appears inside the inspector

#### Scenario: Workflow stage changes
- **WHEN** the active listing's workflow stage changes
- **THEN** the inspector displays the new current stage
- **AND** updates stage-relevant emphasis to match

### Requirement: Listing inspector presents stage-relevant creative fields
The inspector SHALL keep every creative section accessible at every workflow stage and SHALL emphasize the section relevant to the listing's current stage so the user understands what matters now while retaining access to completed-stage context.

#### Scenario: Listing is at the Concept stage
- **WHEN** the active listing's current stage is `Concept`
- **THEN** the phrase and graphic direction section is visually emphasized
- **AND** the idea, notes, tags, and assets sections remain visible and editable

#### Scenario: Emphasis follows stage changes
- **WHEN** the active listing's current stage changes from `Idea` to `Design`
- **THEN** the inspector moves emphasis to the assets section
- **AND** no section becomes unavailable because of the stage change

### Requirement: Listing inspector manages listing tags
FusionCanvas SHALL allow the user to view the listing's linked tags, link an existing reusable store tag, create a new reusable store tag when the entered name has no case-insensitive match in the store, and remove tag links, SHALL normalize tag names to nonblank single-line values, and SHALL NOT delete reusable tag records when a link is removed.

#### Scenario: User links an existing tag
- **WHEN** the user adds a tag whose name matches an existing store tag
- **THEN** FusionCanvas links that reusable tag to the listing on save
- **AND** does not create a duplicate tag record

#### Scenario: User creates a new tag inline
- **WHEN** the user adds a tag name with no case-insensitive match in the listing's store
- **THEN** FusionCanvas creates one normalized reusable store tag and links it to the listing on save

#### Scenario: User removes a tag link
- **WHEN** the user removes a tag from the listing and saves
- **THEN** FusionCanvas removes only the listing-tag link
- **AND** preserves the reusable tag record and its other links

#### Scenario: User submits an invalid tag name
- **WHEN** the user attempts to add an empty or whitespace-only tag name
- **THEN** FusionCanvas rejects the addition
- **AND** leaves the listing's current tag links unchanged

### Requirement: Listing inspector shows related assets
The inspector SHALL list the assets linked to the listing with name, kind, and missing state, SHALL make it clear when the listing has no supporting assets, and SHALL NOT provide asset attach, detach, reorder, or preview actions.

#### Scenario: Listing has related assets
- **WHEN** the active listing has linked assets
- **THEN** the inspector lists each asset with its name and kind
- **AND** marks assets whose workspace file is missing

#### Scenario: Listing has no related assets
- **WHEN** the active listing has no linked assets
- **THEN** the inspector shows an explicit empty state for the assets section

### Requirement: Listing inspector guards unsaved changes
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

### Requirement: Listing inspector keeps inactive listings read-only
FusionCanvas SHALL present an archived or otherwise effectively inactive listing in a read-only inspector with a clear inactive notice and guidance to restore, and SHALL disable editing controls rather than hiding the listing's context.

#### Scenario: User opens an archived listing
- **WHEN** an archived listing becomes the active document context
- **THEN** the inspector displays the listing's details read-only
- **AND** shows an inactive notice with guidance to restore through the lifecycle surface
- **AND** disables save and editing controls

#### Scenario: Listing is inactive through archived ancestry
- **WHEN** the active listing is effectively inactive because its store, niche, group, or a group ancestor is archived
- **THEN** the inspector applies the same read-only presentation and guidance

### Requirement: Listing inspector persists saves atomically and recovers from failure
FusionCanvas SHALL persist each inspector save, including any created tags and tag-link changes, as one atomic workspace snapshot operation, and SHALL keep the user's draft recoverable when validation or persistence fails.

#### Scenario: Workspace reloads after an inspector save
- **WHEN** a successful inspector save is followed by an application or workspace database reload
- **THEN** the listing's title, creative fields, notes, tags, and tag links match the last saved state

#### Scenario: Persistence fails during save
- **WHEN** the repository cannot complete an inspector save
- **THEN** FusionCanvas reports a recoverable error
- **AND** preserves the user's editable draft
- **AND** leaves no partially applied listing, tag, or tag-link changes

### Requirement: Listing inspector follows shared desktop control guidance
FusionCanvas SHALL use compact action sizing, predictable keyboard flow through the inspector fields and actions, and tooltips for any icon-only commands in the inspector surface.

#### Scenario: Inspector actions are presented
- **WHEN** save, discard-guard, or tag-management actions are shown
- **THEN** their buttons are sized to their command groups rather than evenly stretched across the surface
- **AND** saving and cancellation are reachable without pointer-only interaction
