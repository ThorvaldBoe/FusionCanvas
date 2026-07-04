## Context

FusionCanvas already has Phase 0 foundations for store-shaped data: `Store` exists in the domain model, workspace snapshots persist stores to SQLite, the navigation tree exposes stores as top-level contexts, and context-aware tools can resolve an active store from navigation state. What is missing is the Phase 1 user workflow that lets a creator manage stores directly as business, brand, client, or publishing contexts.

The current repository pattern persists complete `WorkspaceSnapshot` values. Store management should build on that contract first, adding application-level commands that load, transform, and save snapshots. A later persistence change can introduce narrower repository methods if mutation volume or concurrency needs justify it.

## Goals / Non-Goals

**Goals:**

- Provide application services for creating, editing, archiving, restoring, listing, and selecting stores.
- Keep store creation lightweight, requiring only a name and allowing optional context.
- Preserve archived store data and make archived stores visible through an intentional archived view.
- Keep active store selection available to navigation, document, and tool-context flows.
- Add focused unit and integration coverage for store workflows.

**Non-Goals:**

- Do not add marketplace account setup, publishing destinations, analytics, billing, permissions, templates, or automated setup.
- Do not replace the existing domain model, SQLite schema, or workspace snapshot repository unless implementation discovers a required defect.
- Do not implement niche, group, listing, tag, asset, or search management beyond showing that those future areas are scoped by the active store.
- Do not add multi-window or multi-user conflict handling for simultaneous store edits.

## Decisions

1. Add a `StoreManagementService` in the application layer.

   The service should expose commands such as create, update, archive, restore, list active stores, list archived stores, and select active store. It should validate names, preserve stable store identity, update timestamps, and save the resulting workspace snapshot through the existing repository abstraction.

   Alternative considered: put store commands directly in Avalonia view models. That would be faster for the first screen, but it would make store behavior harder to test and reuse from navigation, document, or future command surfaces.

2. Treat store context as structured editable fields plus flexible metadata.

   The existing store shape has name, description, archive state, timestamps, and metadata JSON. Phase 1 can map user-facing notes, target market, brand direction, and planning guidance into a small application model backed by metadata JSON while keeping the domain entity stable.

   Alternative considered: add first-class columns/properties for every context field now. That would make current UI binding explicit, but it risks locking early PRD wording into the core model before later AI and marketplace phases clarify which fields deserve stable schema.

3. Keep archive and restore as reversible state changes.

   Archiving should set the store archive state and keep related workspace records intact. Normal active-store lists and selectors should exclude archived stores by default, while an archived view can show and restore them.

   Alternative considered: hard delete inactive stores. That conflicts with the PRD need to preserve historical creative context and would make later analytics or context review harder.

4. Model active store selection in application/UI state, not as persisted global workspace state.

   Opening a store should update the active workspace context for the current app session and drive navigation and document context. Persisting a last-opened store can be added later as a user preference if needed, but it is not required for store management behavior.

   Alternative considered: store active-store selection in the workspace database. That would blur durable creative data with local UI preference and complicate future multi-window behavior.

5. Provide a compact store selector/management surface in the Avalonia shell.

   The UI should let a first-time user create a store, switch between active stores, edit store context, and intentionally reveal archived stores. Navigation can continue to represent store hierarchy; this change adds the management actions around that hierarchy.

   Alternative considered: implement store management only through the navigation tree context menu. That would hide first-store setup and archived-store restoration from users who do not yet have navigable content.

## Risks / Trade-offs

- [Risk] Metadata JSON can become an untyped dumping ground -> Mitigation: centralize metadata mapping in the application service and cover round-trip behavior with tests.
- [Risk] Snapshot-level saves are coarse-grained -> Mitigation: keep store operations small and deterministic, and revisit narrower repository operations only when the app has heavier concurrent edits.
- [Risk] Archived stores might disappear too completely from the UI -> Mitigation: require a dedicated archived-store view or filter with restore actions.
- [Risk] Active-store selection may conflict with open tabs from another store -> Mitigation: update active store through explicit selection while allowing existing document contexts to continue carrying their own store identity.

## Migration Plan

No database migration is expected for the initial implementation because stores already persist name, description, archive state, timestamps, and metadata JSON. Existing workspaces without stores should open into an empty-state flow that invites the user to create the first store. Rollback is limited to removing the service/UI behavior; persisted store records remain compatible with the existing SQLite model.

## Open Questions

- Which store context fields should be shown as first-class UI fields in the first implementation versus stored only as notes/context?
- Should the app remember the last active store after restart as a user preference, or should startup always show the store selector?
- Should archived stores be hidden everywhere by default, or only from the primary selector and active navigation list?
