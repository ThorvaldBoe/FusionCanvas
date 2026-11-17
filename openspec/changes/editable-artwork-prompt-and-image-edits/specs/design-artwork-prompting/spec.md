## ADDED Requirements

### Requirement: Design artwork prompt is generated from current creative context and can be edited
FusionCanvas SHALL build a deterministic default image prompt from the current Item Concept Triangle, its linked Niche context in Store settings, applicable current composition guidance, contrast choice, and print constraints. FusionCanvas SHALL present the prompt in the Design stage before generation, send the exact current prompt text to the image provider, preserve a creator-edited prompt with the Item, and provide an explicit reset action that replaces the edited prompt with a newly built default from current saved context. Invalid or stale composition guidance SHALL NOT introduce obsolete creative assumptions into the default prompt.

#### Scenario: Current default prompt reflects Concept and Store settings
- **WHEN** a creator opens Design for an Item with saved concept values and linked Niche settings
- **THEN** the prompt editor shows a default prompt containing the current idea, exact phrase, graphic direction, applicable niche tone and visual guidance, selected contrast, target design area, and print constraints
- **AND** repeated compilation with the same workspace state produces the same prompt
- **AND** the default does not repeat the entire Design Triangle from SLL or include product/style assumptions from SLL as composition instructions

#### Scenario: Creator edits and uses the image prompt
- **WHEN** a creator edits the prompt and generates artwork
- **THEN** the exact edited text is sent as the image-generation prompt
- **AND** the edited prompt is saved with the Item and restored when Design is reopened

#### Scenario: Creator resets the prompt after source settings change
- **WHEN** Concept or linked Niche settings have changed since the prompt was edited and the creator chooses Reset to default
- **THEN** FusionCanvas rebuilds the prompt from the current saved source values and current contrast choice
- **AND** replaces the saved edit with that newly compiled default

#### Scenario: Prompt default excludes stale SLL guidance
- **WHEN** the saved SLL source fingerprint does not match the current Concept Triangle
- **THEN** the default prompt omits SLL composition guidance
- **AND** retains the current Concept values and linked Niche settings

### Requirement: Design artwork contrast is independent from image background transparency
FusionCanvas SHALL let the creator choose Light on dark or Dark on light contrast for an Item. The selected choice SHALL be represented in the default prompt as foreground artwork color direction, SHALL persist with the Item, and SHALL remain separate from the transparent or opaque image background option.

#### Scenario: Creator chooses Light on dark
- **WHEN** Light on dark is selected before using the default prompt
- **THEN** the default prompt explicitly directs the model to render light foreground artwork for placement on a dark product
- **AND** the choice does not request a dark background fill

#### Scenario: Creator chooses Dark on light
- **WHEN** Dark on light is selected before using the default prompt
- **THEN** the default prompt explicitly directs the model to render dark foreground artwork for placement on a light product
- **AND** the choice does not request a light background fill

#### Scenario: Creator edits the contrast-bearing prompt
- **WHEN** the creator changes contrast while using the generated default prompt
- **THEN** the current prompt reflects the new contrast direction before generation
- **AND** the choice is restored when Design is reopened

### Requirement: Existing Design artwork can be adjusted using an image and a prompt
FusionCanvas SHALL let a creator submit the current assigned artwork as an image reference together with a distinct adjustment-instructions field. The edit instructions SHALL be saved with the Item and SHALL NOT replace or modify the generation prompt. The edit action SHALL be available only when a current slot image exists, the instructions are nonblank, and the selected image endpoint advertises support for at least one input reference. A successful edit SHALL replace the selected Design slot artwork and preserve the immediate prior artwork as an Item-linked Related Image. A failed or cancelled edit SHALL leave the existing slot assignment and artwork unchanged.

#### Scenario: Creator adjusts the assigned artwork
- **WHEN** a creator enters an adjustment instruction and selects Adjust existing artwork for a slot with assigned artwork
- **THEN** the provider request contains the assigned image as an input reference and the separate adjustment instructions as its prompt
- **AND** a successful result is saved as the slot's current artwork
- **AND** the pre-edit artwork is retained as a Related Image linked to the Item
- **AND** the generation prompt remains unchanged

#### Scenario: Editing is unavailable for an endpoint without image-reference support
- **WHEN** the selected endpoint does not advertise support for an input reference
- **THEN** Adjust existing artwork is disabled with an explanation
- **AND** ordinary text-to-image generation remains available when its requirements are met

#### Scenario: Edit fails, is cancelled, or the source context changes
- **WHEN** an image-edit request fails, is cancelled, or its Item/slot/source artwork changes before persistence
- **THEN** FusionCanvas leaves the existing slot assignment and artwork unchanged
- **AND** reports a recoverable or cancellation result without saving a partial edit
