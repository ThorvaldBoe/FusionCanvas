## Context

The Listing Inspector (FC-0106) already presents the active listing's title, idea, phrase, graphic direction, notes, tags, and assets in the document window detail area, but persists edits only through an explicit Save with an unsaved-changes discard guard. Group and listing property editing otherwise lives in two modal editor dialogs (`GroupEditorWindow`, `ListingEditorWindow`) backed by `GroupManagementViewModel` and `ListingManagementViewModel`. The read-only selection summary overlay introduced with group management (`WorkspaceTree.HasSelection`) shares the document window grid cell with the document content and renders above it, so selecting a listing hides the inspector behind the summary.

The application layer already owns every required mutation with atomic semantics: `ListingInspectorService.SaveAsync` (creative fields, notes, tags), `ListingManagementService` (`UpdateListingAsync`, `ArchiveListingAsync`, `RestoreListingAsync`, `DeleteListingAsync`, `SetListingStatusAsync`, `MoveListingStageAsync`), and `GroupManagementService` (`UpdateGroupAsync`, `MoveGroupAsync`, `ArchiveGroupAsync`, `RestoreGroupAsync`). No new application behavior is required beyond carrying the listing description through the inspector contract.

## Goals / Non-Goals

**Goals:**

- Select a group or listing in the navigation tree and edit its information directly in the right details pane with zero save clicks.
- Persist text-field edits automatically when the field loses focus; keep validation inline and non-destructive.
- Keep destructive or structural actions (archive, restore, delete, group move) explicit and confirmed.
- Retire the two editor dialogs and the hidden-inspector layering defect.
- Preserve every existing preservation guarantee: identity, placement, stage, status, archive state, tags, assets, prompts, and unknown metadata.

**Non-Goals:**

- Do not change store, niche, or workspace editing; those keep dialog editors.
- Do not change the asset upload flow; images keep the select-upload-apply Assets surface.
- Do not add new domain entities, metadata keys, or schema migrations.
- Do not remove the tree's inline create/rename, drag-and-drop move, or clipboard operations.
- Do not introduce debounced per-keystroke saving; commits happen on field exit and explicit action only.

## Decisions

### 1. Commit on field exit, not on a timer

Each editable text field commits when it loses focus (Avalonia `LostFocus` routed to the view model in code-behind, consistent with existing code-behind handlers such as `OnTagInputKeyDown`). A single `CommitEditsAsync` entry point per pane saves the pane's current draft through its application service in one atomic snapshot save. No timers, no per-keystroke persistence: saving is predictable and testable without a running dispatcher.

Alternative considered: debounced save-as-you-type. Rejected because it persists half-typed values, complicates validation (a transient empty title is invalid), and is harder to verify deterministically.

### 2. Invalid working titles revert; other edits still save

The working title is the only field with a validation rule that can fail (nonblank, single line, sibling-unique at the service layer for groups). On field-exit commit with an invalid title, the title reverts to its last persisted value and an inline error explains what happened; remaining valid field edits still save. This mirrors platform inline-rename conventions and guarantees auto-save can never trap the user in an unsavable state.

Alternative considered: block navigation until the title is fixed. Rejected because it reintroduces modal flow and can strand focus far from the offending field.

### 3. One details pane per entity kind, coordinated with the active document context

The document window detail area shows, in precedence order: the Listing Inspector when the active context is a listing item, the group details pane when the active context is a group topic, the stage tool host placeholder otherwise. The read-only selection summary overlay becomes visible only for store and niche selections, whose editing stays dialog-based; it no longer renders above listing or group content.

The group details pane follows the same coordination pattern as the inspector: `MainWindowViewModel` loads it when the active document context is a group and clears it otherwise. Normal selection keeps reusing the working tab; persistent tabs (Ctrl+click) are unaffected.

### 4. Lifecycle and structural actions live in the details pane as explicit, confirmed commands

The inspector gains an archive action with inline confirmation, a restore action for archived listings, and a permanent-delete action with inline confirmation that surfaces the service's connected-records guard. The group pane gains destination selection with an explicit Move action (moving a subtree is structural and must not fire from a casual combo-box change) plus archive and restore with confirmation. Tag add/remove in the inspector commits immediately because each is a deliberate click.

After every mutation the pane raises a structure-changed notification; `MainWindowViewModel` refreshes the snapshot, tree, and replacement selection exactly as the retired dialog view models did.

### 5. The description moves into the inspector; tag editing stays with the inspector's existing editor

`ListingInspectorService` gains the listing description (a first-class domain field previously editable only in the dialog) so all core listing information lives in one pane. Tag editing consolidates on the inspector's existing name-based editor (link existing, create inline, remove link) from the accepted listing-inspector behavior; the dialog's duplicate tag editor is retired with the dialog.

### 6. Navigation and tab switches commit instead of prompting

The previous guard asked save/discard/cancel when leaving a listing with unsaved changes. With commit-on-exit, the guard becomes: attempt a commit, then proceed. A validation failure reverts only the invalid field (per decision 2); a persistence failure is reported inline and the persisted state is left unchanged, then navigation proceeds so the user is never trapped.

Alternative considered: keep the discard prompt for the rare failure case. Rejected because with auto-save the steady state is "nothing unsaved," so a prompt would only appear precisely when saving is already broken.

## Risks / Trade-offs

- [Field-exit commit fires while clicking a destructive action] -> All pane mutations (commit, archive, restore, delete, move) share one busy flag per pane; a commit in progress disables the actions, and pointer focus change completes before the click handler runs.
- [Silent title revert surprises the user] -> The inline error states exactly what was reverted and why; the persisted value is restored to the field so nothing looks lost.
- [Archived entities need to stay reachable for restore] -> The tree's archived view and filters remain the review surface; selecting an archived listing or group shows its details read-only with restore guidance and a restore action.
- [Two panes plus overlay compete for the same grid cell] -> Visibility is resolved from the active document context and selection kind in one place (`MainWindowViewModel`), covered by view-model tests.

## Migration Plan

No data migration. The listing description already persists on the listing record; the dialogs and overlay are UI-only concerns. On first run after the change, users simply see the details pane instead of the summary overlay.

Spec-application note: the `group-management` capability's accepted requirements currently live in the completed-but-unarchived change `fc-0103-group-management`. This change's group-management delta applies on top of that spec and must only be archived after `fc-0103-group-management` has been synced or archived into the main specs.

## Open Questions

None. Store/niche/workspace editing stays dialog-based per explicit product direction; image handling stays with the Assets surface.
