## ADDED Requirements

### Requirement: Items have an optional idea-potential rating
FusionCanvas SHALL represent an Item's idea-potential rating as an integer from 0 through 5, where 0 means unrated. Existing Items without a rating SHALL load as unrated. Values outside this range SHALL be rejected at the application boundary and SHALL NOT be persisted.

#### Scenario: New Item starts unrated
- **WHEN** an Item is created without rating input
- **THEN** its rating is 0
- **AND** no fabricated score is shown as persisted creative content

#### Scenario: Existing Item remains compatible
- **WHEN** a workspace containing Items created before this capability is loaded
- **THEN** each Item loads with rating 0
- **AND** all existing Item identity, placement, stage, status, metadata, tags, and files remain unchanged

#### Scenario: Invalid rating is rejected
- **WHEN** an update requests a rating below 0 or above 5
- **THEN** the update fails with a recoverable validation error
- **AND** the previously persisted rating remains unchanged

### Requirement: Users can edit and clear an Item rating
FusionCanvas SHALL show an accessible five-star rating control for an active Item. Selecting star N SHALL set the rating to N; selecting the currently selected rating again or an explicit clear affordance SHALL set it to 0. Rating changes SHALL persist immediately and independently of unrelated text drafts, and SHALL be unavailable for archived or otherwise protected Items.

#### Scenario: User assigns a rating
- **WHEN** an active Item's user selects the fourth star
- **THEN** the Item rating becomes 4 and the control shows four selected stars
- **AND** the change is persisted without requiring a text-field save

#### Scenario: User clears a rating
- **WHEN** the user clears a rated Item or toggles its selected rating off
- **THEN** the rating becomes 0
- **AND** the control communicates that the Item is unrated

#### Scenario: Protected Item cannot be rated
- **WHEN** an archived or otherwise read-only Item is displayed
- **THEN** the rating control is read-only or disabled
- **AND** the existing restore guidance remains available

#### Scenario: Rating remains associated across workflow stages
- **WHEN** an Item with a rating advances, regresses, is archived and restored, or is reopened
- **THEN** the same rating is displayed after reload

### Requirement: Navigation can filter Items by exact rating
FusionCanvas SHALL provide a rating filter with All ratings, Unrated, and exact one-through-five-star options. The filter SHALL apply only to Items, SHALL preserve ancestor topic context, SHALL use AND semantics with every other active filter dimension, and SHALL participate in existing empty-results, clear-all, expansion restoration, and authoritative refresh behavior.

#### Scenario: User filters for unrated Items
- **WHEN** the user selects Unrated
- **THEN** only Items whose rating is 0 appear
- **AND** matching niche/group ancestors remain visible as context

#### Scenario: User filters for an exact score
- **WHEN** the user selects three stars
- **THEN** only Items rated exactly 3 appear
- **AND** Items rated 0, 1, 2, 4, or 5 are hidden

#### Scenario: Rating combines with existing filters
- **WHEN** a rating filter and a tag, text, stage, status, scope, or archived filter are active
- **THEN** an Item appears only when it satisfies every active dimension

#### Scenario: Rating filter has no matches
- **WHEN** the selected rating matches no Item
- **THEN** the navigation tree shows the existing explanatory empty-filter state
- **AND** the user can clear all filters to restore browsing

#### Scenario: Rating mutation refreshes filtered navigation
- **WHEN** an Item's rating changes so it no longer satisfies the active rating filter
- **THEN** its row leaves the filtered tree from authoritative state
- **AND** canonical selection and any open document remain coherent

### Requirement: Rating survives supported workspace data flows
FusionCanvas SHALL preserve the Item rating through workspace save/load, workspace transfer, and any Item import/export surface that claims to round-trip Item metadata. Missing rating data in an older payload SHALL mean 0. Rating persistence SHALL not require a new external service or dependency.

#### Scenario: Rating round-trips through local persistence
- **WHEN** a rated Item is saved and loaded from the same workspace database
- **THEN** its exact rating is restored

#### Scenario: Rating round-trips through workspace transfer
- **WHEN** a workspace containing rated and unrated Items is exported and imported
- **THEN** each Item retains its rating, including 0/unrated
