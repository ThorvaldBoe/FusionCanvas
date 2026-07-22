## Why

Editing listing or group information today requires opening a separate editor dialog (Listing properties, Edit Group Properties) with an explicit Save button, and the Listing Inspector in the document window also saves only through an explicit Save with a discard guard. Worse, the read-only selection summary overlay added with group management renders on top of the document window content, so the Listing Inspector is effectively hidden whenever a listing is selected — the details pane never gets to do its job.

Creators refining many ideas and groups need low-friction editing: select an item in the navigation tree, type in the details pane, and move on. Every explicit Save click and every dialog round-trip is friction that discourages keeping item information current.

## What Changes

- Edit listing details inline in the Listing Inspector (document window detail area) with automatic save when leaving a field; remove the explicit Save/Revert buttons and the unsaved-changes discard prompt. Field-exit commits validate, persist, and report errors inline; an invalid working title reverts to the last persisted value while other valid edits still save.
- Add the listing's description to the inspector so all core listing information is editable in one place.
- Move listing lifecycle actions (archive, restore, permanent delete) from the Listing properties dialog into the inspector with inline confirmations.
- Show group details inline in the document window when a group is active: name, description, and notes auto-save on field exit; destination move, archive, and restore remain explicit actions with confirmation.
- Remove the Listing properties and Edit Group Properties dialogs and their view models; remove the now-redundant Edit Properties commands from the tree context menu and selection summary.
- Fix the document window layering so the read-only selection summary overlay only appears for store and niche selections (which keep dialog-based editing) and no longer covers the listing inspector or group details.
- Stores, niches, and workspaces keep their existing dialog-based editors; complex content such as images keeps the existing select-upload-apply asset flow.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `listing-inspector`: Creative-field edits persist automatically on field exit instead of through an explicit save; the unsaved-changes guard is replaced by commit-on-leave; the inspector gains the description field and hosts archive, restore, and permanent-delete lifecycle actions; archived listings are restored from the inspector.
- `listing-management`: The secondary Listing properties surface is retired; optional properties (description, notes), tag editing, archived review, restore, and permanent deletion move into the tree-plus-details-pane model.
- `group-management`: The secondary Edit Properties dialog is retired; detailed group properties edit inline in the document window with auto-save while create/rename stay in the tree and move/archive/restore stay explicit.

## Impact

- `ListingInspectorViewModel` loses its save/discard flow and gains commit-on-field-exit plus lifecycle actions; `ListingInspectorService` state/save contract gains description.
- A new `GroupDetailsViewModel` provides inline group editing; `GroupManagementViewModel`, `ListingManagementViewModel`, `GroupEditorWindow`, and `ListingEditorWindow` are removed.
- Main window wiring coordinates the group details pane with the active document context and fixes overlay visibility.
- Application, app view-model, and SQLite integration tests are updated; desktop UI verification is recorded per the testing baseline.
- No domain entity, schema, or persistence-format change is required.
