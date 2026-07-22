## ADDED Requirements

### Requirement: Items expose one basic tool for each workflow stage
FusionCanvas SHALL provide built-in Idea, Concept, Design, and Listing Stage Tools for an active Item and SHALL show exactly the tool for the active view stage inside the Stage Tool Host.

#### Scenario: Item opens at its current stage
- **WHEN** an Item becomes the active document
- **THEN** its current stage is also the active view stage
- **AND** the matching built-in Stage Tool is shown
- **AND** the other three primary stage tools are not shown simultaneously

#### Scenario: Earlier stage is reviewed
- **WHEN** the Item is currently at Design and the user selects Idea or Concept in the navigator
- **THEN** the matching earlier Stage Tool is shown read-only
- **AND** Design remains the persisted current stage

### Requirement: Current-stage content is the only editable stage content
FusionCanvas SHALL allow stage-specific content mutations only through the Item's persisted current stage and SHALL require explicit regression before an earlier stage can be edited.

#### Scenario: User attempts to edit reviewed upstream content
- **WHEN** Concept is an earlier active view while Design is current
- **THEN** Concept fields are read-only
- **AND** FusionCanvas does not commit a Concept mutation

#### Scenario: User regresses intentionally
- **WHEN** the user regresses from Design to Concept
- **THEN** Concept becomes current and editable
- **AND** existing Design files and all other downstream data remain preserved

### Requirement: Stage movement has no content completion gates
FusionCanvas SHALL allow adjacent forward and backward stage movement without requiring Idea text, Concept values, or Design files.

#### Scenario: Empty Item advances through all stages
- **WHEN** an editable Draft Item has no stage content and the user advances repeatedly
- **THEN** each adjacent move succeeds through Listing
- **AND** no fabricated stage content is created

#### Scenario: Stage move succeeds
- **WHEN** an allowed adjacent stage move is persisted
- **THEN** the destination becomes current and active
- **AND** every upstream and downstream value remains intact
- **AND** the stage survives workspace reload

### Requirement: Idea stores the original optional idea
The Idea Stage Tool SHALL expose one optional multi-line original Idea value, SHALL hide Audience, and SHALL preserve any existing Audience metadata without editing or deleting it.

#### Scenario: User saves an Idea
- **WHEN** Idea is current and the user explicitly saves multi-line Idea text
- **THEN** the text reloads as the original Idea
- **AND** Concept idea remains unchanged

#### Scenario: Existing Item contains Audience metadata
- **WHEN** the Idea tool loads an Item with stored Audience metadata
- **THEN** no Audience field is shown
- **AND** a later save preserves the stored Audience value

### Requirement: Concept stores three independent optional values
The Concept Stage Tool SHALL expose optional Concept idea, Phrase, and Graphics description values, SHALL store Concept idea separately from original Idea, and SHALL normalize Phrase to one line.

#### Scenario: User saves Concept values
- **WHEN** Concept is current and the user saves all three values
- **THEN** Concept idea and Graphics description preserve their multi-line text
- **AND** Phrase is stored as one normalized line
- **AND** original Idea is unchanged

#### Scenario: Concept remains empty
- **WHEN** Concept is current and all three values are empty
- **THEN** the Item can still advance to Design

### Requirement: Shared Item chrome surrounds the active Stage Tool
FusionCanvas SHALL present shared Overview, Notes, Tags, Related assets, and lifecycle areas around the Stage Tool Host in one vertically scrollable Item surface.

#### Scenario: Item surface is populated
- **WHEN** an Item document is active
- **THEN** Overview shows optional working title, topic path, current stage, and one status selector
- **AND** Notes, Tags, generic Related assets, and applicable lifecycle actions remain outside the active Stage Tool
- **AND** Design files are excluded from generic Related assets

#### Scenario: Window height is reduced
- **WHEN** content exceeds the minimum supported window height
- **THEN** the user can scroll to every shared section and applicable action by keyboard or pointer

### Requirement: Item text uses an explicit guarded save
FusionCanvas SHALL persist working title, current-stage text, and Notes through explicit Save and SHALL keep Tag changes independently immediate.

#### Scenario: User leaves a meaningful text draft
- **WHEN** the user changes Item, tab, active view stage, or lifecycle state with unsaved working title, current-stage text, or Notes
- **THEN** FusionCanvas offers Save, Discard, and Cancel
- **AND** Cancel retains the draft, context, selection, and focus

#### Scenario: User changes a Tag while text is dirty
- **WHEN** the user applies or removes a Tag while an Item text draft is unsaved
- **THEN** the Tag change persists immediately and independently
- **AND** the text draft remains pending

#### Scenario: Save fails
- **WHEN** explicit Save fails validation or persistence
- **THEN** no partial Item text change is persisted
- **AND** the recoverable draft remains available

### Requirement: The basic workflow remains durable and synchronized
FusionCanvas SHALL preserve confirmed Item workflow data across restart, archive and restore, and SHALL propagate every successful Item mutation to all open representations from authoritative persisted state.

#### Scenario: Completed workflow survives restart
- **WHEN** an Item has confirmed data across Idea, Concept, Design, and Listing and the workspace is closed and reopened
- **THEN** the same Item ID, current stage, lifecycle status, archive state, text, Notes, Tags, Design files, related assets, and topic placement are reconstructed
- **AND** the document opens at the persisted current stage with the correct editability

#### Scenario: Item is archived and restored
- **WHEN** an Item with confirmed workflow data is archived and later restored to a valid active topic
- **THEN** all stage, status, metadata, file, Tag, relationship, and topic-placement data remains unchanged
- **AND** the restored Item follows the edit policy for its persisted stage and status

#### Scenario: Successful mutation synchronizes open contexts
- **WHEN** a stage, status, metadata, archive, Tag, or Design-file mutation succeeds for an Item represented in multiple open contexts
- **THEN** every representation and active filter refreshes from the authoritative confirmed state
- **AND** no stale context can silently overwrite that newer state

### Requirement: Workflow controls remain accessible and resist duplicate submission
FusionCanvas SHALL keep essential workflow actions keyboard reachable with meaningful accessible names and SHALL prevent duplicate or conflicting submission while an Item operation is running without discarding the user's current input or selection.

#### Scenario: User operates the workflow by keyboard
- **WHEN** the user navigates the Item surface without a pointer
- **THEN** essential stage, status, Save, import, preview, Export copy, remove, archive, restore, and confirmation actions are reachable in a logical order
- **AND** icon-only actions expose descriptive accessible names or tooltips

#### Scenario: User repeats an action while it is running
- **WHEN** an Item save, transition, or Design-file operation is already running and the user invokes the action again
- **THEN** FusionCanvas performs at most one conflicting mutation
- **AND** preserves the current draft, selection, and recoverable interaction state
