## MODIFIED Requirements

### Requirement: Listing inspector presents stage-relevant creative fields
The Item document surface SHALL show exactly one primary built-in Stage Tool for the active view stage, SHALL make current-stage content editable when lifecycle policy allows, and SHALL present earlier-stage content read-only while retaining shared Item metadata outside the tool.

#### Scenario: Item is at Concept
- **WHEN** Concept is both current and active
- **THEN** the Concept Stage Tool shows editable Concept idea, Phrase, and Graphics description
- **AND** Idea, Design, and Listing primary tools are not shown simultaneously
- **AND** shared Overview, Notes, Tags, Related assets, and lifecycle areas remain available

#### Scenario: User reviews an earlier stage
- **WHEN** Design is current and the user activates Idea
- **THEN** the Idea Stage Tool is visible read-only
- **AND** the user must regress before editing Idea content

### Requirement: Listing inspector edits core creative fields with explicit save
The Item document surface SHALL persist optional working title, current-stage text, and Notes only through explicit Save, SHALL preserve Item identity and placement, hidden legacy fields, unknown metadata, and inherited provenance, and SHALL keep Tags independently immediate.

#### Scenario: User saves valid Item edits
- **WHEN** the user changes optional working title, current-stage text, or Notes and invokes Save
- **THEN** FusionCanvas persists the normalized values and updated timestamp
- **AND** preserves identity, topic placement, archive state, status, stage, assets, prompts, hidden Audience and generic Description, unknown metadata, and provenance

#### Scenario: Save targets a non-current stage
- **WHEN** a stale or incorrect request attempts to save stage content that is not owned by the Item's current stage
- **THEN** FusionCanvas rejects the mutation
- **AND** leaves persisted Item content unchanged

#### Scenario: User switches context with unsaved text
- **WHEN** the Item surface has meaningful unsaved working title, current-stage text, or Notes and the user changes Item, tab, active view stage, or lifecycle state
- **THEN** FusionCanvas asks whether to Save, Discard, or Cancel
- **AND** retains the current draft and focus when cancellation is chosen

#### Scenario: Tag changes while text is unsaved
- **WHEN** the user applies or removes a Tag while text edits are pending
- **THEN** the Tag mutation persists independently
- **AND** the explicit text Save remains pending

### Requirement: Listing inspector aligns status and workflow stage with document context
The Item document surface SHALL display one Item-level lifecycle-status selector in shared Overview/header, SHALL source its options from the approved transition policy, and SHALL keep current stage, active view stage, tree treatment, tabs, and filters synchronized.

#### Scenario: Status options are displayed
- **WHEN** an Item document is active
- **THEN** exactly one status selector is shown
- **AND** it displays the persisted current status and only valid direct transition targets for the current stage and status

#### Scenario: Stage or status changes
- **WHEN** the active Item's stage or status changes successfully
- **THEN** the surface, navigator, tree, tabs, and active filters refresh to the authoritative state
