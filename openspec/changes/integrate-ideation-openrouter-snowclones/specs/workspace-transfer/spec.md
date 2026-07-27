## MODIFIED Requirements

### Requirement: Workspace export produces a portable single-file package
FusionCanvas SHALL allow the user to export any single workspace (active or archived) to one portable package containing the complete workspace subgraph, including its durable Ideation rejections, as structured data at the current schema version, plus referenced managed files and a manifest. Application-wide Snowclones and OpenRouter credentials SHALL remain outside the package.

#### Scenario: User exports a workspace with rejection history
- **WHEN** the selected workspace contains Ideation rejections belonging to its stores and niches
- **THEN** the package contains those rejections with their stable identity, scope, text, optional reason, mode, and timestamp
- **AND** manifest entity counts include the exported rejections

#### Scenario: Rejections belong to another workspace
- **WHEN** the installation contains rejection history for workspaces other than the exported workspace
- **THEN** that rejection history is absent from the package

#### Scenario: Application-wide or secret data exists
- **WHEN** the installation has confirmed Snowclones or a saved OpenRouter credential
- **THEN** neither Snowclone content nor credential material is included in the package

### Requirement: Import restores a workspace with preserved identity
FusionCanvas SHALL import a valid package as a restored active workspace whose records, including packaged Ideation rejections, preserve stable identities, relationships, timestamps, metadata, and workspace-relative file references. Import SHALL retain all destination records unrelated to the imported workspace, including existing Ideation rejections and application-wide Snowclones.

#### Scenario: Package with rejection history is imported
- **WHEN** a valid package contains Ideation rejections and no packaged identity collides with the destination
- **THEN** the rejections reappear in their original store, niche, and optional group scope
- **AND** their text, optional reason, mode, and timestamp are preserved

#### Scenario: Destination already has rejection history
- **WHEN** a different workspace package is imported into an installation with existing Ideation rejections
- **THEN** every existing rejection remains unchanged
- **AND** packaged rejections are added atomically with the imported workspace

#### Scenario: Older package has no rejection records
- **WHEN** an otherwise valid older package contains no Ideation rejections
- **THEN** the workspace imports with an empty rejection history
- **AND** existing destination rejection history remains unchanged

### Requirement: Import is one-shot and refuses duplicate identities
FusionCanvas SHALL refuse to import a package whose workspace-owned entity identities, including Ideation rejection identities, already exist in the installation, and SHALL NOT merge, update, or synchronize an existing workspace from a package.

#### Scenario: Rejection identity collides
- **WHEN** a package contains an Ideation rejection identity already present in the installation
- **THEN** FusionCanvas blocks import before copying files or changing records
- **AND** the existing workspace and rejection history remain unchanged

#### Scenario: Same package is imported twice
- **WHEN** the user imports a package whose workspace and entity identities already exist in the installation
- **THEN** FusionCanvas blocks the import before copying any file or changing any record
- **AND** explains that the workspace already exists in this installation
- **AND** the existing workspace remains unchanged

