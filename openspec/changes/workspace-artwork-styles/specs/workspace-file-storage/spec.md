## ADDED Requirements

### Requirement: Managed files support workspace style reference ownership
FusionCanvas SHALL allow managed workspace image references to be associated with a workspace-scoped artwork style, SHALL preserve that association through workspace snapshot reconstruction, and SHALL prevent removal of a managed file while any workspace record still references it.

#### Scenario: Style reference association survives reload
- **WHEN** a style and its reference image are saved and the workspace is reopened
- **THEN** the style resolves the same workspace-relative managed file reference
- **AND** the image remains available to style management and artwork generation

#### Scenario: Shared reference remains in use
- **WHEN** a managed image referenced by a style is also referenced by another workspace record and the style reference is removed
- **THEN** FusionCanvas removes only the style association
- **AND** preserves the managed file and other record association
