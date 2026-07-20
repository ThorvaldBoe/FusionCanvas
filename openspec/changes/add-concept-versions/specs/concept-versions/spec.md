## ADDED Requirements

### Requirement: Concept entity is part of the core domain model
FusionCanvas SHALL define a Concept as a Phase 2 core domain entity that belongs to exactly one listing and preserves one creative direction for that listing's Concept stage.

#### Scenario: Contributor reviews the core model
- **WHEN** a contributor inspects the domain model after this change is accepted
- **THEN** Concept is represented as a named domain concept owned by a listing
- **AND** the model still does not require future concepts such as Design, Mockup, Marketplace Product, Performance Record, Plugin Data, or Workflow Template

#### Scenario: Concept is item-bound
- **WHEN** a concept is represented in the domain model
- **THEN** it belongs to exactly one listing
- **AND** no free-floating concept exists outside an item
- **AND** the concept is scoped to the Concept stage of that item

### Requirement: Concept versions preserve alternate directions for a listing
FusionCanvas SHALL allow a listing to have zero, one, or many concept versions, SHALL preserve superseded and rejected versions without deletion, and SHALL allow each concept version to record idea, phrase, graphic direction, audience reaction, risks, quality notes, and design-triangle score metadata.

#### Scenario: User creates a concept from an existing idea
- **WHEN** the user creates a concept for a listing that already has idea-stage context
- **THEN** FusionCanvas creates a new concept version with stable identity and timestamps
- **AND** copies or references the existing idea value as the starting idea
- **AND** applies applicable inherited tags and metadata from the current context with provenance

#### Scenario: User creates a concept directly from a later-stage starting point
- **WHEN** the user creates a concept directly from a phrase, graphic direction, or other later-stage starting point
- **THEN** FusionCanvas creates the concept version for the selected listing without requiring a prior separate idea record
- **AND** records the supplied starting values

#### Scenario: User preserves an alternate direction
- **WHEN** the user accepts a large alternate direction from the Basic Concept Tool as a new version instead of overwriting the selected concept
- **THEN** FusionCanvas creates a new concept version
- **AND** leaves the prior concept version available for later review

#### Scenario: Superseded and rejected concepts remain available
- **WHEN** a concept version is superseded or rejected
- **THEN** FusionCanvas preserves the version with its metadata, score, and history
- **AND** does not delete the version
- **AND** keeps it reachable for later review without cluttering the active direction

### Requirement: One selected concept version supplies the current direction
FusionCanvas SHALL mark at most one concept version per listing as the selected direction and SHALL expose that selected concept's idea, phrase, and graphic direction as the current values for the Basic Concept Tool and downstream Design-stage work.

#### Scenario: First concept is selected automatically
- **WHEN** the user creates the first concept for a listing that has no concept and no selection
- **THEN** FusionCanvas selects that concept as the current direction
- **AND** records the selection at the listing level

#### Scenario: Supersede selects the new version and demotes the prior
- **WHEN** the user supersedes the current concept with a new version
- **THEN** FusionCanvas selects the new version
- **AND** marks the prior version superseded
- **AND** keeps the prior version available for review

#### Scenario: Rejecting the selected concept clears the selection
- **WHEN** the user rejects the selected concept and at least one other active concept remains
- **THEN** FusionCanvas clears the selection
- **AND** requires the user to select another concept before downstream tools use it
- **AND** does not silently auto-select a replacement

#### Scenario: Listing has no selected concept
- **WHEN** a listing has zero concepts or its selected concept has been rejected without a replacement
- **THEN** downstream Concept and Design tools report that no concept is selected
- **AND** no concept values are exposed as the current direction

### Requirement: Concept creation uses active context and inherited metadata
FusionCanvas SHALL resolve the active store, niche, topic path, selected item, tags, and metadata from the current context when creating a concept, SHALL apply only inherited values marked applicable to new concept work, and SHALL preserve explicit user overrides before save.

#### Scenario: Context provides applicable inherited values
- **WHEN** the user creates a concept from a context whose resolved inherited tags or metadata apply to new concept work
- **THEN** FusionCanvas applies those values to the concept
- **AND** records their inherited provenance

#### Scenario: User overrides an inherited value
- **WHEN** the user changes an inherited value before saving a new concept
- **THEN** FusionCanvas persists the user's explicit value
- **AND** does not overwrite it with the inherited default

#### Scenario: Parent context has no applicable values
- **WHEN** the user creates a concept in a context with no applicable inherited tags or metadata
- **THEN** FusionCanvas creates the concept without fabricated tags or metadata

### Requirement: Concept operations persist atomically
FusionCanvas SHALL persist concept creation, edit, supersede, reject, selection, and metadata changes in the active workspace database as atomic snapshot operations, reloading the latest snapshot before each mutation and saving once per operation.

#### Scenario: Workspace reloads after concept operations
- **WHEN** a successful concept operation is followed by an application or workspace database reload
- **THEN** the resulting concepts, selections, metadata, and lifecycle states match the last successful persisted state

#### Scenario: Persistence fails during a concept operation
- **WHEN** the repository cannot save a concept operation after an optimistic projection
- **THEN** FusionCanvas reports a recoverable error
- **AND** restores the last confirmed concept, listing, and selection state
- **AND** retains recoverable user input needed to retry when applicable

### Requirement: Concepts are dependent records for listing deletion
FusionCanvas SHALL treat concept versions as dependent creative records for the listing permanent-deletion guard, SHALL block permanent deletion of a listing that has one or more concepts, and SHALL require explicit concept cleanup before a connected listing can be deleted.

#### Scenario: Listing with concepts cannot be permanently deleted
- **WHEN** the user requests permanent deletion of a listing that has one or more concept versions
- **THEN** FusionCanvas blocks the deletion
- **AND** explains that the concepts must be removed or the listing archived instead

#### Scenario: Listing without concepts remains deletable
- **WHEN** the user requests permanent deletion of a listing with no concepts, prompts, or asset links
- **THEN** the existing listing-management deletion behavior applies without a concept blocker

### Requirement: Concept score metadata is advisory
FusionCanvas SHALL store design-triangle score metadata on a concept as an optional advisory block, SHALL NOT use score thresholds to block stage advancement or selection, and SHALL leave scores absent when scoring is unavailable.

#### Scenario: Score is present
- **WHEN** a concept carries a design-triangle score
- **THEN** the score is stored with overall, weak element, confidence, and critique hint fields
- **AND** downstream tools may show it as advisory

#### Scenario: Low score does not block advancement
- **WHEN** a concept has a low or absent score
- **THEN** FusionCanvas does not block the user from advancing the listing to the next stage
- **AND** the user remains responsible for the decision
