## ADDED Requirements

### Requirement: Per-corner refinement instruction fields guide Fine tune and Change
FusionCanvas SHALL provide three small instruction text fields in the Refine with AI panel, one for each design-triangle corner (Concept idea, Phrase, Graphic direction), each unlabeled and placed just below that corner's Fine tune/Change button pair, using the placeholder text `Instructions`. When a field contains non-whitespace text, that instruction SHALL be included in the AI request for that corner's Fine tune or Change action as supplemental guidance. When a field is empty or whitespace-only, behavior SHALL be identical to today. After a Fine tune or Change action for a corner applies a successful result, FusionCanvas SHALL clear that corner's instruction field; a failed or cancelled operation SHALL preserve the instruction text. Instruction text SHALL NOT be persisted and SHALL NOT appear in history.

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

## MODIFIED Requirements

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
