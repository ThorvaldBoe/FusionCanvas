# FC-0408 - Listing Text Generation

## Summary

Listing Text Generation drafts marketplace-oriented titles, descriptions, keywords, and tags.

## Requirements

- Users can generate listing text from listing context.
- Listing text generation should inherit store, niche, topic path, selected concept, design context, tags, and relevant metadata.
- Generated text can include title, description, keywords, tags, and notes.
- Users can review and edit before saving.
- AI output should not publish directly.
- Generated text can update the listing metadata draft after approval.

## Acceptance Criteria

- A user can generate draft listing metadata.
- A user can generate listing text that reflects the current niche, topic, concept, and design context without retyping that context.
- A user can edit generated text before saving.
- Saved text appears in the listing metadata editor.

## Out of Scope

- Direct publishing
- SEO guarantees
- Marketplace compliance validation

## Related Notes

- [[Roadmap]]
- [[FC-0207 - Listing Metadata Editor]]
- [[FC-0403 - Niche AI Context]]
- [[FC-0010 - Context-Aware Tools]]
