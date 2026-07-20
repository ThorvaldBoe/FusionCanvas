## ADDED Requirements

### Requirement: Basic Listing Tool is the default Listing-stage tool
FusionCanvas SHALL provide the Basic Listing Tool as the default built-in Listing-stage tool, registered through the Stage Tool Host, available from the Listing stage for an existing item, and unavailable as a free-floating listing workspace without an item.

#### Scenario: User opens the tool for an existing item
- **WHEN** the user is on the Listing stage with a selected item
- **THEN** the Stage Tool Host displays the Basic Listing Tool as the active Listing-stage tool
- **AND** the tool receives the active store, niche, topic path, item, stage, selected concept, selected final design variants, inherited tags, metadata, and available mockup product settings

#### Scenario: No item is selected
- **WHEN** the Listing stage is active but no item is selected
- **THEN** the Stage Tool Host shows an empty state or creation path
- **AND** does not open the Basic Listing Tool against only a topic context

#### Scenario: Plugin Listing tool coexists
- **WHEN** a future plugin contributes a Listing-stage tool that supports the current context
- **THEN** both the Basic Listing Tool and the plugin tool are selectable through the Stage Tool Host
- **AND** the Basic Listing Tool remains the default when no other tool is selected

### Requirement: Mockup generation requires final artwork and configured products
FusionCanvas SHALL require at least one selected final design variant and a selected configured mockup product before generating mockups, SHALL allow listing metadata editing when no final design exists, and SHALL show a readiness warning when required design or mockup information is missing.

#### Scenario: User generates mockups with final artwork and configured product
- **WHEN** the user invokes mockup generation for an item with at least one final-selected design variant and a selected mockup product configuration
- **THEN** FusionCanvas proceeds with generation

#### Scenario: Generation blocked without final artwork
- **WHEN** the user invokes mockup generation and no final-selected design variant exists
- **THEN** FusionCanvas blocks generation
- **AND** explains that at least one design must be promoted as final first

#### Scenario: Generation blocked without configured product
- **WHEN** the user invokes mockup generation and no mockup product configuration is selected
- **THEN** FusionCanvas blocks generation
- **AND** explains that a mockup product must be configured and selected

#### Scenario: Metadata editing allowed without final artwork
- **WHEN** the user opens the tool for an item with no final-selected design variant
- **THEN** FusionCanvas allows listing metadata editing through the listing-metadata-editor capability
- **AND** shows a readiness warning that final artwork is missing for mockup generation

### Requirement: Mockup generation produces placeholder outputs per color/template combination
FusionCanvas SHALL, for each selected color/template combination, record the inputs needed to produce a mockup — the final design image reference, the mockup template image reference, the color variant (with optional color-specific template image), and the placement parameters — and SHALL create a generated mockup record through the mockup-records service linked to the listing, design, template, color variant, and a placeholder output asset. The actual flat compositing of the design onto the template image will be implemented at a later stage using an existing ImageSharp-based component; the first version stores a placeholder output that records the inputs and placement parameters so the real compositing can be wired in without re-architecting the flow.

#### Scenario: User generates mockups for multiple colors
- **WHEN** the user selects a final design, a mockup product, a template, and multiple color variants
- **THEN** FusionCanvas generates one placeholder mockup per color/template combination
- **AND** records the final design image, template image, color variant, and placement parameters as the mockup's regeneration metadata
- **AND** creates a generated mockup record for each output with the regeneration-metadata block

#### Scenario: Source design dimensions do not match the design area
- **WHEN** the source final design has dimensions that differ from the supplier design area
- **THEN** FusionCanvas records the dimension mismatch in the mockup's metadata
- **OR** warns the user that the design dimensions do not match the product configuration before proceeding

#### Scenario: Generation produces no partial state on failure
- **WHEN** generation fails partway through a batch
- **THEN** FusionCanvas reports which combinations succeeded and which failed
- **AND** leaves the persisted state matching the successful combinations only
- **AND** retains recoverable input so the user can retry the failed combinations

### Requirement: Manual mockup attachment creates manual records
FusionCanvas SHALL allow the user to manually attach an existing mockup image as a manual mockup record through the mockup-records service, with the manual source flag and optional product/template/color/view metadata supplied by the user.

#### Scenario: User attaches an existing mockup image
- **WHEN** the user attaches an existing image as a mockup
- **THEN** FusionCanvas creates a manual mockup record linked to the listing
- **AND** links the image asset to the mockup through asset-management
- **AND** records any supplied product, template, color, view, and marketplace metadata

### Requirement: Tool edits listing metadata through the listing-metadata-editor capability
FusionCanvas SHALL host the listing-metadata-editor surface (or section) for title, description, price, status, notes, marketplace notes, draft tags, and basic preparation fields, and SHALL NOT re-implement metadata editing or operational status behavior owned by listing-lifecycle-status.

#### Scenario: User edits listing metadata from the listing tool
- **WHEN** the user edits marketplace preparation fields in the listing tool
- **THEN** FusionCanvas persists the changes through the listing-metadata-editor service atomically
- **AND** preserves the listing's identity, topic, archive state, concepts, designs, mockups, and unknown metadata

### Requirement: Tool provides a readiness checklist
FusionCanvas SHALL provide a readiness checklist that shows missing final design, mockup product settings, at least one mockup, title, description, price, status, and provider product and color names, and SHALL keep readiness advisory except for the mockup-generation requirements.

#### Scenario: Readiness checklist shows missing items
- **WHEN** the user opens the tool for an item missing one or more readiness items
- **THEN** FusionCanvas lists the missing items
- **AND** does not block manual work except mockup generation when final artwork or product configuration is missing

#### Scenario: Readiness checklist is satisfied
- **WHEN** all readiness items are satisfied
- **THEN** FusionCanvas indicates the listing is ready for manual marketplace setup or future publishing plugins

### Requirement: Tool refuses direct marketplace publishing
FusionCanvas SHALL NOT directly create, update, or publish Printify, Shopify, Etsy, or other marketplace products from the Basic Listing Tool, SHALL store marketplace-specific values locally as draft preparation data, and SHALL delegate publishing actions to future integration specs or plugins selectable through the Stage Tool Host.

#### Scenario: User looks for a publish action
- **WHEN** the user looks for a publish action in the Basic Listing Tool
- **THEN** the tool offers no direct marketplace publishing
- **AND** explains that publishing is delegated to future integration plugins

#### Scenario: Marketplace-specific values are stored locally
- **WHEN** the user enters marketplace-specific preparation values
- **THEN** FusionCanvas stores them locally as draft preparation data through the listing-metadata-editor capability
- **AND** does not call any marketplace API

### Requirement: Tool feeds the Creative History Timeline
FusionCanvas SHALL record important Listing-stage events — generated mockups, attached mockups, metadata changes, and status changes — into the Creative History Timeline in the same atomic snapshot as the originating operation.

#### Scenario: Mockup generation event is recorded
- **WHEN** mockup generation produces one or more mockups
- **THEN** FusionCanvas records a mockup-record-created event for each output in the timeline

#### Scenario: Manual attachment event is recorded
- **WHEN** the user manually attaches a mockup image
- **THEN** FusionCanvas records a mockup-record-created event in the timeline

#### Scenario: Metadata change event is recorded
- **WHEN** the user saves marketplace-preparation metadata
- **THEN** FusionCanvas records a listing-metadata-change event in the timeline

### Requirement: Basic Listing Tool follows shared desktop control guidance
FusionCanvas SHALL present the tool with compact action sizing, clear tooltips for icon-only commands, predictable keyboard flow, accessible generate/attach/save/proceed actions, busy states that prevent duplicate submission, and error states that preserve selection and focus.

#### Scenario: Generation is in progress
- **WHEN** a mockup generation batch is running
- **THEN** FusionCanvas prevents duplicate submission
- **AND** reports per-combination progress and failures
- **AND** preserves the current selection and focus after failure

#### Scenario: Keyboard-only listing work
- **WHEN** the user activates generate, attach, metadata save, or readiness review without a pointer
- **THEN** all essential actions are reachable through the keyboard
- **AND** focus returns to a meaningful control after each transition
