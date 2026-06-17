## ADDED Requirements

### Requirement: Navigation Model Supports Stores
FusionCanvas SHALL ensure that the navigation model supports stores.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The navigation model supports stores

### Requirement: Navigation Model Supports Topics
FusionCanvas SHALL ensure that the navigation model supports topics.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The navigation model supports topics

### Requirement: Navigation Model Supports Items
FusionCanvas SHALL ensure that the navigation model supports items.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The navigation model supports items

### Requirement: Niches Behave As Default Top-level Topics Inside A Store
FusionCanvas SHALL ensure that niches behave as default top-level topics inside a store.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Niches behave as default top-level topics inside a store

### Requirement: Groups Behave As Nested Topics
FusionCanvas SHALL ensure that groups behave as nested topics.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Groups behave as nested topics

### Requirement: Listings Behave As Initial Items
FusionCanvas SHALL ensure that listings behave as initial items.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Listings behave as initial items

### Requirement: Topics Can Contain Child Topics
FusionCanvas SHALL ensure that topics can contain child topics.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Topics can contain child topics

### Requirement: Topics Can Contain Items
FusionCanvas SHALL ensure that topics can contain items.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Topics can contain items

### Requirement: Tree Can Represent Arbitrary Practical Topic Depth
FusionCanvas SHALL ensure that the tree can represent arbitrary practical topic depth.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The tree can represent arbitrary practical topic depth

### Requirement: Moving A Topic Should Preserve Its Subtree
FusionCanvas SHALL ensure that moving a topic should preserve its subtree.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Moving a topic should preserve its subtree

### Requirement: Moving An Item Should Preserve Its Context
FusionCanvas SHALL ensure that moving an item should preserve its context.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Moving an item should preserve its context

### Requirement: Model Should Support Expanding And Collapsing Topics
FusionCanvas SHALL ensure that the model should support expanding and collapsing topics.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The model should support expanding and collapsing topics

### Requirement: Model Should Support Showing All Top-level Topics By Default
FusionCanvas SHALL ensure that the model should support showing all top-level topics by default.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The model should support showing all top-level topics by default

### Requirement: Model Should Support Future Filtering While Preserving Parent Context
FusionCanvas SHALL ensure that the model should support future filtering while preserving parent context.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The model should support future filtering while preserving parent context

### Requirement: Model Should Support Future Conversion Of Item To Topic And Empty Topic To Item
FusionCanvas SHALL ensure that the model should support future conversion of item to topic and empty topic to item.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The model should support future conversion of item to topic and empty topic to item

### Requirement: Tree Should Support Drag And Drop Movement Where Practical
FusionCanvas SHALL ensure that the tree should support drag and drop movement where practical.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The tree should support drag and drop movement where practical

### Requirement: Tree Should Support Renaming Topics And Items Without Breaking Relationships
FusionCanvas SHALL ensure that the tree should support renaming topics and items without breaking relationships.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The tree should support renaming topics and items without breaking relationships

### Requirement: Active Topic Or Item Should Define The Default Scope For Creation Tools
FusionCanvas SHALL ensure that the active topic or item should define the default scope for creation tools.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The active topic or item should define the default scope for creation tools

### Requirement: Selecting Or Activating A Document Tab Should Reveal Or Highlight The Related Location In The Navigation Tree
FusionCanvas SHALL ensure that selecting or activating a document tab should reveal or highlight the related location in the navigation tree.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Selecting or activating a document tab should reveal or highlight the related location in the navigation tree

### Requirement: Acceptance Criteria Remain Satisfied
FusionCanvas SHALL satisfy the acceptance criteria captured in the source PRD.

#### Scenario: Navigation Model Can Represent Store, Niche, Group, And Listing Structure
- **WHEN** the corresponding capability is delivered
- **THEN** The navigation model can represent store, niche, group, and listing structure.

#### Scenario: Model Supports Nested Groups
- **WHEN** the corresponding capability is delivered
- **THEN** The model supports nested groups.

#### Scenario: Model Supports Listings Inside Topics
- **WHEN** the corresponding capability is delivered
- **THEN** The model supports listings inside topics.

#### Scenario: Model Supports Moving Topics And Items Conceptually Without Losing Their Context
- **WHEN** the corresponding capability is delivered
- **THEN** The model supports moving topics and items conceptually without losing their context.

#### Scenario: Model Supports Drag And Drop Movement As A Preferred Interaction Where Practical
- **WHEN** the corresponding capability is delivered
- **THEN** The model supports drag and drop movement as a preferred interaction where practical.

#### Scenario: Active Navigation Context Can Be Used By Creation And Generation Tools
- **WHEN** the corresponding capability is delivered
- **THEN** The active navigation context can be used by creation and generation tools.

#### Scenario: Activating An Open Tab Can Update The Visible Navigation Selection
- **WHEN** the corresponding capability is delivered
- **THEN** Activating an open tab can update the visible navigation selection.

#### Scenario: Model Supports Expansion And Collapse Behavior
- **WHEN** the corresponding capability is delivered
- **THEN** The model supports expansion and collapse behavior.

#### Scenario: Model Is Suitable For Phase 1 Store, Niche, Group, Listing, Search, And Tag Features
- **WHEN** the corresponding capability is delivered
- **THEN** The model is suitable for Phase 1 store, niche, group, listing, search, and tag features.

## Source PRD

# FC-0005 - Navigation Tree

## Summary

The Navigation Tree defines how users browse and reshape the FusionCanvas workspace.

Phase 0 should establish the foundation for hierarchical browsing using stores, topics, and items. Phase 1 will build product workflows on top of this foundation.

## User Need

As a creator, I need to browse my stores, niches, groups, and listings in a structure that feels natural, flexible, and easy to reshape as my work changes.

## Goals

- Establish a clear topic/item navigation model.
- Support stores as top-level workspace contexts.
- Support niches as default top-level topics inside stores.
- Support groups as nested topics.
- Support listings as initial items.
- Prepare for filtering, movement, conversion, expansion, and collapse behavior.
- Support robust restructuring as niches, subtopics, and design families evolve.
- Provide the context used by creation and generation tools.

## Requirements

- The navigation model supports stores.
- The navigation model supports topics.
- The navigation model supports items.
- Niches behave as default top-level topics inside a store.
- Groups behave as nested topics.
- Listings behave as initial items.
- Topics can contain child topics.
- Topics can contain items.
- The tree can represent arbitrary practical topic depth.
- Moving a topic should preserve its subtree.
- Moving an item should preserve its context.
- The model should support expanding and collapsing topics.
- The model should support showing all top-level topics by default.
- The model should support future filtering while preserving parent context.
- The model should support future conversion of item to topic and empty topic to item.
- The tree should support drag and drop movement where practical.
- The tree should support renaming topics and items without breaking relationships.
- The active topic or item should define the default scope for creation tools.
- Selecting or activating a document tab should reveal or highlight the related location in the navigation tree.

## Navigation Concepts

### Store

A store is the top-level business, brand, client, or publishing context.

### Topic

A topic is a folder-like grouping node. Niches and groups are the first topic types.

### Item

An item is a concrete unit of work. Listings are the first item type.

## User Workflows Supported

### Browse Work

The user browses from store to niche to group to listing.

### Reshape Structure

The user moves topics and items as their understanding changes.

For example, a user may start with Dogs -> Dogs and coffee -> Funny dogs and coffee, create many Chihuahua designs, then later create a Chihuahua subtopic and move the existing designs under it. The tree should support this kind of restructuring without data loss.

### Create in Current Context

The user selects a topic and creates a new item. The new item is placed in the selected topic by default.

### Prepare for Filtering

The tree should be able to show matching work while preserving enough hierarchy to understand where results live.

## Acceptance Criteria

- The navigation model can represent store, niche, group, and listing structure.
- The model supports nested groups.
- The model supports listings inside topics.
- The model supports moving topics and items conceptually without losing their context.
- The model supports drag and drop movement as a preferred interaction where practical.
- The active navigation context can be used by creation and generation tools.
- Activating an open tab can update the visible navigation selection.
- The model supports expansion and collapse behavior.
- The model is suitable for Phase 1 store, niche, group, listing, search, and tag features.

## Out of Scope

- Full search and filtering implementation
- Saved views
- Batch operations
- Drag-and-drop polish
- Advanced tree customization
- Multiple simultaneous workspaces

## Open Questions

- Should item-to-topic conversion be implemented in Phase 1 or only planned?
- Should empty-topic-to-item conversion be included in the first usable navigation release?
- Should the navigation tree show assets directly or only through related listings?

## Related Notes

- [[Phase 0 - Foundation]]
- [[Roadmap]]
- [[Product]]
- [[Data Model]]
- [[FC-0107 - Basic Search and Filtering]]
- [[FC-0010 - Context-Aware Tools]]
- [[FC-0009 - Tabbed Document Window]]
