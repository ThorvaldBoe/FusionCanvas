## ADDED Requirements

### Requirement: Listing explains why mockup template selection is empty
The Listing mockup tool SHALL preserve the ready-only template selector and SHALL present actionable diagnostics when no ready template is available. The diagnostics SHALL distinguish an Offering with no active templates from an Offering whose active templates are Draft, identify each affected template by name, and list every current readiness blocker using creator-facing guidance.

#### Scenario: No mockup templates are configured
- **WHEN** an editable Item has an active Offering and selected Colors but that Offering has no active Mockup Templates
- **THEN** the Listing tool keeps mockup generation unavailable
- **AND** explains that no Mockup Templates are configured for the Offering
- **AND** directs the creator to Store settings to add one

#### Scenario: Configured templates are incomplete
- **WHEN** an editable Item has an active Offering but all active Mockup Templates are Draft
- **THEN** the Listing tool keeps the selector empty and generation unavailable
- **AND** shows each active template name with every current missing or invalid readiness requirement
- **AND** directs the creator to Store settings to complete the named template

#### Scenario: A template becomes ready
- **WHEN** a Mockup Template satisfies the authoritative readiness policy after the Listing tool reloads
- **THEN** the template appears in the selector
- **AND** the draft-template diagnostics no longer describe that template as blocking generation

#### Scenario: Readiness diagnostics are unavailable
- **WHEN** template eligibility cannot be loaded because the Offering or workspace context is invalid
- **THEN** the Listing tool shows the returned error or a concise recovery message
- **AND** does not claim that no templates are configured

### Requirement: Listing readiness diagnostics remain presentation-only
The Listing diagnostics SHALL reuse the authoritative Mockup Template readiness result and SHALL NOT change persistence, readiness rules, template eligibility, or the Store editor's draft behavior. The diagnostics SHALL remain available to keyboard and assistive-technology users through ordinary text and controls.

#### Scenario: Diagnostic state does not weaken eligibility
- **WHEN** an active template has one or more readiness blockers
- **THEN** the template remains excluded from the selector and Apply action
- **AND** the displayed diagnostics reflect the same blockers used by the eligibility gate
