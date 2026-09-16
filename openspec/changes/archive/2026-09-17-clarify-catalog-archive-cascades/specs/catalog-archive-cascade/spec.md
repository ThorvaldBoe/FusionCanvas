## ADDED Requirements

### Requirement: Blueprint Offerings expose a reversible archive cascade
FusionCanvas SHALL expose an explicit archive action for an active Blueprint Offering. Before confirmation, the action SHALL show the active catalog-owned descendants that will be archived with it: Options, Option Values, Variants, Placeholders, Mockup Templates, and their template-owned catalog records. Confirmation SHALL archive the Offering and those descendants atomically, preserve every stable identity and relationship, and leave unrelated records unchanged.

#### Scenario: User opens an active Offering
- **WHEN** the user opens an active Blueprint Offering in the Store Editor
- **THEN** the Basics surface presents an explicitly labeled **Archive Blueprint Offering** action
- **AND** the action is disabled for an archived Store or already archived Offering

#### Scenario: User reviews an Offering archive cascade
- **WHEN** the user invokes Archive Blueprint Offering
- **THEN** a confirmation surface lists the Offering and each active catalog-owned dependent record grouped by type and name
- **AND** the confirmation explains that the operation is reversible and does not permanently delete records
- **AND** the user can cancel without changing any record

#### Scenario: User confirms an Offering archive cascade
- **WHEN** the user confirms the displayed cascade
- **THEN** the Offering and all listed active catalog-owned descendants become archived in one atomic operation
- **AND** the Store Editor leaves the archived Offering in a reviewable archived state or returns to its owning Blueprint when no active Offering remains

#### Scenario: External dependents block the cascade
- **WHEN** an Offering or its descendants are referenced by an Item, listing configuration, or other record outside the catalog-owned cascade
- **THEN** the confirmation identifies those exact dependent record types and names
- **AND** the operation does not archive or orphan those external relationships
- **AND** the user receives the required resolution path before retrying

### Requirement: Catalog archive operations provide concrete dependency guidance
Existing archive commands SHALL report the exact active dependents that block archiving, including their record type and display name where available, and SHALL identify the next safe action. Generic guidance such as “referenced by active catalog configuration” alone is insufficient when concrete dependent records can be resolved.

#### Scenario: User archives a referenced Variant
- **WHEN** the user requests archive for a Variant referenced by one or more active Placeholders
- **THEN** the recoverable error names each blocking Placeholder and explains that it must be archived, edited to remove the Variant, or otherwise reassigned first
- **AND** the Variant remains active until the dependency is resolved

#### Scenario: User archives an Offering with catalog descendants
- **WHEN** the user requests archive for an Offering with active catalog-owned descendants
- **THEN** the UI offers the archive cascade rather than requiring the user to discover and archive each descendant manually
