## Context

FC-0104 established listings as manageable product concepts: canonical tree selection, a reusable working tab per listing, an application-owned `IListingManagementService` with atomic snapshot persistence, and a secondary focused properties/lifecycle surface (`ListingEditorWindow`) for optional description/notes and archive/restore/delete. The document window detail area, however, still renders a placeholder stage-tool card (tool name, scope labels, availability messages) for every context — a listing's actual creative content is invisible without opening the secondary dialog. The domain `Listing` carries `Name`, `Description`, `Status`, notes in `MetadataJson["notes"]`, tag links (`ListingTag`), and asset links (`AssetLink`); the workflow stage is currently derived at load time, with FC-0105 proposed to persist it.

PRD FC-0106 asks for one focused place to view and edit a listing's core creative context inside the document window detail area, aligned with the workflow stage navigator. The creator's primary workflow here is frequent: browse listings, understand what each is, and refine idea/phrase/graphic direction/notes/tags as the concept develops. FC-0104's design explicitly reserved this durable inspector for FC-0106. Status-change ownership belongs to FC-0105's document-card control; full tag administration belongs to FC-0108; asset attachment belongs to FC-0109 and later stage tools; marketplace metadata belongs to FC-0207.

UX preflight resolution (per `docs/ux-guidelines.md`): the inspector is primary-workspace, high-frequency viewing with lightweight editing, so it belongs in the existing document detail area and consumes no additional persistent workspace footprint. Sections (overview, creative fields, notes, tags, assets) provide progressive disclosure without hiding completed-stage information. Editing uses explicit Save with dirty tracking and a save/discard/cancel guard — matching the established editing-safety pattern rather than introducing autosave. The inspector hosts no destructive actions; archive/restore/delete remain in the FC-0104 focused surface. Empty states (no creative fields yet, no assets), validation (blank title), blocked (inactive listing), saving/busy, and persistence-failure states are all designed states, not implementation afterthoughts.

## Goals / Non-Goals

**Goals:**

- Present a listing as a product concept in the document detail area: working title, lifecycle status, current workflow stage, topic path, idea, phrase, graphic direction, notes, tags, and related assets.
- Allow lightweight in-place editing of title, idea, phrase, graphic direction, and notes with explicit save, validation, and unsaved-change protection across listing switches, tab switches, and tab closes.
- Manage listing tag links in the inspector: view, add existing store tags, create a reusable store tag inline when the name has no match, and remove links — without deleting reusable tag records.
- Show related assets as a read-only list with an explicit empty state so it is obvious whether a listing has supporting assets yet.
- Keep the inspector aligned with the workflow stage navigator and the document-context status control, emphasizing stage-relevant fields while keeping every section accessible.
- Keep archived or effectively inactive listings readable but read-only, with clear restore guidance.
- Persist inspector saves atomically through the existing repository boundary and recover cleanly from validation or persistence failure.

**Non-Goals:**

- Do not implement marketplace metadata, pricing, publishing, AI critique, Design Triangle scoring, rich-text notes, version comparison, prompt-history editing, or performance data (PRD out-of-scope list).
- Do not add a second lifecycle-status selector; the inspector displays status/stage and defers the change control to the FC-0105 document-card surface.
- Do not implement tag administration (rename, delete, merge of reusable tags) — FC-0108 owns that; the inspector only links/unlinks and creates missing tags by name.
- Do not attach, detach, import, or preview asset files — FC-0109 and future stage tools own asset mutation; the inspector only shows related assets.
- Do not remove or replace the FC-0104 secondary properties/lifecycle surface; it remains the home for description editing and archive/restore/delete.
- Do not add autosave, per-field background persistence, or a schema migration.
- Do not implement stage-specific stage tools (FC-0210–0213); the inspector is the listing's working view, not a stage tool marketplace.

## Decisions

### 1. The inspector is the detail-area working view for listing document contexts

When the active document tab is a listing item, the detail area renders the Listing Inspector instead of the placeholder stage-tool card. Store/topic contexts keep the existing stage-tool placeholder. The inspector is not a dialog, side pane, or overlay: it is the listing's home view and consumes no extra persistent workspace footprint, which keeps browsing and comparing listings fast with the existing tab model.

Alternative considered: a dedicated inspector side pane beside the tree. Rejected because it permanently consumes navigation workspace for a single-item concern and conflicts with the established document-window model (tabs + navigator + detail area) that FC-0009/FC-0010 built.

### 2. A focused application contract builds inspector state and saves atomically

Add `IListingInspectorService` (Application layer) with two responsibilities: build an inspector view state for a listing (core details, creative fields, notes, tags with provenance and names, related assets, status/stage, effective activity, display path) and save the edited form in one atomic operation. The save path reuses the FC-0104 listing-management update semantics (title validation, normalization, unknown-metadata and inherited-provenance preservation, snapshot replacement, one `IWorkspaceRepository.SaveAsync` call) rather than duplicating mutation rules.

Alternative considered: let the inspector view model call `IListingManagementService.UpdateListingAsync` directly and manage tags in the UI layer. Rejected because the inspector needs composed read state (assets, tag names/provenance, stage) that no existing contract returns, and tag resolve-or-create is a workspace mutation that must not live in the App layer.

### 3. Creative fields are documented reserved metadata keys, not new columns

`idea`, `phrase`, and `graphicDirection` are stored as reserved keys in the listing's `MetadataJson`, following the established convention (`notes`, niche creative-context keys). Values are normalized plain text; `phrase` is single-line, `idea`/`graphicDirection` allow multiple lines. Saves preserve unknown metadata keys and `inheritedFrom:` provenance entries exactly as the FC-0104 update path does. No schema migration is required, and FC-0202 (Concept Versions) can later promote these into versioned concept records without a data rescue.

Alternative considered: add `idea`/`phrase`/`graphic_direction` columns via a schema migration. Rejected because Phase 1 only needs lightweight editable text, the metadata-key pattern is already accepted for exactly this category of creative context, and FC-0104 deliberately deferred freezing a column representation to this change.

### 4. Explicit save with a dirty-tracking guard, not autosave

The inspector keeps an in-memory draft distinct from persisted state. Save validates (title non-blank single-line; fields normalized) and persists atomically; a successful save refreshes the draft baseline. Starting another listing selection, switching tabs, or closing a tab with meaningful unsaved changes asks save/discard/cancel; cancel keeps the user on the current listing and draft. Validation or persistence failure keeps the draft intact and shows an inline recoverable error.

Alternative considered: autosave on focus loss. Rejected because it makes accidental edits permanent without an intent signal, conflicts with the app's established explicit-save editing-safety guidance, and complicates undo semantics that Phase 1 does not otherwise have.

### 5. Tag editing links existing tags and creates missing ones by name

The inspector offers the store's existing reusable tags for linking, normalizes new tag names (trimmed, non-blank, single-line), and creates a store-scoped reusable tag only when no case-insensitive name match exists. Removing a tag removes only the listing-tag link. Inherited tag provenance recorded at creation remains visible as informational context; the inspector does not distinguish editing rules for inherited versus explicit links in Phase 1.

Alternative considered: defer all tag editing to FC-0108. Rejected because PRD FC-0106 explicitly requires viewing and managing listing tags in the inspector, and link/unlink plus create-if-missing is the minimal useful subset that does not preempt FC-0108's administration model.

### 6. Related assets are read-only

The inspector lists assets linked to the listing (name, kind, missing state) and shows an explicit "no assets yet" empty state. There is no attach, detach, reorder, or preview in this change.

Alternative considered: allow unlinking assets from the inspector. Rejected because asset-link semantics belong to the upcoming asset features (FC-0109) and deletion guards in FC-0104 treat asset links as dependent creative records; casual unlinking from a summary view would bypass those rules.

### 7. Status and stage are displayed, not re-owned

The inspector displays the listing's current lifecycle status and workflow stage and stays synchronized with the workflow stage navigator and the document-context status control. It introduces no second status selector: the change affordance belongs to FC-0105's document-card control. If FC-0106 is implemented before FC-0105 lands, the inspector renders the status/stage facts available at that time and the same display-only contract applies.

Alternative considered: give the inspector its own status dropdown now. Rejected because FC-0105's proposal designates a single quick-change status surface in the document card, and two competing selectors in one view violate the one-clear-owner UX rule.

### 8. Stage relevance is emphasis, not hiding

All inspector sections remain visible and editable at every stage so completed-stage context stays reachable (the navigator already provides completed-stage review). The section relevant to the current stage — Idea stage: idea; Concept stage: phrase and graphic direction; Design stage: assets; Listing stage: status/readiness summary — receives visual emphasis (for example an accent marker or default-expanded state), and empty fields show quiet placeholder text that invites completion without validation pressure.

Alternative considered: show only the current stage's fields. Rejected because the PRD requires retaining access to completed stages and understanding the whole concept; hiding fields would force stage navigation just to read context.

### 9. Inactive listings open read-only with restore guidance

An archived listing, or one effectively inactive through archived store/niche/group ancestry, opens in the inspector as read-only with an explicit inactive notice and guidance to restore via the existing lifecycle surface. Editing controls are disabled rather than hidden so the state is self-explanatory; the guard still applies to any draft already in progress when a listing becomes inactive.

Alternative considered: block opening inactive listings entirely. Rejected because reviewing archived work is accepted navigator/tab behavior and the PRD wants the inspector useful while browsing.

### 10. Relationship to the FC-0104 secondary properties surface

The FC-0104 `ListingEditorWindow` keeps its role: description editing, archived review, restore, and guarded permanent deletion. The inspector becomes the everyday editing surface for creative context, title, notes, and tags. Title quick-rename in the tree remains as a file-explorer-style gesture; these are distinct contexts, not duplicate controls in one view.

Alternative considered: move all editing into the inspector and retire the dialog's property editing. Rejected as unnecessary churn — the dialog's lifecycle ownership is accepted behavior, and narrowing it can happen later if usage shows confusion.

## Risks / Trade-offs

- [Metadata keys become an untyped catch-all] -> Limit FC-0106 to the three documented reserved keys, reuse the existing preserve-unknown-keys behavior, and add round-trip tests proving unrelated keys and provenance survive saves.
- [Inspector duplicates notes editing that exists in the properties dialog] -> Accept the overlap deliberately: the inspector is the durable everyday surface while the dialog owns lifecycle operations; document the boundary so future changes do not add a third editor.
- [Dirty-guard hooks into tab switch/close paths could leak or double-prompt] -> Centralize the guard in the document-window coordination path with view-model tests for switch, close, cancel, and repeated-prompt scenarios.
- [FC-0105 may land before or after this change] -> Keep status/stage display-only and sourced from the listing record so either order works; revalidate synchronization scenarios after FC-0105 archives.
- [Tag create-if-missing could produce near-duplicate tags] -> Normalize and match case-insensitively within the store before creating; leave merge/rename administration to FC-0108.
- [Snapshot replacement can overwrite concurrent mutations] -> Reload immediately before save and keep mutations serialized through application services, matching the FC-0104 stance; optimistic concurrency remains future work.
- [Large asset lists or long notes could degrade the detail area] -> Render assets as a compact scrollable list and keep the inspector in a single scroll container; no virtualization work in Phase 1.

## Migration Plan

1. Add the inspector application contracts and service (state building + atomic save with tag resolve-or-create), reusing the FC-0104 update path and repository boundary.
2. Add domain/application tests for state building, normalization, metadata/provenance preservation, tag creation, and failure atomicity.
3. Build the inspector view model and Avalonia view; host it in the document detail area for listing contexts behind the existing active-document flow.
4. Wire the dirty-tracking guard into tab switch/close and listing-selection coordination; add view-model tests.
5. Verify persistence round-trips through SQLite integration tests, run the full suite plus strict OpenSpec validation, and perform the real desktop UI verification pass on a disposable workspace.

No database migration is expected. Rollback removes the inspector hosting and service wiring; placeholder detail content returns, and any `idea`/`phrase`/`graphicDirection` metadata keys written remain harmless preserved metadata.

## Open Questions

None for FC-0106. Save behavior is explicit-save (PRD question resolved), notes are plain text only (PRD question resolved), related assets are read-only in the inspector (PRD question resolved), and status/stage editing ownership stays with FC-0105 while the inspector displays both.
