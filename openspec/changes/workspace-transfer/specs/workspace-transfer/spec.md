# Workspace Transfer (delta)

## ADDED Requirements

### Requirement: Workspace export produces a portable single-file package
FusionCanvas SHALL allow the user to export any single workspace (active or archived) from workspace management to one portable package file that contains the complete workspace subgraph (the workspace, its stores, niches, groups, items, assets, prompts, tags, item-tag links, and asset links, including archived entities) as structured data at the current schema version, plus every managed workspace file referenced by the included assets, plus a manifest describing the package.

#### Scenario: User exports an active workspace
- **WHEN** the user selects an active workspace in workspace management, activates export, and chooses a destination file
- **THEN** FusionCanvas writes one package file containing the workspace's complete structured subgraph and every managed file referenced by the workspace's assets
- **AND** the live workspace, its data, and its files remain unchanged

#### Scenario: Export includes archived content
- **WHEN** the exported workspace contains archived stores, niches, groups, items, or other archived entities
- **THEN** the package includes those archived entities with their archive state preserved

#### Scenario: User exports an archived workspace
- **WHEN** the user exports a workspace that is itself archived
- **THEN** FusionCanvas produces the same complete package as for an active workspace
- **AND** the workspace's archived state is preserved in the package

#### Scenario: Export target already exists
- **WHEN** the user chooses a destination file that already exists
- **THEN** FusionCanvas confirms or replaces the destination only through the platform file dialog behavior
- **AND** a failed or cancelled export never leaves a partially written package at the destination

### Requirement: Export records missing managed files without failing
When an included asset's managed file is absent at export time, FusionCanvas SHALL continue the export, include the asset record in the package, and list the missing file in the manifest and in the completion summary instead of failing the export.

#### Scenario: Asset file is missing during export
- **WHEN** an exported workspace contains an asset whose managed workspace file no longer exists
- **THEN** the export completes and the asset record is included in the package
- **AND** the manifest and the export summary identify the missing file
- **AND** the asset appears as missing after the package is imported elsewhere

### Requirement: Package manifest supports safe pre-flight inspection
Each workspace package SHALL contain a manifest carrying a container format version, the schema version of the embedded structured data, the exporting application version, workspace identity and name, export timestamp, entity counts, the list of packaged files with their workspace-relative paths, and any missing-file notes, so that an importer can decide whether the package is readable before applying any change.

#### Scenario: Importer pre-flights a package
- **WHEN** FusionCanvas opens a workspace package for import
- **THEN** it reads the manifest before touching live state
- **AND** the manifest's format version and schema version determine whether the import proceeds, migrates, or is refused

### Requirement: Import restores a workspace with preserved identity
FusionCanvas SHALL import a valid workspace package as a restored active workspace whose records keep their original stable identities, relationships, timestamps, flexible metadata, and workspace-relative file references, whose descendant records keep their archive states, and whose packaged files are restored into managed workspace storage at their original workspace-relative paths. If the packaged top-level workspace was archived, import SHALL activate that top-level workspace so it can become the current workspace scope.

#### Scenario: Package is imported into an installation without that workspace
- **WHEN** the user imports a valid package into an installation that does not contain the package's entities
- **THEN** the restored workspace, stores, niches, groups, items, assets, prompts, tags, and links reappear with their original identities and relationships
- **AND** every packaged file is available under its original workspace-relative reference
- **AND** previously exported assets are usable and previews resolve from the restored managed copies

#### Scenario: Package is imported into an empty installation
- **WHEN** no workspace exists and the user imports a valid package
- **THEN** the restored workspace becomes the first workspace of the installation
- **AND** the no-workspace state is dismissed

#### Scenario: Archived workspace package is imported
- **WHEN** the user imports a valid package whose top-level workspace was archived at export time
- **THEN** FusionCanvas imports the top-level workspace as active and selects it as the current workspace scope
- **AND** archived descendant stores, niches, groups, items, assets, prompts, and tags retain their archive states

### Requirement: Import is one-shot and refuses duplicate identities
FusionCanvas SHALL refuse to import a package whose entity identities already exist in the installation, and SHALL NOT merge, update, or synchronize an existing workspace from a package.

#### Scenario: Same package is imported twice
- **WHEN** the user imports a package whose workspace and entity identities already exist in the installation
- **THEN** FusionCanvas blocks the import before copying any file or changing any record
- **AND** explains that the workspace already exists in this installation
- **AND** the existing workspace remains unchanged

### Requirement: Import resolves workspace name conflicts by suffixing
When an imported workspace's name conflicts with an existing active workspace's normalized name, FusionCanvas SHALL automatically suffix the imported workspace's name to keep it unique among active workspaces and SHALL report the rename in the completion summary.

#### Scenario: Imported name conflicts with an active workspace
- **WHEN** a valid package's workspace name matches an existing active workspace's normalized name
- **THEN** FusionCanvas imports the workspace under an automatically suffixed unique name
- **AND** the completion summary states the original and final names

#### Scenario: Imported name matches only an archived workspace
- **WHEN** a valid package's workspace name matches an archived workspace's normalized name but no active workspace uses it
- **THEN** FusionCanvas imports the workspace under its original name

### Requirement: Import skips files already present in managed storage
When a packaged file's destination workspace-relative path already exists in managed storage, FusionCanvas SHALL keep the existing file, skip the copy, and count the skip in the completion summary instead of overwriting or failing.

#### Scenario: Restore over files orphaned by a deleted workspace
- **WHEN** the user imports a package after deleting the original workspace whose managed files were left behind in storage
- **THEN** the import completes and reuses the files already present at the packaged paths
- **AND** the completion summary reports how many packaged files were skipped because they already existed

### Requirement: Import refuses packages it cannot safely read
FusionCanvas SHALL refuse a workspace package whose container format version or embedded schema version is newer than the importing application supports, and SHALL migrate packages whose embedded schema version is older through the same migration path used for local workspace databases.

#### Scenario: Package requires a newer application
- **WHEN** the user imports a package whose format version or schema version is newer than the current application supports
- **THEN** FusionCanvas blocks the import before changing any state
- **AND** reports that the package requires a newer FusionCanvas version

#### Scenario: Older package migrates on import
- **WHEN** the user imports a valid package whose embedded schema version is older than the current schema version
- **THEN** FusionCanvas applies the known schema migrations during import
- **AND** the restored workspace matches the migrated data

#### Scenario: Package is corrupt or not a workspace package
- **WHEN** the user selects a file that is not a readable FusionCanvas workspace package
- **THEN** FusionCanvas reports a recoverable error
- **AND** leaves all workspace data and managed files unchanged

### Requirement: Package extraction is hardened against untrusted content
FusionCanvas SHALL treat workspace packages as untrusted input: every extracted entry SHALL be validated as a normalized workspace-relative path before it is written, extraction SHALL reject path traversal outside the intended locations, packaged files SHALL be limited to the supported creative asset extensions, and package reading and writing SHALL stream content instead of loading whole packages into memory.

#### Scenario: Package contains a traversal entry
- **WHEN** a package contains an entry whose path escapes the intended extraction or managed-storage locations
- **THEN** FusionCanvas refuses the import as unsafe
- **AND** no entry is written outside the intended locations

#### Scenario: Package contains an unsupported file type
- **WHEN** a package contains a managed file whose extension is outside the supported creative asset set
- **THEN** FusionCanvas skips that file and imports the workspace without it
- **AND** the affected asset appears as missing after import
- **AND** the completion summary warns about the skipped file

### Requirement: Transfers report progress and support cancellation
FusionCanvas SHALL run export and import as asynchronous operations that report progress and offer cancellation, SHALL disable conflicting transfer and workspace-mutation actions while a transfer runs, and SHALL leave no partial workspace or partial destination package behind when cancelled.

#### Scenario: User cancels an import midway
- **WHEN** the user cancels an import while files are being copied
- **THEN** FusionCanvas stops the operation, removes files already copied for that import on a best-effort basis, and saves no workspace records
- **AND** the pre-import workspace state is fully intact

#### Scenario: User cancels an export midway
- **WHEN** the user cancels an export while the package is being written
- **THEN** FusionCanvas stops the operation and leaves no file or only the pre-existing file at the destination

#### Scenario: Transfer is in progress
- **WHEN** an export or import is running
- **THEN** FusionCanvas shows progress, offers cancellation, and disables repeated transfer and workspace-mutation actions until the operation completes

### Requirement: Transfer failures leave no partial state
FusionCanvas SHALL make import record changes as one atomic persisted operation, SHALL remove newly copied files on a best-effort basis when the persisted import fails, and SHALL write export packages through a temporary file that replaces the destination only when the package is complete.

#### Scenario: Persistence fails after files were copied
- **WHEN** file copying succeeds but the merged workspace snapshot cannot be saved
- **THEN** FusionCanvas removes the files it copied for that import on a best-effort basis
- **AND** reports a recoverable error
- **AND** leaves the last confirmed persisted state intact

### Requirement: Import reports a completion summary and selects the restored workspace
FusionCanvas SHALL present a completion summary after an export or import, including entity counts, restored or written file counts, skipped existing files, missing files, skipped unsupported files, and any workspace rename, and SHALL select the imported workspace as the active workspace after a successful import.

#### Scenario: Import completes
- **WHEN** an import finishes successfully
- **THEN** FusionCanvas shows the summary with counts and warnings
- **AND** the restored workspace becomes the active workspace scope

### Requirement: Transfer actions live in focused workspace surfaces
FusionCanvas SHALL expose workspace export and import from the workspace-management surface, and SHALL additionally expose import as a secondary action on the no-workspace state in the main window, without adding any persistent main-window surface for transfer features.

#### Scenario: User exports from workspace management
- **WHEN** the user opens workspace management and selects a workspace
- **THEN** an export action is available for that workspace within the workspace-management surface

#### Scenario: User imports with no workspaces present
- **WHEN** no workspace exists and the main window shows the no-workspace state
- **THEN** a secondary import action is available next to workspace creation
- **AND** a successful import dismisses the no-workspace state

#### Scenario: Import from the no-workspace state fails
- **WHEN** an import started from the no-workspace state fails or is blocked
- **THEN** FusionCanvas reports the problem through the workspace-management surface
- **AND** the no-workspace state remains available for another attempt

#### Scenario: Main window stays free of persistent transfer controls
- **WHEN** at least one workspace exists and the main window is in normal use
- **THEN** no persistent export or import control occupies the main window

### Requirement: Workspace package compatibility policy is documented
FusionCanvas SHALL document a compatibility policy for workspace packages and local workspace databases requiring that every shipped schema version remains migratable by future application versions, and that any deliberately breaking change to the package format or schema is stated explicitly and ships with a migration path.

#### Scenario: Contributor plans a schema-affecting change
- **WHEN** a contributor changes the structured schema or the package container format
- **THEN** the documented policy in `docs/data-model.md` requires backward-compatible migrations for previously shipped versions
- **AND** any intentional break requires an explicit statement and a migration path for existing databases and packages
