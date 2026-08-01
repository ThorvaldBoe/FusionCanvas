# navigation-tree Specification

## Purpose
TBD - created by archiving change fc-0005-navigation-tree. Update Purpose after archive.
## Requirements

### Requirement: Navigation tree represents workspace hierarchy
FusionCanvas SHALL represent workspace navigation as a hierarchy of stores, topics, and items.

#### Scenario: User browses store hierarchy
- **WHEN** a workspace contains a store with niche topics, nested group topics, and listing items
- **THEN** the navigation tree exposes the store as a top-level context
- **AND** the tree exposes niches as top-level topics inside the store
- **AND** the tree exposes groups as nested topics
- **AND** the tree exposes listings as items inside valid topics

#### Scenario: Topic contains nested content
- **WHEN** a topic contains child topics and listing items
- **THEN** the navigation tree preserves both child topic entries and listing item entries under that topic

### Requirement: Navigation topics support practical nesting
FusionCanvas SHALL support nested topic structures for niches and groups without requiring a fixed maximum product workflow depth.

#### Scenario: User creates nested groups
- **WHEN** a creator organizes work as a niche with multiple nested group levels
- **THEN** the navigation model preserves the nested topic path
- **AND** listing items can remain associated with the intended nested topic

### Requirement: Navigation movement preserves context
FusionCanvas SHALL support moving topics and items within valid navigation destinations while preserving contained content and item context.

#### Scenario: User moves a topic
- **WHEN** a topic with child topics and listing items is moved to another valid topic destination
- **THEN** the moved topic retains its child topic subtree
- **AND** the moved topic retains its listing items

#### Scenario: User moves a listing item
- **WHEN** a listing item is moved from one valid topic to another valid topic
- **THEN** the listing remains the same work item
- **AND** its listing context, status, notes, tags, and related assets remain associated with that listing

#### Scenario: User attempts invalid movement
- **WHEN** a move would place a topic or item outside a valid store/topic hierarchy
- **THEN** FusionCanvas rejects the move
- **AND** the existing navigation hierarchy remains unchanged

### Requirement: Navigation tree tracks active context
FusionCanvas SHALL expose a single active navigation context that can be used by creation, generation, and editing workflows.

#### Scenario: User selects a topic
- **WHEN** a user selects a topic in the navigation tree
- **THEN** that topic becomes the active navigation context
- **AND** new topic-scoped work defaults to that topic unless a workflow chooses a more specific destination

#### Scenario: User selects a listing
- **WHEN** a user selects a listing item in the navigation tree
- **THEN** that listing becomes the active navigation context
- **AND** workflows that need a containing topic can resolve the listing's parent topic and store

### Requirement: Navigation tree supports expansion and collapse
FusionCanvas SHALL allow topic nodes to be expanded and collapsed without changing the underlying workspace hierarchy.

#### Scenario: User collapses a topic
- **WHEN** a user collapses a topic with child content
- **THEN** the tree hides that topic's descendants from the visible expanded view
- **AND** the descendants remain in the workspace hierarchy

#### Scenario: User expands a topic
- **WHEN** a user expands a collapsed topic
- **THEN** the tree shows that topic's child topics and listing items according to the current tree view state

#### Scenario: User opens a store
- **WHEN** a user opens a store in the navigation tree
- **THEN** the tree shows the store's top-level topics by default

### Requirement: Navigation selection can reveal related work
FusionCanvas SHALL support revealing and highlighting a navigation node associated with an active document or workspace item.

#### Scenario: User activates an open listing tab
- **WHEN** a user activates an open tab associated with a listing
- **THEN** the navigation tree can reveal the listing's location
- **AND** the tree can highlight the listing as the active navigation context

#### Scenario: Revealed item is inside collapsed parents
- **WHEN** a related listing or topic is revealed inside collapsed parent topics
- **THEN** the tree expands the required parent path enough to make the related node visible

### Requirement: Navigation tree is filter-ready
FusionCanvas SHALL support future filtered views by preserving parent context for matching topics and items.

#### Scenario: Tree projection contains matching item
- **WHEN** a future filter or search result includes a listing item
- **THEN** the navigation model can provide enough parent store and topic context to show where the item lives

#### Scenario: Tree projection contains matching topic
- **WHEN** a future filter or search result includes a nested topic
- **THEN** the navigation model can provide enough parent store and topic context to show the topic's location

### Requirement: Navigation tree filters listings by tag
FusionCanvas SHALL provide a tag filter control above the workspace tree that filters listings to those that have all selected active tags, preserves ancestor topic paths for matching listings, and keeps stable entity selection across filter changes.

#### Scenario: User filters by one tag
- **WHEN** the user selects one active tag chip in the tree tag filter control
- **THEN** the visible tree shows only listings that have that tag
- **AND** preserves the store and topic ancestor path needed to locate each matching listing
- **AND** hides listings without that tag

#### Scenario: User filters by multiple tags with AND semantics
- **WHEN** the user selects multiple active tag chips in the tree tag filter control
- **THEN** the visible tree shows only listings that have every selected tag
- **AND** listings missing any selected tag are hidden

#### Scenario: User clears the tag filter
- **WHEN** the user clears all selected tag chips or closes the filter control
- **THEN** the tree restores its pre-filter expansion state
- **AND** canonical selection is preserved

#### Scenario: Canonical selection is filtered out
- **WHEN** the canonical selection is a listing that does not match the active tag filter
- **THEN** the inspector retains the canonical selection with a clear filtered-out indicator
- **AND** offers a reveal or clear-filter action
- **AND** structural commands continue to operate on canonical identity

#### Scenario: Tag filter excludes archived tags
- **WHEN** the tree tag filter control is open
- **THEN** only active tags of the active store are offered as selectable chips
- **AND** archived tags are excluded

#### Scenario: Tag filter is keyboard reachable
- **WHEN** the tree tag filter control is focused
- **THEN** tag chips can be toggled with keyboard input
- **AND** the filter can be cleared without pointer-only interaction

### Requirement: Navigation presents universal records as Items
The navigation tree SHALL use Item terminology for stage-agnostic records and SHALL use the stable identity-derived fallback for an empty working title.

#### Scenario: Empty-title Item appears in the tree
- **WHEN** an Item has no working title
- **THEN** its row shows `Untitled item · <short ID>`
- **AND** retains normal selection, filtering, inactive treatment, and context actions

#### Scenario: Published or Rejected Item appears
- **WHEN** an Item becomes Published or Rejected
- **THEN** its row updates to the authoritative lifecycle treatment
- **AND** canonical selection remains coherent if active filters remove it

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