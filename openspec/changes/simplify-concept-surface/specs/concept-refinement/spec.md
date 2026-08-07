## MODIFIED Requirements

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
- **THEN** the unified Concept surface appears
- **AND** it is not shown for Idea, Design, or Listing stage surfaces

#### Scenario: Earlier-stage review disables refinement
- **WHEN** the Item's persisted current stage is beyond Concept and the user reviews the Concept stage read-only
- **THEN** every refinement action is disabled
- **AND** no AI request can be started from the read-only surface

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
- **WHEN** the Concept refinement surface is keyboard operated
- **THEN** each working field has a distinct accessible name and is reachable before its associated Fine tune and Change actions
- **AND** when the Concept stage is read-only, all three working fields are read-only and no refinement action can start
