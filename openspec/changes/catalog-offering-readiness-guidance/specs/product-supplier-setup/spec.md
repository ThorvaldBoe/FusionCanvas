## MODIFIED Requirements

### Requirement: Offering details disclose dependent controls in a logical order
The Blueprint Offering detail surface SHALL group controls in dependency order: Basics, Options and Option Values, concrete Variants, Placeholders, Mockup Templates, and Advanced. Fixed-Print-Provider fields SHALL appear only for fixed offerings, Provider-Network identity and guidance SHALL appear only for network offerings, and the surface SHALL show a concise derived readiness summary before creation forms are opened.

#### Scenario: User reviews an offering
- **WHEN** a Blueprint Offering detail surface is active
- **THEN** Basics identifies whether the offering uses a fixed Print Provider or Provider Network
- **AND** the surface shows the current active Variant, Design Area, Mockup Template, and ready Mockup Template counts
- **AND** it shows the current readiness status and named next-step guidance when setup is incomplete or templates are Draft
- **AND** each dependent section shows its current records and count before an add form is opened
- **AND** optional external identifiers are placed in a secondary Advanced section

#### Scenario: User adds an Option
- **WHEN** the user activates Add Option
- **THEN** a focused form requires an Option name and stable Option kind
- **AND** Option Values are configured within that Option's context

#### Scenario: User adds a concrete Variant
- **WHEN** the user activates Add Variant
- **THEN** the form selects one valid Option Value for each required Option according to the offering's rules
- **AND** the saved concrete Variant appears without changing the Blueprint or offering selection

#### Scenario: User adds a Placeholder
- **WHEN** the user activates Add Placeholder for an offering with concrete Variants
- **THEN** the form exposes labeled Position, Decoration method, Width (px), Height (px), and Compatible Variants controls
- **AND** Compatible Variants permits only concrete Variants from the current offering

#### Scenario: User reviews a Choice offering
- **WHEN** the selected offering is the Printify Choice Provider Network
- **THEN** the editor keeps the variable-network warning visible near offering or Placeholder guidance
- **AND** does not show or fabricate a fixed Print Provider

#### Scenario: User reviews the normalized offering editor
- **WHEN** a Blueprint Offering detail surface is active
- **THEN** Basics, Options and Values, Variants, Placeholders, Mockup Templates, and Advanced read and mutate the normalized catalog graph for that offering
- **AND** the editor does not expose legacy free-text Color/Size Variant creation or legacy design-area controls
- **AND** each creation form remains collapsed until its specifically named Add action is activated

#### Scenario: Current-schema Store contains compatibility-only catalog records
- **WHEN** FusionCanvas loads a Store containing a legacy Blueprint or offering identity that has no normalized equivalent because it was created by an earlier schema-11 build
- **THEN** FusionCanvas repairs the missing normalized Blueprint, offering, Option, Option Value, Variant, and Placeholder records atomically using the preserved compatibility identities
- **AND** subsequent normalized edits keep compatibility readers aligned without making the compatibility graph an independent UI editing source
- **AND** repair does not overwrite an existing normalized record or fabricate mockup templates

