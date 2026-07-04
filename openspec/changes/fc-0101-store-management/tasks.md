## 1. Application Service

- [ ] 1.1 Add store management request/result models for create, update, archive, restore, delete, list, and select operations.
- [x] 1.2 Implement a store management service that loads workspace snapshots, applies store changes, updates timestamps, and saves through `IWorkspaceRepository`.
- [x] 1.3 Validate store names for create and rename operations, including empty names and duplicate active-store names.
- [x] 1.4 Add metadata mapping for optional store context fields such as notes, target market, brand direction, and planning guidance.
- [x] 1.5 Preserve store identity and related store-scoped records when renaming, editing context, archiving, or restoring.
- [ ] 1.6 Implement guarded permanent deletion for empty stores and blocked deletion for stores with connected data.

## 2. Active Store Context

- [x] 2.1 Add application state or service behavior for selecting the active store in the current app session.
- [x] 2.2 Ensure archived stores cannot become the active workspace scope through the normal active-store flow.
- [x] 2.3 Ensure selected active store identity and context remain available to navigation, document, and context-aware tool flows.
- [ ] 2.4 Define empty-workspace behavior that prompts first-store creation when no stores exist.

## 3. User Interface

- [ ] 3.1 Replace inline store management in the regular shell with a store selector and store-editor entry point only.
- [ ] 3.2 Add first-store popup flow that asks whether the user wants to create a store when no stores exist.
- [ ] 3.3 Add a dedicated store editor window for creating, renaming, saving context, archiving, restoring, and deleting stores.
- [ ] 3.4 Add archive and restore actions in the store editor with active and archived stores shown separately.
- [ ] 3.5 Update navigation presentation so opening an active store shows that store's niches, groups, and listings through the existing navigation experience.
- [ ] 3.6 Add compact and expanded store selector modes in the regular shell.
- [ ] 3.7 Highlight the selected store in both compact and expanded selector modes.
- [ ] 3.8 Ensure the expanded selector can show many stores without permanently consuming navigation space during normal work.
- [ ] 3.9 Add permanent delete in the store editor with explicit warning and cancellation behavior.
- [ ] 3.10 Block permanent store deletion when any store-scoped data is connected to the store.

## 4. Persistence Integration

- [x] 4.1 Verify SQLite persistence round-trips store context, archive state, timestamps, and metadata JSON used by the store management service.
- [ ] 4.2 Add or adjust repository tests for create, edit, archive, restore, delete, and active/archived store list scenarios.
- [x] 4.3 Confirm no schema migration is required; add a migration only if implementation exposes a missing persisted field.

## 5. Tests and Validation

- [ ] 5.1 Add application tests for creating a first store with minimal configuration.
- [x] 5.2 Add application tests for rename and context-edit behavior preserving store identity and child associations.
- [x] 5.3 Add application tests for archive and restore behavior, including active versus archived list separation.
- [ ] 5.4 Add application or app-view-model tests for active store selection, highlighted selection, compact/expanded selector state, and blocked archived-store selection.
- [ ] 5.5 Add UI view-model tests for first-store popup state, store selector state, store editor flow, archived-store restore flow, and guarded delete flow.
- [ ] 5.6 Add application and persistence tests for permanent delete success, warning cancellation, and blocked delete with connected data.
- [ ] 5.7 Run the relevant domain, application, app, and integration test projects.
