## ADDED Requirements

### Requirement: Generated artwork records preserve reviewable provenance
FusionCanvas SHALL preserve generated artwork as a managed PNG asset that remains visible through the Item's Design supporting-image surface even after the asset is replaced or removed from a Design Area slot. Generated provenance SHALL distinguish the generated asset from a manually imported supporting image and SHALL preserve the target area, model identity, requested and final dimensions, transparency request, and generation time without storing credentials or secrets.

#### Scenario: User reviews generated history
- **WHEN** a generated artwork asset is listed in Supporting Images
- **THEN** the surface identifies it as generated artwork
- **AND** the user can preview, download, or remove it using the existing asset actions

#### Scenario: Generated artwork is removed from a slot
- **WHEN** the user removes generated artwork from its Design Area slot
- **THEN** the slot becomes empty
- **AND** the generated asset remains reviewable as a supporting image until explicitly removed

#### Scenario: Generated asset is removed
- **WHEN** the user explicitly removes a generated asset from Supporting Images
- **THEN** the existing confirmed asset-removal policy applies
- **AND** any slot assignment referencing the asset is removed or blocked according to the existing Design dependency policy

