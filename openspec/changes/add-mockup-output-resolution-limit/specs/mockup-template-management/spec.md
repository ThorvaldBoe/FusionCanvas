## ADDED Requirements

### Requirement: Store-wide mockup resolution is clearly scoped in mockup configuration
The Store mockup configuration surface SHALL present the `Maximum mockup long edge (px)` field as Store-wide configuration, separate from template-specific source-image metadata and placement mapping. The field SHALL provide concise guidance that generated mockups are scaled proportionally, smaller source images are not enlarged, and changing the value affects newly generated mockups only.

#### Scenario: Creator reviews the resolution setting
- **WHEN** the creator opens the editable mockup configuration surface for an active Store
- **THEN** the surface shows one clearly labeled Store-wide maximum-long-edge field
- **AND** it is visually distinct from Color applicability and image-space placement fields
- **AND** the helper text explains proportional downscaling and the treatment of existing outputs

#### Scenario: Archived Store is reviewed
- **WHEN** the creator opens mockup configuration for an archived Store
- **THEN** the Store-wide resolution field is visible with its saved or default value
- **AND** the field is read-only with the rest of the mockup configuration
