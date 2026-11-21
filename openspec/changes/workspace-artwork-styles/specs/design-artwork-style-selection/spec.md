## ADDED Requirements

### Requirement: Design uses a selected workspace style for artwork generation
FusionCanvas SHALL expose the active workspace's styles in the Design-stage artwork style selector, SHALL default an Item with no valid saved style selection to the built-in `General` style, and SHALL persist the selected style identity with the Item. A custom style's description SHALL be included as visual-style guidance in the compiled default artwork prompt. Selecting a custom style SHALL replace the linked Niche's visual-style guidance in that prompt; other applicable Niche context, including audience, humor style, and constraints, SHALL remain eligible. General SHALL contribute no custom style description or reference images.

#### Scenario: Existing Item opens with General selected
- **WHEN** a creator opens Design for an Item with no saved style selection
- **THEN** General is visibly selected
- **AND** the default prompt contains neither custom style guidance nor the linked Niche's visual-style field
- **AND** other current Niche context remains available to prompt compilation

#### Scenario: Creator selects a custom style
- **WHEN** a creator selects a custom style for an Item
- **THEN** the selection persists with that Item and is restored when Design is reopened
- **AND** the default prompt includes that style's title and description
- **AND** the default prompt omits the linked Niche's visual-style guidance
- **AND** applicable non-visual Niche context remains in the prompt

#### Scenario: Creator changes style while using the compiled default prompt
- **WHEN** a creator changes the selected style while no deliberate prompt override is active
- **THEN** FusionCanvas recompiles the prompt using the newly selected style and current saved context
- **AND** generation uses the newly selected style's reference images

#### Scenario: Creator has an explicit prompt override
- **WHEN** a creator has saved an explicit artwork prompt override and changes the selected style
- **THEN** FusionCanvas preserves the exact prompt override according to the existing prompt-editing behavior
- **AND** generation still uses the currently selected style's reference images
- **AND** resetting to the compiled default rebuilds the prompt with the currently selected style description

#### Scenario: Selected style is deleted elsewhere
- **WHEN** a style selected by the current Item is deleted through style management
- **THEN** that Item's selection becomes General
- **AND** the Design selector and compiled default reflect General when next loaded or refreshed

### Requirement: Artwork generation sends the selected style references when supported
For a selected custom style, FusionCanvas SHALL load all of its available managed reference images and send them as image references with the artwork-generation request. It SHALL send no style reference images for General. Generation SHALL be unavailable with an actionable explanation when a reference is missing, cannot be encoded in a supported format, or the selected endpoint does not advertise capacity for every reference; FusionCanvas SHALL NOT silently omit or truncate style references. Existing text-to-image generation with General SHALL remain independent of image-reference capability.

#### Scenario: Endpoint supports every selected style reference
- **WHEN** the selected custom style has available reference images and the selected image endpoint advertises capacity for all of them
- **THEN** the generation request contains every style reference in the style's saved order
- **AND** the request contains the compiled or explicitly overridden prompt text

#### Scenario: Endpoint cannot accept every style reference
- **WHEN** the selected endpoint advertises fewer input references than the selected style contains
- **THEN** the artwork-generation action is disabled with an explanation that identifies the reference-capacity limitation
- **AND** FusionCanvas does not submit a request with a partial reference set

#### Scenario: Selected style reference is missing or unusable
- **WHEN** a selected custom style has a missing managed image or a reference that cannot be prepared for the selected endpoint
- **THEN** FusionCanvas disables generation with actionable guidance
- **AND** does not submit a request containing only the remaining references

#### Scenario: General is selected with a reference-incompatible endpoint
- **WHEN** General is selected and the image endpoint does not support image references
- **THEN** artwork generation remains available when its other requirements are met
- **AND** the request contains no input reference images
