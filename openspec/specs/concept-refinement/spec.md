# Concept Refinement

## Purpose

Defines accepted behavior for the Concept refinement tool: AI-assisted iterative refinement of the Concept-stage design triangle (Concept idea, Phrase, Graphics description) — manual initialization from the item's base idea, per-corner Fine tune and Change operations, session-scoped history with rollback, a deterministic live completeness score, AI-availability gating, bundled design-triangle guidance as prompt context, and single-operation concurrency with cancellation.

## Requirements


### Requirement: Concept refinement lives in the Concept stage surface
FusionCanvas SHALL present one continuous Concept editing surface inside the Item document. The surface SHALL show the original Base idea as read-only context, provide the manual Concept idea, Phrase, and Graphic direction working fields, and provide the Initialize, per-corner Fine tune and Change actions, per-corner Instructions fields, completeness score, and session history in that order. The surface SHALL follow the visibility of the Concept stage surface and SHALL make the three working fields and refinement actions read-only or disabled whenever the Concept fields are read-only. The surface SHALL NOT render a second upper set of Concept idea, Phrase, or Graphic direction editors or a separate `Refine with AI` section heading.

#### Scenario: Unified Concept surface appears
- **WHEN** an Item document shows the Concept stage surface
- **THEN** the surface shows Base idea, Initialize from base idea, the three working fields, their refinement actions and Instructions fields, Triangle completeness, and refinement history in one continuous Concept surface
- **AND** no duplicate upper Concept idea, Phrase, or Graphic direction editors are visible
- **AND** no `Refine with AI` heading or separate refinement framing is visible

#### Scenario: Base idea is visible but not editable in Concept
- **WHEN** an Item document shows the Concept stage surface
- **THEN** the Base idea field displays the Item's original Idea value
- **AND** the Base idea field is read-only even when the Concept stage is current and editable
- **AND** editing Concept content does not change the original Idea value

#### Scenario: Section appears with the Concept stage surface
- **WHEN** an Item document shows the Concept stage surface
- **THEN** the refinement section appears below the three creative fields
- **AND** it is not shown for Idea, Design, or Listing stage surfaces

#### Scenario: Earlier-stage review disables refinement
- **WHEN** the Item's persisted current stage is beyond Concept and the user reviews the Concept stage read-only
- **THEN** every refinement action is disabled
- **AND** no AI request can be started from the read-only surface

### Requirement: Concept stage surface hosts the SLL generation section
FusionCanvas SHALL present an SLL generation section inside the existing Concept stage surface of the Item document, directly below the refinement section, and SHALL follow the visibility of the Concept stage surface. The section SHALL be hidden for Idea, Design, and Listing stage surfaces and SHALL be disabled whenever the Concept fields are read-only.

#### Scenario: Section appears with the Concept stage surface
- **WHEN** an Item document shows the Concept stage surface
- **THEN** the SLL generation section appears below the refinement section
- **AND** it is not shown for Idea, Design, or Listing stage surfaces

#### Scenario: Earlier-stage review disables the SLL section
- **WHEN** the Item's persisted current stage is beyond Concept and the user reviews the Concept stage read-only
- **THEN** the SLL generation section actions are disabled and no SLL AI request can be started

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
FusionCanvas SHALL provide a `Fine tune` action and a `Change` action for each of the three design-triangle corners. Fine tune SHALL ask AI to improve the corner's current value while preserving its direction; Change SHALL ask AI to propose a materially different direction for the corner. Both SHALL evaluate the corner in context of the other two corners' current values, the creative context, and any non-empty user instruction for that corner, SHALL apply the result only to that corner's draft, and SHALL normalize a Phrase result to one line. Fine tune SHALL be disabled for an empty corner; Change SHALL remain available for an empty corner.

#### Scenario: Fine tune improves one corner
- **WHEN** the Concept idea has a value and the user activates Fine tune for it
- **THEN** the AI request includes the current value, the other two corners as context, and any non-empty user instruction for the corner
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

### Requirement: Per-corner refinement instruction fields guide Fine tune and Change
FusionCanvas SHALL provide three small instruction text fields in the unified Concept surface, one for each design-triangle corner (Concept idea, Phrase, Graphic direction), each unlabeled and placed just below that corner's Fine tune/Change button pair, using the placeholder text `Instructions`. When a field contains non-whitespace text, that instruction SHALL be included in the AI request for that corner's Fine tune or Change action as supplemental guidance. When a field is empty or whitespace-only, behavior SHALL be identical to today. After a Fine tune or Change action for a corner applies a successful result, FusionCanvas SHALL clear that corner's instruction field; a failed or cancelled operation SHALL preserve the instruction text. Instruction text SHALL NOT be persisted and SHALL NOT appear in history.

#### Scenario: Instruction steers a Fine tune
- **WHEN** the Phrase instruction field contains "make the phrase shorter" and the user activates Fine tune for the Phrase
- **THEN** the Phrase Fine tune request includes the instruction text
- **AND** on success the instruction field for the Phrase is cleared

#### Scenario: Instruction for a different corner is not included
- **WHEN** the Concept idea instruction field contains text and the user activates Fine tune or Change for the Phrase
- **THEN** the Phrase request does not include the Concept idea instruction text

#### Scenario: Instruction steers a Change
- **WHEN** the Graphic direction instruction field contains a direction and the user activates Change for the Graphic direction
- **THEN** the Graphic direction Change request includes the instruction text
- **AND** on success the instruction field for the Graphic direction is cleared

#### Scenario: Empty instruction leaves behavior unchanged
- **WHEN** a corner's instruction field is empty or whitespace-only and the user activates Fine tune or Change for it
- **THEN** the request is identical to a refinement with no instruction
- **AND** the field remains empty

#### Scenario: Result applied but automatic-save commit fails
- **WHEN** a Fine tune or Change result is applied to the targeted corner's draft but the automatic-save commit fails validation or persistence
- **THEN** the applied value and history entry are retained as defined by the applied-value commit requirement
- **AND** the instruction field for that corner is cleared because the result was applied

#### Scenario: The active Item changes or the session resets
- **WHEN** the active Item changes, the Item document closes, or the refinement session resets
- **THEN** all three instruction fields are cleared

#### Scenario: Failed or cancelled operation preserves the instruction
- **WHEN** a Fine tune or Change operation fails or is cancelled before applying a result
- **THEN** the instruction field for that corner retains the user's text

#### Scenario: Instruction field content has no persistence or history effect
- **WHEN** the user types into an instruction field without applying a successful result
- **THEN** the Item drafts, persisted Item, refinement history, and completeness score remain unchanged

#### Scenario: Instruction fields respect read-only state
- **WHEN** the Concept stage is read-only
- **THEN** all three instruction fields are read-only

### Requirement: Refinement actions use readable editable working triangle values
FusionCanvas SHALL present the Concept idea, Phrase, and Graphic direction values in the unified Concept surface as complete, legible, editable working fields when the Concept stage is editable. The working fields SHALL initialize from the current Item inspector drafts and SHALL synchronize when the corresponding inspector draft changes. When a working field changes, the change SHALL remain a pending local draft until field exit or a required context transition, at which point it SHALL be copied to the corresponding inspector draft and committed through the existing automatic-save path. Fine tune and Change SHALL capture all three working-field values at activation time and SHALL use that captured triangle as the current refinement context.

#### Scenario: Complete current values are legible
- **WHEN** a Concept draft contains text longer than the available single-line panel width
- **THEN** the corresponding refinement working field displays the complete text without ellipsis
- **AND** Concept idea and Graphic direction support multiline editing
- **AND** Phrase remains a single-line input whose text wraps for legibility

#### Scenario: Working values initialize and synchronize from inspector drafts
- **WHEN** an Item Concept session opens or resets
- **THEN** each refinement working field contains its corresponding current inspector draft
- **AND** when one inspector draft later changes through manual editing, AI application, initialization, or rollback, the matching working field updates without overwriting unrelated locally edited working fields

#### Scenario: Fine tune uses the visible working triangle
- **WHEN** the user edits one or more refinement working fields and activates Fine tune for a non-empty corner
- **THEN** the refinement request captures the three current working-field values
- **AND** the selected corner and action are Fine tune

#### Scenario: Change uses the visible working triangle
- **WHEN** the user edits one or more refinement working fields and activates Change for a corner, including an empty target corner
- **THEN** the refinement request captures the three current working-field values
- **AND** the selected corner and action are Change

#### Scenario: Manual working-field edit commits on field exit
- **WHEN** the Concept stage is editable and the user changes a refinement working field and moves focus away from it
- **THEN** only the corresponding inspector draft is updated with the working-field value
- **AND** the value is committed through the automatic-save path
- **AND** the other two triangle values and Base idea remain unchanged

#### Scenario: Manual Concept editing works without AI
- **WHEN** Concept-purpose AI is unavailable but the Concept stage is editable
- **THEN** the three working fields remain editable
- **AND** a manual field edit can be committed and persisted
- **AND** Initialize, Fine tune, and Change remain visible but disabled with actionable guidance

#### Scenario: Pending working edits commit before context transition
- **WHEN** a working field has a pending local edit and the user changes Item, tab, active view stage, lifecycle state, or closes the document
- **THEN** FusionCanvas attempts to copy and commit the pending triangle values before completing the transition
- **AND** it does not silently discard the pending value

#### Scenario: Working-field commit failure preserves local input
- **WHEN** a pending working-field commit fails validation or persistence
- **THEN** the refinement working field retains the user's local value
- **AND** an inline actionable error is shown
- **AND** the affected context does not silently advance past the failed commit

#### Scenario: Failure or cancellation preserves local input
- **WHEN** Fine tune or Change fails or is cancelled before applying a result
- **THEN** the refinement working fields retain the user's current local values
- **AND** inspector drafts, history, and completeness score remain unchanged

#### Scenario: Successful result follows the existing apply path
- **WHEN** Fine tune or Change succeeds
- **THEN** only the selected corner's inspector draft is replaced by the result
- **AND** the corresponding working field synchronizes to the result
- **AND** persistence, history, score, and error handling follow the existing refinement apply behavior

#### Scenario: Working fields remain accessible and respect read-only state
- **WHEN** the Concept refinement panel is keyboard operated
- **THEN** each working field has a distinct accessible name and is reachable before its associated Fine tune and Change actions
- **AND** when the Concept stage is read-only, all three working fields are read-only and no refinement action can start

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
FusionCanvas SHALL assemble refinement AI requests from the bundled canonical Design Triangle framework, the action semantics, the current triangle values, the Item's original Idea, an optional non-empty user instruction for the targeted corner, and applicable user-authored creative context (store, niche, topic, inherited tags and metadata), and MUST exclude credentials, identifiers, timestamps, file paths, and other operational fields from the request payload. The request SHALL instruct the model to respect the framework's social-meaning model, coherent three-corner relationship, and semantic graphic role while preserving the existing Initialize and per-corner response contracts.

#### Scenario: Request includes framework and creative context
- **WHEN** a Fine tune, Change, or Initialize request is assembled
- **THEN** it contains the canonical Design Triangle framework, the action instruction, current triangle values, original Idea text, and applicable creative context
- **AND** it directs the model to preserve or improve wearer signal, viewer inference or effect, intentional Phrase/Graphic relationship, and Graphic semantic role as applicable to the action

#### Scenario: A non-empty instruction is included in the request
- **WHEN** the user provided non-empty instruction text for a Fine tune or Change target corner
- **THEN** the instruction text is included in the request as supplemental guidance
- **AND** the request still instructs the model to treat it as non-overriding guidance that preserves output rules and action semantics

#### Scenario: Operational and secret data is excluded
- **WHEN** source entities contain credentials, database identifiers, timestamps, file paths, or internal provenance
- **THEN** those values are absent from the request payload, logs, and errors

### Requirement: The design-triangle guidance document ships with the app
FusionCanvas SHALL bundle one canonical PoD Design Framework Markdown document with the application and SHALL load it at runtime through an application-facing contract for use as Ideation and Concept refinement prompt context. The document SHALL combine the framework README and canonical Foundations of PoD Design, Design Triangle and Design Pyramid, Sketch Layout Language, and Generating SLL documents in that order. No user interface SHALL display or open the document in this module.

#### Scenario: Canonical framework content is available at runtime
- **WHEN** an Ideation or Concept refinement prompt is assembled
- **THEN** the bundled content includes the social-meaning, Design Triangle, Design Pyramid, SLL, and SLL-generation framework sections as prompt context

#### Scenario: No framework UI
- **WHEN** the user navigates the Idea or Concept stage surfaces
- **THEN** no control displays or opens the framework document

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
