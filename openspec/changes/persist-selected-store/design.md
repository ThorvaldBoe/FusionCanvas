## Context

The store selector currently updates `StoreManagementService._activeStoreId`, but that state is process-local. Application settings already persist `ActiveWorkspaceId` through the versioned JSON settings store and are loaded before the main window is constructed. The fix extends that existing preference boundary to include one optional selected-store identity.

## Goals / Non-Goals

**Goals:**

- Restore the last selected active store on startup.
- Persist only after a successful selector change.
- Validate the restored identity against the active workspace and active-store rules.
- Preserve settings-file compatibility and the existing first-store fallback.

**Non-Goals:**

- Per-workspace history or a new settings UI.
- Changing workspace/store domain persistence.
- Persisting editor drafts, tree selections, tabs, or archived-store selections.
- Adding a new database column or migration.

## Decisions

- Add nullable `ActiveStoreId` to `ApplicationSettings` and the JSON settings document. This reuses the existing local preference mechanism; storing selection in SQLite would incorrectly couple an application preference to workspace data.
- Pass the loaded ID into `StoreManagementService` as an optional initial value. `BuildState` already validates the in-memory selection against active workspace and archive state, so the same guard applies to restored state.
- Add `SettingsViewModel.UpdateActiveStore(Guid?)`, mirroring `UpdateActiveWorkspace`, and call it from the main view model after successful selector selection. This keeps asynchronous settings writes serialized by the existing save chain and avoids persisting rejected requests.
- Keep the existing first-store fallback in `InitializeStores`: it runs only when startup restoration produces no selected store.
- Keep the field optional and retain the current settings version. Missing JSON values deserialize as null, and malformed GUID values are ignored by the existing tolerant GUID reader.

Alternatives considered: storing the selection in workspace SQLite metadata would make the preference travel with workspace data and require a schema/model change; keeping it only in the main view model would not survive restart. Neither fits the application-wide preference boundary as well.

## Risks / Trade-offs

- [Stale ID] A deleted or archived store may remain in settings → validate through the existing service state construction and use the first active store fallback.
- [Workspace mismatch] A single application-level ID may refer to a different workspace after a workspace switch → require workspace identity match before exposing it as active; do not add per-workspace preference maps for this focused fix.
- [Asynchronous write] The app may quit while a settings write is queued → use the existing `AppServices.FlushAsync` shutdown path and settings save chain.

## Migration Plan

No database migration is required. Existing settings documents continue to load with a null selected store ID. New writes include the optional field. Rollback is code-only; documents containing the extra field remain readable by the current loader only if the field is ignored, so rollback compatibility is preserved by the case-insensitive JSON reader and unknown-property behavior.

## Open Questions

None. The fallback and scope rules are resolved by the existing workspace/store selection behavior.

## Implementation Plan

1. Extend `ApplicationSettings` and `JsonApplicationSettingsStore` serialization/deserialization with nullable `ActiveStoreId`; update integration settings tests for round-trip and missing-field compatibility.
2. Extend `StoreManagementService` with an optional initial selected-store ID and pass it from `MainWindowViewModel` using the loaded `SettingsViewModel.ActiveStoreId`.
3. Add `SettingsViewModel.UpdateActiveStore` and invoke it only after `StoreManagement.SelectStoreAsync` succeeds; ensure startup restoration precedes first-store fallback.
4. Add focused App/Application tests for successful selector persistence, restart restoration, invalid/stale fallback, and rejected-selection behavior.
5. Run criterion-level tests, `openspec validate`, and `dotnet test .\FusionCanvas.sln`.

## Acceptance-to-Verification Mapping

| Acceptance scenario | Planned verification |
| --- | --- |
| Selected store survives restart | App test with shared in-memory settings and reconstructed main/store management state; integration JSON round-trip test |
| Missing or stale selected store falls back safely | Application/App service tests for missing, archived, deleted, and workspace-mismatched IDs |
| Existing settings remain backward compatible | Integration test loading a document without `activeStoreId` |
| Selector persists selected store | App view-model test with a recording settings store after successful selection |
| Saved store is no longer selectable | Startup/service state test with archived or mismatched saved ID |
| Failed selection is not persisted | App view-model test using rejected selection result and recording settings store |
