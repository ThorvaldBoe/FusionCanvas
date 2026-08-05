## Why

FusionCanvas needs a reusable vocabulary of familiar phrase structures so future ideation tools can generate varied ideas without embedding that creative knowledge in UI or AI-provider code. Creators also need local control over that vocabulary: they must be able to maintain it, search it, and move it through a simple portable format before the ideation dialog is built.

## What Changes

- Add an application-wide snowclone library persisted locally in SQLite and deliberately kept outside workspace, store, niche, and item ownership.
- Define snowclones with stable identity, a required phrase template, required guiding text, and created/updated timestamps.
- Require brace-delimited placeholders such as `{X}` in every phrase template; allow named, repeated, and multiple placeholders while rejecting empty, unbalanced, or nested placeholders.
- Add application business logic for create, list/search, update, and confirmed permanent delete, including phrase normalization, validation, duplicate prevention, atomic persistence, and recoverable failures.
- Add a focused Snowclone Library dialog with live search across phrase and guidance, list-and-editor CRUD, explicit drafts and saves, unsaved-change protection, keyboard/focus behavior, empty/no-result/busy/error states, and confirmed deletion.
- Add UTF-8 CSV import and export using exactly the headers `Phrase,Guidance`, with standard quoting support, preflight validation, duplicate skipping, atomic import, and a result summary.
- Ship a bundled two-column starter CSV with the initial snowclone `Easily distracted by {X}`. Initialize it once for a new application data store, and offer an explicit action to import the currently bundled library later without silently overwriting user records.
- Expose the dialog as an integration-ready focused surface that the future ideation dialog can open; do not add a temporary main-window or settings entry point.
- Keep workspace transfer, ideation candidate generation, placeholder substitution, AI prompting, phrase tagging/categorization, archive/restore, cloud synchronization, and whole-application backup outside this module.

This is one coherent platform module because persistence, lifecycle rules, starter content, interchange, and the management surface all operate on the same small global library and can be verified together independently of the future ideation workflow.

Dependencies and coordination:

- The module uses the existing application-local SQLite database and its migration chain.
- The active `workspace-transfer` change must continue to exclude application-wide snowclone content from single-workspace packages.
- Concurrent capability-folder reorganization changes may move the likely file locations; implementation follows the accepted layer boundaries and the final folder layout without reopening the behavior specified here.

Primary workflow and UX placement:

- Creators will search or browse snowclones frequently from future ideation, but library maintenance, import, export, and deletion are occasional administration actions.
- Those management actions belong in a focused dialog that consumes no persistent main-workspace area. Until ideation exists, the dialog is constructed and verified as an integration-ready surface without an unrelated temporary launcher.
- New entries remain local drafts until Save; selection changes, import, bundled-library import, and close protect meaningful unsaved edits. Destructive deletion is explicit and confirmed.

Primary risks are ambiguous template syntax, accidental starter-data resurrection or overwrite, malformed CSV, duplicate library clutter, SQLite migration safety, and an unreachable-yet-user-facing dialog. The resolved syntax, one-time initialization marker, opt-in bundled import, preflight/atomic import behavior, normalized uniqueness, migration tests, and deterministic headless dialog tests address those risks.

Verification will map every acceptance scenario to focused domain/application tests, isolated SQLite migration and round-trip tests, CSV boundary tests, and Avalonia headless view/view-model tests. The full solution test baseline and strict OpenSpec validation remain completion gates; an interactive desktop check is optional because no acceptance behavior requires a native display.

## Capabilities

### New Capabilities

- `snowclone-library`: Application-wide snowclone persistence and lifecycle behavior, placeholder validation, bundled starter content, searchable focused management dialog, and two-column CSV import/export.

### Modified Capabilities

None.

## Impact

- **Domain:** a global `Snowclone` model and pure phrase-template normalization/validation rules, without persistence or Avalonia dependencies.
- **Application:** snowclone repository and management/import-export contracts, CRUD orchestration, result/state records, duplicate policy, and bundled-library initialization behavior.
- **Integration:** SQLite table and schema migration, focused snowclone repository implementation, standards-compliant CSV reader/writer, and access to the bundled starter resource.
- **App:** Snowclone Library view model, focused Avalonia window/dialog, file-picker adapter, composition wiring, and a future ideation-owned opener boundary.
- **Data/resources:** versioned SQLite migration plus a shipped `Phrase,Guidance` starter CSV that can grow into the curated library in later builds.
- **Tests:** domain validation, application use cases, persistence/migration and CSV integration tests, view-model tests, and deterministic Avalonia headless dialog tests.
- **Dependencies:** a standards-compliant CSV implementation may require a narrowly scoped library if the runtime facilities cannot correctly cover quoted commas, quotes, and multiline fields; any package choice must remain isolated in Integration and warning-clean.
