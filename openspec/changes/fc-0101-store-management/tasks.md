## 1. Application Service

- [x] 1.1 Add store management request/result models for create, update, archive, restore, delete, list, and select operations.
- [x] 1.2 Implement a store management service that loads workspace snapshots, applies store changes, updates timestamps, and saves through `IWorkspaceRepository`.
- [x] 1.3 Validate store names for create and rename operations, including empty names and duplicate active-store names.
- [x] 1.4 Add metadata mapping for optional store context fields such as notes, target market, brand direction, and planning guidance.
- [x] 1.5 Preserve store identity and related store-scoped records when renaming, editing context, archiving, or restoring.
- [x] 1.6 Implement guarded permanent deletion for empty stores and blocked deletion for stores with connected data.

## 2. Active Store Context

- [x] 2.1 Add application state or service behavior for selecting the active store in the current app session.
- [x] 2.2 Ensure archived stores cannot become the active workspace scope through the normal active-store flow.
- [x] 2.3 Ensure selected active store identity and context remain available to navigation, document, and context-aware tool flows.
- [x] 2.4 Define empty-workspace behavior that prompts first-store creation when no stores exist.

## 3. User Interface

- [x] 3.1 Replace inline store management in the regular shell with a store selector and store-editor entry point only.
- [x] 3.2 Add first-store popup flow that asks whether the user wants to create a store when no stores exist.
- [x] 3.3 Add a dedicated store editor window for creating, renaming, saving context, archiving, restoring, and deleting stores.
- [x] 3.4 Add archive and restore actions in the store editor with active and archived stores shown separately.
- [x] 3.5 Update navigation presentation so opening an active store shows that store's niches, groups, and listings through the existing navigation experience.
- [x] 3.6 Add compact and expanded store selector modes in the regular shell.
- [x] 3.7 Highlight the selected store in both compact and expanded selector modes.
- [x] 3.8 Ensure the expanded selector can show many stores without permanently consuming navigation space during normal work.
- [x] 3.9 Add permanent delete in the store editor with explicit warning and cancellation behavior.
- [x] 3.10 Block permanent store deletion when any store-scoped data is connected to the store.
- [x] 3.11 Move the manage stores entry out of the rail body into a compact icon menu.
- [x] 3.12 Move collapse/expand to a small arrow toggle beside the Stores label with state-specific tooltip.
- [x] 3.13 Remove duplicate visible collapse button from the expanded store selector.
- [x] 3.14 Use compact fixed-width button rows in the first-store prompt and store editor actions.
- [x] 3.15 Replace the separate store-editor Create action with a New store draft that is persisted through Save.
- [x] 3.16 Add discard-changes confirmation when switching editor selection or closing the editor with unsaved changes.
- [x] 3.17 Ensure Manage stores opens with the active store pre-selected in the editor.
- [x] 3.18 Disable store editor action buttons when they are not relevant to the selected store state.
- [x] 3.19 Focus the Store name field after starting a new-store draft.
- [x] 3.20 Select a remaining store by default after deleting the selected store.

## 4. Persistence Integration

- [x] 4.1 Verify SQLite persistence round-trips store context, archive state, timestamps, and metadata JSON used by the store management service.
- [x] 4.2 Add or adjust repository tests for create, edit, archive, restore, delete, and active/archived store list scenarios.
- [x] 4.3 Confirm no schema migration is required; add a migration only if implementation exposes a missing persisted field.
- [x] 4.4 Wire the default desktop app store management path to SQLite persistence instead of an in-memory sample repository.

## 5. Tests and Validation

- [x] 5.1 Add application tests for creating a first store with minimal configuration.
- [x] 5.2 Add application tests for rename and context-edit behavior preserving store identity and child associations.
- [x] 5.3 Add application tests for archive and restore behavior, including active versus archived list separation.
- [x] 5.4 Add application or app-view-model tests for active store selection, highlighted selection, compact/expanded selector state, and blocked archived-store selection.
- [x] 5.5 Add UI view-model tests for first-store popup state, store selector state, store editor flow, archived-store restore flow, and guarded delete flow.
- [x] 5.6 Add application and persistence tests for permanent delete success, warning cancellation, and blocked delete with connected data.
- [x] 5.7 Add app tests for menu-based management entry, small toggle state, tooltip labels, and default SQLite-backed store persistence.
- [x] 5.8 Document shared button sizing and icon-button guidance in `docs/ui-guidelines.md` and link it from store management design/spec artifacts.
- [x] 5.9 Add app tests for new-store draft creation through Save, discard confirmation, guarded close, and editor pre-selection.
- [x] 5.10 Add app tests for store editor Save, Archive, and Delete enabled-state rules.
- [x] 5.11 Add app tests for new-store focus request and post-delete default selection.
- [x] 5.12 Run the relevant domain, application, app, and integration test projects.
