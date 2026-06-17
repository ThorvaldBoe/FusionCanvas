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
