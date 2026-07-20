## ADDED Requirements

### Requirement: Design entity is part of the core domain model
FusionCanvas SHALL define a Design as a Phase 2 core domain entity that belongs to exactly one listing and preserves one concrete visual implementation of that listing's Design stage.

#### Scenario: Contributor reviews the core model
- **WHEN** a contributor inspects the domain model after this change is accepted
- **THEN** Design is represented as a named domain concept owned by a listing
- **AND** the model still does not require future concepts such as Mockup, Marketplace Product, Performance Record, Plugin Data, or Workflow Template

#### Scenario: Design is item-bound
- **WHEN** a design is represented in the domain model
- **THEN** it belongs to exactly one listing
- **AND** no free-floating design exists outside an item
- **AND** the design is scoped to the Design stage of that item

### Requirement: Design records preserve variants for a listing
FusionCanvas SHALL allow a listing to have zero, one, or many design records, SHALL preserve draft, needs-revision, rejected, and superseded variants without deletion, and SHALL allow each design to record name, version, notes, source method, intended use, cleanup state, and an optional reference to the concept it implements.

#### Scenario: User creates a design from a selected concept
- **WHEN** the user creates a design for a listing that has a selected concept
- **THEN** FusionCanvas creates a new design with stable identity and timestamps
- **AND** references the selected concept as the implemented concept
- **AND** applies applicable inherited tags and metadata from the current context with provenance

#### Scenario: User creates a design directly from a strong phrase and graphic direction
- **WHEN** the user creates a design for a listing without a selected concept but with sufficient phrase and graphic direction
- **THEN** FusionCanvas creates the design without requiring a concept reference
- **AND** records the supplied starting values

#### Scenario: User preserves a superseded variant
- **WHEN** the user promotes a new design as final and a prior design was final
- **THEN** FusionCanvas demotes the prior design from the final-selected collection
- **AND** leaves the prior design available for later review without deletion

#### Scenario: Rejected and needs-revision designs remain available
- **WHEN** a design is rejected or marked needs revision
- **THEN** FusionCanvas preserves the design with its metadata, assets, prompt references, and history
- **AND** keeps it reachable for later review without cluttering the final-selected collection

### Requirement: Design approval states are an explicit state machine
FusionCanvas SHALL manage each design through the approval states draft, needs revision, approved, rejected, exported, and ready for export, SHALL validate state transitions, and SHALL NOT require a specific approval state for promotion to final selected artwork; final selection is marked by a `final` tag and the listing-level final-selected collection, independent of approval state.

#### Scenario: Design starts as draft
- **WHEN** the user creates a new design
- **THEN** FusionCanvas assigns the draft state
- **AND** the design is not part of the final-selected collection

#### Scenario: Any design can be promoted to final
- **WHEN** the user promotes a design variant as final selected artwork
- **THEN** FusionCanvas adds the design to the listing's final-selected collection and marks it with a `final` tag
- **AND** does not require the design to be in an approved or ready-for-export state first
- **AND** does not delete other variants

#### Scenario: Rejected design can still be promoted to final
- **WHEN** the user promotes a design in any approval state (including draft, needs revision, approved, rejected, exported, or ready for export) as final selected artwork
- **THEN** FusionCanvas adds the design to the final-selected collection and marks it with a `final` tag
- **AND** the approval state remains unchanged and independent of the final selection

### Requirement: Final selected designs are a listing-level invariant
FusionCanvas SHALL expose the final-selected designs as a listing-level collection marked by a `final` tag on each member, SHALL require at least one final-selected design before the listing may advance to the Listing stage, SHALL NOT require a specific approval state for final membership, and SHALL preserve demoted designs without deletion.

#### Scenario: Listing advances to Listing with final artwork
- **WHEN** the user requests advancement to the Listing stage and at least one final-selected design exists
- **THEN** the workflow-stage-navigator may accept the transition
- **AND** downstream Listing tools can read the final-selected collection

#### Scenario: Listing cannot advance without final artwork
- **WHEN** the user requests advancement to the Listing stage and no final-selected design exists
- **THEN** FusionCanvas blocks the transition
- **AND** explains that at least one design must be promoted as final first

#### Scenario: User demotes a final design
- **WHEN** the user demotes a final-selected design
- **THEN** FusionCanvas removes it from the final-selected collection
- **AND** leaves the design available for review without deletion

### Requirement: Designs reference assets and prompts without owning them
FusionCanvas SHALL associate a design with assets through the existing asset-management context-link model, SHALL allow AI-assisted designs to reference prompt records and store provider metadata and generation settings as documented design metadata, and SHALL NOT delete referenced assets or prompts when a design is removed.

#### Scenario: User connects a design to an asset
- **WHEN** the user links an asset to a design
- **THEN** FusionCanvas creates the asset context link through asset-management
- **AND** the asset remains owned by its store and reachable from its other contexts

#### Scenario: AI-assisted design references prompt history
- **WHEN** a design was produced through AI generation
- **THEN** the design references the relevant prompt record
- **AND** stores provider metadata and generation settings as documented design metadata
- **AND** removing the design does not delete the prompt record

#### Scenario: Design removal preserves referenced assets and prompts
- **WHEN** a design is removed
- **THEN** FusionCanvas removes the design and its asset context links
- **AND** preserves the underlying asset records and prompt records unchanged

### Requirement: Design intended use and cleanup state are structured metadata
FusionCanvas SHALL store design intended use (dark shirts, light shirts, specific colors, product types, marketplaces, export targets) as structured design metadata fields — not as design-scoped tags — and SHALL store cleanup state (needs upscale, needs transparent cleanup, cropped, print-ready) as documented design metadata, and SHALL preserve unknown metadata keys during edits.

#### Scenario: User records intended use for dark shirts
- **WHEN** the user records that a design targets dark shirts and a specific product type
- **THEN** FusionCanvas stores the intended use as structured design metadata atomically
- **AND** later Listing, mockup, and export tools can read the structured fields

#### Scenario: User records cleanup state
- **WHEN** the user marks a design as needing transparent cleanup or print-ready
- **THEN** FusionCanvas records the cleanup state as documented metadata
- **AND** preserves the design's identity, assets, and prompt references

#### Scenario: Unknown metadata is preserved
- **WHEN** a design carries metadata keys outside the documented design set
- **THEN** design edits preserve those unknown keys unchanged

### Requirement: Design creation uses active context and inherited metadata
FusionCanvas SHALL resolve the active store, niche, topic path, selected item, selected concept when available, tags, and metadata from the current context when creating a design, SHALL apply only inherited values marked applicable to new design work, and SHALL preserve explicit user overrides before save.

#### Scenario: Context provides applicable inherited values
- **WHEN** the user creates a design from a context whose resolved inherited tags or metadata apply to new design work
- **THEN** FusionCanvas applies those values to the design
- **AND** records their inherited provenance

#### Scenario: User overrides an inherited value
- **WHEN** the user changes an inherited value before saving a new design
- **THEN** FusionCanvas persists the user's explicit value
- **AND** does not overwrite it with the inherited default

#### Scenario: Parent context has no applicable values
- **WHEN** the user creates a design in a context with no applicable inherited tags or metadata
- **THEN** FusionCanvas creates the design without fabricated tags or metadata

### Requirement: Design operations persist atomically
FusionCanvas SHALL persist design creation, edit, state transitions, final-selection promotion and demotion, cleanup metadata, and tag-link changes in the active workspace database as atomic snapshot operations, reloading the latest snapshot before each mutation and saving once per operation.

#### Scenario: Workspace reloads after design operations
- **WHEN** a successful design operation is followed by an application or workspace database reload
- **THEN** the resulting designs, approval states, final-selected collection, metadata, and tag links match the last successful persisted state

#### Scenario: Persistence fails during a design operation
- **WHEN** the repository cannot save a design operation after an optimistic projection
- **THEN** FusionCanvas reports a recoverable error
- **AND** restores the last confirmed design, listing, and final-selected state
- **AND** retains recoverable user input needed to retry when applicable

### Requirement: Designs are dependent records for listing deletion
FusionCanvas SHALL treat design records as dependent creative records for the listing permanent-deletion guard, SHALL block permanent deletion of a listing that has one or more designs, and SHALL require explicit design cleanup before a connected listing can be deleted.

#### Scenario: Listing with designs cannot be permanently deleted
- **WHEN** the user requests permanent deletion of a listing that has one or more design records
- **THEN** FusionCanvas blocks the deletion
- **AND** explains that the designs must be removed or the listing archived instead

#### Scenario: Listing without designs remains deletable
- **WHEN** the user requests permanent deletion of a listing with no designs, concepts, prompts, or asset links
- **THEN** the existing listing-management deletion behavior applies without a design blocker
