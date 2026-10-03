## ADDED Requirements

### Requirement: Blueprint Offering restore rehydrates its catalog archive cascade
FusionCanvas SHALL expose an explicit restore action for an archived Blueprint Offering in an editable Store. Restoring an Offering SHALL restore the Offering and its catalog-owned descendants from the Offering archive cascade atomically, preserve stable identities and relationships, and leave unrelated records unchanged. The restore action SHALL be unavailable for an active Offering or an archived Store.

#### Scenario: User opens an archived Offering for review
- **WHEN** the user selects an archived Offering while archived Offerings are shown for an active Store
- **THEN** the detail presents **Restore Blueprint Offering**
- **AND** the detail remains read-only until the restore succeeds

#### Scenario: User confirms an Offering restore
- **WHEN** the user confirms restoration of an archived Blueprint Offering
- **THEN** the Offering and its catalog-owned Options, Option Values, Variants, Placeholders, Mockup Templates, and template-owned catalog records become active in one atomic operation
- **AND** stable identities and relationships are preserved
- **AND** the Store Editor refreshes the Blueprint-scoped list with the Offering selected as active

#### Scenario: User cancels an Offering restore
- **WHEN** the user cancels the restore confirmation
- **THEN** the Offering and every descendant remain archived and unchanged

#### Scenario: Archived Store prevents Offering restore
- **WHEN** the selected Store is archived
- **THEN** a restore request is rejected with recoverable guidance
- **AND** the Offering and descendants remain archived

### Requirement: Blueprint Offering permanent deletion removes only its catalog-owned cascade
FusionCanvas SHALL permanently delete a Blueprint Offering only after it is archived and the user explicitly confirms the operation. A successful deletion SHALL remove the Offering and its catalog-owned descendants atomically, including Options, Option Values, Variants, Placeholders, Mockup Templates, template revisions, template-owned source-image and color records, and compatibility projection records owned by the Offering. Unrelated catalog records SHALL remain unchanged.

#### Scenario: Active Offering cannot be permanently deleted
- **WHEN** a permanent-delete request targets an active Blueprint Offering
- **THEN** the request is rejected with recoverable guidance to archive the Offering first
- **AND** the Offering and all relationships remain unchanged

#### Scenario: User confirms deletion of an unreferenced archived Offering
- **WHEN** the user confirms permanent deletion of an archived Offering with no protected external references
- **THEN** FusionCanvas removes the Offering and every catalog-owned descendant in one atomic operation
- **AND** the Offering does not reappear after the Store is reloaded or synchronized
- **AND** unrelated Blueprints, Offerings, catalog records, Items, and assets remain unchanged

#### Scenario: Protected external references block Offering deletion
- **WHEN** an archived Offering or one of its Placeholders is referenced by an Item listing configuration, design-slot assignment, or another protected external relationship
- **THEN** FusionCanvas blocks permanent deletion
- **AND** identifies the blocking relationship type and target where available
- **AND** leaves the Offering, descendants, and external relationships unchanged

#### Scenario: Offering deletion confirmation describes the consequence
- **WHEN** the user opens permanent-delete confirmation for an archived Offering
- **THEN** the confirmation names the Offering, summarizes the catalog-owned cascade, and warns that the deletion cannot be undone
- **AND** cancellation leaves every record unchanged
