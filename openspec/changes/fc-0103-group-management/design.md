## Context

FC-0103 already has application-owned group workflows, hierarchy validation, archive-aware projections, SQLite round trips, and a focused Avalonia editor. The first UI integration nevertheless renders navigation as a flat `ItemsControl`, opens a separate dialog for every group creation, and derives the effective topic from the active document tab. That shape hides the hierarchy and makes routine organization slower than the intended file-explorer-like workflow.

Validated product direction treats groups and future items as one niche-rooted workspace tree. Users must be able to create many groups rapidly, reorganize subtrees by keyboard or pointer, search and filter without losing structural context, select a node without creating tab clutter, and explicitly open a persistent tab when desired.

The implementation remains local-first and follows Clean Architecture. Avalonia owns presentation and gestures; application services own selection, destination resolution, filtering semantics, structural commands, validation, and save orchestration; the domain owns hierarchy invariants; SQLite owns durable representation.

## Goals / Non-Goals

**Goals:**

- Make the niche-rooted hierarchy immediately visible and editable in the persistent left pane.
- Support rapid inline create and rename with predictable keyboard behavior and retained user input after validation or persistence failures.
- Make selected tree context independent of document-tab state and deterministic for later item creation.
- Support drag/drop, cut/copy/paste, persistent sibling ordering, atomic autosave, and rollback after failed saves.
- Provide an extensible filter projection and rich row indicator slots without prematurely defining all future item/status/tag schemas.
- Preserve the existing focused editor for detailed properties and archive/restore.

**Non-Goals:**

- Implementing items, item-specific property editors, or final item-copy semantics beyond defining the shared command boundary.
- Permanent deletion, cross-store moves, batch editing, saved searches, analytics, templates, automation, or production queues.
- Introducing Avalonia Pro controls or coupling application behavior to Avalonia control instances.
- Defining final visual iconography, status vocabularies, or tag schemas.

## Decisions

### 1. Render the workspace with Avalonia `TreeView`

The left workspace pane will bind a built-in Avalonia `TreeView` to hierarchical node view models through `TreeDataTemplate`. Each node exposes stable identity, entity kind, children, expansion, selection, inline-edit state, icon/color/status indicator descriptors, counts, and drop state. A custom row/control template supplies indentation guides, expanders, indicator slots, hover actions, inline text editing, and drop adorners.

The selected store remains in the existing store selector. Its active niches are top-level tree roots; groups and future items appear beneath them. Archived entities remain outside the active tree and are reviewed intentionally.

Alternative considered: `TreeDataGrid`. Rejected because the reference interaction is a compact tree rather than a hierarchical table, multiple user-resizable columns are not required, and Avalonia 12 distributes `TreeDataGrid` as a Pro control.

Alternative considered: continue flattening nodes into an `ItemsControl`. Rejected because it obscures ancestry, duplicates expansion logic, and makes drag/drop, keyboard navigation, filtering, and rich hierarchical presentation unnecessarily custom.

### 2. Separate canonical selection from persistent document tabs

One selection coordinator owns the active tree entity independently of the document window. A normal click or keyboard move selects a visible niche, group, or future item and updates the right-side reusable inspector. Selection reveals the path but does not create a tab.

Ctrl-click explicitly opens or activates a persistent tab for the selected entity. Closing or switching tabs does not silently overwrite canonical tree selection unless the user explicitly navigates from that tab. A later middle-click gesture may map to the same open-tab command.

This replaces the current assumption that the active document tab is the only source of group creation context.

### 3. Resolve creation destination from explicit selected context and default niche

The application resolves a new group's parent in this order:

1. A selected active group becomes the direct parent.
2. A selected future item contributes its containing group, or its niche when ungrouped.
3. A selected active niche becomes the direct parent.
4. With no applicable selection, the selected store's persisted default niche is used.
5. A store with exactly one active niche may establish that niche as its default automatically.
6. A store with multiple active niches must have one explicit active default niche before fallback creation is available.

Archiving the default niche requires selecting a replacement or leaves fallback creation unavailable with corrective guidance. The default is store-scoped and never inferred from alphabetical ordering once multiple niches exist.

### 4. Make inline creation and rename the primary high-frequency workflow

`Ctrl+Shift+N` and the compact New Group action insert a non-persisted inline draft under the resolved parent. The initial unique name (`New group`, `New group (2)`, and so on) is fully selected so typing replaces it. Enter validates and saves, Escape cancels without persistence, and F2 edits the selected group's name inline.

On successful save the new or renamed node remains visible and selected. Shift+Enter commits a new group and immediately inserts another sibling under the same creation anchor, enabling rapid bursts without accidentally nesting each subsequent group. Failed validation retains edit mode and focuses the name. Failed persistence retains the draft and canonical hierarchy and offers retry or cancel.

The focused editor is renamed/reframed as Edit Properties. It handles notes/context, appearance metadata when available, destination selection as an accessible fallback, and archive/restore. It is not opened by ordinary creation or rename.

### 5. Route every structural gesture through shared application commands

Drag/drop and clipboard gestures invoke the same application operations:

- Cut then paste moves the selected complete subtree beneath the target topic.
- Copy then paste duplicates the selected group subtree with new group identities and preserved group metadata.
- Dropping onto a niche or group reparents the subtree.
- Dropping before or after a sibling reparents when necessary and changes sibling order.

The UI clipboard stores stable entity identifiers plus Copy or Cut intent, not live view-model references. Paste resolves the current canonical snapshot and destination again. Invalid self, descendant, archived, missing, or cross-store destinations are rejected before mutation. Cut styling is cleared only after a successful paste or explicit clipboard replacement.

Future item implementation must specify whether copying a group also duplicates contained items and assets. Until that capability exists, FC-0103 duplicates group records and descendant group structure only; the command contract is designed to extend without changing keyboard semantics.

### 6. Persist parent-scoped sibling order

Every active or archived group stores an integer sibling order within its direct parent. Structural commands normalize affected sibling collections atomically so values remain deterministic and compact. Existing records are migrated in current case-insensitive name order, preserving the visual order users already see.

New groups append to their parent's siblings unless an explicit insertion position is supplied. Copy/paste appends by default. A before/after drop inserts at the indicated position. Moving onto a container appends to that container. Archive preserves the group's position for restoration when possible; a conflicting/out-of-range restored position is normalized deterministically.

Alternative considered: retain alphabetical-only ordering. Rejected because a between-row drag would appear successful and then snap back, violating autosave expectations.

### 7. Autosave structural changes with failure rollback

Inline create/rename commits on explicit keyboard confirmation or an unambiguous focus transition. Moves, paste, reorder, archive, and restore save immediately through one application operation and one repository transaction.

The UI may project the expected result immediately for responsiveness, but it keeps the last confirmed projection. On save failure it restores the confirmed hierarchy, selection, expansion, and ordering, retains recoverable drafts where applicable, and displays an actionable non-modal error. Conflicting structural commands are disabled while a save is in flight.

### 8. Filter by projecting canonical hierarchy

A UI-independent `TreeQuery` describes free text and extensible structured predicates such as status, tags, archive state, warnings, or entity type. The projection includes matching nodes and every ancestor required to understand their path. It exposes match/highlight metadata without mutating canonical records or expansion preferences.

Clearing a query restores the pre-filter expansion state. Stable entity IDs preserve selection; if the selected entity is hidden, the inspector may retain it with a clear filtered-out indicator and a reveal/clear-filter action. Structural commands always operate on canonical IDs. Reordering between filtered siblings is disabled when hidden siblings would make the insertion position ambiguous; dropping onto a visible container remains available.

FC-0103 implements free-text group filtering and the extensible projection boundary. Status and tag controls may remain disabled/empty until their owning capabilities provide data.

### 9. Keep rich row presentation semantic and extensible

Tree nodes expose semantic indicator descriptors rather than Avalonia brushes or control instances. The Avalonia layer maps entity kind, color token, status token, badges, counts, warnings, and progress descriptors into visual slots. This supports groups and future items without putting business rules into row templates.

The initial group row requires a group icon, optional color token, name, child count, validation/warning indicator, and hover actions. Status and tag indicators are rendered when supplied but are not invented by FC-0103.

### 10. Preserve existing group lifecycle and hierarchy rules

Existing rules remain authoritative: exactly one direct parent, same-store moves only, no cycles, active destination paths, normalized unique sibling names, archived subtree hiding, explicit archived review, stable identities, and non-cascading archive flags. Copy is the exception to identity preservation because it intentionally creates new records.

## Risks / Trade-offs

- **Deep trees may create visual and performance pressure** -> Bind observable hierarchical collections, avoid rebuilding unchanged branches, preserve expansion state by stable ID, and add virtualization/performance verification with representative data.
- **Inline editing can conflict with global shortcuts** -> Suppress structural shortcuts while a text editor owns focus except Enter/Escape and explicitly supported commands.
- **Filtered drag/drop can imply the wrong position** -> Disable before/after reorder when hidden siblings make placement ambiguous; allow clearly labeled container drops.
- **Copy semantics will become broader when items arrive** -> Keep clipboard payloads and copy orchestration application-owned, and add item-copy policy in the item-management change before enabling item-inclusive duplication.
- **Default niche can become invalid after lifecycle changes** -> Validate it on load and mutation; require or suggest a replacement when archiving the default.
- **Optimistic UI can diverge on save failure** -> Retain a confirmed projection and test complete restoration of hierarchy, order, expansion, and selection.
- **Sibling-order migration changes the schema** -> Use a versioned forward migration, deterministic initialization, compatibility tests, and safe refusal for newer unsupported schema versions.

## Migration Plan

1. Add sibling order and default niche to domain/application contracts and create a versioned SQLite migration with deterministic backfill.
2. Extend application services with destination resolution, selection, ordered move/copy operations, and filter projection.
3. Replace the flat navigation `ItemsControl` with a styled `TreeView` backed by the existing navigation hierarchy and new observable presentation nodes.
4. Add inline create/rename, keyboard commands, reusable inspector selection, and Ctrl-click tab opening.
5. Add drag/drop, clipboard commands, autosave/rollback, filter controls, and rich indicators.
6. Reframe the existing focused editor as secondary Edit Properties and retain archive/restore and accessible move fallback.
7. Verify migrations, application behavior, keyboard-only flows, drag/drop, filtering, rollback, and representative-tree performance.

Rollback can remove the revised UI and application wiring while leaving the additive order/default fields readable. Data migration must not discard hierarchy, metadata, archive state, listings, or stable IDs.

## Open Questions

None blocking FC-0103. Item-inclusive copy behavior remains intentionally delegated to the later item-management specification; FC-0103 copies group structure and group metadata only.

## Retrospective

**Invalidated assumption:** Group creation and structural management were occasional enough to place in a separate focused dialog.

**Observed correction:** Creators need a high-throughput, file-explorer-like hierarchy where routine structural changes are visible, inline, keyboard-driven, and automatically saved.

**Reusable lesson:** Frequency and spatial context should determine interaction placement. Repeated organization belongs directly in the persistent structure; detailed metadata and disruptive lifecycle operations belong in a secondary inspector or focused surface.
