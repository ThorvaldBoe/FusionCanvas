## 1. Persistence Contract

- [x] 1.1 Review the active `FusionCanvas.Domain` workspace model and confirm the persistence snapshot includes Store, Niche, Group, Listing, Asset, Prompt, Tag, and relationship records needed by FC-0003.
- [x] 1.2 Add or update the `FusionCanvas.Application` workspace persistence contract for saving and loading structured workspace data.
- [x] 1.3 Ensure UI-facing code depends on the application contract rather than concrete SQLite types.

## 2. SQLite Integration

- [x] 2.1 Add the SQLite provider dependency to `FusionCanvas.Integration` if it is not already present.
- [x] 2.2 Implement the SQLite workspace repository in `FusionCanvas.Integration`.
- [x] 2.3 Create tables for Phase 0 core entities and relationship records with understandable names and foreign key relationships.
- [x] 2.4 Persist stable IDs, names, optional descriptions, timestamps, archive flags, status values where present, and metadata fields.
- [x] 2.5 Persist asset file references without embedding binary file contents in SQLite.
- [x] 2.6 Load missing database paths as an empty workspace.

## 3. Save Safety and Schema Versioning

- [x] 3.1 Wrap save operations in a transaction so failed saves do not commit partial workspace snapshots.
- [x] 3.2 Enable SQLite foreign key enforcement for repository connections.
- [x] 3.3 Add schema version tracking using `PRAGMA user_version` or an explicit metadata table.
- [x] 3.4 Add migration handling for known older schema versions and a guarded error for newer unsupported schema versions.
- [x] 3.5 Add tests for current, older, and newer schema-version behavior.

## 4. Persistence Tests

- [x] 4.1 Add application-layer tests that verify persistence is consumed through the application contract.
- [x] 4.2 Add integration tests proving save/load round-trips all Phase 0 core entity types.
- [x] 4.3 Add integration tests proving store, niche, group, listing, asset, prompt, and tag relationships survive save/load.
- [x] 4.4 Add integration tests proving metadata, archive state, timestamps, and file reference fields survive save/load.
- [x] 4.5 Add an integration test proving a failed save does not leave a partially committed workspace snapshot.
- [x] 4.6 Add tests or assertions that domain code does not reference SQLite packages or persistence adapters.

## 5. Validation

- [x] 5.1 Run `dotnet test` for the solution or the affected application and integration test projects.
- [x] 5.2 Run `openspec status --change "fc-0003-local-sqlite-persistence"` and verify the change is apply-ready.
- [x] 5.3 Update task checkboxes to reflect completed implementation work before archive.
