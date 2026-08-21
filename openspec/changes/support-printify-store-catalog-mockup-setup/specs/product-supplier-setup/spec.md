## MODIFIED Requirements

### Requirement: Store catalog maintains product and fulfillment structure
FusionCanvas SHALL let a user maintain Blueprints and one or more Blueprint Offerings within an active Store, SHALL associate each offering with either one fixed Print Provider or one named Provider Network, and SHALL preserve that catalog across workspace reload.

#### Scenario: User adds a Blueprint and fixed-Print-Provider offering
- **WHEN** the user saves a valid Blueprint, Print Provider, and fixed-Print-Provider Blueprint Offering in the Store Editor
- **THEN** FusionCanvas persists all records with stable identities scoped to the selected Store
- **AND** the offering remains associated with the Blueprint and Print Provider after reload

#### Scenario: User adds a Provider-Network offering
- **WHEN** the user saves a valid Blueprint Offering whose kind is Provider Network
- **THEN** FusionCanvas requires a stable provider-network code and a display name
- **AND** does not require or fabricate an ordinary Print Provider identity

#### Scenario: Catalog is isolated by Store
- **WHEN** the user opens catalog setup for another Store
- **THEN** FusionCanvas shows only that Store's Blueprints, Print Providers, Blueprint Offerings, Options, Values, Variants, and Placeholders
- **AND** it does not expose or permit editing catalog records from another Store

### Requirement: Offerings retain provider-compatible variants and design areas
FusionCanvas SHALL allow a Blueprint Offering to define Options with required stable `OptionKind` values `Color`, `Size`, or `Other`, reusable Option Values, explicit concrete Variants composed from those values, and Placeholders containing position, decoration method, positive pixel width and height, and compatible concrete Variants.

#### Scenario: User creates typed Options and a concrete Variant
- **WHEN** the user saves Color and Size Options, creates their Option Values, and saves a concrete Variant using values owned by that Blueprint Offering
- **THEN** FusionCanvas persists the Option kinds, values, and explicit Variant membership
- **AND** does not infer Color or Size semantics from editable Option names

#### Scenario: User creates a variant-compatible Placeholder
- **WHEN** the user saves a Placeholder and selects compatible concrete Variants from its Blueprint Offering
- **THEN** FusionCanvas persists its position, decoration method, positive pixel dimensions, and Variant compatibility
- **AND** the Placeholder remains associated only with Variants from that offering

#### Scenario: User enters invalid Placeholder dimensions or references
- **WHEN** the user attempts to save a Placeholder with non-positive dimensions or a compatible Variant from another offering
- **THEN** FusionCanvas rejects the save with recoverable guidance
- **AND** leaves confirmed catalog data unchanged

#### Scenario: User creates a semantically duplicate concrete Variant
- **WHEN** a concrete Variant already contains the same set of Option Values in the same Blueprint Offering
- **THEN** FusionCanvas rejects another active Variant with that same option-value combination
- **AND** preserves the existing Variant identity

### Requirement: Printify Choice is represented as a variable network
FusionCanvas SHALL represent Printify Choice as a Provider-Network Blueprint Offering with a stable provider-network code distinct from its mutable display label, SHALL NOT represent it as a fixed Print Provider, and SHALL disclose that exact provider selection and design consistency can vary.

#### Scenario: User configures a Choice offering
- **WHEN** the user creates a Printify Choice Blueprint Offering
- **THEN** FusionCanvas persists the stable Printify Choice network code and its display label
- **AND** does not require or display a fixed Print Provider identity

#### Scenario: Choice display label changes
- **WHEN** the display label for the Printify Choice Provider Network changes
- **THEN** the offering retains the same stable provider-network identity
- **AND** relationships do not depend on the old label

#### Scenario: User reviews Choice Placeholders
- **WHEN** a configured Choice offering provides selectable Placeholders
- **THEN** FusionCanvas shows a network-consistency warning with those Placeholders
- **AND** the Placeholders remain eligible for Item target and Mockup Template configuration according to their explicit compatibility

### Requirement: Catalog edits preserve selected Item targets
FusionCanvas SHALL prefer archive or deactivation for catalog records with dependents and SHALL require explicit safe handling before a user permanently removes a Blueprint, Print Provider, Blueprint Offering, Option Value, Variant, or Placeholder referenced by Items or Mockup Templates.

#### Scenario: User removes an unreferenced Placeholder
- **WHEN** the user confirms permanent removal of an unreferenced Placeholder
- **THEN** FusionCanvas removes that Placeholder
- **AND** preserves unrelated Blueprints, offerings, Options, Variants, templates, and Items

#### Scenario: User removes a Placeholder referenced by an Item
- **WHEN** the user requests removal of a Placeholder selected by one or more Items
- **THEN** FusionCanvas blocks removal
- **AND** explains that the Item target must first be cleared or replaced

#### Scenario: User removes a Placeholder referenced by a Mockup Template
- **WHEN** the user requests removal of a Placeholder targeted by one or more Mockup Templates
- **THEN** FusionCanvas blocks removal
- **AND** explains that dependent templates must first be reassigned or removed

#### Scenario: User removes a referenced Color Option Value
- **WHEN** the user requests retirement or removal of a Color Option Value referenced by active template-color configuration
- **THEN** FusionCanvas requires explicit handling of those dependents
- **AND** does not silently orphan or retarget them

#### Scenario: User archives a referenced catalog record
- **WHEN** permanent deletion would violate a dependency and archival is valid
- **THEN** FusionCanvas offers or permits archive/deactivation instead
- **AND** retains stable identity and historical attribution while excluding the record from new active selections

### Requirement: Catalog management uses progressive disclosure
The Store Editor catalog surface SHALL present focused levels for Blueprint overview, Blueprint detail, Blueprint Offering detail, and nested Mockup Template setup. It SHALL show only controls relevant to the active level while keeping the current selection and navigation path visible.

#### Scenario: User opens the catalog editor
- **WHEN** the user opens catalog and mockup setup for an active Store
- **THEN** the editor shows the Store fulfillment strategy and a Blueprint overview with an empty state when no Blueprints exist
- **AND** it does not show offering, Option, Variant, Placeholder, or Mockup Template forms until their owning context is opened

#### Scenario: User opens a Blueprint
- **WHEN** the user selects a Blueprint from the overview
- **THEN** the editor shows Blueprint identity, explanatory Blueprint helper text, compact catalog summary, Blueprint details, and its Blueprint Offerings
- **AND** the primary creation action is scoped to adding a Blueprint Offering
- **AND** the editor uses the opened Blueprint as the offering-creation context without showing another Blueprint selector or a duplicate raw offering form

#### Scenario: User opens a Blueprint Offering
- **WHEN** the user selects a Blueprint Offering from Blueprint detail
- **THEN** the editor shows a breadcrumb or equivalent path identifying the Blueprint and offering
- **AND** it discloses offering Basics, Options and Values, Variants, Placeholders, Mockup Templates, and Advanced information without unrelated Blueprint lists
- **AND** the opened offering is the authoritative context for all dependent sections without requiring another Blueprint Offering selection
- **AND** if the normalized offering record is unavailable, the editor explains which dependent sections are unavailable instead of showing empty or unrelated selectors

### Requirement: Catalog controls use unambiguous terminology and ownership
The Store Editor SHALL use the Printify-aligned terms Blueprint, Print Provider, Provider Network, Blueprint Offering, Option, Option Value, Variant, and Placeholder consistently in visible labels and actions. It SHALL explain non-intuitive terms through visible helper text or accessible tooltips and SHALL reserve Product for a later artwork-added sellable product concept.

#### Scenario: User encounters Blueprint for the first time
- **WHEN** the Blueprint overview or empty state is shown
- **THEN** FusionCanvas explains that a Blueprint is a blank catalog product before artwork is added
- **AND** does not relabel the concept as Product

#### Scenario: User encounters Placeholder for the first time
- **WHEN** Placeholder configuration is shown
- **THEN** FusionCanvas explains that a Placeholder is a provider-compatible printable location for concrete Variants
- **AND** does not label the record as a design area or printable area

#### Scenario: User creates catalog records
- **WHEN** the user is at a Blueprint, offering, Option, Variant, Placeholder, or template level
- **THEN** creation actions name the exact record type they affect
- **AND** no ambiguous generic Add action is used when ownership would be unclear

#### Scenario: User removes catalog records
- **WHEN** the user requests removal or archival of a catalog record
- **THEN** the action identifies the exact target type and active/archive consequence
- **AND** explicit confirmation and reference safeguards remain in effect

### Requirement: Offering details disclose dependent controls in a logical order
The Blueprint Offering detail surface SHALL group controls in dependency order: Basics, Options and Option Values, concrete Variants, Placeholders, Mockup Templates, and Advanced. Fixed-Print-Provider fields SHALL appear only for fixed offerings, and Provider-Network identity and guidance SHALL appear only for network offerings.

#### Scenario: User reviews an offering
- **WHEN** a Blueprint Offering detail surface is active
- **THEN** Basics identifies whether the offering uses a fixed Print Provider or Provider Network
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

### Requirement: Catalog navigation preserves editing safeguards
The progressive-disclosure Store Editor SHALL treat back navigation, breadcrumbs, level changes, tab changes, Store changes, and selection changes as guarded transitions. It SHALL preserve drafts when the user cancels a transition and SHALL use explicit save/discard behavior before abandoning meaningful unsaved catalog or Mockup Template edits.

#### Scenario: User navigates with unsaved Blueprint edits
- **WHEN** the user has meaningful unsaved Blueprint edits and selects another Blueprint, opens an offering, or uses Back
- **THEN** the editor offers the applicable Save, Discard, and Cancel choices
- **AND** Cancel keeps the current level, selection, fields, and focus

#### Scenario: User starts a nested draft
- **WHEN** the user starts a new Blueprint, Print Provider, offering, Option, Option Value, Variant, Placeholder, template, or template-color form and cancels before saving
- **THEN** the draft is not persisted
- **AND** the editor returns to the invoking level with a sensible selection and no orphan record

#### Scenario: User completes a destructive action
- **WHEN** the user confirms an allowed archive, restore, removal, or deletion from any disclosure level
- **THEN** application and domain validation decide whether the operation succeeds atomically
- **AND** after success the editor selects a valid remaining record or shows the relevant empty state

#### Scenario: Archived Store catalog is reviewed
- **WHEN** the user selects an archived Store
- **THEN** FusionCanvas shows its strategy, catalog, and Mockup Template relationships read-only
- **AND** does not enable relationship-changing catalog actions

## ADDED Requirements

### Requirement: Catalog setup remains local and listing-independent
This module SHALL configure Store fulfillment strategy, Blueprint catalog records, Placeholder compatibility, and Mockup Template relationships only. It SHALL NOT select listing colors, generate mockups, render or compose images, upload template assets, synchronize Printify, configure credentials, or publish to Shopify.

#### Scenario: User completes Store catalog setup
- **WHEN** the user saves a valid Manual strategy catalog and Mockup Template configuration
- **THEN** FusionCanvas persists the Store configuration locally
- **AND** creates no listing selection, generated mockup, remote product, external credential, or publication request

#### Scenario: Contributor reviews cross-system mapping
- **WHEN** a contributor inspects this module's model
- **THEN** it contains no Shopify option or Variant mapping records
- **AND** documents that a future Shopify adapter must map FusionCanvas colors and concrete Variants explicitly without assuming labels or identifiers match across systems
