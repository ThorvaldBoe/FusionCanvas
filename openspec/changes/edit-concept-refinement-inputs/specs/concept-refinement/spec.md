## ADDED Requirements

### Requirement: Refinement actions use readable editable working triangle values
FusionCanvas SHALL present the Concept idea, Phrase, and Graphic direction values in the Refine with AI panel as complete, legible, editable working fields. The working fields SHALL initialize from the current Item inspector drafts, SHALL synchronize when the corresponding inspector draft changes, and SHALL remain session-local until an AI result is applied through the existing inspector draft and automatic-save path. Fine tune and Change SHALL capture all three working-field values at activation time and SHALL use that captured triangle as the current refinement context.

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

#### Scenario: Local editing has no persistence side effect
- **WHEN** the user edits a refinement working field without applying a successful AI result
- **THEN** the Item inspector draft, persisted Item, refinement history, and completeness score remain unchanged

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
