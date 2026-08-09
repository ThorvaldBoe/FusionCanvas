## MODIFIED Requirements

### Requirement: Design stage supports optional Store design targets
FusionCanvas SHALL let an editable Item at Design select zero or more configured Offering Placeholders from its own Store without requiring a target to continue design work.

#### Scenario: User designs without configured targets
- **WHEN** an editable Item reaches Design with no selected Offering Placeholders
- **THEN** FusionCanvas presents the existing design-file workflow
- **AND** does not block design-file work or progression because no target is selected

#### Scenario: User selects multiple compatible Placeholders
- **WHEN** the user selects two or more Offering Placeholders configured for the Item's Store
- **THEN** FusionCanvas persists the complete selected set atomically
- **AND** each selected Placeholder is displayed as design guidance after reload

#### Scenario: User attempts cross-Store target selection
- **WHEN** a target request refers to an Offering Placeholder from another Store
- **THEN** FusionCanvas rejects the request
- **AND** preserves the Item's prior selected targets

### Requirement: Target selection respects workflow editability
FusionCanvas SHALL expose selected Offering Placeholders read-only whenever the Item's active Design content is read-only.

#### Scenario: User reviews Design from a protected context
- **WHEN** Design-stage editing is unavailable because the Item is protected or an earlier stage is being reviewed
- **THEN** FusionCanvas shows any persisted Offering Placeholder targets as read-only guidance
- **AND** does not commit a target mutation
