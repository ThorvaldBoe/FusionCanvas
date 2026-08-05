## Why

The bundled curated snowclones should be an opt-in resource, not auto-populated content. Auto-importing the 31 defaults on first launch surprises creators who open a Snowclone Library expecting a blank slate, and it makes the bundled set indistinguishable from user-created records. Keeping the library empty by default and requiring an explicit "Import bundled library" action gives creators deliberate control while still shipping the curated set for convenience.

## What Changes

- Stop auto-initializing bundled snowclones into a fresh library: the Snowclone Library now starts **empty** on first load for every data store.
- Keep the curated 31-record bundled CSV shipped in the application, added **only** via the existing "Import bundled library" action (which validates, imports unique records, skips existing normalized phrases, and never overwrites local guidance).
- `SnowcloneLibraryService.InitializeAsync` no longer reads or imports the bundled resource; it simply loads the current (empty) library, matching `LoadAsync`.
- Keep the `starter_initialized` marker storage in place for schema stability but stop treating it as a gate for auto-import (the marker becomes inert).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `snowclone-library`: Change the bundled starter-library requirement so the local library is empty by default and the bundled curated set is added only when the creator explicitly imports it; remove the auto-initialization and no-resurrection scenarios.

## Impact

- **Application:** `SnowcloneLibraryService.InitializeAsync` (now effectively a plain load); `ISnowcloneLibraryService` signature unchanged. `ImportBundledAsync`, the bundled source, and the CSV codec are unchanged.
- **App:** No view-model or view changes; `SnowcloneLibraryViewModel.OpenAsync` and `AppWorkspaceFactory` still call `InitializeAsync`, which now yields an empty library. The "Import bundled library" button already exists.
- **Data/schema:** No migration. The `snowclone_library_state.starter_initialized` column/table remain for compatibility but are no longer consulted as an import gate.
- **Tests:** Replace the auto-import service tests with ones asserting empty-by-default and unchanged explicit-bundled-import behavior.
- **Dependencies/coordination:** Builds on the active `default-snowclones` and `snowclone-library` changes; must be archived only after those base changes are synchronized/archived.

This is one coherent module: it changes the default population behavior and its tests, with no other surface area.
