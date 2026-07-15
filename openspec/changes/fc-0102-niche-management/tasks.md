## 1. Application Service

- [x] 1.1 Add niche management request/result models for create, update, archive, restore, delete, list, and select operations.
- [x] 1.2 Implement a `NicheManagementService` that loads workspace snapshots, applies niche changes, updates timestamps, and saves through `IWorkspaceRepository`.
- [x] 1.3 Validate niche names for create and rename operations, including empty names and duplicate active-niche names within the same store.
- [x] 1.4 Require an active or explicitly selected active store for niche creation and reject archived or missing store contexts.
- [x] 1.5 Add metadata mapping for optional niche context fields such as audience, humor style, visual style guidance, constraints, risks, research notes, and general notes.
- [x] 1.6 Preserve niche identity, store association, child groups, listings, prompts, assets, tag links, asset links, and metadata when renaming, editing context, archiving, or restoring.
- [x] 1.7 Implement guarded permanent deletion for empty niches and blocked deletion for niches with connected data.

## 2. Active Niche Context

- [x] 2.1 Add application state or service behavior for selecting the active niche in the current active store.
- [x] 2.2 Ensure archived niches cannot become active workspace context through the normal active-niche flow.
- [x] 2.3 Ensure selected active niche identity and context remain available to navigation, document, and context-aware tool flows.
- [x] 2.4 Define empty-store behavior that shows a clear create-niche state when an active store has no active niches.
- [x] 2.5 Ensure restore is blocked when an archived niche name conflicts with an active niche in the same store.

## 3. User Interface

- [x] 3.1 Add regular workspace controls for browsing active niches inside the selected store and opening niche creation or management.
- [x] 3.2 Keep active niches as top-level folder/topic nodes under the selected store in the main window sidebar and exclude archived niches from normal navigation by default.
- [x] 3.3 Extend the existing store management window with tabs for Basic info and Niches.
- [x] 3.4 Keep existing store-level fields and store lifecycle actions on the Basic info tab.
- [x] 3.5 Add create, rename, save context, archive, restore, and delete behavior to the Niches tab for the selected store.
- [x] 3.6 Add archive and restore actions in the Niches tab with active and archived niches shown separately for the current store.
- [x] 3.7 Pre-select the active or selected niche when opening store management on the Niches tab, when one exists.
- [x] 3.8 Add discard-changes confirmation when switching niche selection, switching store management tabs, changing selected store, or closing the store management window with unsaved niche changes.
- [x] 3.9 Disable Niches tab action buttons when they are not relevant to the selected niche state.
- [x] 3.10 Add permanent delete in the Niches tab with explicit warning and cancellation behavior.
- [x] 3.11 Block permanent niche deletion when groups, listings, prompts, assets, tag links, asset links, or other connected niche data exist.
- [x] 3.12 Use compact fixed or content-based action button sizing in niche prompts, Niches tab actions, discard confirmations, and delete warnings.

## 4. Persistence Integration

- [x] 4.1 Verify SQLite persistence round-trips niche context, archive state, timestamps, store relationship, and metadata JSON used by the niche management service.
- [x] 4.2 Add or adjust repository tests for create, edit, archive, restore, delete, and active/archived niche list scenarios.
- [x] 4.3 Confirm no schema migration is required; add a migration only if implementation exposes a missing persisted field.
- [x] 4.4 Wire the default desktop app niche management path to the same SQLite-backed workspace repository used by store management.

## 5. Tests and Validation

- [x] 5.1 Add application tests for creating a first niche in an active store with minimal configuration.
- [x] 5.2 Add application tests for duplicate validation scoped to active niches within one store while allowing the same name in another store.
- [x] 5.3 Add application tests for rename and context-edit behavior preserving niche identity and child associations.
- [x] 5.4 Add application tests for archive and restore behavior, including active versus archived list separation and restore name-conflict blocking.
- [x] 5.5 Add application or app-view-model tests for active niche selection and blocked archived-niche selection.
- [x] 5.6 Add UI view-model tests for empty active-store niche state, store management Basic info and Niches tabs, archived-niche restore flow, unsaved-change prompts, enabled-state rules, and guarded delete flow.
- [x] 5.7 Add application and persistence tests for permanent delete success, warning cancellation, and blocked delete with connected data.
- [x] 5.8 Add app tests that selected store changes filter niche management state to the current store.
- [x] 5.9 Run the relevant domain, application, app, and integration test projects.
