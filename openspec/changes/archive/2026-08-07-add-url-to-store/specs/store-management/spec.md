## ADDED Requirements

### Requirement: Store management records a store URL

FusionCanvas SHALL allow users to provide an optional storefront URL for a store and SHALL persist it with the store's context as store-scoped data.

#### Scenario: User creates a store with a URL

- **WHEN** the user creates a store and enters a URL in the store editor Basic info tab before saving
- **THEN** FusionCanvas persists the URL with the store as part of its store context
- **AND** the URL is available for the saved store after the application reloads the workspace database

#### Scenario: User edits a store URL

- **WHEN** the user edits the URL of an existing store in the store editor and saves
- **THEN** FusionCanvas persists the updated URL with that store
- **AND** the update does not require changes to child niches, groups, listings, tags, or assets

#### Scenario: Store URL is optional

- **WHEN** the user creates or saves a store without providing a URL
- **THEN** FusionCanvas does not require a URL
- **AND** the store saves successfully with no URL recorded

#### Scenario: Store URL is workspace- and store-scoped

- **WHEN** a store has a recorded URL
- **THEN** the URL belongs to that store in its workspace
- **AND** other stores do not share or inherit the URL