## ADDED Requirements

### Requirement: Printify catalog import commits atomically and round-trips
SQLite persistence SHALL commit one confirmed Printify catalog import atomically for the selected Store, retain stable provider identities, and reconstruct imported catalog records and relationships after workspace reload.

#### Scenario: Confirmed import is persisted
- **WHEN** a valid confirmed import completes
- **THEN** all created and updated catalog records are committed in one transaction
- **AND** a failure leaves the Store catalog at its pre-import state

#### Scenario: Imported Store is reopened
- **WHEN** the workspace is closed and reopened after a successful import
- **THEN** the imported Blueprints, offerings, options, variants, and design areas/placeholders retain their identities, provider keys, and compatibility relationships

### Requirement: Repeated imports do not duplicate provider records
Persistence SHALL enforce or otherwise validate Store-scoped uniqueness for imported provider identities across Blueprints, offerings, options, option values, variants, and design areas/placeholders.

#### Scenario: Duplicate provider payload is imported
- **WHEN** a repeated import contains an identity already stored for the same Store
- **THEN** the existing record is updated in place
- **AND** no duplicate provider-identity row is persisted
