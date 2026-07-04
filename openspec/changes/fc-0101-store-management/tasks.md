## 1. Application Service

- [x] 1.1 Add store management request/result models for create, update, archive, restore, list, and select operations.
- [x] 1.2 Implement a store management service that loads workspace snapshots, applies store changes, updates timestamps, and saves through `IWorkspaceRepository`.
- [x] 1.3 Validate store names for create and rename operations, including empty names and duplicate active-store names.
- [x] 1.4 Add metadata mapping for optional store context fields such as notes, target market, brand direction, and planning guidance.
- [x] 1.5 Preserve store identity and related store-scoped records when renaming, editing context, archiving, or restoring.

## 2. Active Store Context

- [x] 2.1 Add application state or service behavior for selecting the active store in the current app session.
- [x] 2.2 Ensure archived stores cannot become the active workspace scope through the normal active-store flow.
- [x] 2.3 Ensure selected active store identity and context remain available to navigation, document, and context-aware tool flows.
- [x] 2.4 Define empty-workspace behavior that prompts first-store creation when no stores exist.

## 3. User Interface

- [x] 3.1 Add a compact store selector or management surface to the Avalonia shell.
- [x] 3.2 Add first-store creation flow requiring only a store name with optional basic context.
- [x] 3.3 Add store editing flow for renaming and updating basic store context.
- [x] 3.4 Add archive and restore actions with active and archived stores shown separately.
- [x] 3.5 Update navigation presentation so opening an active store shows that store's niches, groups, and listings through the existing navigation experience.

## 4. Persistence Integration

- [x] 4.1 Verify SQLite persistence round-trips store context, archive state, timestamps, and metadata JSON used by the store management service.
- [x] 4.2 Add or adjust repository tests for create, edit, archive, restore, and active/archived store list scenarios.
- [x] 4.3 Confirm no schema migration is required; add a migration only if implementation exposes a missing persisted field.

## 5. Tests and Validation

- [x] 5.1 Add application tests for creating a first store with minimal configuration.
- [x] 5.2 Add application tests for rename and context-edit behavior preserving store identity and child associations.
- [x] 5.3 Add application tests for archive and restore behavior, including active versus archived list separation.
- [x] 5.4 Add application or app-view-model tests for active store selection and blocked archived-store selection.
- [x] 5.5 Add UI view-model tests for first-store empty state, store selector state, edit flow, and archived-store restore flow.
- [x] 5.6 Run the relevant domain, application, app, and integration test projects.
