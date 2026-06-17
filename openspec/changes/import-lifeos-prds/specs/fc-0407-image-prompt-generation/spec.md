## ADDED Requirements

### Requirement: Users Can Generate Image Prompts From Idea, Phrase, Graphic Direction, Niche Context, Topic Context, And Style Notes
FusionCanvas SHALL allow users to generate image prompts from idea, phrase, graphic direction, niche context, topic context, and style notes.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** the user can generate image prompts from idea, phrase, graphic direction, niche context, topic context, and style notes

### Requirement: Prompt Generation Should Inherit Active Store, Niche, Topic Path, Item, Stage, And Relevant Metadata By Default
FusionCanvas SHALL ensure that prompt generation should inherit active store, niche, topic path, item, stage, and relevant metadata by default.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Prompt generation should inherit active store, niche, topic path, item, stage, and relevant metadata by default

### Requirement: Graphic Direction Generation Can Be Used To Fill Or Improve The Graphic Node Of The Design Triangle
FusionCanvas SHALL ensure that graphic direction generation can be used to fill or improve the graphic node of the design triangle.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Graphic direction generation can be used to fill or improve the graphic node of the design triangle

### Requirement: Graphic Direction Generation Should Use The Idea And Phrase As Context When They Exist
FusionCanvas SHALL ensure that graphic direction generation should use the idea and phrase as context when they exist.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Graphic direction generation should use the idea and phrase as context when they exist

### Requirement: Graphic Direction Is Optional Because Some Designs Are Intentionally Text-only
FusionCanvas SHALL ensure that graphic direction is optional because some designs are intentionally text-only.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Graphic direction is optional because some designs are intentionally text-only

### Requirement: Prompts Can Be Saved To Prompt History
FusionCanvas SHALL ensure that prompts can be saved to prompt history.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Prompts can be saved to prompt history

### Requirement: Users Can Edit Generated Prompts Before Use
FusionCanvas SHALL allow users to edit generated prompts before use.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** the user can edit generated prompts before use

### Requirement: Prompt Outputs Should Remain Connected To The Listing Or Concept
FusionCanvas SHALL ensure that prompt outputs should remain connected to the listing or concept.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Prompt outputs should remain connected to the listing or concept

### Requirement: Prompt Outputs Used For Design Generation Should Also Be Connectable To The Resulting Design Variant
FusionCanvas SHALL ensure that prompt outputs used for design generation should also be connectable to the resulting design variant.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Prompt outputs used for design generation should also be connectable to the resulting design variant

### Requirement: Prompts Should Respect User-provided Constraints
FusionCanvas SHALL ensure that prompts should respect user-provided constraints.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Prompts should respect user-provided constraints

### Requirement: Acceptance Criteria Remain Satisfied
FusionCanvas SHALL satisfy the acceptance criteria captured in the source PRD.

#### Scenario: User Can Generate An Image Prompt For A Listing
- **WHEN** the corresponding capability is delivered
- **THEN** A user can generate an image prompt for a listing.

#### Scenario: User Can Generate A Prompt That Reflects The Current Topic And Niche Context Without Retyping That Context
- **WHEN** the corresponding capability is delivered
- **THEN** A user can generate a prompt that reflects the current topic and niche context without retyping that context.

#### Scenario: User Can Generate Or Improve A Graphic Direction From The Basic Concept Tool
- **WHEN** the corresponding capability is delivered
- **THEN** A user can generate or improve a graphic direction from the Basic Concept Tool.

#### Scenario: User Can Edit And Save The Prompt
- **WHEN** the corresponding capability is delivered
- **THEN** A user can edit and save the prompt.

#### Scenario: Saved Prompts Remain Connected To Creative History
- **WHEN** the corresponding capability is delivered
- **THEN** Saved prompts remain connected to creative history.

#### Scenario: Design Variant Generated From A Saved Prompt Can Reference That Prompt
- **WHEN** the corresponding capability is delivered
- **THEN** A design variant generated from a saved prompt can reference that prompt.

## Source PRD

# FC-0407 - Image Prompt Generation

## Summary

Image Prompt Generation creates visual prompts from listing context.

In the Basic Concept Tool, early graphic assistance may generate or improve the graphic direction before the Design stage turns it into a production prompt.

In the Basic Design Tool, prompt generation may prepare instructions for an external AI tool or for an in-app AI image provider. Prompt generation is distinct from image generation, but both should preserve prompt history and item context when used together.

## Requirements

- Users can generate image prompts from idea, phrase, graphic direction, niche context, topic context, and style notes.
- Prompt generation should inherit active store, niche, topic path, item, stage, and relevant metadata by default.
- Graphic direction generation can be used to fill or improve the graphic node of the design triangle.
- Graphic direction generation should use the idea and phrase as context when they exist.
- Graphic direction is optional because some designs are intentionally text-only.
- Prompts can be saved to prompt history.
- Users can edit generated prompts before use.
- Prompt outputs should remain connected to the listing or concept.
- Prompt outputs used for design generation should also be connectable to the resulting design variant.
- Prompts should respect user-provided constraints.

## Acceptance Criteria

- A user can generate an image prompt for a listing.
- A user can generate a prompt that reflects the current topic and niche context without retyping that context.
- A user can generate or improve a graphic direction from the Basic Concept Tool.
- A user can edit and save the prompt.
- Saved prompts remain connected to creative history.
- A design variant generated from a saved prompt can reference that prompt.

## Out of Scope

- Image generation
- Image editing
- Provider-specific prompt tuning

## Related Notes

- [[Roadmap]]
- [[FC-0205 - Prompt History]]
- [[FC-0208 - Design Triangle View]]
- [[FC-0210 - Basic Concept Tool]]
- [[FC-0211 - Basic Design Tool]]
- [[FC-0010 - Context-Aware Tools]]
