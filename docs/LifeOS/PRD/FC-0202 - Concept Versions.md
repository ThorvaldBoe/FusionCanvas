# FC-0202 - Concept Versions

## Summary

Concept Versions preserve alternate creative directions for a listing.

The active concept version is the working state used by the Basic Concept Tool. It should preserve the idea, phrase, graphic direction, quality notes, and relevant history for a single existing item.

## User Need

Creators need to keep useful rejected or superseded concepts instead of losing the reasoning behind a design.

## Requirements

- A listing can have multiple concept versions.
- Each concept can describe idea, phrase, graphic direction, audience reaction, risks, quality notes, and design triangle score metadata where available.
- Concepts belong to the Concept stage in the core workflow.
- A concept can be created from an existing idea or directly from a phrase, graphic direction, or other later-stage starting point.
- Concept tools should inherit store, niche, topic path, item, and relevant metadata from the current context.
- Concept work requires an existing item. There is no free-floating concept that is not attached to an item.
- One concept can be marked as the current or selected direction.
- The selected concept version supplies the current idea, phrase, and graphic values shown by the Basic Concept Tool.
- Accepting a large alternate direction from the Basic Concept Tool can create a new concept version instead of overwriting the selected one.
- Superseded and rejected concepts remain available.
- Concept history should help explain why a listing evolved.

## Acceptance Criteria

- A user can create more than one concept for a listing.
- A user can create a concept from the current topic or item context without re-entering that context manually.
- A user can start at the Concept stage when they already have enough direction.
- A user cannot work on a concept without a selected item.
- A user can identify the selected concept.
- A user can preserve a promising alternate concept without losing the current direction.
- A user can review old concepts later.
- Rejected concepts do not clutter the active direction.

## Out of Scope

- AI concept refinement
- Version comparison UI
- Automatic scoring
- Design file versioning

## Related Notes

- [[Roadmap]]
- [[Product]]
- [[Data Model]]
- [[FC-0008 - Workflow Stage Navigator]]
- [[FC-0010 - Context-Aware Tools]]
- [[FC-0210 - Basic Concept Tool]]
