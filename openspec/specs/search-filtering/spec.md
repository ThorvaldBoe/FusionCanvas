# Search and Filtering Specification

## Purpose

Defines how FusionCanvas narrows the visible workspace tree by remembered text, tags, and topic scope while preserving the hierarchy that explains where each result lives, with default archived exclusion, intentional archived inclusion, combined AND semantics, clear-all and empty-results behavior, filter-aware structural-command guarding, and the navigation-pane filter surface presentation.

## Requirements

### Requirement: Text search matches remembered workspace content
FusionCanvas SHALL match the navigation text query as a trimmed, case-insensitive substring against niche and group names and against each listing's title, description, notes, and attached tag names. Blank or whitespace-only text SHALL NOT act as a filter.

#### Scenario: User finds a listing by remembered title or idea text
- **WHEN** the user enters text that appears in a listing's title or description
- **THEN** the listing appears in the filtered tree
- **AND** nodes that match nothing and contain no matches are hidden

#### Scenario: User finds a listing by remembered notes
- **WHEN** the user enters text that appears only in a listing's notes
- **THEN** the listing appears in the filtered tree

#### Scenario: User finds a listing by a remembered tag name
- **WHEN** the user enters text that matches the name of a tag attached to a listing
- **THEN** that listing appears in the filtered tree

#### Scenario: User finds a topic by name
- **WHEN** the user enters text that appears in a niche or group name
- **THEN** that topic appears in the filtered tree with its location context

#### Scenario: Text query is blank
- **WHEN** the text query is empty or whitespace only
- **THEN** the text dimension does not restrict the tree

### Requirement: Tag filtering narrows results to listings carrying every selected tag
FusionCanvas SHALL allow filtering by one or more active tags from the active store, and a listing SHALL match the tag dimension only when it is linked to every selected tag. Topics SHALL NOT match the tag dimension directly but SHALL remain visible as ancestor context for matching listings.

#### Scenario: User filters by one tag
- **WHEN** the user selects a single tag in the filter surface
- **THEN** listings linked to that tag appear with their ancestor topics as context
- **AND** listings without that tag are hidden unless they contain no matching descendants

#### Scenario: User filters by multiple tags
- **WHEN** the user selects multiple tags
- **THEN** only listings linked to all of the selected tags appear

#### Scenario: Selected tag has no matching listings
- **WHEN** no listing in scope carries every selected tag
- **THEN** the tree shows the empty-results state instead of unrelated nodes

### Requirement: Topic scope narrows the visible tree to the current topic subtree
FusionCanvas SHALL offer a scope choice between the whole active store and the current topic and below, where the current topic resolves from canonical selection: a selected niche or group, or the containing topic of a selected listing. The scope choice SHALL be unavailable when no topic can be resolved from the current selection.

#### Scenario: User scopes to a selected niche
- **WHEN** a niche is selected and the user chooses the current-topic scope
- **THEN** the filtered tree shows only that niche and its subtree

#### Scenario: User scopes to a nested group
- **WHEN** a nested group is selected and the user chooses the current-topic scope
- **THEN** the filtered tree shows only that group and its descendants

#### Scenario: Selected listing supplies its containing topic
- **WHEN** a listing is selected and the user chooses the current-topic scope
- **THEN** the scope resolves to the listing's containing group, or its niche when ungrouped

#### Scenario: No topic can be resolved
- **WHEN** the current selection cannot resolve a niche or group
- **THEN** the current-topic scope is unavailable and the whole-store scope applies

### Requirement: Archived work stays excluded unless intentionally included
Filtered results SHALL exclude archived and effectively archived topics and listings by default. FusionCanvas SHALL reveal them only through an explicit include-archived choice, SHALL mark included archived rows as inactive, and SHALL NOT let them become the active navigation context or valid structural destinations. Archive and restore behavior itself remains unchanged.

#### Scenario: Archived listing is excluded by default
- **WHEN** an archived listing's title matches the active text query and include-archived is off
- **THEN** the archived listing does not appear in the filtered tree

#### Scenario: User intentionally includes archived work
- **WHEN** the user enables include-archived with a matching archived listing
- **THEN** the archived listing appears with inactive visual treatment and its preserved topic context

#### Scenario: Included archived row is invoked
- **WHEN** the user selects an included archived topic or listing
- **THEN** it does not become the active navigation context for creation or structural workflows
- **AND** the existing lifecycle surface remains the review and restore path

### Requirement: Active filters combine with AND semantics
FusionCanvas SHALL apply every active filter dimension together, so a node matches only when it satisfies the text, tag, scope, and archived-inclusion dimensions simultaneously. The text dimension matches when any single searchable field matches.

#### Scenario: Text and tag filters intersect
- **WHEN** a text query and a tag selection are both active
- **THEN** only listings matching the text and carrying every selected tag appear

#### Scenario: Scope and text filters combine
- **WHEN** a current-topic scope and a text query are both active
- **THEN** only matches inside the scoped subtree appear

#### Scenario: One dimension is cleared
- **WHEN** the user clears one active dimension while others remain
- **THEN** the tree re-widens to satisfy only the remaining dimensions

### Requirement: Filtered results preserve parent context
While any filter is active, FusionCanvas SHALL keep the ancestor store-to-topic path of every match visible as context, SHALL distinguish context ancestors from direct matches, and SHALL auto-expand the filtered tree enough to reveal matches without permanently changing the user's expansion state.

#### Scenario: Matching listing shows where it lives
- **WHEN** a listing inside a nested group matches
- **THEN** its ancestor niche and groups remain visible as context rows
- **AND** the context rows are visually distinguished from direct matches

#### Scenario: Matching nested topic shows its location
- **WHEN** a nested group matches the active filters
- **THEN** its parent path remains visible so the topic's location is understandable

### Requirement: Clearing filters restores normal browsing
FusionCanvas SHALL provide a single clear action that resets the text query, tag selection, topic scope, and archived inclusion together, SHALL restore the expansion state captured before filtering began, and SHALL preserve the current selection when it remains visible.

#### Scenario: User clears all filters
- **WHEN** the user invokes the clear action with active filters
- **THEN** every filter dimension resets
- **AND** the tree returns to normal browsing with the pre-filter expansion state restored

#### Scenario: Selection survives clearing
- **WHEN** the selected node remains visible after filters are cleared
- **THEN** the selection and reveal state are preserved

#### Scenario: No filters are active
- **WHEN** no filter dimension is active
- **THEN** the clear action is unavailable or has no effect

### Requirement: Empty filter results explain and recover
When active filters match nothing, FusionCanvas SHALL replace the tree content with an explanatory empty state that names the active filtering and offers the clear action, instead of rendering a blank tree.

#### Scenario: No nodes match the active filters
- **WHEN** the active filter combination matches no topic or listing
- **THEN** the tree area explains that nothing matches
- **AND** offers a clear-filters action

#### Scenario: User clears from the empty state
- **WHEN** the user invokes clear from the empty-results state
- **THEN** the full unfiltered tree returns

### Requirement: Structural placement commands remain unavailable while filtering
FusionCanvas SHALL keep sibling positioning and other structural commands that depend on complete ordering unavailable while any filter dimension is active, with actionable guidance, and SHALL restore them when filters clear.

#### Scenario: User attempts sibling positioning during a tag filter
- **WHEN** any filter dimension is active and the user attempts to position a group between siblings
- **THEN** FusionCanvas blocks the action
- **AND** explains that filtering must be cleared first

#### Scenario: Filters are cleared
- **WHEN** all filter dimensions are cleared
- **THEN** structural placement commands become available again

### Requirement: Navigation filter surface keeps search persistent and discloses occasional filters progressively
FusionCanvas SHALL keep the text search box persistently visible above the navigation tree, SHALL place the tag, scope, and archived-inclusion controls in a compact flyout opened from a filter command, SHALL indicate when filters are active, and SHALL keep every filter control keyboard reachable with predictable focus behavior. Applied filters SHALL take effect immediately without a separate commit step.

#### Scenario: User opens the filter flyout
- **WHEN** the user invokes the filter command above the tree
- **THEN** the flyout presents the tag, scope, and archived-inclusion controls
- **AND** changes apply to the tree immediately

#### Scenario: Filters are active
- **WHEN** any non-text filter dimension is active
- **THEN** the filter command indicates active filtering so the state is discoverable while the flyout is closed

#### Scenario: User dismisses the flyout
- **WHEN** the user closes the flyout with Escape or by moving focus away
- **THEN** the applied filters remain in effect
- **AND** focus returns to a predictable control in the navigation pane
