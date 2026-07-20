## ADDED Requirements

### Requirement: Basic Concept Tool is the default Concept-stage tool
FusionCanvas SHALL provide the Basic Concept Tool as the default built-in Concept-stage tool, registered through the Stage Tool Host, available from the Concept stage for an existing item, and unavailable as a free-floating concept workspace without an item.

#### Scenario: User opens the tool for an existing item
- **WHEN** the user is on the Concept stage with a selected item
- **THEN** the Stage Tool Host displays the Basic Concept Tool as the active Concept-stage tool
- **AND** the tool receives the active store, niche, topic path, item, stage, selected concept, inherited tags, and metadata

#### Scenario: No item is selected
- **WHEN** the Concept stage is active but no item is selected
- **THEN** the Stage Tool Host shows an empty state or creation path
- **AND** does not open the Basic Concept Tool against only a topic context

#### Scenario: Plugin Concept tool coexists
- **WHEN** a future plugin contributes a Concept-stage tool that supports the current context
- **THEN** both the Basic Concept Tool and the plugin tool are selectable through the Stage Tool Host
- **AND** the Basic Concept Tool remains the default when no other tool is selected

### Requirement: Manual concept refinement works without AI
FusionCanvas SHALL allow the user to manually edit idea, phrase, and graphic nodes, mark phrase or graphic as not used, and save changes to the current concept version through the concept-versions service, with no AI provider required.

#### Scenario: User edits a node manually
- **WHEN** the user edits the idea, phrase, or graphic value and commits
- **THEN** FusionCanvas persists the change to the current concept version through the concept-versions service
- **AND** records a manual-edit history entry

#### Scenario: User saves without an AI provider
- **WHEN** no AI concept-refinement provider is configured
- **THEN** manual editing, not-used markers, notes, and save remain fully available
- **AND** AI actions are shown as disabled with an explanation

### Requirement: AI-assisted refinement targets the selected node
FusionCanvas SHALL allow the user to request AI improvement of the currently selected triangle node when an AI concept-refinement provider is configured, SHALL use the full item, niche, topic path, triangle values, tags, metadata, prior concept versions, accepted/rejected suggestions, and nearby sibling items as context, and SHALL affect only the selected node unless the user accepts a broader rewrite.

#### Scenario: User requests AI improvement
- **WHEN** the user invokes AI improvement for the selected node and a provider is configured
- **THEN** FusionCanvas sends the full context to the provider
- **AND** presents the returned suggestion alongside the current value without overwriting it

#### Scenario: AI suggestion affects only the selected node
- **WHEN** the user accepts an AI suggestion for one node
- **THEN** FusionCanvas updates only that node on the current concept version
- **AND** leaves the other triangle values unchanged

#### Scenario: Broader rewrite creates a new concept version
- **WHEN** the user explicitly accepts a broader rewrite
- **THEN** FusionCanvas creates a new concept version through the concept-versions service
- **AND** selects the new version as the current direction

#### Scenario: No provider is configured
- **WHEN** no AI concept-refinement provider is configured
- **THEN** AI improvement actions are disabled
- **AND** the tool explains that an AI provider must be configured

### Requirement: AI suggestions never overwrite saved concept data without approval
FusionCanvas SHALL NOT overwrite saved concept data with an AI suggestion without explicit user approval, and SHALL let the user accept, edit, reject, regenerate, or save an AI suggestion as a new item.

#### Scenario: User accepts a suggestion
- **WHEN** the user accepts an AI suggestion
- **THEN** FusionCanvas updates the current concept version (or creates a new one for a broader rewrite) through the concept-versions service
- **AND** records an accepted-suggestion history entry

#### Scenario: User rejects a suggestion
- **WHEN** the user rejects an AI suggestion
- **THEN** FusionCanvas preserves the suggestion as negative guidance where practical
- **AND** records a rejected-suggestion history entry
- **AND** leaves the current concept unchanged

#### Scenario: User regenerates
- **WHEN** the user regenerates an AI suggestion
- **THEN** FusionCanvas requests a new suggestion for the same node and context
- **AND** preserves the prior suggestion as rejected unless the user explicitly discards it

#### Scenario: User edits a suggestion before accepting
- **WHEN** the user edits an AI suggestion before accepting
- **THEN** FusionCanvas persists the edited value as the user's explicit input
- **AND** records the accept with the edited content

### Requirement: Promising alternates can be saved as new items
FusionCanvas SHALL allow the user to save a promising AI suggestion (or manual alternate) as a new item under the current topic in one action, SHALL copy the relevant idea, phrase, and graphic values and source metadata, SHALL apply applicable inherited tags and topic metadata, and SHALL leave the current item open in the Concept tool.

#### Scenario: User saves an alternate as a new item
- **WHEN** the user invokes save-as-new-item for a suggestion or alternate direction
- **THEN** FusionCanvas creates a new listing under the current topic through the listing-management service
- **AND** assigns it the Idea workflow stage
- **AND** copies the relevant idea, phrase, graphic, and source metadata
- **AND** applies applicable inherited tags and topic metadata
- **AND** leaves the current item open in the Concept tool

#### Scenario: New item appears for triage
- **WHEN** the saved-as-new-item listing is created
- **THEN** it is reachable in the Idea Inbox for triage
- **AND** does not carry the source item's concept, design, or mockup records

### Requirement: Concept history pane records and restores entries
FusionCanvas SHALL provide a local history pane for the current item and concept workflow that records manual edits, AI suggestion requested/accepted/rejected, score updates, concept version changes, restored states, and suggestions saved as new items, and SHALL allow the user to restore a previous history entry through the concept-versions service.

#### Scenario: History entry is recorded
- **WHEN** a triangle value changes, an AI suggestion is requested, accepted, or rejected, a score is updated, the current concept version changes, or a suggestion is saved as a new item
- **THEN** the history pane receives a new entry with enough data to understand and restore the change

#### Scenario: Important entries feed the timeline
- **WHEN** an important Concept-stage event occurs
- **THEN** FusionCanvas records a creative-history event through the timeline writer in the same atomic snapshot

#### Scenario: User restores a previous history entry
- **WHEN** the user restores a previous history entry
- **THEN** FusionCanvas creates or selects a concept version through the concept-versions service that reflects the restored state
- **AND** records a restored-state history entry
- **AND** does not delete the current concept version

### Requirement: Advisory scoring is displayed when available
FusionCanvas SHALL display an advisory quality indicator in the middle of the triangle when the selected concept carries a score, SHALL request scores only on explicit user action when a provider is configured, and SHALL NOT use scores to block stage advancement or selection.

#### Scenario: Score is shown
- **WHEN** the selected concept carries a score
- **THEN** the middle of the triangle shows the advisory score, weak element, readiness state, and critique hint

#### Scenario: Score is requested explicitly
- **WHEN** the user requests a score and a provider is configured
- **THEN** FusionCanvas requests a score from the provider and writes it to the concept through the concept-versions service
- **AND** records a score-updated history entry

#### Scenario: Low score does not block advancement
- **WHEN** a concept has a low or absent score
- **THEN** the user may still advance the item toward Design
- **AND** FusionCanvas does not gate advancement on the score

### Requirement: Tool advances the item toward Design through the navigator
FusionCanvas SHALL allow the user to advance the item toward the Design stage by requesting the workflow-stage-navigator, and SHALL NOT set the stage field directly.

#### Scenario: User proceeds to Design
- **WHEN** the user invokes the proceed action and the navigator accepts the transition
- **THEN** FusionCanvas advances the listing to the Design stage through the workflow-stage-navigator
- **AND** the Stage Tool Host updates to the Design stage context

#### Scenario: Advancement is blocked
- **WHEN** the workflow-stage-navigator rejects the transition
- **THEN** FusionCanvas reports the actionable reason
- **AND** leaves the listing's stage and concept state unchanged

### Requirement: Basic Concept Tool follows shared desktop control guidance
FusionCanvas SHALL present the tool with compact action sizing, clear tooltips for icon-only commands, predictable keyboard flow, accessible accept/reject/regenerate/save-as-new-item/proceed actions, busy states that prevent duplicate submission, and error states that preserve selection and focus.

#### Scenario: AI request is in progress
- **WHEN** an AI improvement or score request is running
- **THEN** FusionCanvas prevents duplicate submission
- **AND** keeps the tool available to report success or an actionable error
- **AND** preserves the current concept and focus after failure

#### Scenario: Keyboard-only refinement
- **WHEN** the user activates node editing, AI improvement, accept, reject, regenerate, save-as-new-item, restore, or proceed without a pointer
- **THEN** all essential actions are reachable through the keyboard
- **AND** focus returns to a meaningful control after each transition
