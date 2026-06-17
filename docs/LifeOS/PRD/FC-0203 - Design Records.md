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
