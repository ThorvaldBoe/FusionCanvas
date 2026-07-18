## Context

The accepted domain model already represents `Listing` as a store-scoped item with optional niche/group placement, draft status, archive state, timestamps, description, and extensible metadata. SQLite already persists listings and their tag and asset relationships through `WorkspaceSnapshot`, while `WorkspaceNavigation` can render and move listings under niches and groups. There is no listing-management application service or user-facing capture/management workflow, and navigation currently rejects or omits a listing whose only parent is its store.

FC-0104 is a user-facing, cross-layer change for individual creators who capture ideas frequently and reorganize them as their understanding improves. One-line capture and listing selection are frequent daily actions; editing, moving, and duplicating are occasional; archive, restore, and permanent deletion are rare. The primary workspace must remain a compact navigation-and-creative-work surface, and FC-0105/FC-0106 retain ownership of workflow status and the full listing inspector.

## Goals / Non-Goals

**Goals:**

- Provide application services for listing creation, core-detail editing, movement, duplication, archive, restore, deletion, listing, and selection.
- Accept active store, niche, and group locations, including listings placed directly under a store.
- Make one-line capture available beside the active navigation context without requiring assets or later-stage data.
- Preserve listing identity and every related record during movement and reversible lifecycle actions.
- Materialize applicable inherited tags and metadata at creation through the shared context-resolution boundary.
- Keep occasional management in a focused surface with complete draft, focus, validation, loading, error, and destructive-action behavior.
- Reuse the existing workspace snapshot repository and SQLite schema where possible.

**Non-Goals:**

- Do not implement workflow-stage or lifecycle-status editing from FC-0105.
- Do not implement the document-window Listing Inspector, stage-specific creative fields, or asset management from FC-0106 and later changes.
- Do not implement marketplace metadata, publishing, version history, performance data, bulk operations, or listing-to-group conversion.
- Do not permit cross-store movement or duplication in this change because tags, assets, prompts, and inherited context are store-scoped.
- Do not copy asset or prompt relationships when duplicating a listing.

## Decisions

1. Add a dedicated listing-management application boundary over `WorkspaceSnapshot`.

   A `ListingManagementService` will expose request/result/state records and perform validation and atomic snapshot mutations through `IWorkspaceRepository`, following the existing store and niche management pattern. This centralizes ownership checks and relationship-preservation rules without expanding the repository into entity-specific CRUD methods.

   Alternative considered: mutate the navigation snapshot directly from view models. That would duplicate validation in the UI, make persistence failures harder to handle, and bypass a reusable application contract.

2. Represent listing placement as one validated store, niche, or group location.

   Listing-management requests will use an explicit location reference. A store location writes both `NicheId` and `GroupId` as null; a niche location writes `NicheId`; a group location writes `GroupId` and retains the group's direct niche identifier where available. The service will resolve the full group ancestry for validation, require the destination and listing to share a store for moves, and reject missing or archived destinations.

   Navigation will be generalized to render store-only listings directly under the store node and to accept store destinations for listing movement. Existing topic-only references remain appropriate for group movement.

   Alternative considered: translate store-level capture into an implicit default niche. This contradicts the PRD, creates surprising structure, and makes a supposedly fast capture path depend on an unrelated setup decision.

3. Treat the required one-line value as the listing's working title or idea line.

   Creation requires one nonblank trimmed line and defaults the listing to draft state. Optional description/notes and context-derived metadata may be supplied, but assets, prompts, tags, marketplace fields, and final artwork are not prerequisites. More structured idea, phrase, graphic, workflow, status, tag, and asset editing remains owned by later feature specs.

   Alternative considered: add separate title and idea columns now. FC-0106 and Phase 2 refine those fields; adding schema prematurely would make FC-0104 larger and risk freezing the wrong representation.

4. Apply inherited context as an explicit creation input.

   The listing workflow will request resolved context from the accepted context-aware service for the selected location. Only tags and metadata identified as applicable to new work are copied into the new listing's tag links and metadata, alongside source/provenance markers that distinguish inherited values. User overrides supplied before save win over defaults. Moving does not recalculate inherited context, because a move reorganizes an existing concept rather than rewriting it.

   Alternative considered: dynamically derive every inherited value whenever a listing is read. That would cause old listings to change when parent context changes and would make preservation across moves ambiguous.

5. Duplicate into the selected source location as a new draft variation.

   Duplication creates a new identity and timestamps, copies the working title with a non-destructive `Copy` suffix, description/notes, listing metadata, and tag links, and resets the operational status to Draft. Asset links, prompts, and future dependent/version records remain attached only to the source. The user may choose another valid location before confirming, but it must remain in the same store.

   Alternative considered: copy all related records. Asset reuse and version lineage need explicit product rules in later features; implicit copying risks treating shared or final artwork as belonging to an unreviewed variation.

6. Make archive reversible and permanent deletion guarded.

   Archive toggles the listing's archive flag and removes it from normal active navigation without changing its location or relationships. Restore returns it to its preserved active location; if that store or topic is archived, restoration is blocked with a path to restore or choose an active destination. Permanent deletion requires explicit confirmation and is blocked while prompts, asset links, or future dependent records reference the listing. Successful deletion removes the listing and its listing-tag links atomically but never deletes reusable tag records.

   Alternative considered: cascade every connected record. That would make a compact management command capable of destroying creative history and files, contrary to the local-first safety model.

7. Split frequent capture from focused management.

   The primary navigation pane owns a compact New Listing command and one-line draft anchored to the selected store, niche, or group. Successful capture selects and reveals the listing and may open it through the existing document-tab flow. Rename/edit, move, duplicate, archived review, restore, and delete live in a focused listing-management surface opened from the selected item or compact action menu; this surface does not permanently consume the document workspace.

   When capture starts, focus moves to the idea line; Escape cancels an unchanged draft and explicit cancellation protects meaningful input. The focused surface preselects the invoking listing, uses explicit Save, detects unsaved changes before selection changes or close, and returns focus to the invoking/replacement navigation control. Commands are enabled only for applicable active/archived states and use compact button sizing.

   Alternative considered: permanently show listing administration below the navigation tree. Listings are numerous and management is intermittent, so this would displace higher-value navigation and stage-tool content.

8. Model all interaction states in the presentation layer.

   No active store shows a blocked creation state with a store-selection/setup path. An empty valid location keeps New Listing available. In-progress operations disable duplicate submission while retaining the draft. Validation and repository errors remain inline and preserve input. Success refreshes navigation and selection. Archiving or deleting the selected listing selects a sensible nearby active listing or its parent location; otherwise an actionable empty state is shown. Confirmation cancellation leaves data and selection unchanged.

   Alternative considered: let each view infer aftermath behavior. Central state/result contracts make selection and errors deterministic and testable across capture and focused management.

## Risks / Trade-offs

- [Store-level listings broaden the existing navigation shape] -> Add targeted navigation delta scenarios and tests for build, selection, reveal, and movement at store level.
- [Snapshot replacement can overwrite concurrent mutations] -> Serialize mutations through the existing application workflow and reload immediately before validation/save; revisit optimistic concurrency when multi-process editing exists.
- [Metadata JSON can become an untyped catch-all] -> Limit FC-0104 keys to documented listing context/provenance and preserve unknown keys during edits and duplication.
- [Duplicate semantics may evolve when asset/version features arrive] -> Keep asset and prompt copying explicitly off by default and expose duplication through an application request that later changes can extend.
- [Archived parent topics can make restoration ambiguous] -> Preserve the original location and block restore with an actionable explanation instead of silently relocating the listing.
- [FC-0103 group management may not yet be implemented] -> Keep group handling behind the common location contract and test it using existing persisted group/navigation entities; the UI exposes only destinations present in the active snapshot.

## Migration Plan

1. Add the listing application contracts and unit tests against in-memory snapshots.
2. Generalize listing navigation locations and add store-level tree/movement coverage.
3. Implement the listing service and verify round trips through the existing SQLite repository; no database schema migration is expected.
4. Add compact capture and focused management presentation with interaction-state tests.
5. Run domain, application, integration, and app test suites, then validate the OpenSpec change.

Rollback removes the new service and presentation wiring and restores topic-only navigation behavior. Existing listing rows remain readable because FC-0104 does not change the SQLite schema; any store-level listings created before rollback must first be moved to a niche/group to remain visible in the older navigation implementation.

## Open Questions

None for FC-0104. The PRD's delete, duplication, and conversion questions are resolved above; later changes may deliberately revise those choices with their own delta specs.
