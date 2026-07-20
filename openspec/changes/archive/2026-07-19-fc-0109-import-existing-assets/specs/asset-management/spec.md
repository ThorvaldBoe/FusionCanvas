# Asset Management (Delta)

## ADDED Requirements

### Requirement: Asset import copies files into managed workspace storage
FusionCanvas SHALL allow a user to import an existing local file as an asset by copying it into managed workspace storage through the workspace file storage boundary, creating the asset record and one context link as a single atomic operation, and treating the managed copy as the authoritative file after import.

#### Scenario: User imports a file to a listing
- **WHEN** the user chooses a supported local file and confirms import for an active listing
- **THEN** FusionCanvas copies the file into managed workspace storage
- **AND** creates a store-owned asset record with a workspace-relative reference, the original file name, and the original source path preserved as metadata
- **AND** links the asset to that listing in the same persisted snapshot
- **AND** subsequent asset views use the managed workspace copy, not the original source path

#### Scenario: File type is not supported
- **WHEN** the user selects a file whose extension is outside the supported creative asset set
- **THEN** FusionCanvas blocks the import before copying
- **AND** explains that the file type is not supported
- **AND** leaves persisted workspace state unchanged

#### Scenario: Source file is unavailable
- **WHEN** the chosen source file no longer exists or cannot be read
- **THEN** FusionCanvas reports a recoverable error
- **AND** creates no asset record, link, or managed copy

#### Scenario: Persistence fails after the copy
- **WHEN** the managed copy succeeds but the workspace snapshot cannot be saved
- **THEN** FusionCanvas removes the newly copied managed file on a best-effort basis
- **AND** reports a recoverable error
- **AND** leaves the last confirmed persisted state intact

#### Scenario: User imports the same file twice
- **WHEN** the user imports a source file that was already imported
- **THEN** FusionCanvas creates a second independent asset with its own managed copy
- **AND** does not deduplicate, merge, or warn about the earlier import

### Requirement: Assets attach to one active workspace context
FusionCanvas SHALL attach each imported asset to exactly one context: a listing, niche, group, or store in the active workspace, and SHALL validate that the target exists, belongs to the asset's store, and is active including its ancestor chain before import.

#### Scenario: User imports reference material to a broader context
- **WHEN** the user imports a file for an active niche, group, or store
- **THEN** FusionCanvas creates the asset owned by that store and links it to the chosen context

#### Scenario: Target context is archived or unavailable
- **WHEN** the requested target is archived, missing, or hidden by an archived ancestor
- **THEN** FusionCanvas blocks the import with actionable guidance
- **AND** creates no asset record, link, or managed copy

### Requirement: Assets carry an explicit purpose label
FusionCanvas SHALL label every imported asset with a purpose using the domain asset kinds, SHALL pre-select a purpose derived from the file extension during import, and SHALL allow the user to change an asset's purpose afterwards as its own persisted operation.

#### Scenario: Import pre-selects purpose from extension
- **WHEN** the user chooses a file in the import flow
- **THEN** FusionCanvas pre-selects the matching asset kind for known creative extensions
- **AND** falls back to an unknown or other kind when the extension has no known mapping
- **AND** lets the user confirm or change the purpose before completing the import

#### Scenario: User relabels an asset
- **WHEN** the user changes an asset's purpose in the asset surface
- **THEN** FusionCanvas persists the new purpose and updated timestamp atomically
- **AND** preserves the asset's file reference, context link, and remaining metadata

### Requirement: Context assets are visible from the relevant work
FusionCanvas SHALL provide a focused asset surface for a selected listing, niche, group, or store that lists the assets linked to that context with their names, purposes, and file state, and SHALL list every store-owned asset, including unlinked assets, in the store-level view.

#### Scenario: User reviews listing assets
- **WHEN** the user opens the asset surface for a listing
- **THEN** FusionCanvas shows the assets linked to that listing with purpose labels and managed file names
- **AND** the managed workspace copy is the reference used for file state

#### Scenario: Store view shows all store assets
- **WHEN** the user opens the asset surface for a store
- **THEN** FusionCanvas lists every asset owned by that store
- **AND** labels each asset with its linked context or an unlinked indicator

#### Scenario: Context has no assets
- **WHEN** the user opens the asset surface for a context with no linked assets
- **THEN** FusionCanvas shows an empty state that explains assets can be imported
- **AND** keeps the import action available

### Requirement: Missing managed files are detected and presented
FusionCanvas SHALL detect when a listed asset's managed workspace file no longer exists and SHALL present that asset with a distinct missing state without attempting repair or relinking.

#### Scenario: Managed file is missing
- **WHEN** an asset's workspace-relative reference does not resolve to an existing managed file
- **THEN** the asset surface marks the asset as missing
- **AND** keeps its purpose and context information visible
- **AND** keeps removal available

#### Scenario: No automatic repair occurs
- **WHEN** a missing asset is displayed
- **THEN** FusionCanvas does not prompt for relinking, does not search for the file, and does not modify the asset record as part of displaying it

### Requirement: Asset removal is confirmed and complete
FusionCanvas SHALL permanently remove an asset only after explicit confirmation, removing the asset record and its context links atomically and deleting the managed workspace file after the persisted removal succeeds, while cancellation SHALL leave all state unchanged.

#### Scenario: User confirms removal
- **WHEN** the user confirms removal of an asset
- **THEN** FusionCanvas removes the asset record and its links in one persisted operation
- **AND** deletes the managed workspace file on a best-effort basis after the save succeeds
- **AND** updates the asset surface selection to a remaining asset or the empty state

#### Scenario: User cancels removal
- **WHEN** the user cancels the removal confirmation
- **THEN** FusionCanvas leaves the asset record, links, managed file, and current selection unchanged

#### Scenario: Persistence fails during removal
- **WHEN** the snapshot save fails during a confirmed removal
- **THEN** FusionCanvas keeps the asset record and does not delete the managed file
- **AND** reports a recoverable error
- **AND** preserves the user's context so removal can be retried

#### Scenario: Removal unblocks dependent deletion guards
- **WHEN** the last asset linked to a listing has been removed
- **THEN** the existing listing permanent-deletion guard no longer treats that asset as a blocker

### Requirement: Asset relationships survive workspace reorganization
FusionCanvas SHALL preserve asset records and their context links when listings or groups move within their store, and SHALL keep unlinked assets reachable at store level when an accepted deletion cascade removes their link targets.

#### Scenario: Listing moves to another topic
- **WHEN** a listing with linked assets moves to another active topic in its store
- **THEN** the asset records, links, purposes, and managed files are unchanged
- **AND** the listing's asset surface shows the same assets after the move

#### Scenario: Group deletion cascade unlinks an asset
- **WHEN** an accepted group permanent deletion removes the entity an asset was linked to
- **THEN** the asset record and managed file remain
- **AND** the asset appears in the store-level asset view with an unlinked indicator

### Requirement: Asset operations persist atomically
FusionCanvas SHALL persist asset import, purpose relabeling, and removal as atomic snapshot operations in the active workspace database, reloading the latest snapshot before each mutation and saving once per operation.

#### Scenario: Workspace reloads after asset operations
- **WHEN** a successful import, relabel, or removal is followed by an application or workspace database reload
- **THEN** the assets, links, purposes, and file references match the last successful persisted state

### Requirement: Asset surface follows shared desktop control guidance
FusionCanvas SHALL present the focused asset surface with compact action sizing, tooltips for icon-only commands, keyboard-reachable import, relabel, removal, and cancellation, busy states that prevent duplicate submission, and error states that preserve selection and window context.

#### Scenario: Import is in progress
- **WHEN** an import file copy or save is running
- **THEN** FusionCanvas disables repeated import, relabel, and removal actions until the operation completes
- **AND** reports success or an actionable error without closing the surface

#### Scenario: User completes import from the keyboard
- **WHEN** the user activates the import action without a pointer
- **THEN** the file picker and purpose selection are keyboard accessible
- **AND** the newly imported asset is selected after success
