# FC-0003 - Local SQLite Persistence

## Summary

Local SQLite Persistence defines how FusionCanvas stores structured application data locally.

The purpose is to support local-first ownership, reliable organization, and future search/filter behavior without depending on cloud services.

## User Need

As a creator, I need FusionCanvas to keep my structured workspace data locally so I can own my work and use the application without depending on an external service.

## Goals

- Establish local structured storage as the primary source of truth.
- Preserve core workspace data between sessions.
- Support future search, filtering, and organization.
- Keep storage expectations simple enough for early development.
- Avoid cloud dependency in the foundational product.

## Requirements

- The application stores core structured data locally.
- The application can load previously saved workspace data.
- Store, niche, group, listing, asset, prompt, and tag data should be persistable when included in the active model.
- Structured data should support relationships between core entities.
- Storage should distinguish active and archived records where relevant.
- Flexible metadata should be possible for information that may evolve.
- Persistence behavior should protect user data from avoidable loss.
- The local data store should remain understandable to contributors.

## Data Categories

Phase 0 persistence should account for:

- stable entity identity
- names and basic descriptions
- parent-child relationships
- lifecycle or archive state
- timestamps where useful
- flexible metadata
- references to files rather than embedded large files

## User Workflows Supported

### Save Workspace Data

The user creates or updates foundational workspace data and expects it to remain available after closing and reopening the application.

### Preserve Relationships

The user expects stores, niches, groups, listings, tags, and assets to remain connected correctly across sessions.

### Prepare for Search and Filtering

The product should store enough structured information to support Phase 1 browsing and filtering.

## Acceptance Criteria

- Core structured data can be saved locally.
- Core structured data can be loaded again later.
- Relationships between entities are preserved.
- The application does not require cloud services for its primary data.
- The storage approach supports Phase 1 organization features.

## Out of Scope

- Cloud sync
- Multi-user collaboration
- Marketplace data sync
- Advanced database optimization
- Full backup/restore workflow
- Encryption
- Import/export packages

## Open Questions

- Which flexible metadata fields are required before Phase 1?
- Should archived data remain in the main local store?
- What minimum migration/versioning behavior is required before active development accelerates?

## Related Notes

- [[Phase 0 - Foundation]]
- [[Roadmap]]
- [[Architecture]]
- [[Data Model]]
- [[Principles]]
