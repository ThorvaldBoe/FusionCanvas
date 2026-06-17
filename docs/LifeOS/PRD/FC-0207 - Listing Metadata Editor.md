# FC-0207 - Listing Metadata Editor

## Summary

The Listing Metadata Editor manages marketplace-oriented text, price, status, and preparation information for a listing.

## User Need

Creators need one place to prepare listing title, description, price, keywords, tags, status, and marketplace notes before publishing.

## Requirements

- Users can edit listing title, description, price, keywords, tags, status, and notes.
- Listing metadata belongs to the Listing stage in the core workflow.
- The editor should inherit relevant idea, concept, design, niche, topic, and store context.
- Metadata can be stored before a marketplace integration exists.
- Marketplace-specific fields can be captured where useful.
- Provider product, color, shipping, and marketplace notes can be stored as draft preparation data.
- Draft metadata should remain connected to the listing.
- Users can distinguish creative notes from publishing metadata.

## Acceptance Criteria

- A user can prepare listing text for a product.
- A user can prepare listing metadata using context already captured in earlier workflow stages.
- A user can prepare price and basic marketplace notes without publishing.
- A user can store keywords and marketplace notes.
- Metadata remains available when the listing moves.
- The editor supports preparation without publishing.

## Out of Scope

- Marketplace API publishing
- SEO scoring
- AI listing text generation
- Bulk metadata editing

## Related Notes

- [[Roadmap]]
- [[Product]]
- [[Data Model]]
- [[FC-0008 - Workflow Stage Navigator]]
- [[FC-0010 - Context-Aware Tools]]
- [[FC-0212 - Basic Listing Tool]]
