## ADDED Requirements

### Requirement: Mockup Templates can be saved as partial Drafts
FusionCanvas SHALL allow an editable Blueprint Offering to persist a Mockup Template with a stable identity and nonblank name even when readiness configuration is incomplete. Target Design Area, applicable Colors, template image source, and image-space mapping SHALL be optional for Draft persistence. Any supplied relationship or structured value SHALL still satisfy its own ownership and value invariants.

#### Scenario: User saves a name-only template without provider integration
- **WHEN** the user creates a Mockup Template with a nonblank name in an editable Offering while provider-catalog data is unavailable and no Design Area, Color, image, or mapping is selected
- **THEN** FusionCanvas persists the template exactly once as a Draft
- **AND** the saved template remains associated with that Offering after reload

#### Scenario: User saves available partial configuration
- **WHEN** the user saves a template with a nonblank name and any subset of valid same-Offering Design Area, Color, image-source, or mapping configuration
- **THEN** FusionCanvas preserves every supplied value in the saved Draft
- **AND** does not require absent readiness inputs or a provider-catalog request

#### Scenario: Minimum template identity is missing
- **WHEN** the user attempts to save a template without an editable Offering context or nonblank name
- **THEN** FusionCanvas rejects the save with concise guidance
- **AND** does not create or partially update a template

#### Scenario: Supplied partial configuration is invalid
- **WHEN** a Draft save supplies a cross-Offering or archived relationship, a non-Color applicability value, or a non-positive or out-of-bounds mapping
- **THEN** FusionCanvas rejects the invalid supplied configuration with recoverable guidance
- **AND** preserves both the editor draft and the last confirmed template state

### Requirement: Readiness is derived from current render configuration
FusionCanvas SHALL derive Mockup Template lifecycle as `Draft` or `ReadyForUse`; lifecycle SHALL NOT be independently user-editable or persisted as a mutable status. An active template SHALL be Ready for use only when its current configuration has an active same-Offering target Design Area, at least one active same-Offering Color whose implied active sellable Variants exist and are all compatible with that Design Area, a usable template image reference with positive image dimensions, a positive mapping fully inside those image bounds, and no known incompatibility between the selected image and selected Colors. Every other active template SHALL be Draft.

#### Scenario: Complete compatible template is Ready for use
- **WHEN** an active template has every required image, mapping, Design Area, Color, and Variant-compatibility input
- **AND** all supplied values satisfy ownership and bounds rules
- **THEN** FusionCanvas derives `ReadyForUse`
- **AND** reports no unmet readiness requirements

#### Scenario: One or more readiness inputs are absent
- **WHEN** an active template lacks any required readiness input
- **THEN** FusionCanvas derives `Draft`
- **AND** reports every unmet readiness requirement rather than only the first one

#### Scenario: Catalog change makes a complete template incompatible
- **WHEN** a related Design Area, Color, or sellable Variant changes so the current template configuration no longer satisfies readiness
- **THEN** FusionCanvas derives `Draft` without rewriting an independent lifecycle field
- **AND** identifies each catalog compatibility blocker

#### Scenario: Archived template has complete configuration
- **WHEN** a Mockup Template is archived while its retained configuration is otherwise complete
- **THEN** FusionCanvas does not expose it as Ready for use
- **AND** preserves the configuration and revision history for review

### Requirement: Draft and Ready transitions preserve attributable revisions
FusionCanvas SHALL create an initial attributable revision for a newly saved Draft and SHALL advance the revision when a persisted change affects future render output, including target Design Area, applicable Colors, image source, image dimensions, or mapping. Revision snapshots SHALL permit incomplete configuration and SHALL retain prior snapshots unchanged.

#### Scenario: Name-only Draft is created
- **WHEN** a user first saves a name-only Mockup Template
- **THEN** FusionCanvas creates revision 1 with nullable readiness configuration
- **AND** the revision remains attributable to the stable template identity

#### Scenario: Draft becomes Ready for use
- **WHEN** the user adds the missing valid render configuration and saves
- **THEN** FusionCanvas advances the template revision exactly once
- **AND** the new current revision is Ready for use while the prior Draft revision remains unchanged

#### Scenario: Ready template becomes Draft
- **WHEN** the user explicitly removes an image, mapping, target Design Area, or all applicable Colors and saves
- **THEN** FusionCanvas advances the template revision exactly once
- **AND** derives Draft for the new current revision without altering earlier Ready revision history

#### Scenario: Non-output metadata changes
- **WHEN** the user changes only template identity metadata that does not affect render output
- **THEN** FusionCanvas preserves the current revision number
- **AND** re-evaluates readiness from the unchanged current configuration

### Requirement: Provider catalogs assist but do not authorize persistence
FusionCanvas SHALL treat provider-catalog candidates as optional assistance. Creating or updating a Mockup Template SHALL NOT require a provider-catalog source, synchronization, network access, or a second provider lookup. When provider metadata is available, FusionCanvas MAY use it to prefill choices and SHALL enforce any known image/Color compatibility before deriving Ready for use.

#### Scenario: Provider catalog is unavailable
- **WHEN** provider-catalog data is absent, empty, unavailable, or failed
- **THEN** the user can save otherwise valid partial configuration as a Draft
- **AND** the readiness result identifies missing image configuration without presenting provider setup as a prerequisite for saving

#### Scenario: Provider candidate prefills configuration
- **WHEN** optional provider data supplies a selected image and its supported Colors
- **THEN** FusionCanvas may prefill the image dimensions and compatible Color choices
- **AND** persistence is authorized by the submitted template values and Offering ownership rather than by re-fetching the provider catalog

#### Scenario: Known provider compatibility is violated
- **WHEN** persisted or currently supplied provider metadata establishes that a selected image does not support one or more selected Colors
- **THEN** FusionCanvas derives Draft and reports the incompatibility
- **AND** does not expose the template to preview/render consumers

### Requirement: Render consumers use an authoritative readiness gate
FusionCanvas SHALL expose one application-facing readiness result for Mockup Templates and SHALL exclude Draft, archived, missing, and incompatible templates from customer-facing or design-preview rendering eligibility.

#### Scenario: Preview workflow queries eligible templates
- **WHEN** a preview or render workflow requests usable Mockup Templates for an Offering
- **THEN** the application returns only active templates whose authoritative readiness result is `ReadyForUse`
- **AND** the consumer does not duplicate or weaken the readiness rules

#### Scenario: Draft template is selected by stale identity
- **WHEN** a caller requests a template that exists but is currently Draft
- **THEN** FusionCanvas rejects preview/render use with the current readiness blockers
- **AND** leaves the template available for manual editing

