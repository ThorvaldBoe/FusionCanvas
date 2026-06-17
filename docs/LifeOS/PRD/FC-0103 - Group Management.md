# FC-0103 - Group Management

## Summary

Group Management lets creators organize listings into flexible containers such as collections, campaigns, experiments, batches, series, or temporary work areas.

Groups are practical planning structures. They should be easy to create, rename, move, archive, and nest as a creator's understanding of the work changes.

## User Need

As a Print on Demand creator, I need flexible groups inside niches so I can organize listings by campaign, collection, experiment, production batch, or any other working context.

## Goals

- Let users organize listings below niches.
- Support nested groups for flexible structure.
- Make groups easy to reshape as work evolves.
- Preserve child work when groups move.
- Keep groups lightweight and useful.

## Requirements

- The user can create a group inside a niche.
- The user can create a group inside another group.
- The user can rename a group.
- The user can edit group notes or context.
- The user can move a group to another valid location.
- The user can archive and restore a group.
- A group can contain listings.
- A group can contain child groups.
- Moving a group preserves its child groups and listings.
- Groups can be used for campaigns, collections, experiments, batches, series, or temporary planning.

## User Workflows

### Create a Group

The user creates a group when a set of listings should be organized together.

The group may represent a seasonal campaign, design series, experiment, backlog, production batch, or collection.

### Nest Groups

The user creates child groups when a topic needs more structure.

For example, a holiday group might contain subgroups for shirts, stickers, mugs, or phrase variations.

### Move or Archive a Group

The user moves a group when the structure changes.

The user archives a group when it is no longer active but should remain available for reference.

## Acceptance Criteria

- A user can create groups inside niches.
- A user can create nested groups where useful.
- A user can place listings inside groups.
- A user can move a group without losing or orphaning child work.
- A user can archive a group without deleting its context.

## Out of Scope

- Batch operations
- Saved views
- Automated campaign planning
- Group-level analytics
- Group templates
- Production queue behavior

## Open Questions

- Should empty groups be convertible into listings during Phase 1?
- Should groups be allowed to move between niches?
- Should archived groups hide their child listings by default?

## Related Notes

- [[Phase 1 - MVP Creative Workspace]]
- [[Roadmap]]
- [[Product]]
- [[Data Model]]
