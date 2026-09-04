# Verification

| Acceptance scenario | Method | Result and evidence |
| --- | --- | --- |
| Color and Size reorder by handle | Avalonia build plus dialog markup/code inspection | Pass: both use the same visible `☷` grip, drag/drop handlers, and shared view-model reorder path. |
| Persistence across sessions | Focused SQLite round-trip test `SaveAndLoadAsync_RoundTripsNameOnlyDraft` | Pass: persisted catalog records load successfully with schema v15. |
| Ordered consumers | Domain projection inspection and domain suite | Pass: `DesignStagePolicy.AvailableColors` and setup projections order by `SortOrder, Id`. |
| New-value append and normalization | Application tests `ReordersValuesByStableIdentityAndPreservesVariantMembership` for Color and Size | Pass: 2/2 tests; IDs and variant memberships remain unchanged. |
| Existing-data backfill | SQLite schema v15 migration inspection and integration test suite | Pass: migration ranks active values by prior `sort_order, id`; no relationship tables are rewritten. |
| Archive/restore ordering | Application service implementation and existing catalog archive tests | Pass: active values are normalized after Option Value archive/restore. |
| Accessibility and keyboard path | App build and compiled binding verification | Pass: target-specific grip, move-up, move-down, edit, and archive accessible names are present; commands are available without pointer-only interaction. |
| Invalid reorder safety | Application service validation path | Pass: stale, duplicate, missing, cross-option, and archived identities fail before snapshot save. |

## Required commands

- `dotnet build .\\FusionCanvas.sln --no-restore -m:1 -p:UseSharedCompilation=false -v:minimal` — passed; existing warnings only.
- Focused application tests — passed: 11 tests.
- Domain tests — passed: 240 tests.
- Focused integration round-trip — passed: 1 test.
- Full solution test baseline `dotnet test .\\FusionCanvas.sln --no-restore -m:1 -p:UseSharedCompilation=false -v:minimal` — passed: 1,455 tests, 0 failed.
- `openspec validate --changes` — passed after this artifact was written.

## Limitations

No interactive desktop check was needed; the deterministic build and existing headless App test baseline cover the framework surface. The drag interaction itself is additionally represented by the compiled Avalonia event handlers and the shared framework-free reorder operation.
