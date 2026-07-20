## Context

FC-0104 established the listing as the primary item entity with title, description, notes, metadata, tag links, and a secondary properties surface that edits description and notes with explicit save and dirty tracking. FC-0105 established listing-lifecycle-status for operational status. FC-0103 established that listings move and duplicate within their store while preserving metadata and tag links. The in-flight concept-versions, design-records, and mockup-records changes add the creative records whose context the Listing stage needs to inherit. The Phase 2 PRD requires one place to prepare title, description, price, keywords, tags, status, and marketplace notes before publishing, to inherit earlier-stage context, to store metadata before any marketplace integration exists, and to distinguish creative notes from publishing metadata.

What is missing is the marketplace-preparation metadata model, the focused editor, and the separation of creative notes from publishing metadata.

## Goals / Non-Goals

**Goals:**

- Add marketplace-preparation metadata fields: price (with optional currency), marketplace notes, product type, provider product reference, shipping profile notes, and draft keywords.
- Provide a focused Listing Metadata Editor surface that inherits earlier-stage context and edits the marketplace-preparation fields with explicit save, dirty tracking, and unsaved-change prompts.
- Distinguish creative notes (listing-management) from publishing metadata (this capability).
- Preserve marketplace metadata through listing moves, archive/restore, and duplication, including unknown metadata keys.
- Persist metadata edits through the existing atomic snapshot model without a schema migration.

**Non-Goals:**

- Do not implement marketplace API publishing, SEO scoring, AI listing text generation, or bulk metadata editing.
- Do not change listing title, description, notes, tag links, or operational status behavior; those belong to listing-management, tag-management, and listing-lifecycle-status.
- Do not implement provider catalog import or shipping profile automation.
- Do not model every marketplace requirement up front; marketplace-specific details live in metadata until a direct integration proves which fields need structure.

## Decisions

### 1. Marketplace-preparation metadata is documented listing metadata

Price, marketplace notes, product type, provider product reference, shipping profile notes, and draft keywords are stored as documented listing metadata keys (`listing.price`, `listing.marketplaceNotes`, `listing.productType`, `listing.providerProductRef`, `listing.shippingNotes`, `listing.draftKeywords`). The listing-management metadata rules already preserve unknown keys during edits and duplication, so no schema migration is required. A later integration may promote specific keys to structured fields through its own delta.

Alternative considered: add dedicated columns for each marketplace field. Rejected because the PRD explicitly says marketplace-specific details should live in metadata until a direct integration proves which fields need structure.

### 2. Creative notes and publishing metadata are visibly separated

The listing properties surface keeps creative description and notes (listing-management) in one section and marketplace-preparation metadata (this capability) in a distinct section. Both use explicit save and dirty tracking, but their drafts are independent: saving creative notes does not save marketplace metadata and vice versa. This lets creators prepare publishing data without entangling it with in-house creative notes.

Alternative considered: merge all fields into one save action. Rejected because the PRD requires distinguishing creative notes from publishing metadata.

### 3. Price is a decimal with optional currency

Price is stored as a decimal value with an optional ISO 4217 currency code (defaulting to the store's configured currency when available). FusionCanvas does not validate price against any marketplace rule; it only validates that the value, when present, is a non-negative decimal.

Alternative considered: store price as a free-form string. Rejected because downstream integrations benefit from a numeric value.

### 4. Draft keywords are a separate concept from tags

Draft keywords are stored as a list of strings in `listing.draftKeywords` for marketplace preparation, distinct from the store-scoped reusable tag entities owned by tag-management. Keywords are preparation data that may later be mapped to marketplace-specific keyword fields by integrations; tags are reusable classification labels. Both can coexist on a listing.

Alternative considered: reuse tag-management for keywords. Rejected because marketplace keywords are often per-listing and per-marketplace, not reusable store-scoped labels.

### 5. Reuse the focused-surface and dirty-tracking pattern

The Listing Metadata Editor is a focused surface or a clearly delineated section of the existing listing properties surface, opened from a selected listing. It preselects the active listing, uses explicit save for marketplace-metadata drafts, detects meaningful unsaved changes before switching or closing, uses content-sized actions, and returns focus to the invoking or replacement control. It follows the same shared desktop control guidance as the listing properties surface.

Alternative considered: dock marketplace fields permanently beside the tree. Rejected because marketplace preparation is occasional work that should not consume primary workspace.

### 6. Context inheritance through the accepted boundary

The editor requests resolved context for the selected listing from the accepted context-aware boundary, including idea, selected concept, selected final designs, mockups, niche, topic path, and store. Creators can copy or reference inherited values into marketplace fields manually; FusionCanvas does not auto-populate marketplace fields from inherited context because marketplace text is preparation data the creator should approve.

Alternative considered: auto-generate title and description from concept and design context. Rejected because AI listing text generation is out of scope and auto-population risks publishing unreviewed content.

## Risks / Trade-offs

- [Metadata keys can drift across marketplaces] -> Keep keys documented and let later integrations promote specific keys to structured fields.
- [Two independent save actions can confuse users] -> Clearly label the two sections and their save buttons, and warn only on meaningful unsaved changes within each section.
- [Draft keywords and tags can overlap] -> Document the distinction and let a later change map between them if needed.

## Migration Plan

No database migration is expected. Marketplace-preparation fields are stored as documented listing metadata keys. Existing listings load with absent marketplace metadata.

## Open Questions

- Should the editor surface be a standalone focused surface or a section of the existing listing properties surface? (Default: a clearly delineated section of the existing properties surface, to avoid surface proliferation.)
- Should draft keywords support per-marketplace grouping now, or a flat list? (Default: flat list with optional marketplace tags in metadata; per-marketplace grouping deferred to integrations.)
