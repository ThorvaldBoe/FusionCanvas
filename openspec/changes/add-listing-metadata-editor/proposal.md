## Why

Listings already capture a working title, description, and notes, but FusionCanvas cannot yet prepare the marketplace-oriented metadata a creator needs before publishing: price, marketplace notes, product type, provider product reference, shipping profile notes, and draft keywords. FC-0207 adds the Listing Metadata Editor as the focused Listing-stage surface that captures marketplace preparation data using context from earlier stages, keeps it connected to the listing through moves and lifecycle changes, and distinguishes creative notes from publishing metadata without publishing anything.

## What Changes

- Add marketplace-preparation metadata fields to the listing: price (with optional currency), marketplace notes, product type, provider product reference, shipping profile notes, and draft keywords.
- Add a focused Listing Metadata Editor surface that inherits idea, concept, design, niche, topic, and store context and edits the marketplace-preparation fields with explicit save, dirty tracking, and unsaved-change prompts.
- Distinguish creative notes (owned by listing-management) from publishing metadata (owned by this capability) so creators can tell preparation data from in-house notes.
- Keep marketplace metadata connected to the listing through moves, archive/restore, and duplication, preserving unknown metadata keys.
- Allow marketplace-specific fields to be captured as draft preparation data without any marketplace integration, API, or publishing action.
- Reuse listing-lifecycle-status for operational status and listing-management for title, description, notes, tag links, and the secondary properties surface.

## Capabilities

### New Capabilities

- `listing-metadata-editor`: Defines the focused Listing-stage editor for marketplace-preparation metadata (price, marketplace notes, product type, provider product reference, shipping profile notes, draft keywords), with context inheritance, creative-notes versus publishing-metadata separation, persistence through listing moves and lifecycle, and atomic snapshot persistence.

### Modified Capabilities

None. FC-0207 reuses `listing-management` for the listing properties surface and title/description/notes editing, `listing-lifecycle-status` for operational status, `context-aware-tools` for inherited context, and `tag-management` for tag links without changing their accepted requirements.

## Impact

- Adds marketplace-preparation metadata fields to the listing model through the existing snapshot boundary; no schema migration is expected beyond documented metadata keys.
- Adds a `ListingMetadataEditorService` application service, view model, and a focused surface or section that edits the marketplace-preparation fields.
- Reuses the existing listing properties surface coordination, dirty tracking, unsaved-change prompts, and shared desktop control guidance.
- Adds domain, application, app, and UI tests for field editing, context inheritance, creative-notes separation, preservation through moves and lifecycle, unknown-metadata preservation, and atomic persistence.
