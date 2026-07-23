## RENAMED Requirements

- FROM: `### Requirement: Listing inspector edits core creative fields with explicit save`
  TO: `### Requirement: Listing inspector edits core creative fields with automatic save`
- FROM: `### Requirement: Listing inspector guards unsaved changes`
  TO: `### Requirement: Listing inspector commits pending edits when the context changes`

## MODIFIED Requirements

### Requirement: Listing inspector edits core creative fields with automatic save
FusionCanvas SHALL allow the user to edit the Item's optional working title, current-stage text (Idea, or Concept idea, Phrase, and Graphics description, as owned by the Item's current stage), and Notes inline in the Item document surface and SHALL persist those edits automatically when the edited field loses focus, validating the title, normalizing values, preserving Item identity and placement, hidden fields, unknown metadata keys, and inherited provenance, without an explicit save action, Save or Discard buttons, or an unsaved-changes prompt. Tags remain independently immediate and are not part of the text draft.

#### Scenario: Field exit persists valid edits
- **WHEN** the user changes the working title, current-stage text, or Notes and the edited field loses focus
- **THEN** FusionCanvas persists the normalized values and updated timestamp
- **AND** preserves the Item's identity, topic placement, archive state, status, stage, assets, prompts, hidden Audience and generic Description, unknown metadata, and inherited provenance
- **AND** refreshes the inspector draft baseline to the saved state

#### Scenario: Field exit with no changes performs no write
- **WHEN** a text field loses focus and its content matches the last saved baseline
- **THEN** FusionCanvas performs no save and reports no error

#### Scenario: Invalid title reverts while other valid edits persist
- **WHEN** a field-exit commit carries a working title containing line breaks
- **THEN** the working title reverts to its last saved value with an inline explanation
- **AND** the other valid edited fields still persist
- **AND** the user is not prompted and no transition is blocked

#### Scenario: Phrase is normalized to one line
- **WHEN** a field-exit commit carries a Phrase containing line breaks
- **THEN** FusionCanvas normalizes the Phrase to a single line before persisting

#### Scenario: Save preserves unrelated metadata and provenance
- **WHEN** the Item's metadata contains keys or inherited-provenance entries the inspector does not edit
- **THEN** a successful automatic save preserves those entries unchanged

#### Scenario: Commits are serialized against newer state
- **WHEN** a field-exit commit runs while the Item's persisted state has advanced beyond the inspector's baseline
- **THEN** the commit applies through the stage-aware expected-state guard so a stale inspector never silently overwrites newer Item content
- **AND** a rejected commit reports inline and keeps the user's draft

#### Scenario: Tag changes while a text commit is pending
- **WHEN** the user applies or removes a Tag while a text commit is in flight or text edits are pending
- **THEN** the Tag mutation persists independently and immediately
- **AND** the pending text commit is not discarded or blocked

### Requirement: Listing inspector commits pending edits when the context changes
FusionCanvas SHALL commit pending working title, current-stage text, and Notes edits following the automatic-save rules before an Item selection change, document-tab switch or close, active-view-stage change, or lifecycle transition completes, SHALL NOT ask whether to save, discard, or cancel, and SHALL NOT block the transition while a commit can proceed. When the commit cannot proceed, FusionCanvas keeps the user in the current context with the draft preserved and an inline error.

#### Scenario: User switches context with pending edits
- **WHEN** the Item surface has pending working title, current-stage text, or Notes edits and the user changes Item, tab, active view stage, or lifecycle state
- **THEN** FusionCanvas commits the pending edits following the automatic-save rules before the transition completes
- **AND** does not show a save, discard, or cancel prompt

#### Scenario: Failed commit keeps context and draft
- **WHEN** a commit triggered by a context change fails validation or persistence
- **THEN** the transition does not complete
- **AND** the user remains in the current context with the draft preserved and an inline error

#### Scenario: Context change with a clean draft
- **WHEN** the user changes Item, tab, active view stage, or lifecycle state and no text edits are pending
- **THEN** the transition proceeds immediately without a write

### Requirement: Listing inspector keeps inactive listings read-only
FusionCanvas SHALL present an archived or otherwise effectively inactive listing in a read-only inspector with a clear inactive notice and guidance to restore, and SHALL disable editing controls rather than hiding the listing's context.

#### Scenario: User opens an archived listing
- **WHEN** an archived listing becomes the active document context
- **THEN** the inspector displays the listing's details read-only
- **AND** shows an inactive notice with guidance to restore through the lifecycle surface
- **AND** disables editing controls

#### Scenario: Listing is inactive through archived ancestry
- **WHEN** the active listing is effectively inactive because its store, niche, group, or a group ancestor is archived
- **THEN** the inspector applies the same read-only presentation and guidance

### Requirement: Listing inspector follows shared desktop control guidance
FusionCanvas SHALL use compact action sizing, predictable keyboard flow through the inspector fields and actions, and tooltips for any icon-only commands in the inspector surface.

#### Scenario: Inspector actions are presented
- **WHEN** tag-management or lifecycle actions are shown
- **THEN** their buttons are sized to their command groups rather than evenly stretched across the surface
- **AND** field editing and lifecycle confirmations are reachable without pointer-only interaction
