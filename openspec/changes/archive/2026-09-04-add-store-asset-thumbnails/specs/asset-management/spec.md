## MODIFIED Requirements

### Requirement: Context assets are visible from the relevant work
FusionCanvas SHALL provide a focused asset surface for a selected listing, niche, group, or store that lists the assets linked to that context with their names, purposes, file state, and compact previews when the managed file is a supported image, and SHALL list every store-owned asset, including unlinked assets, in the store-level view.

#### Scenario: Existing image assets show thumbnails
- **WHEN** the asset surface lists an asset whose managed workspace file exists and has a supported image extension
- **THEN** FusionCanvas shows a compact thumbnail from the managed workspace copy
- **AND** keeps the asset name, purpose, context, and file state visible

#### Scenario: User opens an enlarged asset preview
- **WHEN** the user activates an available asset thumbnail
- **THEN** FusionCanvas opens a larger in-app preview of the managed workspace copy
- **AND** closing the preview leaves the asset record, selection, and list unchanged

#### Scenario: Preview is unavailable
- **WHEN** an asset is missing, unreadable, or not an image
- **THEN** FusionCanvas does not show a broken thumbnail or attempt external repair
- **AND** keeps the existing metadata and asset actions available
