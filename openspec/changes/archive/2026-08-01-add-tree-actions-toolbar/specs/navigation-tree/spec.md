## ADDED Requirements

### Requirement: Navigation tree hosts a tree-actions toolbar
The left navigation pane SHALL present a compact tree-actions toolbar docked between the filter controls and the workspace tree. The toolbar SHALL host actions that apply to the visible tree as a whole rather than to a single node. Its first action SHALL be an expand/collapse-all toggle presented as an icon-only button whose icon reflects the pending action, whose hover tooltip names the pending action, and whose accessibility name matches the tooltip.

#### Scenario: Toolbar sits between filters and tree
- **WHEN** the left navigation pane renders with an active store
- **THEN** the tree-actions toolbar appears between the filter controls and the workspace tree
- **AND** the workspace tree renders directly below the toolbar

#### Scenario: Toggle presents the pending action
- **WHEN** the toggle's pending action is to expand all topics
- **THEN** the button shows the expand-all icon
- **AND** its hover tooltip and accessibility name read "Expand all groups"
- **WHEN** the toggle's pending action is to collapse all topics
- **THEN** the button shows the collapse-all icon
- **AND** its hover tooltip and accessibility name read "Collapse all groups"

### Requirement: Navigation tree expands and collapses all topics together
The navigation tree SHALL provide a single toggle action that expands or collapses every topic node (niches and groups) in the visible tree without changing the underlying workspace hierarchy. The first activation SHALL expand all topics, matching the tree's default collapsed state; each subsequent activation SHALL perform the opposite of the previous toggle activation. Manual expansion or collapse of individual nodes SHALL NOT change the toggle's remembered state. Expansion state produced by the toggle SHALL be retained across tree refreshes in the same way manual expansion is, for the duration of the application session.

#### Scenario: First activation expands every topic
- **WHEN** the user activates the tree-actions toggle while the tree shows a store with nested groups and items in its default collapsed state
- **THEN** every niche and group node expands
- **AND** the items inside those topics become visible

#### Scenario: Second activation collapses every topic
- **WHEN** the user activates the toggle again after an expand-all
- **THEN** every niche and group node collapses
- **AND** only the top-level topics remain visible

#### Scenario: Manual node changes do not redirect the toggle
- **WHEN** the toggle's last action was expand-all
- **AND** the user manually collapses one group
- **AND** the user activates the toggle
- **THEN** every topic node collapses, because the toggle still performs collapse-all

#### Scenario: Toggle expansion survives a tree refresh
- **WHEN** the user has expanded all topics with the toggle
- **AND** the tree rebuilds its projection after a structural change such as a rename or creation
- **THEN** the topic nodes remain expanded

#### Scenario: Toggle is disabled while filters are active
- **WHEN** any tree filter is active
- **THEN** the expand/collapse-all toggle is disabled
- **AND** its tooltip explains that filtering already expands the tree

#### Scenario: Toggle is disabled when nothing can expand
- **WHEN** the visible tree contains no topic nodes with children
- **THEN** the expand/collapse-all toggle is disabled
- **AND** its tooltip explains that no groups are available to expand or collapse

#### Scenario: Collapse-all protects an in-progress edit
- **WHEN** an inline create or rename editor is open on a tree node
- **AND** the user activates collapse-all
- **THEN** every topic node collapses except the ancestors of the edited node
- **AND** the editor remains visible and active