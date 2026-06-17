# FC-0101 - Store Management

## Summary

Store Management lets creators define the top-level business, brand, client, or publishing contexts that organize all FusionCanvas work.

In Phase 1, a store is the main boundary for organizing niches, groups, listings, tags, assets, and creative context. It should be simple enough to create immediately, while still meaningful enough to support multiple brands or businesses.

## User Need

As a Print on Demand creator, I need to separate work by store or brand so each business context keeps its own niches, listings, notes, assets, and creative direction.

## Goals

- Allow users to create and maintain stores.
- Make stores the primary workspace scope.
- Keep multiple brands or businesses separated.
- Preserve store-level context for future creative and operational work.
- Allow inactive stores to be archived without deleting useful context.

## Requirements

- The user can create a store.
- The user can rename a store.
- The user can edit basic store information.
- The user can archive and restore a store.
- The user can distinguish active stores from archived stores.
- The user can browse into a store and see its niches, groups, and listings.
- The user can maintain notes or context that apply to the store as a whole.
- Store context should be available anywhere the user is working inside that store.

## User Workflows

### Create a Store

The user creates a new store when they want to start organizing work for a brand, shop, client, or business unit.

The user should be able to provide a name and optional basic context without completing advanced setup.

### Edit Store Context

The user updates store information when brand direction, target market, notes, or planning context changes.

Store context should help the user remember what this store is for and how work inside it should be approached.

### Archive a Store

The user archives a store when it is inactive, completed, paused, abandoned, or no longer part of active work.

Archiving should reduce clutter without destroying historical context.

## Acceptance Criteria

- A new user can create their first store without needing advanced configuration.
- A user with multiple brands can keep work separated by store.
- A store can contain niches, groups, listings, tags, and assets.
- Archived stores do not clutter the normal active workspace.
- Archived stores remain available for review or restoration.

## Out of Scope

- Marketplace account connection
- Publishing destination setup
- Store analytics
- Billing or sales reporting
- Multi-user permissions
- Store templates
- Automated store setup

## Open Questions

- Should store-level tags be shared across all niches and listings in that store?
- Should archived stores be hidden by default everywhere or only in the main store selector?
- What store context belongs in Phase 1 versus later AI or marketplace phases?

## Related Notes

- [[Phase 1 - MVP Creative Workspace]]
- [[Roadmap]]
- [[Product]]
- [[Data Model]]
