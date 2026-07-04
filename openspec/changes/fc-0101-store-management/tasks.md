## 1. Application Service

- [ ] 1.1 Add store management request/result models for create, update, archive, restore, list, and select operations.
- [ ] 1.2 Implement a store management service that loads workspace snapshots, applies store changes, updates timestamps, and saves through `IWorkspaceRepository`.
- [ ] 1.3 Validate store names for create and rename operations, including empty names and duplicate active-store names.
- [ ] 1.4 Add metadata mapping for optional store context fields such as notes, target market, brand direction, and planning guidance.
- [ ] 1.5 Preserve store identity and related store-scoped records when renaming, editing context, archiving, or restoring.

## 2. Active Store Context

- [ ] 2.1 Add application state or service behavior for selecting the active store in the current app session.
- [ ] 2.2 Ensure archived stores cannot become the active workspace scope through the normal active-store flow.
- [ ] 2.3 Ensure selected active store identity and context remain available to navigation, document, and context-aware tool flows.
- [ ] 2.4 Define empty-workspace behavior that prompts first-store creation when no stores exist.

## 3. User Interface

- [ ] 3.1 Add a compact store selector or management surface to the Avalonia shell.
- [ ] 3.2 Add first-store creation flow requiring only a store name with optional basic context.
- [ ] 3.3 Add store editing flow for renaming and updating basic store context.
- [ ] 3.4 Add archive and restore actions with active and archived stores shown separately.
- [ ] 3.5 Update navigation presentation so opening an active store shows that store's niches, groups, and listings through the existing navigation experience.

## 4. Persistence Integration

- [ ] 4.1 Verify SQLite persistence round-trips store context, archive state, timestamps, and metadata JSON used by the store management service.
- [ ] 4.2 Add or adjust repository tests for create, edit, archive, restore, and active/archived store list scenarios.
- [ ] 4.3 Confirm no schema migration is required; add a migration only if implementation exposes a missing persisted field.

## 5. Tests and Validation

- [ ] 5.1 Add application tests for creating a first store with minimal configuration.
- [ ] 5.2 Add application tests for rename and context-edit behavior preserving store identity and child associations.
- [ ] 5.3 Add application tests for archive and restore behavior, including active versus archived list separation.
- [ ] 5.4 Add application or app-view-model tests for active store selection and blocked archived-store selection.
- [ ] 5.5 Add UI view-model tests for first-store empty state, store selector state, edit flow, and archived-store restore flow.
- [ ] 5.6 Run the relevant domain, application, app, and integration test projects.
