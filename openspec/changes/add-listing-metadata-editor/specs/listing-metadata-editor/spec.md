## ADDED Requirements

### Requirement: Listings carry marketplace-preparation metadata
FusionCanvas SHALL allow a listing to carry optional marketplace-preparation metadata — price with optional currency, marketplace notes, product type, provider product reference, shipping profile notes, and draft keywords — stored as documented listing metadata keys, and SHALL preserve unknown metadata keys during edits.

#### Scenario: User edits marketplace metadata
- **WHEN** the user edits one or more marketplace-preparation fields for a selected listing and saves
- **THEN** FusionCanvas persists the values as documented listing metadata keys atomically
- **AND** preserves the listing's identity, topic, archive state, status, tags, assets, prompts, concepts, designs, mockups, and unknown metadata

#### Scenario: User omits marketplace metadata
- **WHEN** a listing is created or edited without marketplace-preparation fields
- **THEN** FusionCanvas creates or updates the listing without fabricated marketplace metadata
- **AND** omitted fields do not block creation or editing

#### Scenario: Price is validated as a non-negative decimal
- **WHEN** the user enters a price value
- **THEN** FusionCanvas accepts a non-negative decimal with an optional currency code
- **AND** rejects non-numeric or negative values with an actionable error
- **AND** leaves the persisted listing unchanged on rejection

### Requirement: Listing Metadata Editor provides a focused surface
FusionCanvas SHALL provide a focused Listing Metadata Editor surface or clearly delineated section that edits marketplace-preparation metadata for a selected listing, with explicit save, dirty tracking, unsaved-change prompts, and shared desktop control guidance.

#### Scenario: User opens the metadata editor
- **WHEN** the user invokes Edit Marketplace Metadata for a selected listing
- **THEN** FusionCanvas opens the focused surface or section with that listing preselected
- **AND** keeps canonical tree and document context intact
- **AND** loads existing marketplace-preparation metadata into editable fields

#### Scenario: User saves marketplace metadata
- **WHEN** the user edits marketplace-preparation fields and invokes save
- **THEN** FusionCanvas persists the changes atomically
- **AND** clears the dirty state for the marketplace-metadata section without affecting the creative-notes section

#### Scenario: User leaves meaningful unsaved marketplace metadata
- **WHEN** the focused surface contains meaningful unsaved marketplace-metadata changes and the user switches listing or closes
- **THEN** FusionCanvas asks whether to save, discard, or cancel
- **AND** retains the current draft and focus when cancellation is chosen

#### Scenario: Persistence fails during a metadata save
- **WHEN** the repository cannot save a marketplace-metadata edit after an optimistic projection
- **THEN** FusionCanvas reports a recoverable error
- **AND** preserves the draft and focus so the user can retry
- **AND** leaves the persisted listing unchanged

### Requirement: Creative notes and publishing metadata are visibly separated
FusionCanvas SHALL keep creative description and notes (owned by listing-management) in a distinct section from marketplace-preparation metadata (owned by this capability), SHALL use independent save actions for each section, and SHALL label the two sections so creators can distinguish in-house creative notes from publishing preparation data.

#### Scenario: User edits creative notes and marketplace metadata independently
- **WHEN** the user edits creative notes in the creative-notes section and marketplace metadata in the publishing-metadata section
- **THEN** saving one section does not save the other
- **AND** each section tracks its own dirty state independently

#### Scenario: User switches listings with one section dirty
- **WHEN** the user switches listings while one section has meaningful unsaved changes and the other does not
- **THEN** FusionCanvas prompts about the dirty section only
- **AND** leaves the clean section unchanged

### Requirement: Marketplace metadata inherits earlier-stage context for review
FusionCanvas SHALL resolve the active store, niche, topic path, selected item, selected concept, selected final designs, mockups, and inherited tags and metadata from the current context when the Listing Metadata Editor is opened, and SHALL expose them as read-only reference so the creator can prepare publishing metadata from approved creative context.

#### Scenario: Editor shows inherited creative context
- **WHEN** the user opens the Listing Metadata Editor for a listing with a selected concept and final designs
- **THEN** FusionCanvas shows the selected concept, final designs, and inherited tags as read-only reference
- **AND** does not auto-populate marketplace fields from inherited context without explicit user action

#### Scenario: Earlier-stage context is missing
- **WHEN** the user opens the editor for a listing with no selected concept or final designs
- **THEN** FusionCanvas shows a readiness note that earlier-stage context is incomplete
- **AND** still allows marketplace-metadata editing

### Requirement: Marketplace metadata survives listing reorganization and lifecycle
FusionCanvas SHALL preserve marketplace-preparation metadata when a listing moves within its store, is archived and restored, or is duplicated, and SHALL keep the metadata connected to the listing without requiring re-entry.

#### Scenario: Listing moves to another topic
- **WHEN** a listing with marketplace metadata moves to another active topic in its store
- **THEN** the marketplace-preparation metadata is unchanged
- **AND** the metadata editor shows the same values after the move

#### Scenario: Listing is archived and restored
- **WHEN** a listing with marketplace metadata is archived and later restored
- **THEN** the marketplace-preparation metadata is preserved through archive and restore
- **AND** the metadata editor shows the same values after restore

#### Scenario: Listing is duplicated
- **WHEN** a listing with marketplace metadata is duplicated
- **THEN** the duplicate receives a copy of the marketplace-preparation metadata
- **AND** the duplicate's metadata is independent of the source after duplication

### Requirement: Marketplace metadata operations persist atomically
FusionCanvas SHALL persist marketplace-metadata edits as atomic snapshot operations in the active workspace database, reloading the latest snapshot before each mutation and saving once per operation.

#### Scenario: Workspace reloads after metadata operations
- **WHEN** a successful marketplace-metadata operation is followed by an application or workspace database reload
- **THEN** the resulting marketplace-preparation metadata matches the last successful persisted state

#### Scenario: Metadata editor prevents duplicate submission
- **WHEN** a marketplace-metadata save is running or fails validation or persistence
- **THEN** FusionCanvas prevents duplicate submission
- **AND** keeps the surface available to report success or an actionable error
- **AND** preserves the draft and selection after failure
