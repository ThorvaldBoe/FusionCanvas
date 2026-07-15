## Context

FusionCanvas already has the Phase 0 topic model needed for niches: `Niche` is a top-level topic inside a `Store`, workspace snapshots persist core entities to SQLite, the navigation tree exposes niches as top-level store topics, and context-aware tools can resolve an active niche from navigation state. FC-0101 also established the store-management pattern for application-level commands that load a workspace snapshot, transform it, and save it through `IWorkspaceRepository`.

Niche management should build on those accepted foundations. The change should add the Phase 1 user workflow for creating, editing, archiving, restoring, browsing, and eventually deleting niches, while treating later AI research, trend scoring, analytics, and marketplace validation as future capabilities.

Niche setup should follow [FusionCanvas UI Guidelines](../../../docs/ui-guidelines.md), especially the guidance that store and niche setup preserve durable creative context and that regular workspace controls stay compact, navigable, and free of heavy administration forms.

## Goals / Non-Goals

**Goals:**

- Provide application services for creating, updating, archiving, restoring, listing, deleting, and selecting niches inside a store.
- Require a valid active or explicit store context before creating a niche.
- Keep niche creation lightweight, requiring only a name and allowing optional creative context.
- Preserve niche identity, store association, child groups, listings, prompts, assets, tags, and metadata when renaming, editing context, archiving, or restoring.
- Make active niches the default top-level folder/topic nodes shown under the selected store in the main window sidebar.
- Keep archived niches available through an intentional archived view while excluding them from normal active navigation by default.
- Keep niche context available to navigation, document, and context-aware tool flows.
- Extend the existing store management window with tabs for basic store info and store-scoped niches instead of introducing a separate niche management window.
- Add focused application, persistence, and UI view-model coverage for niche workflows.

**Non-Goals:**

- Do not add AI niche research, automated recommendations, trend tracking, niche scoring, marketplace demand analysis, or analytics views.
- Do not implement group, listing, tag, asset, prompt-history, or search management beyond preserving existing associations and exposing niche context for future features.
- Do not introduce cross-store niche libraries or sharing.
- Do not replace the existing domain model, SQLite schema, or workspace snapshot repository unless implementation discovers a required missing persisted field.
- Do not allow archived niches to become active creation context through normal active navigation.

## Decisions

1. Add a `NicheManagementService` in the application layer.

   The service should expose commands such as create, update, archive, restore, list active niches, list archived niches, delete empty niche, and select active niche. It should validate names within the containing store, preserve stable niche identity, update timestamps, and save the resulting workspace snapshot through the existing repository abstraction.

   Alternative considered: put niche commands directly in Avalonia view models. That would be faster for the first screen, but it would duplicate store-management lessons and make niche behavior harder to test from future command surfaces.

2. Scope niche names and lifecycle state to a store.

   A niche belongs to one store and active niche name uniqueness should be enforced within that store. Identical niche names can exist in different stores because stores represent separate brand or business contexts.

   Alternative considered: enforce global niche names. That would make cross-store filtering simpler later, but it weakens the store boundary and creates friction for creators who reuse audience terms across separate shops.

3. Treat niche context as a small typed application model backed by flexible metadata.

   User-facing fields should cover practical Phase 1 context such as audience, humor style, visual style guidance, constraints, risks, research notes, and general notes. These can map to metadata JSON where the current domain shape does not have first-class fields, keeping the entity stable while making UI and service code explicit.

   Alternative considered: add a separate table or first-class domain property for every niche context field now. That would make bindings direct, but it risks freezing early planning language before later AI and analytics phases clarify which fields deserve durable schema.

4. Keep archive and restore reversible.

   Archiving should mark the niche archived and preserve child groups, listings, prompts, assets, tags, and metadata. Normal active navigation and active niche lists should exclude archived niches by default, while an archived niche view can show and restore them.

   Alternative considered: move archived niches into a separate store-level holding area. That adds complexity and could break topic paths for child work. A reversible archive state preserves relationships more cleanly.

5. Use the navigation tree as the normal niche browsing surface.

   When an active store is opened, the main window sidebar should show active niches as the top-level folder/topic nodes directly under that store. Niche creation and management can be invoked from compact navigation commands or store management actions, but normal browsing should remain in the existing left navigation pane rather than a separate niche dashboard.

   Alternative considered: create a standalone niche board as the primary browsing surface. That might be attractive for future performance or AI summaries, but it would duplicate the navigation tree before groups and listings exist.

6. Extend the existing store management window with a Niches tab.

   The store management window should become a store setup surface with at least a Basic info tab and a Niches tab for the selected store. Basic info keeps the existing store fields and store archive/delete behavior. Niches shows active and archived niches for that selected store, and provides create, save, archive, restore, delete, and niche-context editing. This keeps store-scoped setup in one place and avoids introducing a second management window before the workflow needs it.

   Alternative considered: create a standalone niche editor window like the store editor. That would isolate the feature, but it makes the user manage store setup in two places even though niches cannot exist without a store.

   Alternative considered: inline edit all niche context below the selected tree node. That gives immediate access, but it makes the navigation pane too heavy and competes with the document/tool area.

7. Add guarded permanent deletion for empty niches.

   Permanent deletion should be available only after explicit warning and only when no groups, listings, prompts, assets, tags, or other niche-scoped data remain connected to the niche. Archive remains the normal path for inactive niches with history.

   Alternative considered: omit deletion entirely in Phase 1. That would be safest, but it leaves no cleanup path for mistaken empty niche creation.

## Risks / Trade-offs

- [Risk] Flexible metadata can become inconsistent across services -> Mitigation: centralize niche context mapping in the application service and cover round-trip behavior with tests.
- [Risk] Archived niches may disappear too completely from normal work -> Mitigation: require an archived niche area in the store management Niches tab with restore actions.
- [Risk] Creating niches without a selected store can produce orphaned data -> Mitigation: require explicit store identity or active store context for every create command and reject archived stores.
- [Risk] Snapshot-level saves remain coarse-grained -> Mitigation: keep niche operations deterministic and revisit narrower repository commands only if mutation volume or concurrency needs justify it.
- [Risk] Duplicate validation can surprise users who intentionally reuse names -> Mitigation: scope duplicate checks to active niches in the same store and allow the same names in different stores.
- [Risk] Deleting empty niches could still feel destructive -> Mitigation: require explicit confirmation and block deletion whenever connected data exists.

## Migration Plan

No database migration is expected for the initial implementation because niches already persist as core workspace entities with stable identity, store relationship, name, optional description, timestamps, archive state, and flexible metadata. Existing workspaces should load normally; stores without niches should show an empty active-niche state with a clear create action. If implementation exposes a missing persisted field, add a narrowly scoped SQLite migration and persistence tests.

Rollback is limited to removing the service/UI behavior; persisted niche records remain compatible with the existing core workspace model.

## Open Questions

- Should a listing be allowed to move freely between active niches in FC-0102, or should that wait for listing management?
- Should archived niches hide their child groups and listings from normal search by default when search arrives in FC-0107?
