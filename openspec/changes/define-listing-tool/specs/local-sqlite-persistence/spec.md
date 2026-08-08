## ADDED Requirements

### Requirement: SQLite persists listing preparation as an additive extension
The local SQLite repository SHALL persist the listing-preparation aggregate and its optional strategy, ownership, provider/channel, publication, synchronization, error, and conflict state without replacing or duplicating the existing Item, tag, asset, or catalog records.

#### Scenario: Listing preparation round-trips
- **WHEN** a workspace containing manual listing preparation data is saved and reloaded
- **THEN** common listing values, strategy state, field ownership, readiness/publication state, and references are reconstructed with their stable identities and values intact

#### Scenario: Optional provider state round-trips
- **WHEN** a workspace contains a Shopify binding, channel state, provider metadata, or publish/sync diagnostics
- **THEN** those optional values round-trip without changing the canonical Item, tag, asset, or catalog relationships

### Requirement: Listing migration is additive and no-data-loss
SQLite schema migration SHALL add listing-preparation storage transactionally, preserve existing workspaces as valid manual listings, and leave confirmed common data unchanged when optional provider fields are absent.

#### Scenario: Older workspace migrates
- **WHEN** a pre-listing-preparation workspace is opened
- **THEN** migration creates the required optional listing storage and valid manual listing state for existing Items
- **AND** preserves every existing Item identity, title, description, tag link, asset link, catalog reference, archive state, status, stage, and metadata value

#### Scenario: Migration fails
- **WHEN** an additive listing migration cannot complete
- **THEN** the transaction rolls back
- **AND** the repository does not expose a partially migrated listing snapshot

### Requirement: Listing persistence validates ownership and references
The SQLite repository SHALL reject listing-preparation rows whose Item, Store, provider/channel identity, ownership metadata, or media/product/variant references are invalid or cross-store, and SHALL preserve atomic save behavior.

#### Scenario: Cross-store listing reference is saved
- **WHEN** listing data references an asset, product, variant, or channel binding outside the Item's Store
- **THEN** the repository rejects the snapshot with actionable validation
- **AND** leaves confirmed persisted data unchanged
