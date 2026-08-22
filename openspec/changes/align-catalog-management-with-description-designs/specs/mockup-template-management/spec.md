## MODIFIED Requirements

### Requirement: Mockup Templates connect provider images to one Design Area
FusionCanvas SHALL provide focused management of Mockup Templates for one Blueprint Offering. It SHALL present a scannable template collection beside one selected-or-new editor whose provider image and visual Design Area mapping are more prominent than supporting configuration. Each template SHALL identify a provider-catalog mockup image, one authoritative target Design Area from the same Offering, and applicable Variant coverage derived through the existing color-level template binding and Design Area compatibility rules.

#### Scenario: User opens Mockup Template management
- **WHEN** the user opens Mockup Template management for a Blueprint Offering
- **THEN** FusionCanvas lists only templates belonging to that Offering
- **AND** each template summary identifies its name, target Design Area, applicable Color or derived Variant summary, revision, and lifecycle state
- **AND** the selected template opens in a focused editor

#### Scenario: Mockup Template management preserves master-detail composition
- **WHEN** Mockup Template management has one or more records or an active draft
- **THEN** FusionCanvas presents the template collection and one focused selected-or-new editor as visually distinct peer regions
- **AND** divides the editor into a prominent provider-image and visual-mapping region plus a supporting configuration region
- **AND** groups identity, target Design Area, Color applicability, numeric mapping, Advanced provider data, and save actions in the supporting region
- **AND** may stack those regions only when available width requires it

#### Scenario: Provider image is unavailable
- **WHEN** Manual strategy or unavailable provider-catalog data supplies no selectable mockup image
- **THEN** FusionCanvas shows a clear unavailable or empty state inside the preview region
- **AND** does not fabricate an image or provider reference

#### Scenario: User creates a template from a provider-catalog image
- **WHEN** the user chooses an available provider-catalog mockup image, a Design Area from the same Offering, and valid color-level applicability
- **THEN** FusionCanvas creates a template draft linked to that image and authoritative Design Area identity
- **AND** derives compatible concrete size/color Variants rather than persisting per-size template overrides

#### Scenario: Target Design Area is incompatible
- **WHEN** the selected Design Area does not cover every concrete Variant implied by the template's color-level applicability
- **THEN** FusionCanvas rejects confirmation and identifies the incompatible Variants
- **AND** never silently accepts a partially compatible template

#### Scenario: Offering has no Design Areas
- **WHEN** the user opens template management before any Design Area exists
- **THEN** FusionCanvas shows a blocked empty state explaining that a Design Area is required
- **AND** provides a route back to Design Area management without fabricating a target
