## MODIFIED Requirements

### Requirement: Store editor owns product and fulfillment setup
FusionCanvas SHALL provide a Catalog & mockups tab in the dedicated Store Editor for the selected Store, SHALL place fulfillment strategy, Blueprint catalog, and Mockup Template administration there, and SHALL keep this occasional configuration out of the regular workspace rail and application Settings window.

#### Scenario: User opens catalog setup for active Store
- **WHEN** the user opens Manage stores and selects an active Store
- **THEN** FusionCanvas provides a Catalog & mockups tab for that Store
- **AND** the regular workspace remains focused on Store selection and creative work

#### Scenario: Store has no configured Blueprints
- **WHEN** the user opens Catalog & mockups for a Store with no Blueprints
- **THEN** FusionCanvas shows the Manual fulfillment strategy, explanatory Blueprint empty guidance, and a New Blueprint action
- **AND** does not fabricate Blueprint, Print Provider, Provider Network, Variant, Placeholder, or Mockup Template data

#### Scenario: User changes editor context with an unsaved catalog draft
- **WHEN** the user has meaningful unsaved Blueprint, offering, Option, Variant, Placeholder, template, or template-color changes and changes Store, tab, selection, or closes the Store Editor
- **THEN** FusionCanvas offers the applicable save/discard/cancel or discard/keep-editing safeguard
- **AND** cancellation or keep-editing retains the current draft, context, selection, and focus

#### Scenario: User reviews archived Store setup
- **WHEN** the user selects an archived Store in the Store Editor
- **THEN** FusionCanvas shows its fulfillment strategy, Blueprint catalog, and Mockup Template configuration read-only
- **AND** does not enable create, edit, archive-conflicting, or relationship-changing catalog mutations
