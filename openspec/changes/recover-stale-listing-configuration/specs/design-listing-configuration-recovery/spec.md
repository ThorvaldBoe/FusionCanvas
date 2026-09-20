## ADDED Requirements

### Requirement: Stale listing configurations expose one constrained recovery path
FusionCanvas SHALL identify an Item listing configuration as stale when its persisted Blueprint Offering is missing, archived, or otherwise unavailable for active selection. When the Item is at Design and would otherwise be editable, FusionCanvas SHALL keep the stale configuration visible for review, SHALL keep all ordinary Design mutations unavailable, and SHALL expose one recovery path containing only active Blueprint Offerings owned by the same Store.

#### Scenario: Otherwise editable Item has a stale configuration
- **WHEN** a Draft Item at its current Design stage has a persisted listing configuration whose Blueprint Offering is no longer active
- **THEN** FusionCanvas shows the stale configuration and explains that it must be replaced before Design work can continue
- **AND** the recovery control is enabled with active same-Store Offerings while colors, rows, slots, artwork generation, Supporting Image mutation, and other Design mutations remain unavailable

#### Scenario: Item is protected for another reason
- **WHEN** the Item or Store is archived, Published, Rejected, not at its editable Design stage, or otherwise protected independently of the stale configuration
- **THEN** FusionCanvas keeps the stale configuration reviewable
- **AND** does not enable configuration recovery or any other Design mutation

#### Scenario: No active replacement is available
- **WHEN** an otherwise recoverable Item has no active Blueprint Offering in its Store
- **THEN** FusionCanvas keeps the Design surface reviewable and does not offer an invalid replacement
- **AND** provides actionable guidance to create or restore an Offering in Store Editor

### Requirement: Recovery requires explicit informed confirmation
FusionCanvas SHALL treat choosing a replacement as a pending recovery rather than an immediate persistence action. Before mutation, FusionCanvas SHALL name the stale and replacement configurations and SHALL explain that Offering-specific Design colors, variant rows, row-color relationships, slot assignments, and artwork target/transparency preferences will be reset while the Item's creative history, managed asset files, Supporting Images, generic relationships, workflow/lifecycle state, and downstream Listing data remain stored.

#### Scenario: Creator chooses a replacement
- **WHEN** the creator selects an active same-Store Offering from the stale-configuration recovery control
- **THEN** FusionCanvas presents a confirmation with the stale and replacement Offering identities and the reset/preservation summary
- **AND** does not persist the replacement before confirmation

#### Scenario: Creator cancels recovery
- **WHEN** the creator cancels the pending recovery by its Cancel action or Escape
- **THEN** FusionCanvas retains the stale listing configuration and every existing relationship unchanged
- **AND** returns focus to the recovery control with the stale configuration still selected for review

### Requirement: Confirmed recovery resets only Offering-specific Design relationships
FusionCanvas SHALL replace the stale Item listing configuration and clear its selected Design colors, Design variant rows, row-color relationships, Design slot assignments, and saved artwork target/transparency preferences in one atomic persistence operation. FusionCanvas SHALL preserve the Item and its Idea, Concept, SLL, managed Asset files, Supporting Images, generic Item relationships, workflow/lifecycle state, and downstream Listing data, and SHALL NOT assert that preserved downstream outputs are compatible with the replacement Offering.

#### Scenario: Creator confirms a valid replacement
- **WHEN** the creator confirms an active same-Store replacement for an otherwise recoverable Item
- **THEN** FusionCanvas atomically persists the replacement listing configuration and clears all Offering-specific Design relationships and preferences named by the confirmation
- **AND** preserves the Item's creative history, managed files, Supporting Images, generic relationships, workflow/lifecycle state, and downstream Listing data

#### Scenario: Replacement has different Placeholders or Variants
- **WHEN** the confirmed replacement has Placeholders, Variants, colors, names, dimensions, positions, or provider identities that differ from the stale Offering
- **THEN** FusionCanvas does not automatically match or reattach any prior color, row, slot, or Asset assignment
- **AND** presents Design state derived only from the replacement Offering so the creator can make new explicit assignments

#### Scenario: Recovery validation or persistence fails
- **WHEN** the replacement is missing, archived, from another Store, becomes stale before confirmation, or the atomic save fails
- **THEN** FusionCanvas reports an actionable recoverable error and refreshes replacement availability when applicable
- **AND** preserves the stale configuration and every existing Design and downstream relationship without partial mutation

### Requirement: Successful recovery restores normal Design editing
After successful recovery, FusionCanvas SHALL refresh the Design state from authoritative persistence, select the replacement Offering, restore ordinary Design editing allowed by the Item's current workflow state, and preserve the recovered state across application reload.

#### Scenario: Recovery succeeds
- **WHEN** the atomic replacement completes successfully
- **THEN** the replacement Offering is the selected listing configuration and ordinary Design controls become editable
- **AND** the empty replacement-derived color, row, slot, and artwork-target state is shown without data from the stale Offering

#### Scenario: Recovered Item is reopened
- **WHEN** the workspace or Item is closed and reopened after successful recovery
- **THEN** FusionCanvas reconstructs the replacement listing configuration and its editable Design state
- **AND** the cleared Offering-specific relationships do not reappear while preserved creative, Asset, Supporting Image, and downstream Listing data remain available

