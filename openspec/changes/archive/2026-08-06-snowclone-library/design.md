## Context

FusionCanvas currently stores all workspaces in one application-local SQLite database through `IWorkspaceRepository` and `SqliteWorkspaceRepository`. The persisted `WorkspaceSnapshot` contains only workspace-owned entities. There is no application-wide creative library contract, no CSV interchange layer, and no implemented ideation dialog.

The canonical data model already describes phrases and snowclones as future creative vocabulary and explicitly leaves global phrase libraries open. This module resolves that boundary: snowclones are global reusable input for later ideation, not children of whichever workspace happens to be active.

The current SQLite schema is version 5 and schema creation/migration is private to `SqliteWorkspaceRepository`. The active `workspace-transfer` change plans to create filtered temporary databases through that repository. A snowclone migration must therefore use the same schema-version authority while ensuring workspace packages never carry global content.

The user-facing surface is occasional library administration. Per the UI/UX guidelines it belongs in a focused dialog, not persistent main-window space. The future ideation dialog will own the launch action; this module must leave a constructible, headless-testable dialog without inventing a temporary route.

Stakeholders are creators maintaining reusable phrase structures, future ideation-tool implementers consuming the library, maintainers curating the bundled CSV, and contributors evolving the SQLite schema.

## Goals / Non-Goals

**Goals:**

- Persist one application-wide library of validated snowclones in local SQLite.
- Provide atomic CRUD behavior, normalized duplicate prevention, deterministic list/search projection, and recoverable failures.
- Define a parseable brace-placeholder convention without implementing substitution.
- Ship an editable starter record now and a replaceable two-column bundled CSV for future curated builds.
- Initialize bundled content once without silently restoring deletions or overwriting edits; allow explicit additive import of the current bundle.
- Import and export exact `Phrase,Guidance` UTF-8 CSV with correct quoting and document-level validation.
- Provide a complete focused management dialog with draft protection, confirmations, busy/error/empty states, and keyboard behavior.
- Keep single-workspace transfer and deletion isolated from global library content.
- Verify all observable behavior deterministically, including the Avalonia visual tree and interaction state that carry framework risk.

**Non-Goals:**

- Opening the dialog from a current main-window or Settings command.
- Building the ideation dialog, candidate generation, random selection, placeholder substitution, prompt construction, or AI integration.
- Associating a snowclone with a workspace, store, niche, item, concept, generated candidate, or history record.
- Categories, tags, favorites, ordering controls, archive/restore, usage analytics, cloud sync, or whole-application backup.
- Automatically synchronizing later bundled CSV changes into an initialized user library.
- Arbitrary CSV mappings, extra columns, alternative header names, Excel application control, or spreadsheet editing.

## Decisions

### D1 — Snowclones are global domain records, not `WorkspaceEntity` records

Add `FusionCanvas.Domain.Snowclones.Snowclone` with:

```text
Id
Phrase
Guidance
CreatedAt
UpdatedAt
```

It does not inherit `WorkspaceEntity` and has no workspace/store foreign key, archive flag, description alias, or metadata JSON. `Phrase` is the user-visible template and list label; `Guidance` is the explanatory creative instruction.

*Rationale:* global ownership is the behavior the future ideation tool needs, and the record has no current lifecycle or extensibility requirement that justifies inherited workspace fields.

*Alternatives:* store- or workspace-owned phrases duplicate common creative knowledge and leak into workspace transfer; reusing `Prompt` confuses reusable input vocabulary with contextual prompt history.

### D2 — Use a dedicated library repository against the existing SQLite file

Add application contract `ISnowcloneRepository` with load/save of a focused `SnowcloneLibrarySnapshot`:

```text
Snowclones
StarterLibraryInitialized
```

`SqliteSnowcloneRepository` implements it against the same database path as `SqliteWorkspaceRepository`. Saving replaces only `snowclones` and the singleton library-state row inside one transaction; it never reads, deletes, or reinserts workspace tables. Application use cases follow the existing load → pure mutation → one save pattern.

Schema ownership must remain singular. Extract the current schema creation/version logic into an internal Integration helper such as `SqliteDatabaseSchema` used by both repositories, bump the schema from 5 to 6, and add:

```sql
snowclones(
  id TEXT PRIMARY KEY,
  phrase TEXT NOT NULL,
  normalized_phrase TEXT NOT NULL UNIQUE,
  guidance TEXT NOT NULL,
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL
)

snowclone_library_state(
  singleton_id INTEGER PRIMARY KEY CHECK(singleton_id = 1),
  starter_initialized INTEGER NOT NULL
)
```

The v5 → v6 migration is additive and inserts the singleton state as not initialized. Schema migration never imports starter data; application initialization owns that behavior. `WorkspaceSnapshot` remains unchanged, and workspace snapshot saves do not touch either snowclone table.

*Rationale:* one file preserves the current local-data deployment model and schema compatibility chain, while a focused contract prevents global data from becoming workspace-owned.

*Alternatives:* adding snowclones to `WorkspaceSnapshot` makes workspace transfer semantics wrong; a second database adds path, backup, migration, and composition complexity without a demonstrated need; granular SQL CRUD contracts make atomic multi-row import and initialization harder to keep consistent.

### D3 — Centralize phrase validation and duplicate normalization in Domain

Add a pure `SnowcloneTemplatePolicy` (exact final type name may follow the completed capability-folder reorganization) that:

1. trims outer phrase whitespace;
2. rejects CR or LF;
3. scans the phrase once, accepting each `{...}` span only when it is balanced, non-nested, and contains non-whitespace text;
4. requires at least one accepted placeholder;
5. trims outer guidance whitespace and requires nonblank guidance;
6. creates a duplicate key by collapsing every Unicode whitespace run to one ASCII space and applying invariant case folding to the entire phrase, including placeholder names.

The stored phrase preserves meaningful internal spacing and placeholder spelling after outer trim; only the duplicate key collapses/folds. Repeated placeholders and any nonempty placeholder text without braces are valid. Literal braces and nested placeholders have no escape syntax in this version.

The database unique constraint on `normalized_phrase` is defense in depth. Application validation produces user-facing errors before persistence.

*Rationale:* a small scanner is clearer and safer than a permissive regular expression for unmatched/nested braces, and the policy is usable by CRUD, starter initialization, and CSV import.

### D4 — CRUD uses one focused application service and immutable state/results

Add `ISnowcloneLibraryService` / `SnowcloneLibraryService` with operations to initialize, load/search, create, update, delete, import parsed records, and prepare export records. Follow current management-service conventions:

- injectable clock and ID delegates for deterministic tests;
- request/result/state records rather than exceptions for expected validation failures;
- exceptions from persistence translated at the App boundary into recoverable error state while confirmed data is reloaded or retained;
- create/update validate phrase and guidance, then enforce duplicate key against all other records;
- delete requires an existing ID; confirmation remains a UI responsibility, while the service performs one atomic permanent removal;
- state sorts by phrase with an invariant, case-insensitive comparison and filters in memory across phrase and guidance with ordinal case-insensitive substring matching.

Search remains an in-memory projection because the library is expected to be modest and no advanced search requirement exists. No FTS table or index is added.

### D5 — Bundled CSV is the single curated-content source

Ship an embedded UTF-8 resource at a stable Integration-owned location such as:

`src/FusionCanvas.Integration/Snowclones/Resources/starter-snowclones.csv`

Initial content:

```csv
Phrase,Guidance
"Easily distracted by {X}","Replace {X} with something the target audience is enthusiastically obsessed with, such as dogs, books, coffee, or gardening."
```

An `IBundledSnowcloneSource` application port supplies a stream; an Integration implementation opens the embedded resource. Application startup calls `InitializeAsync` once through composition:

1. load the focused snapshot;
2. if `StarterLibraryInitialized` is true, do nothing;
3. parse and validate the entire bundled CSV using the normal import pipeline;
4. merge unique rows without overwriting any existing record;
5. save the rows and `StarterLibraryInitialized = true` in one transaction.

Invalid bundled data or save failure leaves the marker false and saves no rows, allowing retry and making a faulty build diagnosable. Later builds may replace the embedded CSV. They do not auto-sync an initialized library. “Import bundled library” explicitly reruns additive merge, skips normalized duplicates, preserves local guidance, and reports counts. Because the marker remains true after user deletion, startup never resurrects deleted content.

*Alternatives:* automatic synchronization needs stable curated IDs, tombstones, conflict policy, and update semantics that the required two-column source cannot express; seeding whenever the table is empty resurrects intentional deletion; compiling records into C# makes curation unnecessarily technical.

### D6 — Use one exact CSV codec without a new external package

Add `ISnowcloneCsvCodec` as an application-facing stream contract and implement it in Integration. Use the runtime `Microsoft.VisualBasic.FileIO.TextFieldParser` with strict UTF-8 decoding for standards-compliant quoted-field reading, including commas, escaped quotes, CRLF/LF, and multiline guidance. Write with `StreamWriter` and a small deterministic RFC 4180 field escaper that doubles quotes and quotes any field containing comma, quote, CR, or LF.

The codec:

- requires exactly two case-sensitive headers in order: `Phrase`, `Guidance`;
- returns source row numbers when parser information permits;
- treats a structurally unreadable document as invalid;
- emits the exact header and alphabetical data rows;
- leaves semantic phrase/guidance validation and duplicate decisions to the application service.

The import use case parses the whole document, semantically validates every row, and calculates all additions/skips before one repository save. Any structural or semantic error rejects the entire document. Duplicates against local data or an earlier valid row are skipped, not errors. A persistence error or cancellation leaves the previous library.

*Rationale:* the platform parser covers the CSV edge cases that are unsafe to hand-roll, while a two-column writer is trivial and testable. This avoids a new NuGet dependency and keeps CSV technology in Integration.

### D7 — The dialog is a focused list/editor with explicit drafts

Add an App capability area containing likely types:

- `SnowcloneLibraryViewModel`
- `SnowcloneLibraryWindow.axaml` / `.axaml.cs`
- `ISnowcloneCsvFilePicker` and `AvaloniaSnowcloneCsvFilePicker`
- small dialog-owned summary/draft presentation records as needed

Layout:

```text
Snowclone Library
|-- Search
|-- Import CSV | Export CSV | Import bundled library
|-- Left: filtered snowclone list + New
`-- Right: phrase field, multiline guidance, Save, Delete, status/error
```

Actions use compact/content-based widths. Search is focused on ordinary open when records exist; New puts focus in the single-line phrase field. Existing selection is preselected sensibly; a filtered-out selected record is retained in the editor until the user deliberately selects another visible record or clears the search, preventing silent draft loss.

New creates an in-memory draft. Save is enabled only for a meaningful changed draft that is not busy; Delete is enabled only for a persisted selection. Deletion uses an inline/view-model confirmation state so it is deterministic under headless Avalonia. After confirmed deletion, select the next visible record, then previous, otherwise show empty/no-results.

Meaningful unsaved changes before selection change, import, bundled import, or close open a Save/Discard/Cancel decision state:

- Save validates and persists, then resumes the pending transition only on success.
- Discard restores confirmed state and resumes the transition.
- Cancel keeps the draft and current focus context.
- A blank untouched new draft is discarded without prompting.

Import/export pickers are App abstractions over Avalonia `StorageProvider`; cancellation is a no-op. Busy state disables conflicting mutation and duplicate submissions. Load/init/import/export/save/delete errors are shown inline and retain confirmed state plus recoverable input.

### D8 — Build the dialog now but give it no temporary product entry point

`SnowcloneLibraryWindow` is constructible with its view model and can be shown modally by a future owner. The composition root creates the repository/service dependencies and can expose a factory/controller needed for later ideation integration, but neither `MainWindow.axaml` nor Settings gains a button or menu item in this module.

Headless tests instantiate the view and drive its view model/commands directly. This is an intentional platform outcome: the management component is complete and independently verified, while discoverability becomes part of the future ideation delivery module.

*Alternative:* a temporary Settings launcher makes this module manually reachable but creates product navigation that the requester explicitly said will belong to ideation and would later need removal.

### D9 — Workspace transfer contains schema compatibility, never global content

Version-6 databases used as workspace-transfer payloads may contain empty snowclone tables because they share the schema. Filtering/writing a `WorkspaceSnapshot` never loads or saves snowclone content, entity counts omit it, and importing a workspace package never merges it into the live library. Starter initialization is not part of schema opening, so opening a temporary package database cannot seed it.

This behavior must be covered whether `workspace-transfer` lands before or after this module. No delta is made to that active change from this proposal.

### D10 — Verification is deterministic and layered

Domain tests own template scanning and duplicate normalization. Application tests own CRUD, initialization, merge/skip, search projection, state, atomic orchestration, and deterministic timestamps/IDs with fakes. Integration tests own v5 → v6 migration, coexistence with workspace saves, SQLite round trips/unique constraint, embedded-resource parsing, and CSV quoting/encoding. App tests own command state and headless view risks: construction, compiled bindings, named controls, selection, focus, search filtering, prompts, busy state, and no current launcher.

An optional live desktop observation can assess visual density and native file-picker behavior after implementation, but it is supplemental and cannot change the acceptance verdict.

## Risks / Trade-offs

- **[Two repositories share one SQLite schema]** → centralize schema creation/versioning in one internal helper and integration-test both opening orders plus workspace/snowclone save coexistence.
- **[A later workspace-transfer implementation accidentally copies global rows]** → keep snowclones out of `WorkspaceSnapshot`, document D9, and test filtered package behavior when that capability is present.
- **[Curated corrections do not reach initialized users automatically]** → preserve local ownership; ship the latest full CSV for new installs and provide explicit additive bundled import. A future sync policy requires its own stable-ID/conflict design.
- **[Deleting all rows could look like an uninitialized library]** → persist initialization state independently of row count.
- **[Malformed or hostile CSV causes partial state or excessive ambiguity]** → strict headers/UTF-8, complete preflight, row diagnostics, atomic save, cancellation checks, and no formula evaluation.
- **[CSV formula injection when exported files are opened elsewhere]** → snowclone text is creative user data and must round-trip exactly; FusionCanvas does not evaluate formulas. Do not silently prefix or mutate content. Document that export is data, not an executable spreadsheet.
- **[In-memory search/import scales poorly for an unexpectedly huge library]** → accept for this module; CSV parsing is streamed but validated records are held until atomic commit. Add indexing/batching only after real scale evidence.
- **[Dialog has no current launcher]** → make the boundary explicit, construction/headless-test it now, and require the ideation module to own discoverability.
- **[Schema rollback]** → migration is additive and transactional. If an application rollback to schema-5 code occurs, its existing refuse-newer safeguard prevents unsafe writes; restoring a pre-migration database backup is the only downgrade path.

## Migration Plan

1. Introduce shared schema management and the additive v5 → v6 migration in Integration.
2. On first application startup after upgrade, schema opening creates empty snowclone tables and a false initialization marker.
3. Application composition runs library initialization; valid bundled content and the true marker commit together.
4. Existing workspace content is not rewritten by the focused library repository. Existing workspace repository behavior and tests remain unchanged except for using shared schema management.
5. If initialization fails, startup remains usable where current application behavior permits, the marker stays false, and the recoverable library error is available to the future dialog/diagnostics; the next initialization attempt retries.
6. Rollback does not attempt a destructive downgrade. Schema-5 builds safely refuse the version-6 database; users restore a pre-upgrade backup if they must return to an older build.

## Implementation Plan

### 1. Domain

- Add `src/FusionCanvas.Domain/Snowclones/Snowclone.cs`.
- Add a pure template policy/parser under the same capability folder. Return normalized values and explicit validation failures rather than throwing for expected user input.
- Implement canonical duplicate-key generation in the policy so Application and Integration use one definition.
- Add `tests/FusionCanvas.Domain.Tests/Snowclones/SnowcloneTemplatePolicyTests.cs` for valid single/named/repeated/multiple placeholders, every invalid brace/newline case, guidance validation, whitespace normalization, placeholder-case equivalence, and preservation of display text.

### 2. Application contracts and service

- Add a `Snowclones` capability area with `ISnowcloneRepository`, `SnowcloneLibrarySnapshot`, `ISnowcloneLibraryService`, state/summary/request/result records, `ISnowcloneCsvCodec`, CSV row/read/write records, and `IBundledSnowcloneSource`.
- Implement `SnowcloneLibraryService` with injected repository, codec, bundled source, clock, and ID factory. Keep file-system and Avalonia types out of the contracts.
- Sequence initialization and imports as parse → semantic preflight → duplicate partition → one focused save. Preserve the marker rules and return counts/errors in result records.
- Implement CRUD and in-memory search/sort projection. Update preserves ID/CreatedAt; create uses one timestamp for CreatedAt/UpdatedAt; delete is permanent.
- Add application tests with deterministic fakes for every CRUD, validation, duplicate, search, initialization, bundled-import, atomic-failure, and cancellation path.

### 3. Integration persistence and migration

- Extract schema-version creation/migration from `SqliteWorkspaceRepository` into a shared internal schema component without changing existing schema-1-through-5 behavior.
- Bump to schema 6; add snowclone/state tables and the v5 → v6 additive migration in one transaction where supported by the existing migration idiom.
- Implement `SqliteSnowcloneRepository` using the same database path. Load ordered records/state; validate duplicate keys before save; replace only focused tables in one transaction.
- Update `AppWorkspaceFactory` (or its post-reorganization equivalent) to construct both repositories against the same configured database path and expose the snowclone service/runtime dependency without placing snowclones in `WorkspaceSnapshot`.
- Add isolated Integration tests for fresh schema, v5 migration with populated workspace data, both repository opening orders, snowclone round trip, unique constraint, failed-save rollback, workspace save preserving snowclones, snowclone save preserving workspaces, and newer-version refusal.
- If workspace-transfer code is present at implementation time, add/adjust its integration test to assert global rows are absent after a workspace package round trip and package entity counts exclude them.

### 4. CSV and bundled resource

- Add the embedded `starter-snowclones.csv` with exact headers and initial row under the Integration Snowclones capability area; configure the project resource explicitly.
- Implement the stream CSV codec with strict UTF-8, `TextFieldParser` reads, deterministic escaping writes, exact headers, row diagnostics, and cancellation checks at row boundaries.
- Implement the bundled source by opening the embedded resource; missing resource is a recoverable initialization failure.
- Add Integration tests for BOM/no-BOM UTF-8, commas, quotes, CRLF/LF, multiline guidance, exact header rejection (missing/reordered/extra/case-changed), malformed quotes, empty document, deterministic ordering, round trip, cancellation, and successful parsing of the actual embedded starter resource.

### 5. App view model and file picker

- Add `SnowcloneLibraryViewModel` following existing hand-rolled MVVM/command conventions. Separate confirmed selected record from editable draft; model pending transitions for selection/import/bundled import/close.
- Add an App-owned `ISnowcloneCsvFilePicker` and Avalonia adapter using `StorageProvider` with `.csv` filters, read/write streams, cancellation, and no Avalonia storage types in Application.
- Implement load/initialize, live search, New/Save/Delete, import/export/bundled import, result summary, error, busy, unsaved-decision, and delete-confirmation command/state behavior.
- Ensure picker cancellation does not mutate state and that failed save/import/export retains retry input.
- Add framework-free view-model tests for state transitions, command enablement, selection after create/delete/search, pending transitions, blank-draft dismissal, busy duplicate prevention, picker cancellation, summaries, and error recovery.

### 6. Avalonia focused dialog

- Add `SnowcloneLibraryWindow.axaml` and code-behind only for view ownership, focus coordination, close interception, and setting the picker from the window `StorageProvider`.
- Use compiled bindings and semantic theme resources. Implement the compact list/editor layout, search, empty/no-results states, busy indicator, inline error/summary, Save/Discard/Cancel prompt, and delete confirmation.
- Provide a constructible/factory boundary for future ideation ownership. Do not add a launcher to MainWindow or Settings.
- Add headless view tests for construction/bindings, required control tree, list selection/search, focus on New, action enablement, confirmation states, busy/error presentation, keyboard reachability/routed commands, and absence of a current launcher.

### 7. Completion verification

- Create `verification.md` during apply/verify and map each scenario below to exact test names/results.
- Run focused layer tests while implementing, then `dotnet test .\FusionCanvas.sln`.
- Run `openspec validate snowclone-library --strict` using the CLI-supported strict syntax discovered at implementation time, plus repository-wide validation if required by the workflow.
- Perform changed-scope drift review for architecture, SQLite/workspace-transfer separation, bundled resource shipping, CSV contract, UI guidelines, and security.
- Optional supplemental desktop check: open the dialog through a temporary test harness only if useful, verify visual density and native CSV pickers with an isolated database, then remove the harness. It is not acceptance evidence and must not become a product launcher.

### Decisions not to reopen during implementation

- Global application ownership and storage in the existing SQLite file through a focused repository.
- Exclusion from `WorkspaceSnapshot` and workspace transfer content.
- Record fields: ID, Phrase, Guidance, CreatedAt, UpdatedAt only.
- Brace-delimited validation rules and normalized phrase uniqueness.
- Exact two-column, case-sensitive, ordered `Phrase,Guidance` CSV contract.
- Complete-document validation, duplicate skipping, and one atomic import save.
- Embedded CSV as the curated source, one-time automatic initialization, no silent later sync, explicit additive bundled import.
- Permanent confirmed deletion with no archive state.
- Focused list/editor dialog, explicit drafts, and no temporary main-window/Settings launcher.
- No ideation, substitution, AI, categorization, association, or usage tracking in this module.

## Acceptance-to-Verification Mapping

| Acceptance scenario | Planned verification |
|---|---|
| Snowclone survives application data reload | SQLite repository round-trip Integration test |
| Workspace lifecycle does not affect snowclones | Integration coexistence tests; workspace-transfer package test when present |
| Snowclone operation fails during persistence | Application failing-repository atomic-state test plus SQLite rollback test |
| Phrase contains one valid placeholder | Domain policy test |
| Phrase contains named, repeated, or multiple placeholders | Domain parameterized policy tests |
| Phrase has invalid placeholder structure | Domain parameterized rejection tests |
| Guidance is missing | Domain validation plus Application recoverable-result test |
| Create duplicates an existing phrase | Application duplicate-create test |
| Edit collides with another phrase | Application duplicate-update/draft-preservation test |
| Creator saves a new snowclone | Application deterministic ID/timestamp test; App view-model state test |
| Creator updates an existing snowclone | Application identity/timestamp preservation test |
| Creator cancels or abandons a blank draft | App view-model transition test |
| Creator confirms snowclone deletion | Application delete test plus App selection-aftermath/headless confirmation test |
| Creator cancels snowclone deletion | App view-model/headless focus restoration test |
| Search matches phrase | Application projection and App binding tests |
| Search matches guidance | Application projection and App binding tests |
| Search has no matches | App view-model and headless no-results-state tests |
| Snowclone library initializes for the first time | Application initialization test plus actual embedded-resource/SQLite integration test |
| Creator deletes the initial starter record | Application marker persistence and reload test |
| Creator imports the bundled library explicitly | Application additive-merge/count/preserve-guidance tests |
| Bundled starter data is invalid | Application invalid-bundle atomicity/marker test |
| Creator exports the library | Integration exact-header/order/round-trip tests plus App picker-command test |
| Creator imports a valid CSV | CSV Integration parsing plus Application atomic-import/count tests |
| CSV header or row is invalid | Integration structural diagnostics and Application semantic preflight/no-save tests |
| CSV contains only duplicates | Application zero-add/skipped-summary test |
| Creator cancels a CSV picker | App view-model picker-cancel state-preservation tests |
| Future owner opens the Snowclone Library dialog | Avalonia headless construction/load/empty/preselection tests |
| Dialog has an active search | App view-model and headless selection/draft coherence tests |
| Library operation is running | App view-model command-state and headless busy-state tests |
| Dialog operation fails | App view-model/headless error and recoverable-input tests |
| Contributor reviews current entry points | App structural test/assertion that MainWindow and Settings expose no launcher |
| Creator leaves meaningful unsaved edits | App view-model Save/Discard/Cancel transition tests and headless prompt test |
| Creator starts a new draft | App view-model draft test and Avalonia headless focus test |
| Creator completes or cancels a confirmation | Avalonia headless keyboard/focus/routed-interaction tests |
| Contributor reviews module scope | Changed-scope review of references/projects plus strict OpenSpec validation |

## Open Questions

None. Global ownership, placeholder syntax, duplicate policy, starter lifecycle, CSV columns, dialog placement, and deferred ideation scope were resolved during discovery.
