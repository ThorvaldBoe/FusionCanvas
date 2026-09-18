## ADDED Requirements

### Requirement: Blueprint permanent deletion is available only after archival
FusionCanvas SHALL reject permanent deletion of an active Blueprint. A confirmed permanent deletion request SHALL target an archived Blueprint, remove that Blueprint and its owned catalog and compatibility-projection records atomically, preserve unrelated records, and leave no compatibility projection capable of recreating the deleted Blueprint.

#### Scenario: Active Blueprint cannot be permanently deleted
- **WHEN** a permanent-delete request targets an active Blueprint
- **THEN** the request fails with recoverable guidance to archive the Blueprint first
- **AND** the Blueprint and its relationships remain unchanged

#### Scenario: Archived Blueprint is permanently deleted
- **WHEN** the user confirms permanent deletion of an archived Blueprint with no protected external references
- **THEN** FusionCanvas removes the Blueprint, its owned Blueprint Offerings, Options, Option Values, Variants, Placeholders, Mockup Templates, template-owned records, and compatibility projection records atomically
- **AND** the deleted Blueprint does not reappear after the Store is reloaded or synchronized
- **AND** unrelated Blueprints, catalog records, Items, and assets remain unchanged

#### Scenario: Permanent deletion is blocked by protected references
- **WHEN** an archived Blueprint or one of its owned offerings or Placeholders is still referenced by an Item listing configuration, design slot assignment, or another protected external relationship
- **THEN** FusionCanvas blocks permanent deletion
- **AND** identifies the blocking relationship type and target where available
- **AND** leaves the Blueprint and all relationships unchanged

### Requirement: Blueprint archive and deletion confirmations describe the consequence
Blueprint lifecycle confirmations SHALL identify whether the operation is reversible archival or irreversible permanent deletion. Archive confirmation SHALL state that connected catalog configuration is archived and preserved; permanent-delete confirmation SHALL state that the archived Blueprint and its owned records cannot be recovered.

#### Scenario: User confirms archive
- **WHEN** the user opens archive confirmation for an active Blueprint
- **THEN** the confirmation describes the reversible archive cascade
- **AND** it does not describe the operation as permanent deletion

#### Scenario: User confirms permanent deletion
- **WHEN** the user opens permanent-delete confirmation for an archived Blueprint
- **THEN** the confirmation names the Blueprint and warns that deletion cannot be undone
