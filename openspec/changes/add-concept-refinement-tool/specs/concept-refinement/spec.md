## ADDED Requirements

### Requirement: Concept refinement lives in the Concept stage surface
FusionCanvas SHALL present a Concept refinement section inside the existing Concept stage surface of the Item document, directly below the Concept idea, Phrase, and Graphics description fields. The section SHALL follow the visibility of the Concept stage surface and SHALL be disabled whenever the Concept fields are read-only.

#### Scenario: Section appears with the Concept stage surface
- **WHEN** an Item document shows the Concept stage surface
- **THEN** the refinement section appears below the three creative fields
- **AND** it is not shown for Idea, Design, or Listing stage surfaces

#### Scenario: Earlier-stage review disables refinement
- **WHEN** the Item's persisted current stage is beyond Concept and the user reviews the Concept stage read-only
- **THEN** every refinement action is disabled
- **AND** no AI request can be started from the read-only surface

### Requirement: Refinement actions are gated on Concept AI availability
FusionCanvas SHALL derive refinement availability from the Concept-purpose AI availability reported by the application AI text-generation boundary, SHALL keep the Initialize, Fine tune, and Change actions visible but disabled with actionable guidance whenever availability is missing, and SHALL re-evaluate availability after AI settings change while the document is open. The completeness score SHALL remain live regardless of AI availability.

#### Scenario: Concept AI is ready
- **WHEN** the Concept-purpose AI availability is ready
- **THEN** Initialize, Fine tune, and Change actions are enabled subject to their individual preconditions

#### Scenario: Concept AI is not configured
- **WHEN** the Concept-purpose AI availability reports a missing credential, missing model, or invalid configuration
- **THEN** the refinement actions remain visible but disabled
- **AND** the guidance identifies the missing prerequisite and directs the user to AI settings

#### Scenario: Availability refreshes after settings change
- **WHEN** the user saves AI settings while an Item document with the Concept surface is open
- **THEN** refinement availability is re-evaluated without reopening the document

### Requirement: Initialization from the base idea is manual and derives the full triangle
FusionCanvas SHALL provide one `Initialize from base idea` action that asks AI to derive Concept idea, Phrase, and Graphic direction from the Item's original Idea text and the resolved creative context, using the Concept AI purpose. Initialization SHALL be enabled only when the original Idea has non-whitespace content and all three Concept fields are empty, and SHALL NOT be triggered implicitly by entering or showing the Concept stage.

#### Scenario: Initialize derives the triangle
- **WHEN** the Item has original Idea text, all three Concept fields are empty, and the user activates Initialize
- **THEN** AI-derived Concept idea, Phrase, and Graphic direction values are applied to the three drafts on success

#### Scenario: No base idea
- **WHEN** the Item has no non-whitespace original Idea text
- **THEN** Initialize is disabled
- **AND** guidance explains that a base idea is required

#### Scenario: Fields already contain values
- **WHEN** any of the three Concept fields is non-empty
- **THEN** Initialize is disabled
- **AND** Fine tune and Change remain the available refinement actions

#### Scenario: Entering the Concept stage performs no AI call
- **WHEN** an Item document opens or switches to the Concept stage surface
- **THEN** no refinement AI request is started implicitly

### Requirement: Fine tune and Change refine one corner in context of the others
FusionCanvas SHALL provide a `Fine tune` action and a `Change` action for each of the three design-triangle corners. Fine tune SHALL ask AI to improve the corner's current value while preserving its direction; Change SHALL ask AI to propose a materially different direction for the corner. Both SHALL evaluate the corner in context of the other two corners' current values and the creative context, SHALL apply the result only to that corner's draft, and SHALL normalize a Phrase result to one line. Fine tune SHALL be disabled for an empty corner; Change SHALL remain available for an empty corner.

#### Scenario: Fine tune improves one corner
- **WHEN** the Concept idea has a value and the user activates Fine tune for it
- **THEN** the AI request includes the current value and the other two corners as context
- **AND** on success only the Concept idea draft is replaced by the improved value

#### Scenario: Change replaces one corner's direction
- **WHEN** the user activates Change for the Graphics description
- **THEN** on success only the Graphics description draft is replaced by a materially different direction that remains coherent with the other two corners

#### Scenario: Phrase result is normalized to one line
- **WHEN** a Fine tune or Change result for the Phrase contains line breaks
- **THEN** the applied Phrase value is normalized to a single line before it reaches the draft

#### Scenario: Fine tune an empty corner
- **WHEN** a corner has no non-whitespace value
- **THEN** its Fine tune action is disabled
- **AND** its Change action remains available

### Requirement: Applied refinement values commit through the automatic-save draft path
FusionCanvas SHALL apply AI-derived values and rollback-restored values to the same drafts as manual edits and SHALL persist them through the Item inspector's automatic-save rules with the stage-aware expected-state guard, without an explicit save action. A failed commit SHALL keep the user's draft and report a recoverable inline error.

#### Scenario: Applied values persist automatically
- **WHEN** a refinement action applies new values to the drafts
- **THEN** the values commit through the same automatic-save path as a field-exit edit
- **AND** the persisted Item reloads with the applied values

#### Scenario: Commit fails after application
- **WHEN** the automatic commit of applied values fails validation or persistence
- **THEN** no partial Item change is persisted
- **AND** the draft remains with a recoverable inline error
- **AND** the history entry appended for the applied value is retained because it reflects the real draft state

### Requirement: Session history records refinement actions and manual commits
FusionCanvas SHALL maintain a per-Item-document session history of the design triangle, appending one labeled entry for each applied Initialize, Fine tune, or Change action and one entry for each committed manual field edit, in chronological order with the current state last. An AI-triggered commit SHALL NOT add a separate manual-edit entry. The history SHALL be discarded when the Item document session ends and SHALL NOT be persisted to workspace storage.

#### Scenario: AI action appends one entry
- **WHEN** a Fine tune action applies successfully
- **THEN** the history gains exactly one entry labeled with the action and corner, such as `Fine-tuned Phrase`
- **AND** the entry captures the resulting three triangle values

#### Scenario: Manual commit appends an entry
- **WHEN** a manual edit to a Concept field commits through the automatic-save path
- **THEN** the history gains one entry labeled with the edited field

#### Scenario: History is session-scoped
- **WHEN** the Item document is closed and the Item is reopened
- **THEN** the refinement history starts empty

### Requirement: History rollback restores an earlier triangle state
FusionCanvas SHALL allow the user to select an earlier history entry to restore its three triangle values into the drafts through the automatic-save path. Rollback SHALL NOT append a history entry. When the user applies a new refinement action or commits a new manual edit after rolling back, entries after the restored point SHALL be discarded.

#### Scenario: Roll back to an earlier state
- **WHEN** the user selects an earlier history entry
- **THEN** the three drafts take that entry's values and commit
- **AND** the selected entry becomes the current state without adding an entry

#### Scenario: New action after rollback discards later entries
- **WHEN** the user rolls back to an earlier entry and then applies a Fine tune action
- **THEN** entries that were recorded after the restored entry are discarded
- **AND** the new action's entry is appended after the restored entry

### Requirement: A live deterministic score shows triangle completeness
FusionCanvas SHALL compute a design-triangle completeness score deterministically in the Domain layer from the three current triangle values and SHALL display it in the refinement section, updating on every draft change whether caused by manual edit, AI application, or rollback. The score SHALL be a percentage from 0 to 100 that reflects presence and substantive content of the corners, SHALL be 0 when all corners are empty, SHALL be 100 when all three corners have substantive content, and SHALL be presented as completeness (remaining optimization potential), not as a judgment of design quality.

#### Scenario: Empty triangle scores zero
- **WHEN** all three Concept fields are empty
- **THEN** the score displays 0 percent

#### Scenario: Complete triangle scores one hundred
- **WHEN** all three Concept fields have substantive content
- **THEN** the score displays 100 percent

#### Scenario: Score follows draft changes
- **WHEN** a corner gains or loses substantive content through a manual edit, AI application, or rollback
- **THEN** the displayed score updates without an AI call

#### Scenario: Score remains live without AI
- **WHEN** Concept-purpose AI is unavailable
- **THEN** the score still updates from the current draft values

### Requirement: One refinement operation runs at a time with cancellation
FusionCanvas SHALL allow at most one in-flight refinement AI operation per Item document, SHALL disable the refinement actions while an operation runs, SHALL cancel the in-flight operation when the Item document closes or the active Item changes, and SHALL never apply a late result to a different Item or after cancellation. When an AI operation fails or is cancelled before any value is applied to the drafts, drafts, history, and score SHALL remain unchanged and failures SHALL surface a recoverable inline error. Failure of the automatic-save commit after a value has been applied SHALL follow the applied-value commit requirement instead.

#### Scenario: Actions disabled while running
- **WHEN** a refinement operation is in flight
- **THEN** Initialize, Fine tune, and Change actions are disabled until it completes, fails, or is cancelled

#### Scenario: Item switch cancels in-flight operation
- **WHEN** a refinement operation is in flight and the user switches to another Item or closes the document
- **THEN** the operation is cancelled
- **AND** its late result is never applied

#### Scenario: Operation fails
- **WHEN** a refinement AI operation fails before any value is applied to the drafts
- **THEN** drafts, history, and score remain unchanged
- **AND** a recoverable inline error is reported near the refinement actions

### Requirement: Refinement requests use guidance and creative context without operational or secret data
FusionCanvas SHALL assemble refinement AI requests from the bundled design-triangle guidance document, the action semantics, the current triangle values, the Item's original Idea, and applicable user-authored creative context (store, niche, topic, inherited tags and metadata), and MUST exclude credentials, identifiers, timestamps, file paths, and other operational fields from the request payload.

#### Scenario: Request includes guidance and creative context
- **WHEN** a Fine tune, Change, or Initialize request is assembled
- **THEN** it contains the design-triangle guidance content, the action instruction, current triangle values, original Idea text, and applicable creative context

#### Scenario: Operational and secret data is excluded
- **WHEN** source entities contain credentials, database identifiers, timestamps, file paths, or internal provenance
- **THEN** those values are absent from the request payload, logs, and errors

### Requirement: The design-triangle guidance document ships with the app
FusionCanvas SHALL bundle a design-triangle guidance markdown document with the application and SHALL load it at runtime through an application-facing contract for use as refinement prompt context. The bundled content SHALL be a basic placeholder description of the design triangle that the maintainer can replace with the formal document later. No user interface SHALL display the document in this module.

#### Scenario: Guidance content is available at runtime
- **WHEN** a refinement prompt is assembled
- **THEN** the bundled design-triangle guidance text is included as prompt context

#### Scenario: No guidance UI
- **WHEN** the user navigates the Concept stage surface
- **THEN** no control displays or opens the guidance document

### Requirement: The refinement section remains accessible and theme coherent
FusionCanvas SHALL make the refinement actions keyboard reachable in a logical order after the Concept fields, SHALL give icon-only or compact actions meaningful accessible names, SHALL present history entries as a selectable list, and SHALL resolve busy, disabled, error, and selection states from shared application theme resources.

#### Scenario: Keyboard operation
- **WHEN** the user navigates the Concept surface without a pointer
- **THEN** Initialize, per-corner Fine tune and Change actions, and the history list are reachable in a predictable order
- **AND** history rollback is operable by keyboard

#### Scenario: Theme coherence
- **WHEN** the application appearance changes while the Concept surface is visible
- **THEN** the refinement section adopts the active theme
- **AND** disabled, busy, selected-history, and error states remain distinguishable
