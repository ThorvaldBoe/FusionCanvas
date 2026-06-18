## Why

FusionCanvas needs durable local structured storage before Phase 1 workspace features start relying on stores, topics, listings, assets, prompts, and tags across sessions. Defining SQLite persistence now preserves the local-first product principle while keeping the first storage layer understandable for contributors.

## What Changes

- Introduce local SQLite as the primary source of truth for structured workspace data.
- Persist Phase 0 core entities from the active model, including Store, Niche, Group, Listing, Asset, Prompt, and Tag.
- Preserve stable identities, basic fields, timestamps, archive state where relevant, flexible metadata, and relationships between persisted entities.
- Store references to files instead of embedding large file contents in SQLite.
- Add basic database initialization, schema versioning, and migration expectations to protect user data from avoidable loss.
- Keep cloud sync, multi-user collaboration, encryption, backup/restore, import/export packages, and advanced query optimization out of this change.

## Capabilities

### New Capabilities
- `local-sqlite-persistence`: Defines local SQLite storage expectations for structured FusionCanvas workspace data, including entity persistence, relationship preservation, schema initialization, metadata, and migration boundaries.

### Modified Capabilities

## Impact

- Affected code: `FusionCanvas.Application` will need persistence-facing contracts/use cases, and `FusionCanvas.Integration` will need a SQLite-backed implementation when this change is implemented.
- Affected tests: `FusionCanvas.Application.Tests` and `FusionCanvas.Integration.Tests` should cover persistence contracts, schema initialization, save/load behavior, relationships, and migration/version safeguards.
- Affected specs: a new `local-sqlite-persistence` capability will become accepted behavior after implementation and archive.
- Dependencies: requires a SQLite data access package or equivalent .NET SQLite provider; no cloud service, UI framework behavior, workspace file copying, marketplace SDK, AI provider, or plugin host contract is required.
