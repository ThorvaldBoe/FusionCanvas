## ADDED Requirements

### Requirement: Basic Design Tool is the default Design-stage tool
FusionCanvas SHALL provide the Basic Design Tool as the default built-in Design-stage tool, registered through the Stage Tool Host, available from the Design stage for an existing item, and unavailable as a free-floating design workspace without an item.

#### Scenario: User opens the tool for an existing item
- **WHEN** the user is on the Design stage with a selected item
- **THEN** the Stage Tool Host displays the Basic Design Tool as the active Design-stage tool
- **AND** the tool receives the active store, niche, topic path, item, stage, selected concept, inherited tags, metadata, and AI capabilities

#### Scenario: No item is selected
- **WHEN** the Design stage is active but no item is selected
- **THEN** the Stage Tool Host shows an empty state or creation path
- **AND** does not open the Basic Design Tool against only a topic context

#### Scenario: Plugin Design tool coexists
- **WHEN** a future plugin contributes a Design-stage tool that supports the current context
- **THEN** both the Basic Design Tool and the plugin tool are selectable through the Stage Tool Host
- **AND** the Basic Design Tool remains the default when no other tool is selected

### Requirement: Tool derives a design brief from the selected concept or sufficient listing fields
FusionCanvas SHALL derive a design brief from the selected concept when one exists, SHALL fall back to listing-level idea, phrase, and graphic direction when they are sufficient to form a brief and no concept is selected, and SHALL show a readiness note when neither is available.

#### Scenario: Brief derived from selected concept
- **WHEN** the user opens the tool for an item with a selected concept
- **THEN** the tool shows a design brief derived from the selected concept's idea, phrase, and graphic direction

#### Scenario: Brief derived from listing fields
- **WHEN** the user opens the tool for an item with no selected concept but sufficient listing-level idea, phrase, and graphic direction
- **THEN** the tool shows a brief derived from those listing fields
- **AND** does not require a concept to be selected

#### Scenario: Insufficient brief
- **WHEN** the user opens the tool for an item with no selected concept and insufficient listing-level creative fields
- **THEN** the tool shows a readiness note that a concept should be selected or brief fields supplied
- **AND** still allows manual import so the user can proceed

### Requirement: Manual and external-AI import create design variants
FusionCanvas SHALL support importing manually created source files and exported artwork, and externally generated AI artwork, by creating or updating design variants through the design-records service with the appropriate source method and linking the imported assets through asset-management.

#### Scenario: User imports manual artwork
- **WHEN** the user imports a manually created source file or export
- **THEN** FusionCanvas imports the file as an asset through asset-management
- **AND** creates or updates a design variant with the manual source method
- **AND** links the asset to the design

#### Scenario: User imports externally generated AI artwork
- **WHEN** the user imports artwork generated in an external AI tool and supplies prompt or source-tool information
- **THEN** FusionCanvas creates a design variant with the external-AI source method
- **AND** records the prompt and source metadata through prompt-history when supplied

### Requirement: In-app AI generation uses rich creative context
FusionCanvas SHALL allow the user to generate design variants inside FusionCanvas when an image-generation provider is configured, SHALL build generation context from store, niche, topic path, item, selected concept, idea, phrase, graphic direction, style notes, constraints, inherited tags and metadata, relevant sibling items, and accepted/rejected/superseded history, and SHALL keep generated variants as drafts until the user approves or promotes them.

#### Scenario: User generates variants in-app
- **WHEN** the user invokes generation with a provider configured and supplies Design-stage generation instructions
- **THEN** FusionCanvas sends the full creative context to the image-generation provider
- **AND** imports each generated image as an asset
- **AND** creates draft design variants with the in-app-AI source method
- **AND** records a prompt-history entry with provider/model and generation settings

#### Scenario: Generation never overwrites final artwork
- **WHEN** a generation result would replace existing final selected artwork
- **THEN** FusionCanvas does not overwrite final artwork without explicit user approval
- **AND** keeps the generated variant as a draft

#### Scenario: No image-generation provider is configured
- **WHEN** no image-generation provider is configured
- **THEN** in-app generation actions are disabled
- **AND** the tool explains that an image-generation provider must be configured

### Requirement: Design variants carry full production metadata
FusionCanvas SHALL allow each design variant to carry name, status, notes, source method, intended use as structured metadata, cleanup state, related assets, and tags, and SHALL preserve unknown metadata keys during edits.

#### Scenario: User edits variant metadata
- **WHEN** the user edits a variant's name, status, notes, intended use, or cleanup state
- **THEN** FusionCanvas persists the changes through the design-records service atomically
- **AND** preserves the variant's identity, assets, prompt references, and unknown metadata

#### Scenario: User records intended use as structured metadata
- **WHEN** the user records that a variant targets dark shirts, light shirts, specific colors, product types, or marketplaces
- **THEN** FusionCanvas stores the intended use as structured design metadata atomically
- **AND** later Listing, mockup, and export tools can read the structured fields

### Requirement: Final selection is explicit and multi-variant
FusionCanvas SHALL allow the user to promote one or more variants as final selected artwork regardless of their approval state, SHALL mark final membership with a `final` tag and listing-level collection membership, SHALL NOT delete rejected, draft, or superseded variants on promotion, and SHALL require at least one final-selected variant before advancing to Listing.

#### Scenario: User promotes a variant as final
- **WHEN** the user promotes a variant as final, regardless of its approval state
- **THEN** FusionCanvas adds it to the listing's final-selected collection through the design-records service and marks it with a `final` tag
- **AND** does not require the variant to be in an approved or ready-for-export state first
- **AND** does not delete other variants

#### Scenario: Import does not auto-promote
- **WHEN** the user imports or generates a new variant
- **THEN** FusionCanvas does not automatically add it to the final-selected collection

#### Scenario: User demotes a final variant
- **WHEN** the user demotes a final-selected variant
- **THEN** FusionCanvas removes it from the final-selected collection and clears the `final` tag
- **AND** leaves the variant available for review without deletion

### Requirement: Cleanup actions are built in
FusionCanvas SHALL offer rudimentary built-in cleanup actions — crop to visible artwork, transparency inspection, transparent-border removal, upscale flag, replacement attachment, and mark needs revision — SHALL record outcomes as asset or design metadata, and SHALL NOT become a full image editor.

#### Scenario: User runs a cleanup action
- **WHEN** the user invokes a cleanup action on a variant
- **THEN** FusionCanvas performs the action through its built-in cleanup implementation
- **AND** records the outcome as asset or design metadata atomically

#### Scenario: No external image editor required
- **WHEN** the user runs a built-in cleanup action
- **THEN** FusionCanvas does not require an external image editor or plugin
- **AND** keeps the built-in cleanup actions available without extension points for Phase 2

### Requirement: Tool feeds the Creative History Timeline
FusionCanvas SHALL record important Design-stage events — manual imports, external AI imports, in-app AI generations, prompt records, variant creation, variant rejection, cleanup actions, final variant promotion, and final selection changes — into the Creative History Timeline in the same atomic snapshot as the originating operation.

#### Scenario: Import event is recorded
- **WHEN** the user imports manual or external-AI artwork
- **THEN** FusionCanvas records a design-import event in the timeline

#### Scenario: Generation event is recorded
- **WHEN** in-app generation produces one or more variants
- **THEN** FusionCanvas records a design-in-app-generation event in the timeline

#### Scenario: Final selection event is recorded
- **WHEN** the user promotes or demotes a final-selected variant
- **THEN** FusionCanvas records a design-final-selection-change event in the timeline

### Requirement: Tool advances the item toward Listing through the navigator
FusionCanvas SHALL allow the user to advance the item toward the Listing stage by requesting the workflow-stage-navigator, SHALL require at least one final-selected design variant before advancement, and SHALL NOT set the stage field directly.

#### Scenario: User proceeds to Listing with final artwork
- **WHEN** the user invokes the proceed action and at least one final-selected variant exists
- **THEN** FusionCanvas requests advancement through the workflow-stage-navigator
- **AND** the Stage Tool Host updates to the Listing stage context when the navigator accepts

#### Scenario: Advancement is blocked without final artwork
- **WHEN** the user invokes the proceed action and no final-selected variant exists
- **THEN** FusionCanvas blocks the advancement
- **AND** explains that at least one design must be promoted as final first

#### Scenario: Navigator rejects the transition
- **WHEN** the workflow-stage-navigator rejects the transition
- **THEN** FusionCanvas reports the actionable reason
- **AND** leaves the listing's stage and design state unchanged

### Requirement: Basic Design Tool follows shared desktop control guidance
FusionCanvas SHALL present the tool with compact action sizing, clear tooltips for icon-only commands, predictable keyboard flow, accessible import/generate/promote/demote/proceed actions, busy states that prevent duplicate submission, and error states that preserve selection and focus.

#### Scenario: Generation is in progress
- **WHEN** an in-app generation or cleanup action is running
- **THEN** FusionCanvas prevents duplicate submission
- **AND** keeps the tool available to report success or an actionable error
- **AND** preserves the current variant and focus after failure

#### Scenario: Keyboard-only design work
- **WHEN** the user activates import, generate, promote, demote, cleanup, or proceed without a pointer
- **THEN** all essential actions are reachable through the keyboard
- **AND** focus returns to a meaningful control after each transition
