## ADDED Requirements

### Requirement: Archived Blueprint Offering deletion is dependency-safe and atomic
FusionCanvas SHALL keep archived Blueprint Offerings and their catalog-owned descendants available for review and restoration until an explicitly confirmed permanent deletion succeeds. Permanent deletion SHALL be blocked by named external Item, listing-configuration, design-slot, or other protected references rather than detaching or orphaning those relationships. Archived Stores SHALL remain read-only and SHALL reject Offering restore or permanent deletion requests.

#### Scenario: Archived Offering remains reviewable before deletion
- **WHEN** the user opens an archived Offering from the Blueprint-scoped archived Offering list
- **THEN** FusionCanvas shows its archived identity and catalog-owned setup summary
- **AND** the user can choose restore or permanent deletion without editing catalog fields

#### Scenario: Offering deletion reports named blockers
- **WHEN** permanent deletion finds one or more protected references to the Offering or its descendants
- **THEN** the result names the blocking record types and display names where available
- **AND** identifies clearing or remapping those references as the next safe action
- **AND** no partial deletion or relationship detachment occurs

#### Scenario: Offering deletion preserves Store isolation
- **WHEN** the user permanently deletes an archived Offering
- **THEN** only records owned by that Offering in the selected Store are removed
- **AND** records belonging to other Offerings, Blueprints, or Stores remain unchanged

