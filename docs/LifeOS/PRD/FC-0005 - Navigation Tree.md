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
