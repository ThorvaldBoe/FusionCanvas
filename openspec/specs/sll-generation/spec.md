# SLL Generation

## Purpose

Defines accepted behavior for AI-powered Sketch Layout Language (SLL) generation in the Concept stage surface, including availability gating, generate/regenerate semantics with replacement, persistence with the item, single-operation concurrency with cancellation, request assembly without operational or secret data, the untrusted-content boundary for workspace and user content, theme-coherent accessible presentation, and codec-owned SLL document serialization outside the Domain layer.

## Requirements

### Requirement: SLL generation section is gated on triangle completeness and SLL AI availability
FusionCanvas SHALL present an SLL generation section inside the Concept stage surface, below the refinement section, and SHALL make its Generate action available only when the design triangle is complete (deterministic completeness score of 100), the SLL-purpose AI is available, and the Concept fields are editable. The section SHALL remain visible but explicitly disabled with actionable guidance whenever any of these preconditions is unmet, and SHALL re-evaluate both AI availability and completeness live as values change while the document is open.

#### Scenario: Complete triangle enables generation
- **WHEN** the design triangle scores 100 and SLL-purpose AI is available and the Concept stage is editable
- **THEN** the Generate SLL action is enabled

#### Scenario: Incomplete triangle disables generation
- **WHEN** any triangle corner is empty or non-substantive so the score is below 100
- **THEN** the Generate SLL action is disabled
- **AND** guidance directs the user to complete the triangle

#### Scenario: SLL AI unavailable disables generation
- **WHEN** the SLL-purpose AI reports a missing credential, missing model, or invalid configuration
- **THEN** the Generate SLL action is disabled
- **AND** the guidance identifies the missing prerequisite and directs the user to AI settings

#### Scenario: Read-only earlier-stage review disables generation
- **WHEN** the Item's persisted current stage is beyond Concept and the user reviews the Concept stage read-only
- **THEN** the Generate SLL action is disabled and no SLL AI request can be started

#### Scenario: Completeness gate refreshes live
- **WHEN** the user edits a triangle corner so the completeness score crosses 100 either direction
- **THEN** the Generate SLL action's enabled state updates without an AI call

### Requirement: Generate produces a full minimal SLL artifact
FusionCanvas SHALL provide one `Generate SLL` action that asks AI to derive a Sketch Layout Language artifact from the three triangle values, the original Idea, and the resolved creative context, using the SLL AI purpose. On success the action SHALL present a single full minimal SLL containing, in order: important assumptions (if any), communication intent, the normalized Design Triangle, one plain-ASCII composition sketch, execution notes, and validation with the largest risk. The presented SLL SHALL preserve the supplied phrase exactly unless a proposed revision is explicitly recorded as such. The action SHALL be enabled only when the design triangle is complete, SHALL not be triggered implicitly by entering or showing the Concept stage, and SHALL NOT be started from a read-only surface.

#### Scenario: Generate derives a full minimal SLL
- **WHEN** the triangle is complete and the user activates Generate SLL
- **THEN** on success the section displays an SLL with assumptions, communication intent, the triangle, one ASCII sketch, execution notes, and validation
- **AND** the phrase used in the SLL matches the supplied phrase unless a revision is explicitly labeled

#### Scenario: No implicit generation on stage entry
- **WHEN** an Item document opens or switches to the Concept stage surface
- **THEN** no SLL AI request is started implicitly

#### Scenario: Unlabeled phrase mutation is rejected
- **WHEN** a generated SLL's normalized Design Triangle mutates the supplied phrase without an explicit revision label
- **THEN** the response is treated as invalid and not applied
- **AND** the previous SLL (if any) remains displayed and a recoverable inline error is reported

### Requirement: Generate and regenerate replace the current SLL
FusionCanvas SHALL allow the user to generate once and then regenerate, where each regenerate action replaces the previous SLL with a newly generated one rather than appending a variant. The Generate action and a regenerating action SHALL follow the same availability and concurrency rules, and the displayed SLL SHALL be the most recent successful result.

#### Scenario: Regenerate replaces the current SLL
- **WHEN** the user activates the regenerate action after a successful generation
- **THEN** a new SLL is generated and replaces the previously displayed one

#### Scenario: Regeneration failure preserves the existing SLL
- **WHEN** a regenerate action fails before producing a result
- **THEN** the previously displayed SLL remains unchanged
- **AND** a recoverable inline error is reported near the SLL section

#### Scenario: Stale SLL after a triangle edit
- **WHEN** a successful SLL exists and the user edits a triangle corner so the completeness score drops below 100
- **THEN** the existing SLL remains displayed with a visible stale marker
- **AND** the regenerate action is disabled until the triangle is complete again

### Requirement: The generated SLL persists with the item
FusionCanvas SHALL store the most recent successful SLL with the Item so that it survives reopening the document, committed through the Concept-stage automatic-save path with the stage-aware expected-state guard. A failed persistence SHALL keep the current SLL in the document and report a recoverable inline error.

#### Scenario: SLL survives reopen
- **WHEN** a generated SLL is persisted and the Item document is closed and reopened
- **THEN** the SLL section shows the persisted SLL

#### Scenario: Persistence failure is recoverable
- **WHEN** an automatic commit of a generated SLL fails validation or persistence
- **THEN** the SLL remains in the document and a recoverable inline error is reported

### Requirement: One SLL operation runs at a time with cancellation
FusionCanvas SHALL allow at most one in-flight SLL AI operation per Item document, SHALL disable the SLL actions while an operation runs, SHALL cancel the in-flight operation when the Item document closes or the active Item changes, and SHALL never apply a late result to a different Item or after cancellation. When an SLL operation fails or is cancelled before a result is produced, the existing SLL and its display SHALL remain unchanged and failures SHALL surface a recoverable inline error.

#### Scenario: Actions disabled while running
- **WHEN** an SLL operation is in flight
- **THEN** generate and regenerate actions are disabled until it completes, fails, or is cancelled

#### Scenario: Item switch cancels in-flight operation
- **WHEN** an SLL operation is in flight and the user switches to another Item or closes the document
- **THEN** the operation is cancelled and its late result is never applied

#### Scenario: Operation fails
- **WHEN** an SLL AI operation fails before producing a result
- **THEN** the existing SLL display remains unchanged and a recoverable inline error is reported

### Requirement: SLL requests use framework guidance and creative context without operational or secret data
FusionCanvas SHALL assemble SLL AI requests from the bundled Design Triangle framework document, the action instruction, the current triangle values, the Item's original Idea, and applicable user-authored creative context (store, niche, topic, inherited tags and metadata), and MUST exclude credentials, identifiers, timestamps, file paths, and other operational fields from the request payload.

#### Scenario: Request includes framework and creative context
- **WHEN** an SLL generate or regenerate request is assembled
- **THEN** it contains the Design Triangle framework content, the SLL generation instruction, the current triangle values, the original Idea text, and applicable creative context

#### Scenario: Operational and secret data is excluded
- **WHEN** source entities contain credentials, database identifiers, timestamps, file paths, or internal provenance
- **THEN** those values are absent from the request payload, logs, and errors

### Requirement: SLL AI requests treat workspace and user content as untrusted data
FusionCanvas SHALL instruct the SLL AI, in the system message, that all supplied workspace and user content — the original Idea, the Design Triangle values, store/niche/topic names and descriptions, tags, and metadata — is untrusted creative material provided as data, that it must not be interpreted as or obeyed as instructions, and that the SLL output rules always take precedence over any supplied content. This boundary SHALL be established in the system message independently of the existing operational and secret data exclusion.

#### Scenario: System message bounds supplied content as untrusted data
- **WHEN** an SLL generate or regenerate request is assembled
- **THEN** the system message states that supplied content is untrusted creative material, not instructions
- **AND** the system message states that the SLL output rules take precedence over any supplied content

#### Scenario: Untrusted-content boundary does not replace operational/secret exclusion
- **WHEN** source entities contain credentials, database identifiers, timestamps, file paths, or internal provenance
- **THEN** those values remain absent from the SLL request payload
- **AND** the untrusted-content boundary rule is present in addition to, not in place of, that exclusion

### Requirement: The SLL section is theme coherent and accessible
FusionCanvas SHALL make the Generate and regenerate actions keyboard reachable in a logical order after the Concept fields and refinement section, SHALL give the actions and the rendered SLL meaningful accessible names, and SHALL resolve busy, disabled, error, and rendering states from shared application theme resources.

#### Scenario: Keyboard operation
- **WHEN** the user navigates the Concept surface without a pointer
- **THEN** the Generate and regenerate actions are reachable in a predictable order after the refinement actions

#### Scenario: Theme coherence
- **WHEN** the application appearance changes while the Concept surface is visible
- **THEN** the SLL section adopts the active theme and busy, disabled, and error states remain distinguishable

### Requirement: SLL document serialization is owned outside the Domain layer
FusionCanvas SHALL serialize and deserialize SLL documents through an Application-defined codec implemented outside the Domain layer, and the Domain `SllDocument` type SHALL carry no serialization logic and no dependency on a serialization framework. The codec SHALL preserve the existing SLL document wire format so an SLL persisted by a prior build round-trips back to an equal document, and SHALL treat malformed input as a recoverable failure rather than throwing.

#### Scenario: Domain SLL document type carries no serialization
- **WHEN** a contributor inspects the Domain `SllDocument` type
- **THEN** it exposes no serialization or deserialization methods
- **AND** the Domain project has no dependency on a JSON or persistence-framework package

#### Scenario: SLL document round-trips through the codec
- **WHEN** an SLL document is serialized through the SLL codec and the result is deserialized through the same codec
- **THEN** the deserialized document is equal to the original document

#### Scenario: Malformed SLL input is a recoverable failure
- **WHEN** the SLL codec is asked to deserialize text that is not a valid SLL document
- **THEN** the codec reports failure without throwing
- **AND** no partial document is produced

#### Scenario: App layer uses the codec contract, not the Domain type, to serialize
- **WHEN** the SLL session stores or re-displays a generated SLL
- **THEN** it serializes and deserializes through the Application codec contract
- **AND** the App layer does not construct an Integration serialization type directly
