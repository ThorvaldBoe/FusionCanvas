## ADDED Requirements

### Requirement: Store mockup output resolution is configurable
FusionCanvas SHALL expose a store-scoped `Maximum mockup long edge (px)` setting in the Store mockup configuration surface. The setting SHALL accept only a positive whole-pixel value, SHALL default to 2000 pixels when no value has been saved, and SHALL preserve existing store metadata and configuration when read or written.

#### Scenario: Store opens without a saved resolution
- **WHEN** the creator opens mockup configuration for a Store whose saved metadata has no maximum mockup long-edge value
- **THEN** the setting shows 2000 pixels
- **AND** the Store remains otherwise unchanged until the creator saves a different value

#### Scenario: Creator saves a valid resolution
- **WHEN** the creator enters a positive whole-pixel value and saves the Store mockup configuration
- **THEN** FusionCanvas persists that value as Store-scoped configuration
- **AND** the value is shown when the Store mockup configuration is reopened

#### Scenario: Creator enters an invalid resolution
- **WHEN** the creator enters an empty, non-numeric, zero, negative, fractional, or otherwise invalid value
- **THEN** FusionCanvas shows inline validation guidance
- **AND** prevents saving the invalid value
- **AND** preserves the last valid saved value

### Requirement: Mockup output resolution preserves image proportions
When generating a new mockup, FusionCanvas SHALL scale the composed output proportionally so its longest edge is no greater than the Store's configured maximum. FusionCanvas SHALL NOT enlarge a source template whose longest edge is already below the configured maximum, SHALL preserve the source template aspect ratio, and SHALL leave the source image and saved image-space mapping unchanged.

#### Scenario: Large template is generated
- **WHEN** a ready Mockup Template source image exceeds the Store's configured maximum long edge and the creator applies the template
- **THEN** the generated mockup's longest edge is no greater than the configured maximum
- **AND** the generated mockup preserves the source template aspect ratio
- **AND** artwork remains positioned within the mapped Design Area

#### Scenario: Small template is generated
- **WHEN** a ready Mockup Template source image is smaller than the Store's configured maximum long edge and the creator applies the template
- **THEN** FusionCanvas stores the generated mockup at the source template dimensions
- **AND** it does not upscale the image

#### Scenario: Resolution setting changes after outputs exist
- **WHEN** the creator changes and saves the Store resolution setting after generated mockups already exist
- **THEN** existing generated mockups remain unchanged
- **AND** later generation uses the newly saved value

### Requirement: Generated mockup resolution is attributable
Each newly generated mockup SHALL retain the existing Item Color, template, template revision, and source Design attribution and SHALL additionally record the effective rendered width, rendered height, and resolution-policy value used for that output.

#### Scenario: Generated output records effective dimensions
- **WHEN** FusionCanvas successfully persists a generated mockup
- **THEN** its metadata identifies the effective rendered width and height and the Store resolution value used
- **AND** the output remains attributable to its source template revision and Design Asset
