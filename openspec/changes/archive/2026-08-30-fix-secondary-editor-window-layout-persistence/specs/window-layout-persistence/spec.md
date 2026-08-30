## MODIFIED Requirements

### Requirement: Window placement persistence applies to non-transient windows

FusionCanvas SHALL persist the latest normal-state position and size of every non-transient window and SHALL NOT persist placement for transient confirmation dialogs.

The non-transient windows are the main window, Settings, Workspace Management, Store Editor, Assets, Ideation, Reject Idea, Snowclone Library, Rejected Phrases, Design Preview, Item Import, Option Value Management, Add Variant, Bulk Add Variants, Design Area Editor, and Mockup Template Editor. Transient confirmation dialogs include the group action and delete confirmations, the group selection dialog, the Ideation discard confirmation, and the Design Area archive confirmation.

#### Scenario: Non-transient secondary window reopens at its last placement
- **WHEN** the user moves or resizes a non-transient secondary window and later reopens it
- **THEN** FusionCanvas restores the window to its last normal-state position and size
- **AND** the restoration uses the same screen-safe normalization as the main window

#### Scenario: Reusable Store Management editor reopens at its last placement
- **WHEN** the user moves or resizes Option Value Management, Add Variant, Bulk Add Variants, Design Area Editor, or Mockup Template Editor and later reopens that same editor
- **THEN** FusionCanvas restores that editor's own last normal-state position and size
- **AND** it does not apply geometry belonging to a different editor window

#### Scenario: Transient confirmation dialog keeps default placement
- **WHEN** a transient confirmation dialog opens
- **THEN** FusionCanvas uses the dialog's default placement
- **AND** no geometry is persisted for that dialog
