# FC-0204 - Asset Relationships

## Summary

Asset Relationships connect files and references to the entities they support.

## User Need

Creators need assets to remain connected to listings, concepts, designs, mockups, stores, and niches.

## Requirements

- Assets can be related to listings.
- Assets can be related to concepts, designs, mockups, stores, or niches where useful.
- Asset purpose should be visible.
- A single asset may support more than one context if the workflow requires it.
- Moving a listing or group should preserve asset relationships.
- Missing or unavailable assets should be understandable to the user.

## Acceptance Criteria

- A user can see which assets support a listing.
- A user can see which design an asset belongs to.
- Asset relationships survive workspace reorganization.
- Assets can be used as context, not just stored as loose files.

## Out of Scope

- Asset deduplication
- Cloud sync
- Automatic file matching
- Image processing

## Related Notes

- [[Roadmap]]
- [[Data Model]]
- [[FC-0109 - Import Existing Assets]]
