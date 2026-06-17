## ADDED Requirements

### Requirement: Listing Can Have One Or More Design Records
FusionCanvas SHALL ensure that a listing can have one or more design records.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** A listing can have one or more design records

### Requirement: Design Can Reference The Concept It Implements
FusionCanvas SHALL ensure that a design can reference the concept it implements.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** A design can reference the concept it implements

### Requirement: Designs Belong To The Design Stage In The Core Workflow
FusionCanvas SHALL ensure that designs belong to the Design stage in the core workflow.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Designs belong to the Design stage in the core workflow

### Requirement: Design Can Be Created From A Selected Concept Or Directly From A Strong Phrase And Graphic Direction
FusionCanvas SHALL ensure that a design can be created from a selected concept or directly from a strong phrase and graphic direction.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** A design can be created from a selected concept or directly from a strong phrase and graphic direction

### Requirement: Design Tools Should Inherit Store, Niche, Topic Path, Phrase, Graphic Direction, Style Guidance, And Relevant Metadata
FusionCanvas SHALL ensure that design tools should inherit store, niche, topic path, phrase, graphic direction, style guidance, and relevant metadata.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Design tools should inherit store, niche, topic path, phrase, graphic direction, style guidance, and relevant metadata

### Requirement: Design Can Track Name, Version, Notes, And Approval State
FusionCanvas SHALL ensure that a design can track name, version, notes, and approval state.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** A design can track name, version, notes, and approval state

### Requirement: Designs Can Be Marked Draft, Needs Revision, Approved, Rejected, Exported, Or Ready For Export
FusionCanvas SHALL ensure that designs can be marked draft, needs revision, approved, rejected, exported, or ready for export.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Designs can be marked draft, needs revision, approved, rejected, exported, or ready for export

### Requirement: Designs Can Be Associated With Assets
FusionCanvas SHALL ensure that designs can be associated with assets.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Designs can be associated with assets

### Requirement: Design Can Represent Manually Created Artwork, Externally Generated AI Artwork, In-app Generated AI Artwork, Or Plugin-produced Artwork
FusionCanvas SHALL ensure that a design can represent manually created artwork, externally generated AI artwork, in-app generated AI artwork, or plugin-produced artwork.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** A design can represent manually created artwork, externally generated AI artwork, in-app generated AI artwork, or plugin-produced artwork

### Requirement: Listing Can Preserve Many Draft, Rejected, Or Superseded Design Variants
FusionCanvas SHALL ensure that a listing can preserve many draft, rejected, or superseded design variants.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** A listing can preserve many draft, rejected, or superseded design variants

### Requirement: Design Can Identify Its Intended Use, Such As Dark Shirts, Light Shirts, Specific Shirt Colors, Product Types, Marketplaces, Or Export Targets
FusionCanvas SHALL ensure that a design can identify its intended use, such as dark shirts, light shirts, specific shirt colors, product types, marketplaces, or export targets.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** A design can identify its intended use, such as dark shirts, light shirts, specific shirt colors, product types, marketplaces, or export targets

### Requirement: One Or More Designs Can Be Promoted As Final Selected Artwork For Downstream Listing, Mockup, Export, Or Product-variant Workflows
FusionCanvas SHALL ensure that one or more designs can be promoted as final selected artwork for downstream listing, mockup, export, or product-variant workflows.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** One or more designs can be promoted as final selected artwork for downstream listing, mockup, export, or product-variant workflows

### Requirement: Promoting A Design As Final Does Not Delete Other Variants
FusionCanvas SHALL ensure that promoting a design as final does not delete other variants.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Promoting a design as final does not delete other variants

### Requirement: AI-generated Or AI-assisted Designs Can Reference Prompt History, Provider Metadata, And Generation Settings Where Available
FusionCanvas SHALL ensure that aI-generated or AI-assisted designs can reference prompt history, provider metadata, and generation settings where available.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** AI-generated or AI-assisted designs can reference prompt history, provider metadata, and generation settings where available

### Requirement: Basic Cleanup Or Preparation State Can Be Tracked, Such As Needs Upscale, Needs Transparent Cleanup, Cropped, Or Print-ready
FusionCanvas SHALL ensure that basic cleanup or preparation state can be tracked, such as needs upscale, needs transparent cleanup, cropped, or print-ready.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Basic cleanup or preparation state can be tracked, such as needs upscale, needs transparent cleanup, cropped, or print-ready

### Requirement: Acceptance Criteria Remain Satisfied
FusionCanvas SHALL satisfy the acceptance criteria captured in the source PRD.

#### Scenario: User Can Create A Design Record For A Listing
- **WHEN** the corresponding capability is delivered
- **THEN** A user can create a design record for a listing.

#### Scenario: User Can Reach The Design Stage With Minimal Friction When The Phrase And Visual Direction Are Already Clear
- **WHEN** the corresponding capability is delivered
- **THEN** A user can reach the Design stage with minimal friction when the phrase and visual direction are already clear.

#### Scenario: User Can Create Design Work That Uses The Current Topic And Niche Context
- **WHEN** the corresponding capability is delivered
- **THEN** A user can create design work that uses the current topic and niche context.

#### Scenario: User Can Track Whether A Design Is Approved Or Needs Revision
- **WHEN** the corresponding capability is delivered
- **THEN** A user can track whether a design is approved or needs revision.

#### Scenario: User Can Connect Design Records To Supporting Assets
- **WHEN** the corresponding capability is delivered
- **THEN** A user can connect design records to supporting assets.

#### Scenario: Multiple Design Versions Can Be Preserved
- **WHEN** the corresponding capability is delivered
- **THEN** Multiple design versions can be preserved.

#### Scenario: User Can Distinguish Draft Variants From Selected Final Artwork
- **WHEN** the corresponding capability is delivered
- **THEN** A user can distinguish draft variants from selected final artwork.

#### Scenario: User Can Preserve Separate Variants For Dark And Light Shirts
- **WHEN** the corresponding capability is delivered
- **THEN** A user can preserve separate variants for dark and light shirts.

#### Scenario: User Can Connect An AI-generated Design To Its Prompt And Source Metadata When Available
- **WHEN** the corresponding capability is delivered
- **THEN** A user can connect an AI-generated design to its prompt and source metadata when available.

## Source PRD

# FC-0203 - Design Records

## Summary

Design Records track concrete visual implementations of a listing concept.

A design record is not merely an image attachment. It represents a specific attempt, version, or selected implementation of the concept for the current item.

## User Need

Creators need to distinguish product concepts from actual design executions and export-ready files.

## Requirements

- A listing can have one or more design records.
- A design can reference the concept it implements.
- Designs belong to the Design stage in the core workflow.
- A design can be created from a selected concept or directly from a strong phrase and graphic direction.
- Design tools should inherit store, niche, topic path, phrase, graphic direction, style guidance, and relevant metadata.
- A design can track name, version, notes, and approval state.
- Designs can be marked draft, needs revision, approved, rejected, exported, or ready for export.
- Designs can be associated with assets.
- A design can represent manually created artwork, externally generated AI artwork, in-app generated AI artwork, or plugin-produced artwork.
- A listing can preserve many draft, rejected, or superseded design variants.
- A design can identify its intended use, such as dark shirts, light shirts, specific shirt colors, product types, marketplaces, or export targets.
- One or more designs can be promoted as final selected artwork for downstream listing, mockup, export, or product-variant workflows.
- Promoting a design as final does not delete other variants.
- AI-generated or AI-assisted designs can reference prompt history, provider metadata, and generation settings where available.
- Basic cleanup or preparation state can be tracked, such as needs upscale, needs transparent cleanup, cropped, or print-ready.

## Acceptance Criteria

- A user can create a design record for a listing.
- A user can reach the Design stage with minimal friction when the phrase and visual direction are already clear.
- A user can create design work that uses the current topic and niche context.
- A user can track whether a design is approved or needs revision.
- A user can connect design records to supporting assets.
- Multiple design versions can be preserved.
- A user can distinguish draft variants from selected final artwork.
- A user can preserve separate variants for dark and light shirts.
- A user can connect an AI-generated design to its prompt and source metadata when available.

## Out of Scope

- Image editing
- Automated export
- Mockup generation
- Marketplace publishing

## Related Notes

- [[Roadmap]]
- [[Product]]
- [[Data Model]]
- [[FC-0008 - Workflow Stage Navigator]]
- [[FC-0010 - Context-Aware Tools]]
- [[FC-0211 - Basic Design Tool]]
