## 1. Application Layer

- [x] 1.1 Add the listing description to `ListingInspectorState` and `ListingInspectorSaveRequest`, persist it to the listing record, and keep all existing preservation guarantees (identity, placement, unknown metadata, inherited provenance).
- [x] 1.2 Update application tests for description round-trip, normalization, and preservation.

## 2. Listing Inspector Inline Editing

- [x] 2.1 Rework `ListingInspectorViewModel` for automatic save: expose description, replace Save/Revert/discard-prompt with `CommitEditsAsync` that no-ops when clean, validates and reverts an invalid title with an inline error while still persisting other valid edits, reports persistence failures inline, and keeps the draft for retry.
- [x] 2.2 Commit immediately when a tag is added or removed.
- [x] 2.3 Replace the leave-guard prompt with commit-then-proceed semantics for selection changes, tab switches, and tab closes.
- [x] 2.4 Add inspector lifecycle actions: archive with inline confirmation, restore for archived listings (with blocked-topic error), and permanent delete with inline confirmation and the connected-records guard, raising a structure-changed notification for tree refresh and replacement selection.
- [x] 2.5 Update the inspector view: add the description field, remove Save/Revert buttons and the discard prompt, add the lifecycle action row with inline confirmations, and wire text-field `LostFocus` to commit through code-behind.

## 3. Group Details Inline Editing

- [x] 3.1 Add a `GroupDetailsViewModel` that loads the active group with destinations, edits name/description/notes with commit-on-field-exit (invalid or duplicate name reverts with an inline error while other edits persist), and exposes explicit move, confirmed archive, and restore actions with a read-only archived state.
- [x] 3.2 Add the group details pane to the main window document area, shown when the active document context is a group, wired to commit on field exit.

## 4. Main Window Coordination and Dialog Removal

- [x] 4.1 Coordinate the group details pane with the active document context (load on group context, clear otherwise) and refresh tree, inspector, and replacement selection after pane mutations.
- [x] 4.2 Restrict the read-only selection summary overlay to store and niche selections so it no longer covers the listing inspector or group details pane.
- [x] 4.3 Remove `GroupManagementViewModel`, `ListingManagementViewModel`, `GroupEditorWindow`, `ListingEditorWindow`, their window-sync code, the tree Edit Properties command and context-menu item, and the selection-summary Edit properties button.
- [x] 4.4 Route active-workspace updates directly to the group and listing management services.

## 5. Tests

- [x] 5.1 Rewrite `ListingInspectorViewModelTests` for automatic save: commit on field exit, invalid-title revert with other edits persisted, tag commit, leave-guard commit, lifecycle actions, busy-state duplicate prevention, and failure recovery.
- [x] 5.2 Add `GroupDetailsViewModelTests` for load, auto-save, name-validation revert, move, archive/restore, read-only archived state, and failure recovery.
- [x] 5.3 Update `MainWindowViewModelTests` for overlay visibility by selection kind, group details coordination, commit-on-leave, and lifecycle replacement selection; remove the retired dialog view-model tests.
- [x] 5.4 Update SQLite integration tests for inspector description persistence and reload.

## 6. Verification

- [x] 6.1 Run `dotnet test .\FusionCanvas.sln` and resolve regressions.
- [x] 6.2 Run strict OpenSpec validation for this change.
- [x] 6.3 Record verification evidence, including the not-applicable desktop UI pass under OpenCode.
