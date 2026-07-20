## Why

FusionCanvas already persists store-scoped `Tag` records and `ListingTag` links, and the editable tree query already accepts a tag filter, but there is no way for a creator to create, rename, recolor, archive, delete, or apply tags through the application. The PRD (FC-0108) asks for flexible cross-cutting labels so work can be found and grouped without forcing every organizational need into the niche/group hierarchy. That workflow is missing today: tags exist only as data and as inherited context on listing creation, never as a managed, filterable, user-facing surface.

## What Changes

- Add a `tag-management` application service that owns store-scoped tag lifecycle and tag application to listings: create, rename, change color, archive, restore, delete, apply-to-listing, remove-from-listing, list/lookup, and tag-filter query support, all atomic and reload-safe.
- Add a dedicated **Tags** tab in the existing store management window for occasional tag administration: create, rename, recolor, archive, restore, and confirmed permanent deletion. Reuse the store/niche editor pattern rather than adding a new window.
- Add an inline tag editor on the listing secondary properties surface so creators can apply and remove tags on the selected listing with autocomplete from the store's active tags, create-on-the-fly, colored chips, and keyboard-first add/remove. Tag apply/remove persists immediately and atomically; the existing explicit Save continues to own description/notes only.
- Activate tag filtering in the editable workspace tree using a compact tag filter control (chips/popover) above the tree. Selecting tags filters listings through the existing `TreeQuery.TagIds` boundary, preserves ancestor paths, keeps stable selection, and clearly indicates filtered-out selection.
- Render tag color chips on listing rows, in the tag editor, in the filter control, and in the Tags tab so colored labels support visual scanning.
- Add a dedicated `Color` field on the `Tag` domain entity and a versioned SQLite migration (schema 3 → 4) that adds a nullable `color` column to the `tags` table and backfills empty values for existing tags.
- Make permanent tag deletion remove every `ListingTag` link for that tag atomically after explicit confirmation; the warning states that the tag will be removed from all listings. Reusable tag records are otherwise preserved by listing deletion (already specified by `listing-management`).
- Reuse optimistic UI, rollback, busy-state protection, unsaved-change handling, compact action sizing, icon tooltips, and keyboard-accessible confirmation patterns from FC-0103/FC-0104.

### Out of Scope

- Applying tags to topics (niches/groups), assets, prompts, or future entities. The PRD's "apply tags to topics where useful" is intentionally deferred to a follow-up change that will generalize `ListingTag` into a generic `EntityTag` with its own migration. FC-0108 keeps the existing `listing_tags` table and listing-only application.
- Tag hierarchy, automation, analytics, marketplace keyword optimization, AI tag suggestions, required tag schemas, saved tag views, and bulk tag operations.
- Global (cross-store) tags. Tags remain store-scoped, matching the existing domain model and persistence.

## Capabilities

### New Capabilities

- `tag-management`: Defines store-scoped tag lifecycle (create, rename, recolor, archive, restore, delete), tag application to and removal from listings, tag lookup, tag-filter query support, and the desktop surfaces that expose these actions (store management Tags tab, listing tag editor, tree tag filter).

### Modified Capabilities

- `listing-management`: The secondary listing properties surface gains explicit tag editing (apply/remove with autocomplete, create-on-the-fly, colored chips, keyboard add/remove). Tag apply/remove persists immediately and atomically, separate from the description/notes Save.
- `navigation-tree`: The accepted "filter-ready" tag filter becomes active: a tag filter control projects the tree through `TreeQuery.TagIds`, preserves ancestor paths, keeps stable selection, and indicates filtered-out selection.
- `local-sqlite-persistence`: The `tags` table gains a nullable `color` column via a versioned schema 3 → 4 migration with safe backfill and existing forward/backward compatibility guarantees.

## Impact

- **Domain:** Add a nullable `Color` field to the `Tag` record; keep `ListingTag` as the only tag link. No new entities.
- **Application:** New `ITagManagementService` (requests, results, summaries, state, lifecycle, apply/remove, lookup, filter support). `ListingManagement` and `ToolContextResolver` keep their existing tag-link and inherited-context behavior; the listing properties surface routes tag edits through the new service.
- **Integration:** SQLite migration 3 → 4 adds `tags.color`; round-trip color, archive state, and listing-tag links. Existing snapshot validation, atomic saves, and schema-version safeguards are reused.
- **Desktop UI:** Add a Tags tab to the store management window; add a tag editor region to the listing properties surface; add a tag filter control above the workspace tree; render colored tag chips on listing rows, the editor, the filter, and the Tags tab. Follow shared button, tooltip, keyboard, confirmation, and unsaved-change guidance.
- **Testing:** Domain, application, integration, app, and desktop UI verification for tag lifecycle, color round-trip, apply/remove, filtering, archive/restore, confirmed deletion with link cleanup, rollback, and migration compatibility. No new test projects.
- **Compatibility:** Existing tags receive empty color during migration; existing `ListingTag` rows, listing context inheritance, archive behavior, and stable IDs remain intact. Topic/asset tagging remains unimplemented and is explicitly deferred.
