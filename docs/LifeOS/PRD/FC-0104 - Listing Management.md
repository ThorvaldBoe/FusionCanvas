# FC-0104 - Listing Management

## Summary

Listing Management lets creators create and maintain listings as the primary working objects in FusionCanvas.

A listing represents a product concept, not just an image file. It can exist before artwork, mockups, or marketplace metadata exist, and it should preserve the creative context needed to move the idea forward.

## User Need

As a Print on Demand creator, I need to capture and organize product concepts as listings so ideas do not get lost before they become designs or published products.

## Goals

- Make listings the main unit of creative work.
- Allow listings to exist before final assets exist.
- Support common listing actions such as create, edit, move, duplicate, archive, restore, and delete.
- Preserve context when listings move.
- Keep listing creation fast enough for early idea capture.

## Requirements

- The user can create a listing inside a valid store, niche, or group context.
- The user can rename a listing.
- The user can edit listing details.
- The user can duplicate a listing.
- The user can move a listing to another valid location.
- The user can archive and restore a listing.
- The user can delete a listing when appropriate.
- A listing can exist without attached assets.
- A listing can capture the core creative idea behind a potential product.
- A listing can be created from a selected topic with only a single line of idea text.
- A listing created from a topic can inherit applicable tags and metadata from its parent context.
- Moving a listing preserves its status, notes, tags, and attached assets.
- A listing should be treated as a product concept rather than a single file.

## User Workflows

### Capture a Listing

The user creates a listing when they have a product idea, phrase, graphic direction, or partial concept worth preserving.

The listing should be quick to create even when many details are unknown.

The fastest capture path should require only a selected topic and one line of text. Additional fields should remain optional so idea capture does not interrupt creative flow.

### Refine a Listing

The user updates the listing as the idea becomes clearer.

The listing may gain a phrase, graphic direction, notes, tags, status changes, and assets over time.

### Move or Duplicate a Listing

The user moves a listing when it belongs in a different group or niche.

The user duplicates a listing when they want to create a variation while preserving the original concept.

## Acceptance Criteria

- A user can capture a product idea quickly as a listing.
- A user can create a listing from a selected topic with one line of text.
- A user can create listings before design files exist.
- A listing created from a topic is placed in that topic and receives applicable inherited context.
- A user can move a listing between groups or niches as its fit becomes clearer.
- A user can duplicate a listing for variations.
- A user can archive or restore listings without losing context.

## Out of Scope

- Marketplace product creation
- Full listing metadata optimization
- Design version management
- Concept version history
- Performance history
- Bulk listing operations

## Open Questions

- Should delete be available in Phase 1, or should archive/reject be preferred?
- Should listing duplication include assets by default?
- Should a listing be convertible into a group during Phase 1?

## Related Notes

- [[Phase 1 - MVP Creative Workspace]]
- [[Roadmap]]
- [[Product]]
- [[Data Model]]
- [[FC-0404 - Idea Generation]]
