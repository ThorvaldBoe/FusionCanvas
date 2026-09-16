## MODIFIED Requirements

### Requirement: Catalog edits preserve selected Item targets
FusionCanvas SHALL prefer archive or deactivation for catalog records with dependents and SHALL require explicit safe handling before a user permanently removes a Blueprint, Print Provider, Blueprint Offering, Option Value, Variant, or Placeholder referenced by Items or Mockup Templates. For a Blueprint Offering, the Store Editor SHALL offer a confirmed reversible cascade that archives its active catalog-owned descendants in dependency order while preserving stable identities and relationships. External Item, listing-configuration, or other non-catalog references SHALL remain protected and SHALL be reported as named blockers rather than silently orphaned.

#### Scenario: User removes an unreferenced Placeholder
- **WHEN** the user confirms permanent removal of an unreferenced Placeholder
- **THEN** FusionCanvas removes that Placeholder
- **AND** preserves unrelated Blueprints, offerings, Options, Variants, templates, and Items

#### Scenario: User removes a Placeholder referenced by an Item
- **WHEN** the user requests removal of a Placeholder selected by one or more Items
- **THEN** FusionCanvas blocks removal
- **AND** explains that the Item target must first be cleared or replaced

#### Scenario: User removes a Placeholder referenced by a Mockup Template
- **WHEN** the user requests removal of a Placeholder targeted by one or more Mockup Templates
- **THEN** FusionCanvas blocks removal
- **AND** explains that dependent templates must first be reassigned or removed

#### Scenario: User removes a referenced Color Option Value
- **WHEN** the user requests retirement or removal of a Color Option Value referenced by active template-color configuration
- **THEN** FusionCanvas requires explicit handling of those dependents
- **AND** does not silently orphan or retarget them

#### Scenario: User archives a referenced catalog record
- **WHEN** permanent deletion would violate a dependency and archival is valid
- **THEN** FusionCanvas offers or permits archive/deactivation instead
- **AND** retains stable identity and historical attribution while excluding the record from new active selections

#### Scenario: User archives a Blueprint Offering
- **WHEN** the user confirms the Offering archive cascade
- **THEN** the Offering, its active Options and Option Values, Variants, Placeholders, Mockup Templates, and template-owned catalog records are archived atomically
- **AND** named external blockers prevent the cascade until resolved
