## ADDED Requirements

### Requirement: Workspace styles are managed as reusable artwork guidance
FusionCanvas SHALL provide styles scoped to one workspace. Each custom style SHALL have a nonblank title, a nonblank description, and one or more reference images. Titles SHALL be unique within a workspace without regard to case or surrounding whitespace. The built-in `General` style SHALL always be available, require no reference image, and SHALL NOT be renamed, edited, or deleted.

#### Scenario: User opens style management
- **WHEN** the user opens style management for the active workspace
- **THEN** FusionCanvas lists that workspace's custom styles and the built-in General style
- **AND** it presents a focused editor for the selected style or a new draft
- **AND** it does not show styles from another workspace

#### Scenario: User creates a valid style
- **WHEN** the user saves a style with a unique nonblank title, nonblank description, and at least one valid reference image
- **THEN** FusionCanvas creates the style in the active workspace with a stable identity
- **AND** makes its title, description, and ordered reference images available for artwork generation

#### Scenario: User attempts an invalid or duplicate style
- **WHEN** the user saves a style with a blank title or description, no reference image, or a title already used in that workspace after trimming and case normalization
- **THEN** FusionCanvas rejects the save with field-level guidance
- **AND** leaves the prior saved style unchanged

#### Scenario: User updates a style
- **WHEN** the user saves valid changes to a custom style's title, description, or reference images
- **THEN** FusionCanvas updates that style while preserving its identity and workspace ownership
- **AND** leaves Items' selected style identity unchanged

#### Scenario: User changes style selection with an unsaved draft
- **WHEN** the user selects another style or closes management with meaningful unsaved edits
- **THEN** FusionCanvas asks whether to discard the draft or continue editing
- **AND** continuing preserves the draft, selection, and focus

### Requirement: Style reference images use managed workspace files
FusionCanvas SHALL import style reference images into managed workspace storage, associate each managed file with its owning style and stable order, and keep durable references workspace-relative. Failed imports or saves SHALL leave the saved style and its existing files unchanged and SHALL clean up newly staged files when possible.

#### Scenario: User imports a style reference
- **WHEN** the user adds a valid local image to a style
- **THEN** FusionCanvas copies it into managed workspace storage
- **AND** the style references the managed copy rather than the original path

#### Scenario: User removes or replaces a style reference
- **WHEN** the user saves a style after removing or replacing a reference image
- **THEN** the saved style uses only its newly saved ordered references
- **AND** an image still referenced by another style or workspace record is not deleted
- **AND** an unreferenced removed managed file may be deleted after the structured update succeeds

#### Scenario: A style reference file is missing
- **WHEN** a saved reference path does not resolve inside managed workspace storage
- **THEN** FusionCanvas identifies that reference as unavailable in style management
- **AND** does not read outside the workspace boundary or fabricate replacement image content

### Requirement: Style management is reachable from workspace and Design contexts
FusionCanvas SHALL make style management available from Workspace settings for the active workspace and SHALL provide an `Add new` shortcut beside the Design-stage style selector. The shortcut SHALL open management for the current workspace and return to the originating Design context after management closes.

#### Scenario: User opens management from Workspace settings
- **WHEN** the user activates Manage artwork styles for the active workspace
- **THEN** FusionCanvas opens that workspace's style-management surface

#### Scenario: User adds a style from Design
- **WHEN** the user activates Add new beside the Design-stage style selector
- **THEN** FusionCanvas opens style management without losing the current Item or Design-stage selection
- **AND** after a style is saved and management closes, the new style is available in the selector

### Requirement: Deleting a custom style is confirmed and preserves Item work
FusionCanvas SHALL require explicit confirmation before deleting a custom style. If Items in the workspace currently select that style, deletion SHALL set those Items' style selection to General while preserving their other metadata and design data. The General style SHALL not be deletable.

#### Scenario: User cancels style deletion
- **WHEN** the user declines the deletion confirmation
- **THEN** the style, its references, and every Item selection remain unchanged

#### Scenario: User confirms deletion of a selected style
- **WHEN** the user confirms deletion of a style selected by one or more Items
- **THEN** FusionCanvas deletes the style and updates those Items to use General atomically
- **AND** does not delete reference files still used by other records
- **AND** selects a sensible remaining style or shows the empty custom-style state in management

#### Scenario: User attempts to delete General
- **WHEN** the user requests deletion of General
- **THEN** FusionCanvas blocks the operation and keeps General available

