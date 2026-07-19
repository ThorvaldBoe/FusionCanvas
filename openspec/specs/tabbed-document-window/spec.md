# tabbed-document-window Specification

## Purpose
TBD - created by archiving change fc-0009-tabbed-document-window. Update Purpose after archive.
## Requirements
### Requirement: Workspace includes a tabbed document window
The main workspace SHALL include a document window that can display multiple open document contexts as tabs.

#### Scenario: User views the document window
- **WHEN** the main workspace is shown with one or more open document contexts
- **THEN** the document window displays a tab strip for those contexts
- **AND** the document window displays the active context's workflow area and detail area

### Requirement: Document contexts open as tabs
The document window SHALL allow navigation-open actions to open item, topic, workflow stage, or working view contexts as document tabs without closing unrelated open tabs.

#### Scenario: User opens multiple contexts from navigation
- **WHEN** the user opens one context from navigation and then opens another context
- **THEN** both contexts are represented as open tabs
- **AND** the second context becomes the active document context

#### Scenario: User opens an already open context
- **WHEN** the user opens a context that already has an open tab
- **THEN** the existing tab becomes active
- **AND** no duplicate tab is created for the same document context

### Requirement: Tabs identify open contexts
Each document tab SHALL display a title that identifies the open item, topic, workflow stage, or working view.

#### Scenario: User scans open tabs
- **WHEN** multiple document tabs are open
- **THEN** each tab displays a title for its document context
- **AND** the active tab is visually distinguishable from inactive tabs

### Requirement: Active tab controls active document context
Selecting a document tab SHALL make that tab's document context the active workspace context.

#### Scenario: User switches tabs
- **WHEN** the user selects an inactive document tab
- **THEN** that tab becomes active
- **AND** the workspace active document context changes to the selected tab's context

### Requirement: Active tab coordinates navigation context
The workspace SHALL update navigation selection or revealed hierarchy context to match the active document tab when the active tab has a known navigation location.

#### Scenario: User activates a tab with a known hierarchy location
- **WHEN** the user selects a document tab whose context belongs to a known store, niche, group, topic, or item hierarchy
- **THEN** the navigation pane reflects that context's location
- **AND** the context can be understood without closing other open tabs

### Requirement: Active tab coordinates workflow stage navigator
The workflow stage navigator SHALL reflect the current item and workflow stage for the active document tab.

#### Scenario: User switches between contexts with different stages
- **WHEN** the user switches from a tab in one workflow stage to a tab in another workflow stage
- **THEN** the workflow stage navigator updates to the newly active tab's stage
- **AND** the workflow area remains associated with the active document context

### Requirement: Document window separates workflow and detail areas
The document window SHALL place workflow-stage controls above a larger detail area for the active document context.

#### Scenario: User views an active document context
- **WHEN** a tab is active
- **THEN** the document window shows a workflow area for that context
- **AND** the document window shows a larger lower detail area for the current stage or view

### Requirement: Detail area follows active tab and workflow stage
The document window detail area SHALL display the relevant stage tool or working view for the active document context and current workflow stage.

#### Scenario: Active context changes
- **WHEN** the active document tab changes
- **THEN** the detail area updates to match the new active context and workflow stage

#### Scenario: Workflow stage changes for active context
- **WHEN** the active document context's workflow stage changes
- **THEN** the detail area updates to the default view or tool for the new stage

### Requirement: Detail area can host built-in and future stage tools
The detail area SHALL be structured so built-in stage views and future Stage Tool Host-provided views can occupy the same document detail region.

#### Scenario: Default stage tool is available
- **WHEN** the active document context has a default tool for its current workflow stage
- **THEN** the detail area displays that default tool in the document window

### Requirement: Tabs can be closed
The document window SHALL allow users to close tabs that are no longer needed.

#### Scenario: User closes an inactive tab
- **WHEN** the user closes an inactive tab
- **THEN** that tab is removed from the document window
- **AND** the active tab remains active

#### Scenario: User closes the active tab while other tabs remain
- **WHEN** the user closes the active tab and at least one other tab remains
- **THEN** the document window selects a remaining tab as the active context
- **AND** navigation, workflow stage, and detail area state update to the new active context

#### Scenario: User closes the last tab
- **WHEN** the user closes the only open tab
- **THEN** the document window has no active document context
- **AND** the detail area returns to an empty workspace state

### Requirement: Listing document contexts present the listing inspector
When the active document context is a listing item, the document window detail area SHALL present the Listing Inspector as that context's working view instead of placeholder stage-tool content, while non-item document contexts retain the existing stage tool or working view behavior.

#### Scenario: User activates a listing tab
- **WHEN** the active document tab's context is a listing item
- **THEN** the detail area displays the Listing Inspector for that listing beneath the workflow stage navigator
- **AND** the inspector follows the active tab when the user switches between listing tabs

#### Scenario: User switches between item and non-item contexts
- **WHEN** the user switches from a listing tab to a store or topic tab
- **THEN** the detail area returns to the stage tool or working view behavior for the non-item context
- **AND** switching back to the listing tab restores the inspector for that listing

#### Scenario: Listing details change outside the inspector
- **WHEN** the active listing's title, placement, archive state, status, or stage changes through another accepted surface while its tab is open
- **THEN** the detail area reflects the updated persisted listing context

