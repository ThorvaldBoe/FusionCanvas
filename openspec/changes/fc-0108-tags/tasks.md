## 1. Domain and Persistence Foundation

- [x] 1.1 Add a nullable `Color` field to the `Tag` domain record with normalized `#RRGGBB` validation at the domain boundary, preserving existing constructor callers via a defaulted parameter.
- [x] 1.2 Update `WorkspaceSnapshot` construction and normalization so tags carry `Color` through the snapshot and existing snapshot factories remain source-compatible.
- [x] 1.3 Add a versioned SQLite migration from schema version 3 to 4 that adds the nullable `tags.color` column with empty backfill, bumps `CurrentSchemaVersion` to 4, and preserves existing tags, listing-tag links, store ownership, archive state, metadata, and stable IDs.
- [x] 1.4 Update `SqliteWorkspaceRepository` to write and read `tags.color` (normalized to `#RRGGBB` or null) and to round-trip color through save and load.
- [x] 1.5 Add SQLite integration tests for color round-trip, null color round-trip, 3 → 4 migration from a pre-migration database, new-database creation at schema 4, invalid color handling, and safe refusal of newer unsupported schema versions.

## 2. Tag Management Application Layer

- [x] 2.1 Define `ITagManagementService` contracts: requests, results, summaries, state, active/archived projections, lookup, autocomplete vocabulary, and tag-filter view for the tree, with recoverable error reporting.
- [x] 2.2 Implement tag create with store-scoped context, trimmed nonblank single-line name, case-insensitive active-name uniqueness within the store, optional description, optional normalized color, stable identity, timestamps, and one atomic save.
- [x] 2.3 Implement rename, recolor, and description edits that preserve stable identity, store ownership, archive state, and all `ListingTag` links, with duplicate-active-name rejection and invalid-color rejection.
- [x] 2.4 Implement archive and restore with active-name conflict blocking on restore, preserved `ListingTag` links, and exclusion of archived tags from active vocabulary views.
- [x] 2.5 Implement confirmed permanent deletion that atomically removes the tag and every `ListingTag` link referencing it, reports the affected listing count, preserves all listing records and other relationships, and rolls back on failure.
- [x] 2.6 Implement apply-to-listing and remove-from-listing operations that validate store ownership, reject cross-store tags, reject duplicates, persist one `ListingTag` link change atomically, and leave the reusable tag record intact.
- [x] 2.7 Implement create-on-the-fly: typed name not matching an active tag creates and applies a new active tag in one atomic save; exact normalized match to an active tag applies the existing tag; exact match to an archived tag offers restore-and-apply; Escape creates nothing.
- [x] 2.8 Implement active-tag vocabulary lookup for the listing tag editor autocomplete and the tree tag filter control, excluding archived tags and respecting active store scope.
- [x] 2.9 Add application tests for create, rename, recolor, description edit, archive, restore with conflicts, deletion with and without links, apply/remove, create-on-the-fly, vocabulary lookup, cross-store rejection, duplicate rejection, invalid color/name, atomicity, and repository-failure rollback.

## 3. Listing Tag Editor Integration

- [x] 3.1 Wire `ITagManagementService` through `AppWorkspaceFactory`, `MainWindowViewModel`, and the listing properties surface without exposing repository details to the UI.
- [x] 3.2 Add a tag editor region to the listing secondary properties surface that shows the selected listing's applied tags as colored chips with remove affordances.
- [x] 3.3 Implement an autocomplete text input backed by the active tag vocabulary, with create-on-the-fly on commit, duplicate-name matching to existing active tags, archived-tag restore-and-apply prompt, and Escape-to-clear without losing existing chips.
- [x] 3.4 Implement keyboard flow: arrow-key autocomplete navigation, Enter to apply or create, Backspace to remove the last chip, focus retained after apply/remove for rapid multi-tag entry.
- [x] 3.5 Make tag apply/remove persist immediately and atomically through the tag management service, visually independent of the description/notes draft and its explicit Save action, with a busy indicator and rollback on failure.
- [x] 3.6 Block tag edits when the selected listing is effectively hidden by an archived ancestor, with actionable guidance.
- [x] 3.7 Render archived-applied tags in a muted archived chip style so historical classification is not silently lost.
- [x] 3.8 Add view-model and app tests for autocomplete, create-on-the-fly, duplicate handling, archived-tag restore prompt, keyboard flow, immediate persistence, rollback, archived-parent blocking, and independence from the description/notes Save.

## 4. Tree Tag Filter

- [x] 4.1 Add a compact tag filter control above the workspace tree that lists the active tags of the active store as toggleable colored chips through the tag management vocabulary view.
- [x] 4.2 Build a `TreeQuery` with `TagIds` from the selected chips using AND semantics (a listing must have all selected tags) and feed it through the existing tree projection.
- [x] 4.3 Preserve ancestor topic paths for matching listings, keep stable entity selection across filter changes, and restore pre-filter expansion when the filter is cleared.
- [x] 4.4 Show a filtered-out indicator and reveal/clear-filter action when the canonical selection is hidden by the active tag filter.
- [x] 4.5 Exclude archived tags from the filter control and make the control keyboard reachable and clearable without pointer-only interaction.
- [x] 4.6 Add view-model and app tests for single-tag filtering, multi-tag AND filtering, clear-restore expansion, filtered-out selection, archived-tag exclusion, and keyboard flow.

## 5. Tags Tab in Store Management

- [x] 5.1 Add a Tags tab to the existing store management window alongside Basic info and Niches, scoped to the selected store, that does not change the selected store when switched to.
- [x] 5.2 Render an active tags list and a separate archived tags review area, with an empty active-tag state that offers a clear way to create a tag without requiring a listing.
- [x] 5.3 Add a side editor exposing name, color picker, optional description, Save, Archive, Restore, and Delete actions with state-coherent enablement for new drafts, unchanged existing tags, and dirty existing tags.
- [x] 5.4 Implement explicit Save for name/color/description drafts with dirty tracking and save/discard/cancel handling when switching tags or closing the window with meaningful unsaved changes.
- [x] 5.5 Implement confirmed permanent deletion with a warning that names the tag and the count of listings it is applied to, keyboard-accessible confirmation/cancellation, and post-deletion selection fallback.
- [x] 5.6 Apply compact fixed or content-based button widths, icon tooltips for icon-only commands, predictable tab order, inline errors, busy states, and duplicate-submission prevention.
- [x] 5.7 Add view-model and UI tests for Tags tab open, active/archived separation, empty state, dirty unsaved changes, deletion confirmation and blocking, restore conflicts, color picker validation, compact action sizing, and shared control guidance.

## 6. Colored Chip Rendering

- [x] 6.1 Render colored tag chips with the tag's color (or default accent when null) as semantic color-token-plus-name descriptors mapped by the Avalonia layer in the listing editor, the tree filter, the Tags tab, and on listing rows.
- [x] 6.2 Add colored tag indicator slots to listing rows using the existing rich row indicator slots reserved by FC-0103, with a visible-chip cap and a "+N" overflow to protect row density.
- [x] 6.3 Ensure chip color updates propagate across all surfaces after a rename, recolor, archive, restore, apply, remove, or delete operation without rebuilding unrelated branches.
- [x] 6.4 Add app tests for chip color binding, archived chip style, overflow behavior, and cross-surface color propagation.

## 7. Verification

- [x] 7.1 Run `dotnet test .\FusionCanvas.sln` and resolve regressions across domain, application, integration, and app test suites.
- [ ] 7.2 Manually verify through the built desktop application using a disposable workspace database and actual keyboard input: create, rename, recolor, archive, restore, and delete tags in the Tags tab; apply and remove tags on a listing; create-on-the-fly; filter by one and multiple tags with AND semantics; clear filter; filtered-out selection; archived-tag chip style; reload persistence; and rollback after a fault.
- [ ] 7.3 Manually verify through the built desktop application using actual pointer input: color picker, chip remove affordances, filter chip toggling, and Tags tab list/editor selection.
- [ ] 7.4 Verify representative stores with many tags and listings remain responsive in the real desktop UI and that chip overflow, filter, and expansion state behave correctly.
- [x] 7.5 Confirm FC-0108 adds no topic or asset tagging, no global tags, no tag hierarchy, automation, analytics, marketplace keyword optimization, AI tag suggestions, required tag schemas, saved tag views, or bulk operations.
- [x] 7.6 Run strict OpenSpec validation and confirm every tag-management scenario is covered by implementation or automated tests; record the tested build/environment, disposable-data isolation, desktop scenarios and results, code-level-only limitations, and automated regression results in `verification.md`.
