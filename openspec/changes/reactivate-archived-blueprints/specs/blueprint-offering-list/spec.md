## MODIFIED Requirements

### Requirement: Blueprint lifecycle actions follow the record state
The Store Editor SHALL expose **Archive Blueprint** for an active Blueprint. An archived Blueprint SHALL expose **Reactivate Blueprint** and **Delete permanently** only while its Store is editable and archived Blueprints are shown. Reactivation and permanent deletion SHALL require explicit confirmation. Reactivating a Blueprint SHALL atomically restore the Blueprint and its owned Blueprint Offerings and catalog descendants, preserving stable identities and relationships.

#### Scenario: User reviews an active Blueprint
- **WHEN** an active Blueprint is selected
- **THEN** the Blueprint detail presents Archive Blueprint
- **AND** it does not present Reactivate Blueprint or Delete permanently

#### Scenario: User reviews an archived Blueprint
- **WHEN** an archived Blueprint is selected while archived Blueprints are shown in an editable Store
- **THEN** the detail identifies the Blueprint as archived
- **AND** presents Reactivate Blueprint and Delete permanently
- **AND** does not present Archive Blueprint

#### Scenario: User confirms Blueprint reactivation
- **WHEN** the user confirms Reactivate Blueprint
- **THEN** the Blueprint and its owned Blueprint Offerings, Options, Option Values, Variants, Placeholders, Mockup Templates, and template-owned catalog records are restored atomically
- **AND** every restored record retains its stable identity and relationships
- **AND** existing Item and design relationships remain unchanged
- **AND** the Blueprint remains selected and is identified as active

#### Scenario: User cancels Blueprint reactivation
- **WHEN** the user cancels the reactivation confirmation
- **THEN** the Blueprint and all related records remain unchanged

#### Scenario: User reviews an archived Blueprint in an archived Store
- **WHEN** an archived Blueprint is selected from an archived Store
- **THEN** Reactivate Blueprint and Delete permanently are unavailable
