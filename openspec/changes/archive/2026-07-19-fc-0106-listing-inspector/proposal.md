## Why

FC-0104 made listings creatable and organizable, but the document window still shows only placeholder stage-tool cards for an active listing: a creator cannot see or update what the listing actually is — its idea, phrase, graphic direction, notes, tags, and attached assets — without opening the secondary properties dialog or reading raw tree rows. PRD FC-0106 requires one focused, durable Listing Inspector in the document detail area so a creator can understand a listing as a product concept and refine its creative context while browsing, without turning Phase 1 into a marketplace listing editor.

## What Changes

- Add a Listing Inspector as the detail-area working view for listing document contexts, replacing today's placeholder stage-tool card content for listings and aligning with the workflow stage navigator above it.
- Display core listing details in one glance: working title, lifecycle status, current workflow stage, topic path, idea, phrase, graphic direction, notes, tags, and related assets, with an explicit empty state when a listing has no assets or no creative fields filled in yet.
- Support lightweight in-place editing of the working title, idea, phrase, graphic direction, and notes with explicit Save, dirty tracking, and a save/discard/cancel guard when the user switches listing, switches tab, or closes a tab with unsaved changes (resolves the PRD autosave-vs-explicit-save open question in favor of explicit save, consistent with the established properties surface and UX editing-safety guidance).
- Introduce the Phase 1 creative fields `idea`, `phrase`, and `graphicDirection` as documented reserved listing metadata keys (plain text; notes remain plain text only), preserving unknown metadata keys and inherited provenance; no database migration.
- Add tag management in the inspector: view linked tags, add an existing store tag, create a new reusable store tag inline when the name has no match, and remove tag links without deleting reusable tags (full tag administration remains with FC-0108).
- Show related assets as a read-only list (name, kind, missing state) so it is obvious whether a listing has supporting assets yet; attaching or detaching assets stays with future asset features (resolves the PRD related-assets open question toward visibility only).
- Keep the inspector coherent for unavailable work: an archived or effectively inactive listing opens read-only with guidance to restore, and a persistence failure keeps the draft recoverable.
- Display lifecycle status and workflow stage in the inspector and keep them synchronized with the document-context status control and stage navigator; the inspector does not add a second competing status selector (status-change ownership stays with the FC-0105 document-card control).

## Capabilities

### New Capabilities

- `listing-inspector`: The document detail-area inspector for listings — core-detail display, stage-relevant creative-field presentation, explicit-save editing with unsaved-change guards, tag link management, read-only related-asset visibility, status/stage alignment, state-coherent read-only behavior for inactive listings, and recoverable error handling.

### Modified Capabilities

- `tabbed-document-window`: When the active document context is a listing item, the detail area presents the Listing Inspector as its working view instead of placeholder stage-tool content; non-item contexts keep existing stage-tool behavior.

## Impact

- **Domain**: no new entities; documented reserved listing metadata keys (`idea`, `phrase`, `graphicDirection`) join the existing `notes` key convention.
- **Application**: new focused inspector contract/service that builds inspector state (details, creative fields, tags with provenance, related assets, status/stage, effective activity) and saves the form atomically — resolving or creating store tags by name and delegating the listing update through the existing FC-0104 listing-management mutation path and repository boundary.
- **Integration**: no schema migration; inspector state and save round-trip through the existing SQLite snapshot model (listings, tags, listing-tag links, assets, asset links).
- **App**: new inspector view + view model hosted in the document window detail area for listing tabs; placeholder card remains for store/topic contexts; unsaved-change guard hooks into tab switch/close and selection coordination; shared compact-control and tooltip guidance applies.
- **Tests**: domain/application tests for inspector state building, field normalization, metadata preservation, tag resolve-or-create, and atomic save; integration tests for tag creation and round-trip persistence; view-model tests for dirty tracking, guard, and state coherence; plus a real desktop UI verification pass on a disposable workspace.
- **Dependencies**: FC-0104 (listing-management) is the accepted foundation. FC-0105 (listing-lifecycle-status) is proposed but not yet merged; FC-0106 displays whatever status/stage facts exist at implementation time and adds no competing status control, so it lands cleanly before or after FC-0105. Boundaries respected toward FC-0107 (search/filter), FC-0108 (tag administration), FC-0109 (asset import), and FC-0207 (marketplace metadata editor).
- PRD intent source: `docs/LifeOS/PRD/FC-0106 - Listing Inspector.md`.
