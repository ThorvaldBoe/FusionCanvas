## Why

FusionCanvas already models groups as nested topics, but the first FC-0103 implementation exposed that hierarchy as a flat list and made a separate dialog the primary creation workflow. Creator feedback established that group management is a high-frequency structural activity: the hierarchy must be immediately visible, inline-editable, keyboard-efficient, searchable, and safe to reorganize without interrupting the workspace.

FC-0103 is revised so the niche-rooted navigation tree becomes the primary group-management surface. The focused editor remains useful for occasional metadata and lifecycle operations, while routine creation, rename, selection, movement, copy, and filtering happen directly in the tree.

## What Changes

- Replace the flat navigation presentation with a compact Avalonia `TreeView` rooted at the selected store's niches, using rich node templates for hierarchy guides, icons, colors, status indicators, badges, and future item rows.
- Add high-throughput inline group creation and rename, including `Ctrl+Shift+N`, `F2`, Enter-to-save, Escape-to-cancel, selected default names, and a commit-and-add-another sibling flow.
- Make tree selection an explicit workspace context independent of document tabs. Normal selection updates the right-side inspector; Ctrl-click opens or activates a persistent tab.
- Resolve new-group destinations from the selected group, selected item's containing topic, selected niche, or the store's explicit default niche when no applicable topic is selected.
- Support structural movement through drag-and-drop and cut/paste, subtree duplication through copy/paste, immediate persistence, invalid-drop feedback, and UI rollback after persistence failures.
- Persist parent-scoped sibling order so drops between rows remain stable instead of reverting to alphabetical order.
- Add a filter-ready tree projection for free text, status, tags, archive state, and future properties while retaining ancestor paths and stable entity selection.
- Retain the detailed group editor as a secondary "Edit properties" surface for notes/context, appearance metadata, archive/restore, and other lower-frequency properties.
- Add a group-row context menu for child creation, rename, clipboard operations, and explicitly confirmed permanent subtree deletion.
- Preserve existing hierarchy validation, stable identities, archive behavior, same-store movement, atomic snapshot saves, and active/archived projections.

## Capabilities

### New Capabilities

- `group-management`: Defines niche-rooted hierarchical presentation, inline and detailed editing, selected-topic context, ordered structural operations, clipboard and drag/drop behavior, filtering, archive/restore, and document-inspector coordination.

### Modified Capabilities

- `navigation-tree`: The accepted hierarchical navigation model is now rendered and edited as a real tree rather than flattened into navigation buttons.
- `local-sqlite-persistence`: Group sibling order and store default-niche selection require persisted fields and a forward-compatible migration.
- `tabbed-document-window`: Navigation selection updates the reusable inspector without automatically opening a tab; Ctrl-click explicitly opens or activates a persistent tab.

## Impact

- **Domain/application:** Add sibling ordering, default-niche resolution, copy/delete-subtree commands, filter projection contracts, canonical selection, and structural-operation results suitable for optimistic UI rollback.
- **Persistence:** Add versioned persistence for group sibling order and store default niche; retain stable IDs and transactional snapshot replacement.
- **Desktop UI:** Replace the flat `ItemsControl` navigation with a styled Avalonia `TreeView`, inline editors, keyboard shortcuts, drag/drop adorners, filter controls, rich row indicators, and a selection-bound inspector.
- **Testing:** Extend domain, application, integration, and view-model coverage for inline drafts, keyboard flows, ordering, default destinations, clipboard semantics, drag/drop validation, filtered paths, inspector selection, Ctrl-click tabs, autosave, and rollback.
- **Compatibility:** Existing groups receive deterministic initial sibling order during migration. Existing hierarchy, archive, listing relationships, and metadata remain intact.

## Revision Note

The original design decision that rejected inline editing and made the focused dialog the primary creation surface is superseded. User validation showed that hierarchical editing is a core workspace workflow, not an occasional management task. The corrected design keeps frequent structural actions inline and moves only detailed metadata and lifecycle operations to the focused editor.
