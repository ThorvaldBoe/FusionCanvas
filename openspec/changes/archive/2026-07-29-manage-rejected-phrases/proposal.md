## Why

Creators already reject ideation candidates durably (the `IdeationRejection` records captured by the `add-ideation-tool` change), and Ideation already feeds those records back as negative-guidance context for later generation. But creators have no way to review, correct, curate, or seed that guidance: a mis-captured reason stays wrong forever, a phrase rejected by mistake cannot be removed, and a creator who knows from experience that a phrase like "Talk to me about {X}" should be avoided has no way to record that guidance without running a generation and rejecting it first. This module adds a simple, focused management surface for those rejected phrases so the negative-guidance library stays accurate and useful.

## What Changes

- Add a focused **Rejected Phrases** management dialog, launched from the Ideation dialog, that lists every durable ideation rejection in the active workspace with live text search across phrase and reason.
- Provide a store/niche/group scope filter so the creator can narrow the workspace-wide list to a specific scope; the active Ideation scope is the default filter when the dialog opens.
- Allow selecting a rejection to edit its **phrase** and **reason** while preserving identity, store/niche/group scope, generation mode, and `CreatedAt`, and advance a new optional `UpdatedAt` timestamp.
- Allow creating a new rejected phrase manually; creation defaults to the active Ideation scope (store, niche, and optional group) and reuses the existing `Basic` `IdeationMode` value, so manually curated guidance is indistinguishable in shape from captured rejections and is immediately usable by Ideation context assembly.
- Allow permanently deleting a selected rejection after an explicit confirmation, including sensible next-selection behavior.
- Enforce normalized phrase uniqueness within the same store/niche/group scope so curation cannot produce duplicate guidance.
- Add an optional `UpdatedAt` column to the `ideation_rejections` SQLite table through the next versioned migration; `UpdatedAt` is null until a rejection is first edited.
- Add a single `Manage rejected phrases…` action to the Ideation dialog; the management dialog is owned by the Ideation dialog (modal, single-instance) and never mutates Ideation candidate state.
- Keep archive/restore of rejections, CSV import/export, cross-workspace synchronization, application-wide rejection libraries, changing scope or mode of an existing record, and changing the existing reject-from-Ideation capture flow outside this module.

This is one coherent platform module because the management surface, the small domain rule additions (uniqueness within scope, `UpdatedAt`), and the SQLite migration all operate on the same small existing `IdeationRejection` model and can be verified together independently of the Ideation generation flow.

Dependencies and coordination:

- The module reuses the existing `IdeationRejection` domain entity, the `ideation_rejections` SQLite table introduced by the active `add-ideation-tool` change, and the `WorkspaceSnapshot` save/load/migration pipeline.
- The active `workspace-transfer` change packages ideation rejections per workspace; this module does not change transfer semantics. Manual creation and edits write through the same `WorkspaceSnapshot` path that transfer already consumes, so packaged rejections remain accurate.
- The active `integrate-ideation-openrouter-snowclones` change reads rejections into Ideation context; this module does not change context assembly, but edited or manually created records will flow through the existing assembly unchanged because they remain ordinary `IdeationRejection` records.

Primary workflow and UX placement:

- Reviewing and curating rejected phrases is an occasional maintenance action tied to ideation, not persistent main-workspace area work.
- The dialog belongs as a focused auxiliary surface opened from the Ideation dialog, co-located with where rejections are created and where negative guidance is consumed. It consumes no persistent main-window footprint.
- New entries and edits are explicit drafts until Save; selection changes, search, and close protect meaningful unsaved edits. Destructive deletion is explicit and confirmed.

Primary risks are ambiguous scope/uniqueness semantics, edit-audit regression, stale Ideation context after a curation change, SQLite migration safety, modal-dialog nesting over the existing Ideation modal, and accidental loss of captured rejections through deletion. The resolved within-scope uniqueness, `UpdatedAt` migration with null preservation, refresh of the active workspace snapshot before management operations, single-instance nested modal ownership, confirmed destructive deletion, and deterministic headless dialog tests address those risks.

Verification will map every acceptance scenario to focused domain tests for within-scope uniqueness and edit invariants, application use-case tests with deterministic in-memory repositories, isolated SQLite migration and round-trip tests for `UpdatedAt` and edited/reason rows, view-model tests for list/filter/edit/create/delete/draft/unsaved/confirmation state, and Avalonia headless dialog tests for construction, bindings, scope filter, search, selection, draft protection, keyboard reachability, and single-instance nesting. The full solution test baseline and strict OpenSpec validation remain completion gates; an interactive desktop check is optional because no acceptance behavior requires a native display.

## Capabilities

### New Capabilities

- `rejected-phrase-management`: Focused management surface and application use cases for viewing, filtering, editing, creating, and deleting durable ideation rejections in the active workspace, plus the within-scope uniqueness rule, optional `UpdatedAt` audit timestamp, and the Ideation-dialog-owned launcher.

### Modified Capabilities

- `local-sqlite-persistence`: Adds the optional `UpdatedAt` column to `ideation_rejections` through the next versioned migration, with null preservation for existing rows and round-trip behavior for edited and never-edited rejections.

## Impact

- **Domain:** extend `IdeationRejection` with an optional `UpdatedAt` value (null until first edit), add pure within-scope normalized-uniqueness and edit-invariant rules, and define a small manual-creation record. No persistence or Avalonia dependencies.
- **Application:** add a `RejectedPhraseManagementService` and repository contract surface (create/list-with-filter/update/delete) over the existing workspace snapshot, with result/state records, within-scope duplicate policy, draft/unsaved-change semantics, and recoverable failures. Ideation context assembly is unchanged; manually created and edited records are ordinary `IdeationRejection` records.
- **Integration:** add the next versioned SQLite migration adding the nullable `ideation_rejections.updated_at` column, extend save/load mapping for the new column, and round-trip edited and never-edited rows.
- **App:** add a `RejectedPhrasesViewModel`, a focused Avalonia window/dialog, a single `Manage rejected phrases…` action in the Ideation dialog, composition wiring, and single-instance nested-modal ownership and theme/focus behavior.
- **Data/resources:** one versioned SQLite migration only; no bundled starter content.
- **Tests:** domain uniqueness/invariant tests, application use-case tests, persistence migration and round-trip tests, view-model tests, and deterministic Avalonia headless dialog tests.
- **Dependencies:** no new packages; reuses existing SQLite, Avalonia, and test infrastructure.
