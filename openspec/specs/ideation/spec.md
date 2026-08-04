# Ideation

## Purpose

Ideation generates, compares, accepts, and rejects multiple contextual Idea-stage directions without leaving the workspace.

## Requirements

### Requirement: Ideation opens as a focused Idea-stage dialog
FusionCanvas SHALL expose Ideation as an auxiliary action for an active Idea-stage view whose context resolves to an active niche, and SHALL open the action in one owned modal dialog without replacing the active document or manual Idea editor.

#### Scenario: User opens Ideation from a selected group
- **WHEN** the Idea stage is active for a selected group and placeholder AI access is available
- **THEN** activating `Ideation…` opens one modal Ideation dialog owned by the main window
- **AND** the current tab, navigation selection, and manual Idea content remain unchanged

#### Scenario: User opens Ideation from an Item
- **WHEN** the Idea view is active for an Item whose parent topic is active
- **AND** placeholder AI access is available
- **THEN** the dialog uses the Item's parent group or niche as its creation scope
- **AND** generation does not edit the selected Item

#### Scenario: Context has no active niche
- **WHEN** the current Idea-stage context cannot resolve an active niche
- **THEN** FusionCanvas does not allow the Ideation dialog to open
- **AND** it communicates that an active niche context is required

### Requirement: Ideation availability uses placeholder API access
FusionCanvas SHALL treat a non-empty `FUSIONCANVAS_AI_API_KEY` environment value as placeholder AI access for this module, SHALL never persist or transmit that value, and SHALL keep the Ideation action visible but disabled when the value is absent.

#### Scenario: Placeholder API access is present
- **WHEN** `FUSIONCANVAS_AI_API_KEY` contains a non-whitespace value
- **THEN** the Ideation action is enabled for a supported Idea-stage context
- **AND** the fake generator can be invoked

#### Scenario: Placeholder API access is absent
- **WHEN** `FUSIONCANVAS_AI_API_KEY` is missing, empty, or whitespace
- **THEN** the Ideation action remains visible but disabled
- **AND** its unavailable guidance explains that placeholder AI access must be configured

#### Scenario: Generator request is assembled
- **WHEN** FusionCanvas prepares a request for the fake generator
- **THEN** the placeholder API-key value is absent from the request payload, logs, errors, and durable workspace data

### Requirement: The dialog captures mode, guidance, count, and visible scope
The Ideation dialog SHALL display the resolved store, niche, and optional group scope, SHALL provide one optional multi-line guidance field, SHALL provide an extensible mode selector initially containing `Basic` and `Snowclones`, SHALL constrain desired candidate count to 1 through 20 with a default of 5, and SHALL expose a count-up arrow button and a count-down arrow button beside the count text field that adjust the desired count by one within the 1 through 20 range. The count text field SHALL continue to accept free-text entry, the count-up and count-down arrows SHALL be the only controls that adjust the count by clicking, and the arrows SHALL NOT submit generation or otherwise alter dialog state beyond the count text.

#### Scenario: Dialog opens for a group
- **WHEN** Ideation opens from group `Pugs` in niche `Dogs`
- **THEN** the dialog visibly identifies the store, `Dogs`, and `Pugs` scope
- **AND** `Basic`, an empty guidance field, and a count of 5 are initially selected
- **AND** a count-up arrow and a count-down arrow are visible immediately beside the count text field

#### Scenario: Dialog opens at niche root
- **WHEN** Ideation opens without a selected group
- **THEN** the dialog visibly identifies the active store and niche
- **AND** it communicates that created candidates will be placed at the niche root

#### Scenario: User enters an invalid count
- **WHEN** the desired count is outside 1 through 20 or is not a complete valid number
- **THEN** Generate is unavailable
- **AND** the dialog communicates the allowed range without losing the user's guidance
- **AND** both the count-up and count-down arrows remain enabled so the user can recover by clicking

#### Scenario: User clicks the count-up arrow from a valid in-range count
- **WHEN** the count text is a whole number `n` where `1 <= n < 20`
- **AND** the user clicks the count-up arrow
- **THEN** the count text becomes the whole number `n + 1`
- **AND** no range error is shown

#### Scenario: User clicks the count-down arrow from a valid in-range count
- **WHEN** the count text is a whole number `n` where `1 < n <= 20`
- **AND** the user clicks the count-down arrow
- **THEN** the count text becomes the whole number `n - 1`
- **AND** no range error is shown

#### Scenario: Count-up arrow is disabled at the maximum
- **WHEN** the count text is a whole number equal to 20
- **THEN** the count-up arrow is disabled
- **AND** invoking it by keyboard does not change the count text

#### Scenario: Count-down arrow is disabled at the minimum
- **WHEN** the count text is a whole number equal to 1
- **THEN** the count-down arrow is disabled
- **AND** invoking it by keyboard does not change the count text

#### Scenario: Count-up arrow recovers from invalid text
- **WHEN** the count text is empty or is not a complete valid whole number
- **AND** the user clicks the count-up arrow
- **THEN** the count text becomes the default count of 5
- **AND** no range error is shown

#### Scenario: Count-down arrow recovers from invalid text
- **WHEN** the count text is empty or is not a complete valid whole number
- **AND** the user clicks the count-down arrow
- **THEN** the count text becomes the minimum count of 1
- **AND** no range error is shown

#### Scenario: Count arrow clamps an out-of-range parseable number before stepping
- **WHEN** the count text parses as a whole number outside 1 through 20
- **AND** the user clicks either count arrow
- **THEN** the count text first reflects the clamped value (`1` for below-range, `20` for above-range)
- **AND** the click then steps from that clamped value so a second consecutive click moves one step further into the range

#### Scenario: Count arrows are disabled while a batch is running
- **WHEN** a generation batch has started and not all requested operations have completed
- **THEN** both count arrows are disabled alongside the count text field, mode selector, guidance field, and Generate button
- **AND** existing candidates remain visible

### Requirement: Basic mode generates concise contextual candidates
Basic mode SHALL use the configured provider-independent AI text service to asynchronously request the desired number of varied, concise Idea candidates from the resolved creative context and optional guidance. Each request SHALL include the bundled canonical Design Triangle framework as system prompt context and SHALL instruct the model to produce one Idea-stage direction grounded in a wearer signal, intended viewer inference or effect, and audience-recognizable shared context, without producing a full refined Concept, finished design specification, or SLL artifact.

#### Scenario: User requests grumpy pug ideas
- **WHEN** the active niche is `Dogs`, the selected group is `Pugs`, the guidance is `Grumpy`, and the user generates in Basic mode
- **THEN** each request includes the canonical Design Triangle framework and asks for one short Idea direction that incorporates the pug and grumpy context
- **AND** the requested direction has a meaningful wearer-facing social proposition rather than generic decorative or topic-only copy
- **AND** it asks for neither a full refined Concept, a finished design specification, nor an SLL artifact

#### Scenario: Guidance is empty
- **WHEN** the user generates in Basic mode without guidance
- **THEN** generation still uses the canonical framework and the resolved store, niche, optional group, active Idea, and rejected-Idea context

### Requirement: Snowclones mode fills an in-memory template
Snowclones mode SHALL choose a template for each requested candidate from the application-wide persisted Snowclone Library, SHALL fill its variable positions using the resolved creative context, SHALL include the bundled canonical Design Triangle framework as system prompt context, and SHALL avoid repeating a template within one batch while unused catalog entries remain. It SHALL preserve the Snowclone contract by requesting only one completed phrase with no explanation unless essential, while asking for a result that expresses an audience-relevant identity, experience, attitude, or tension instead of generic humor.

#### Scenario: Snowclone candidate is generated
- **WHEN** the selected template is `Talk to me about {X}` and the active context concerns grumpy pugs
- **THEN** the AI request includes the canonical Design Triangle framework and asks for one relevant completed phrase such as `Talk to me about grumpy pugs`
- **AND** the result contains no unresolved placeholder and no explanation

#### Scenario: Batch fits within the catalog
- **WHEN** the requested count does not exceed the number of available Snowclone templates
- **THEN** each candidate in that batch uses a different template

#### Scenario: Batch exceeds the catalog
- **WHEN** the requested count exceeds the number of available Snowclone templates
- **THEN** templates may repeat only after every catalog entry has been used
- **AND** the generator still attempts to return distinct completed phrases

### Requirement: Generation exposes bounded asynchronous progress
FusionCanvas SHALL generate candidates asynchronously with at most four concurrent fake-generation operations, SHALL prevent a duplicate Generate submission while a batch is running, and SHALL expose a spinner and completed-versus-requested progress without blocking the UI thread.

#### Scenario: Batch is running
- **WHEN** generation has started and not all requested operations have completed
- **THEN** a visible spinner and progress message are shown
- **AND** Generate, mode, guidance, and count controls cannot start or alter the running batch
- **AND** existing candidates remain visible

#### Scenario: Some operations fail
- **WHEN** at least one parallel generation succeeds and at least one fails
- **THEN** successful unique candidates remain available
- **AND** the dialog reports the partial failure without fabricating missing results

#### Scenario: All operations fail
- **WHEN** every operation in a generation batch fails
- **THEN** no candidate is added
- **AND** the dialog reports a recoverable error and re-enables generation controls

#### Scenario: Generator returns duplicates
- **WHEN** generated texts differ only by surrounding whitespace, repeated whitespace, or letter case
- **THEN** the candidate list retains only one normalized equivalent
- **AND** progress still completes without an unbounded retry loop

### Requirement: Generated candidates remain transient until decided
FusionCanvas SHALL display each successful candidate in an Ideas candidate list with Create and Reject actions, and SHALL keep undecided candidates only in the current dialog session.

#### Scenario: Candidate is generated
- **WHEN** a unique generation operation succeeds
- **THEN** its concise Idea text is appended to the Ideas candidate list
- **AND** it has one Create action and one Reject action

#### Scenario: Dialog session ends
- **WHEN** the dialog closes after any required discard confirmation
- **THEN** every undecided candidate is discarded
- **AND** no undecided candidate is written to workspace persistence

### Requirement: Creating a candidate produces an Idea-stage Item
Creating a candidate SHALL use the Item-management application boundary to create one normal Draft Item at the Idea stage, SHALL store the full candidate as the original Idea, SHALL use its first non-empty sentence as the working title, and SHALL apply the current context's creation defaults.

#### Scenario: Candidate is created in a selected group
- **WHEN** the dialog scope is group `Pugs` and the user creates a candidate
- **THEN** a Draft Idea-stage Item is created directly in `Pugs`
- **AND** its original Idea contains the full candidate text
- **AND** applicable inherited metadata and tags remain available through the existing creation behavior

#### Scenario: Candidate is created without a selected group
- **WHEN** the dialog scope is an active niche with no group and the user creates a candidate
- **THEN** a Draft Idea-stage Item is created at that niche root

#### Scenario: Candidate creation succeeds
- **WHEN** the Item is durably created
- **THEN** that candidate disappears from the list
- **AND** the navigation tree and other open representations refresh from authoritative workspace state

#### Scenario: Candidate creation fails
- **WHEN** Item creation fails validation or persistence
- **THEN** no partial Item is created
- **AND** the candidate remains in the list with a recoverable error

### Requirement: Rejecting a candidate records optional reasoning
Rejecting a candidate SHALL open a focused confirmation dialog with one optional reason field and explicit OK and Cancel actions, and SHALL durably record the rejection before removing the candidate.

#### Scenario: User confirms rejection with a reason
- **WHEN** the user enters a reason and confirms rejection
- **THEN** FusionCanvas stores the candidate text, reason, mode, store, niche, optional group, and creation timestamp
- **AND** the candidate disappears only after the rejection is durably saved

#### Scenario: User confirms rejection without a reason
- **WHEN** the reason is empty and the user confirms rejection
- **THEN** FusionCanvas stores the rejection with no reason
- **AND** removes the candidate after persistence succeeds

#### Scenario: User cancels rejection
- **WHEN** the user cancels the rejection dialog
- **THEN** no rejection is stored
- **AND** the candidate remains in the list

#### Scenario: Rejection persistence fails
- **WHEN** the user confirms rejection but persistence fails
- **THEN** no partial rejection is committed
- **AND** the candidate remains in the list with a recoverable error

### Requirement: Discarding candidates or running work requires confirmation
FusionCanvas SHALL ask for confirmation before Clear All discards one or more candidates, before Close discards candidates, or before Close cancels an active batch, and SHALL preserve dialog state when the user declines.

#### Scenario: User confirms Clear All
- **WHEN** candidates exist and the user confirms Clear All
- **THEN** all transient candidates are removed
- **AND** created Items and durable rejections remain unchanged

#### Scenario: User cancels Clear All
- **WHEN** the user declines the Clear All confirmation
- **THEN** the candidate list, selection, and dialog input remain unchanged

#### Scenario: User closes with candidates
- **WHEN** candidates exist and the user confirms Close
- **THEN** the candidates are discarded and the dialog closes
- **AND** keyboard focus returns to the Ideation launch action when it remains available

#### Scenario: User closes during generation
- **WHEN** a batch is running and the user confirms Close
- **THEN** FusionCanvas cancels outstanding fake-generation operations
- **AND** ignores late results
- **AND** discards transient candidates and closes the dialog

#### Scenario: User declines Close
- **WHEN** the user declines a Close confirmation
- **THEN** the dialog remains open with its input, candidates, progress, selection, and focus preserved

### Requirement: Ideation remains accessible and theme coherent
FusionCanvas SHALL make essential Ideation controls keyboard reachable in a logical order, SHALL provide meaningful accessible names and status announcements, and SHALL resolve dialog, progress, candidate, confirmation, disabled, error, and focus states from shared application themes.

#### Scenario: User operates Ideation with a keyboard
- **WHEN** keyboard focus enters the dialog
- **THEN** mode, guidance, count text, count-up arrow, count-down arrow, Generate, candidate Create and Reject actions, Clear All, and Close are reachable in a predictable order
- **AND** focus enters the guidance field initially
- **AND** the count-up arrow is reached before the count-down arrow, both after the count text field and before the Generate button

#### Scenario: Count arrows expose accessible names
- **WHEN** an assistive technology inspects the count arrows
- **THEN** the count-up arrow reports an accessible name of `Increment idea count`
- **AND** the count-down arrow reports an accessible name of `Decrement idea count`

#### Scenario: Candidate action completes
- **WHEN** Create or Reject removes a candidate
- **THEN** focus moves to the next candidate action when one exists
- **AND** otherwise moves to a stable dialog action

#### Scenario: Application theme changes
- **WHEN** the application appearance changes while Ideation or a confirmation dialog is open
- **THEN** every open Ideation surface adopts the active theme
- **AND** busy, disabled, warning, destructive, selected, and error states remain distinguishable
