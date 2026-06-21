## Context

FusionCanvas is a local-first desktop application in Phase 0. The shell and Clean Architecture projects exist, and the core domain model change introduces Store, Niche, Group, Listing, Asset, Prompt, and Tag as the first durable product concepts.

Local SQLite persistence is the first integration-backed source of truth for structured workspace data. It should sit behind application-facing contracts so domain code remains persistence-neutral and UI code does not own database behavior.

## Goals / Non-Goals

**Goals:**

- Add an application-facing workspace persistence contract for saving and loading structured workspace data.
- Implement a SQLite-backed repository in `FusionCanvas.Integration`.
- Persist Phase 0 core entities, relationships, archive flags, timestamps, and flexible metadata.
- Store file references as structured paths or identifiers instead of embedding large file contents.
- Initialize the schema automatically for a local workspace database.
- Add minimal schema versioning and migration safeguards before real user data depends on the database.
- Cover save/load and relationship preservation with automated tests.

**Non-Goals:**

- No cloud sync, multi-user collaboration, encryption, or remote storage.
- No backup/restore workflow or import/export package builder.
- No advanced search, filtering, full-text indexing, analytics, or optimization work.
- No workspace file copying behavior; file ownership belongs to the workspace file storage change.
- No UI for choosing, creating, repairing, or migrating workspaces.
- No marketplace, AI provider, plugin, concept, design, mockup, or performance persistence beyond references present in the Phase 0 active model.

## Decisions

### Use an application-facing repository contract

Persistence should be exposed through `FusionCanvas.Application` as a small contract that saves and loads a structured workspace snapshot. This keeps UI code from depending on SQLite and keeps domain entities independent of database concerns.

Alternative considered: let the Avalonia app instantiate SQLite commands directly. That would be quick but would put integration behavior into the UI layer and make future workspace workflows harder to test.

### Keep SQLite implementation in the integration layer

The concrete repository should live in `FusionCanvas.Integration` and use a .NET SQLite provider such as `Microsoft.Data.Sqlite`. The integration layer is the correct boundary for external storage adapters under the accepted architecture guidance.

Alternative considered: create a dedicated persistence project immediately. That may become useful later, but Phase 0 does not yet need another project boundary.

### Persist an explicit structured schema instead of a single JSON document

The database should use tables for Phase 0 core entities and relationship join records. This supports contributor understanding, foreign key enforcement, future filtering, and incremental migrations.

Alternative considered: save the entire workspace as one JSON blob in SQLite. That would be simpler initially but would weaken relationship validation and make Phase 1 organization features more expensive.

### Preserve flexible metadata as JSON text columns

Core entities should have stable columns for identity, names, relationships, timestamps, and state, while flexible metadata can be stored as JSON text. This gives early features room to evolve without making every optional field a schema migration.

Alternative considered: create typed columns for every possible future metadata value. That would overfit Phase 0 to workflows that are still being specified.

### Use transactions for save operations

Saving workspace data should run inside a transaction so partial writes do not leave the local database in a mixed state. The first implementation can replace the persisted snapshot atomically enough for Phase 0, with more granular commands added later if needed.

Alternative considered: write each entity independently without a shared transaction. That increases avoidable data-loss risk if an operation fails mid-save.

### Add minimal schema versioning before migrations grow

The database should record a schema version and apply known migrations in order. At Phase 0 this can be intentionally small, but contributors need a clear place to add future schema changes and tests.

Alternative considered: rely only on `CREATE TABLE IF NOT EXISTS`. That can initialize a database but does not safely handle later column, table, or relationship changes.

## Risks / Trade-offs

- [Risk] Snapshot replacement can become inefficient as workspaces grow -> Mitigation: accept it for Phase 0 and keep the repository contract narrow enough to evolve toward command-level persistence later.
- [Risk] Flexible metadata can hide important fields from typed tests -> Mitigation: keep core relationships and workflow-critical state in explicit columns.
- [Risk] Schema changes may accidentally damage local data -> Mitigation: add schema version tracking, migration tests, and transactional migration behavior.
- [Risk] Database paths and workspace paths can become confused -> Mitigation: limit this change to structured data and store file references only; workspace file ownership is specified separately.
- [Risk] FC-0003 implementation may begin before FC-0002 is archived -> Mitigation: map persistence to the active core model and adjust during review if accepted domain names shift.

## Migration Plan

- Create new databases with the current schema version.
- Enable foreign key enforcement for each SQLite connection.
- Record schema version in a small metadata table or SQLite `user_version`.
- Apply future migrations only from known older versions to the current version inside a transaction.
- Refuse to open databases from newer schema versions with a clear application-level error rather than attempting unsafe writes.

## Open Questions

- Should Phase 0 use SQLite `PRAGMA user_version` or an explicit metadata table for schema versioning?
- Should invalid or malformed metadata JSON be rejected on save, normalized to `{}`, or preserved as opaque text?
- Should archived records remain in the same tables with flags for Phase 0, or should later features introduce separate archival behavior?
