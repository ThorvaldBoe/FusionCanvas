## ADDED Requirements

### Requirement: Workspace transfer includes artwork styles
Workspace export and import SHALL preserve each custom artwork style and its ordered managed reference images as part of the workspace package, alongside the existing workspace subgraph and files.

#### Scenario: Export includes style records and images
- **WHEN** a workspace package is exported with custom artwork styles
- **THEN** the package contains the style records, ordered image references, and every available referenced managed image file

#### Scenario: Import restores style records and images
- **WHEN** a valid package containing custom artwork styles is imported
- **THEN** styles and images are restored with their original stable identities, workspace-relative paths, and ordering
- **AND** Items that selected the styles still resolve those selections

#### Scenario: Missing style reference is noted during export
- **WHEN** a style reference file is missing when its workspace is exported
- **THEN** the export completes and reports the missing file in the package manifest and completion summary
