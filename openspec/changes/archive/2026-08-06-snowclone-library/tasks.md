## 1. Domain model and template policy

- [x] 1.1 Add the global `Snowclone` domain record with ID, Phrase, Guidance, CreatedAt, and UpdatedAt only, outside `WorkspaceEntity` ownership.
- [x] 1.2 Implement the pure brace-placeholder scanner and normalization result so valid display text is preserved while empty/unbalanced/nested placeholders, missing placeholders, newlines, and blank guidance return explicit validation errors.
- [x] 1.3 Implement the canonical duplicate key: trim outer phrase whitespace, collapse Unicode whitespace runs, and compare all phrase text including placeholder names case-insensitively.
- [x] 1.4 Add Domain tests covering valid single/named/repeated/multiple placeholders, every invalid structure, blank guidance, stored-text preservation, and whitespace/placeholder-case duplicate equivalence.
- [x] 1.5 Run `dotnet test .\tests\FusionCanvas.Domain.Tests\FusionCanvas.Domain.Tests.csproj` and correct Domain behavior or approved artifacts for any failed snowclone criterion.

## 2. Application contracts and snowclone use cases

- [x] 2.1 Add the `Snowclones` application capability contracts: focused repository snapshot with starter marker, library service, CRUD requests/results/state, CSV rows/read-write results, bundled-source port, and CSV codec port without SQLite, filesystem, or Avalonia types.
- [x] 2.2 Implement load and in-memory projection with invariant alphabetical phrase order and ordinal case-insensitive substring filtering across phrase and guidance.
- [x] 2.3 Implement create and update with domain validation, normalized duplicate prevention, injected clock/ID delegates, identity/CreatedAt preservation, and one focused atomic save.
- [x] 2.4 Implement permanent delete of an existing ID and return confirmed state suitable for deterministic post-delete selection by the App layer.
- [x] 2.5 Implement first-use initialization: parse/preflight the bundled CSV, add unique rows and the completed marker in one save, no-op after completion, and retain a false marker/no partial rows on failure.
- [x] 2.6 Implement explicit bundled-library import and user CSV import as parse-all → validate-all → partition unique/duplicates → one save, preserving existing guidance and reporting added/skipped counts.
- [x] 2.7 Implement export preparation as exact alphabetical Phrase/Guidance rows and ensure expected validation failures return recoverable results rather than exceptions.
- [x] 2.8 Add Application tests for CRUD, identity/timestamps, validation and collisions, search, delete, one-time marker behavior, deleted-starter non-resurrection, explicit bundled merge, document-level rejection, duplicate-only import, cancellation, and failing-repository atomicity.
- [x] 2.9 Run `dotnet test .\tests\FusionCanvas.Application.Tests\FusionCanvas.Application.Tests.csproj` and correct Application behavior or approved artifacts for any failed snowclone criterion.

## 3. Shared SQLite schema and focused repository

- [x] 3.1 Extract the existing SQLite schema/version/migration authority into one internal Integration component shared by workspace and snowclone repositories without changing migrations 1 through 5.
- [x] 3.2 Add the transactional v5 → v6 migration for `snowclones` with unique normalized phrase and the singleton `snowclone_library_state` marker initialized false; update fresh schema creation and newer-version refusal.
- [x] 3.3 Implement `SqliteSnowcloneRepository` load/save against the configured application database, replacing only snowclone/state rows in one transaction and never touching workspace tables.
- [x] 3.4 Wire the focused repository and library service through the application composition/runtime using the same database path while keeping `WorkspaceSnapshot` unchanged.
- [x] 3.5 Add Integration tests for fresh v6 schema, populated v5 migration, both repository opening orders, round trip, unique defense, rollback, newer-version refusal, workspace-save preservation of snowclones, and snowclone-save preservation of all workspace content.
- [x] 3.6 If workspace-transfer implementation is present, add or update its round-trip/entity-count tests to prove workspace packages contain no snowclone rows or counts; otherwise record this dependency explicitly in verification evidence.
- [x] 3.7 Run the focused Integration persistence tests and correct migration/coexistence behavior before adding UI composition.

## 4. CSV codec and bundled starter resource

- [x] 4.1 Add the embedded UTF-8 `starter-snowclones.csv` with exact `Phrase,Guidance` headers and the approved `Easily distracted by {X}` row/guidance; configure it as a shipped embedded resource.
- [x] 4.2 Implement the bundled-source adapter and make a missing/unreadable embedded resource a recoverable initialization failure.
- [x] 4.3 Implement strict UTF-8 CSV reading with the runtime quoted-field parser, exact case-sensitive ordered headers, structural diagnostics/row numbers, quoted commas, escaped quotes, and multiline guidance.
- [x] 4.4 Implement deterministic CSV writing with exact headers, alphabetical rows supplied by Application, and RFC 4180 escaping without identity/timestamp columns.
- [x] 4.5 Add Integration tests for BOM/no-BOM UTF-8, LF/CRLF, comma/quote/multiline round trips, malformed CSV, missing/reordered/extra/case-changed headers, empty input, deterministic output, cancellation, and the actual embedded starter resource.
- [x] 4.6 Run the focused Integration CSV/resource tests and correct codec or approved CSV-contract artifacts for any failed criterion.

## 5. Dialog view model and storage-provider adapter

- [x] 5.1 Add the App snowclone capability area and `SnowcloneLibraryViewModel` with separate confirmed selection and editable draft, live search, empty/no-results projection, summaries/errors, and observable command state.
- [x] 5.2 Implement New/Save/Delete state: blank drafts discard silently, meaningful drafts remain unpersisted until Save, Delete is persisted-record-only, confirmation is explicit, and successful deletion chooses next visible then previous visible selection.
- [x] 5.3 Implement pending selection/import/bundled-import/close transitions with Save/Discard/Cancel; resume only after successful Save or explicit Discard and preserve draft/selection/focus intent on Cancel or failure.
- [x] 5.4 Implement initialization, import, export, and bundled-import commands with busy/conflict guards, picker cancellation as a no-op, result summaries, and retry-preserving error recovery.
- [x] 5.5 Add `ISnowcloneCsvFilePicker` and its Avalonia `StorageProvider` adapter with `.csv` open/save filters and stream lifetime/error handling, keeping storage types out of Application.
- [x] 5.6 Add framework-free App tests for command enablement, search/selection coherence, create/update/delete aftermath, blank and meaningful drafts, all pending-transition decisions, busy duplicate prevention, picker cancellation, summaries, and recoverable errors.
- [x] 5.7 Run the focused App view-model tests and correct interaction state or approved artifacts for any failed criterion.

## 6. Avalonia Snowclone Library dialog

- [x] 6.1 Add the compiled-binding `SnowcloneLibraryWindow` with compact search/import/export/bundled-import commands, filtered list and New action, phrase/guidance editor, Save/Delete/Close actions, and semantic theme resources.
- [x] 6.2 Bind and visually distinguish initial loading, empty library, no search results, busy progress, inline summary/error, unsaved Save/Discard/Cancel, and delete-confirmation states.
- [x] 6.3 Implement code-behind only for ownership, `StorageProvider` picker attachment, focus transitions, and close interception; ensure New focuses Phrase and confirmations return focus meaningfully.
- [x] 6.4 Expose a constructible/factory boundary for the future ideation owner, and verify no Snowclone Library launcher is added to MainWindow or Settings.
- [x] 6.5 Add Avalonia headless tests for window construction/compiled bindings, required visual tree, initial preselection/empty state, live search/no-results, filtered-selection draft coherence, New focus, action enablement, busy/error states, confirmation flows, keyboard reachability, and absent current launcher.
- [x] 6.6 Run `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj` and correct UI-owned behavior or approved artifacts for any failed criterion.

## 7. Criterion-level verification and completion gates

- [x] 7.1 Create `verification.md` and map all 35 acceptance scenarios in `specs/snowclone-library/spec.md` to exact automated test names, results, and evidence; record workspace-transfer coordination status and any explicit limitations.
- [x] 7.2 Run focused Domain, Application, Integration, and App snowclone tests; for every failed criterion, correct implementation or approved artifacts and rerun that criterion plus relevant regressions.
- [x] 7.3 Run `dotnet test .\FusionCanvas.sln` and record the complete deterministic baseline result in `verification.md`.
- [x] 7.4 Run strict validation for `snowclone-library` using the installed OpenSpec CLI syntax, then run the repository-required validation scope; correct all errors and record commands/results.
- [x] 7.5 Perform changed-scope drift review against the accepted architecture, local SQLite persistence, UI/UX, testing, and security requirements, including global ownership, package exclusion, starter-resource shipping, exact CSV behavior, no temporary launcher, and no out-of-scope ideation behavior.
- [x] 7.6 Review warnings, project/package changes, and public-repository safety; confirm no secrets, untrusted path use, spreadsheet execution, unnecessary dependency, or unrelated refactor entered the change.
- [x] 7.7 Optionally perform the documented isolated live-dialog/file-picker observation only if it provides additional native-platform or visual evidence; remove any temporary launcher/harness and keep the acceptance verdict based on deterministic evidence.
- [x] 7.8 Complete final criterion-level acceptance review; do not mark the module complete while any scenario lacks passing evidence or an explicitly approved artifact correction.
