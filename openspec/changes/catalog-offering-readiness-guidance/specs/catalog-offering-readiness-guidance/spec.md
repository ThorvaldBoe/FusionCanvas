## ADDED Requirements

### Requirement: Offering readiness is a derived, actionable projection
FusionCanvas SHALL expose an Offering-scoped readiness summary derived from the active normalized catalog records for that Offering. The summary SHALL include active Variant, Design Area, and Mockup Template counts, the count of active Mockup Templates that satisfy the authoritative Mockup Template readiness policy, a status, and structured issues that identify missing catalog prerequisites or named incomplete templates.

#### Scenario: Offering has a ready Mockup Template
- **WHEN** an active Blueprint Offering has at least one active Variant, Design Area, and Mockup Template and at least one active Mockup Template satisfies the authoritative readiness policy
- **THEN** the readiness summary reports the exact active counts and ready-template count
- **AND** reports a mockup-setup-ready status with no blocking issues
- **AND** does not claim that an Item has selected Colors or Design artwork

#### Scenario: Offering is missing catalog prerequisites
- **WHEN** an active Blueprint Offering has no active Variants, Design Areas, or Mockup Templates
- **THEN** the readiness summary reports zero for each corresponding count
- **AND** identifies each missing prerequisite separately with creator-facing next-step guidance
- **AND** reports that the Offering is not ready for mockup generation

#### Scenario: Offering has only incomplete Mockup Templates
- **WHEN** an active Blueprint Offering has active Mockup Templates but none satisfy the authoritative readiness policy
- **THEN** the readiness summary reports the active-template count and zero ready templates
- **AND** identifies each incomplete template by name
- **AND** includes every current readiness blocker for each named template
- **AND** does not include any incomplete template in the ready-template count

#### Scenario: Summary observes current state without mutating it
- **WHEN** the readiness summary is calculated while the Store or Offering is being reviewed
- **THEN** the calculation reads the current workspace snapshot only
- **AND** it does not create, archive, update, retarget, or otherwise mutate catalog, template, Item, Design, or Listing records

