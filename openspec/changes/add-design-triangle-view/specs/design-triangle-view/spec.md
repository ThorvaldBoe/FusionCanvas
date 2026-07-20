## ADDED Requirements

### Requirement: Design Triangle View presents idea, phrase, and graphic together
FusionCanvas SHALL present a Design Triangle View for the selected concept version with the idea node at the top, the phrase node at the lower left, and the graphic node at the lower right, sourced from the concept-versions service.

#### Scenario: View shows the selected concept
- **WHEN** the Design Triangle View is opened for a listing with a selected concept
- **THEN** the idea, phrase, and graphic values are loaded from the selected concept
- **AND** each node displays its current value or not-used marker

#### Scenario: No concept is selected
- **WHEN** the Design Triangle View is opened for a listing with no selected concept
- **THEN** the view reports that no concept is selected
- **AND** does not fabricate idea, phrase, or graphic values

### Requirement: Each triangle node is editable and selectable as a refinement target
FusionCanvas SHALL allow the user to edit each triangle node inline and SHALL expose the currently selected node as an explicit refinement-target property that the hosting tool can read.

#### Scenario: User edits a node inline
- **WHEN** the user edits the idea, phrase, or graphic value in its node and commits
- **THEN** FusionCanvas persists the change through the concept-versions service
- **AND** refreshes the node display from the selected concept

#### Scenario: User selects a node as the refinement target
- **WHEN** the user selects a triangle node
- **THEN** FusionCanvas sets that node as the current refinement target
- **AND** exposes the selected target through an explicit property

#### Scenario: Persistence fails during a node edit
- **WHEN** the concept-versions service cannot save a node edit
- **THEN** FusionCanvas reports a recoverable error
- **AND** preserves the draft and focus so the user can retry
- **AND** leaves the persisted concept unchanged

### Requirement: Idea is required; phrase and graphic support a not-used marker
FusionCanvas SHALL treat the idea node as required and non-empty, SHALL allow the user to mark the phrase or graphic node as intentionally not used, and SHALL distinguish a not-used marker from an empty pending value.

#### Scenario: Idea node rejects empty input
- **WHEN** the user commits an empty or whitespace-only idea value
- **THEN** FusionCanvas rejects the edit
- **AND** preserves the prior idea value and the recoverable input

#### Scenario: User marks phrase as not used
- **WHEN** the user marks the phrase node as not used for a graphic-only design
- **THEN** FusionCanvas records the not-used marker on the concept
- **AND** displays the node distinctly from an empty pending value

#### Scenario: User marks graphic as not used
- **WHEN** the user marks the graphic node as not used for a text-only design
- **THEN** FusionCanvas records the not-used marker on the concept
- **AND** displays the node distinctly from an empty pending value

### Requirement: Advisory scoring is optional and non-blocking
FusionCanvas SHALL display a design-triangle score in the middle of the triangle when the selected concept carries one, SHALL show a neutral state when no score is present, and SHALL NOT use scores to block stage advancement or selection.

#### Scenario: Score is displayed
- **WHEN** the selected concept carries a design-triangle score
- **THEN** the middle of the triangle shows the overall score, weak element, readiness state, and critique hint
- **AND** labels the score as advisory

#### Scenario: No score is present
- **WHEN** the selected concept has no score
- **THEN** the middle of the triangle shows a neutral state
- **AND** the view does not report a failure

#### Scenario: Low score does not block advancement
- **WHEN** a concept has a low or absent score
- **THEN** FusionCanvas does not block the user from advancing the listing to the next stage
- **AND** the user remains responsible for the decision

### Requirement: Concept notes capture concerns and alternatives
FusionCanvas SHALL provide a notes area in the Design Triangle View for concerns, alternatives, or improvement ideas, SHALL store the notes on the concept through the concept-versions service, and SHALL preserve notes across tool switches and reloads.

#### Scenario: User adds concept notes
- **WHEN** the user enters concerns, alternatives, or improvement ideas in the notes area and saves
- **THEN** FusionCanvas persists the notes on the concept atomically
- **AND** preserves the notes across tool switches and reloads

#### Scenario: User leaves meaningful unsaved notes
- **WHEN** the notes area contains meaningful unsaved changes and the user switches concept or closes
- **THEN** FusionCanvas asks whether to save, discard, or cancel
- **AND** retains the current draft and focus when cancellation is chosen

### Requirement: Design Triangle View uses stage-tool-host context
FusionCanvas SHALL receive an explicit context snapshot from the Stage Tool Host — active store, niche, topic path, item, stage, selected concept, inherited tags, and metadata — and SHALL access additional context through application-layer accessors rather than scraping UI state.

#### Scenario: View receives context from the host
- **WHEN** the Design Triangle View is opened as a hosted Concept-stage tool
- **THEN** it receives the active store, niche, topic path, item, stage, selected concept, inherited tags, and metadata
- **AND** does not read context from the tree view model or direct storage

#### Scenario: Context changes
- **WHEN** the active tab, selected navigation node, selected item, selected concept, or workflow stage changes
- **THEN** the Design Triangle View refreshes its context and selected-concept state

### Requirement: Design Triangle View follows shared desktop control guidance
FusionCanvas SHALL present the triangle view with compact action sizing, clear tooltips for icon-only commands, predictable keyboard flow, accessible editing and cancellation, busy states that prevent duplicate submission, and error states that preserve selection and focus.

#### Scenario: Node editing is keyboard accessible
- **WHEN** the user activates a node without a pointer
- **THEN** editing, commit, and cancel are reachable through the keyboard
- **AND** focus returns to the node after a successful save or recoverable error

#### Scenario: Busy state prevents duplicate submission
- **WHEN** a node edit or notes save is running or fails
- **THEN** FusionCanvas prevents duplicate submission
- **AND** keeps the view available to report success or an actionable error
- **AND** preserves the draft and focus after failure
